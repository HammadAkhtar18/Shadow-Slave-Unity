using System;
using System.Collections;
using System.Collections.Generic;
using ShadowSlave.Attributes;
using ShadowSlave.Core;
using UnityEngine;

namespace ShadowSlave.Combat
{
    /// <summary>
    /// Modular combat component: state machine, attack lifecycle, hit tracking,
    /// and damage routing. Mirrors essentials of UE UShadowSlaveCombatComponent.
    /// Sphere sweeps / anim notify hit windows / full dodge movement are deferred;
    /// call <see cref="TryApplyHit"/> from future animation or detection systems.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatComponent : MonoBehaviour
    {
        [SerializeField] private AttackData lightAttackData = AttackData.DefaultLight;
        [SerializeField] private AttackData heavyAttackData = AttackData.DefaultHeavy;
        [SerializeField] private DodgeData dodgeData = DodgeData.Default;
        [SerializeField] private bool allowFriendlyFire;
        [SerializeField] private bool requireGroundedForDodge = true;

        private ECombatState _currentCombatState = ECombatState.Neutral;
        private AttackData _activeAttackData;
        private int _currentAttackInstanceId;
        private bool _hitWindowActive;
        private bool _isMontageDriven;
        private bool _isTransitioningState;
        private readonly Dictionary<int, int> _hitCountsThisAttack = new Dictionary<int, int>();

        private Coroutine _hitWindowCoroutine;
        private Coroutine _recoveryCoroutine;

        public event Action<ECombatState, ECombatState> OnCombatStateChanged;
        public event Action<EAttackType> OnAttackExecuted;
        public event Action<EAttackType, int> OnAttackStarted;
        public event Action<EAttackType, int> OnAttackEnded;
        /// <summary>UE OnTargetHit — fired when a hit is successfully applied to a target.</summary>
        public event Action<GameObject, DamageInfo> OnTargetHit;
        /// <summary>Alias naming for clarity; same payload as OnTargetHit's damage info path.</summary>
        public event Action<GameObject, DamageInfo> OnHitLanded;
        public event Action<DamageInfo> OnDamageDealt;
        public event Action<DamageInfo> OnDamageReceived;

        public ECombatState CombatState => _currentCombatState;
        public int CurrentAttackInstanceId => _currentAttackInstanceId;
        public bool IsHitWindowActive => _hitWindowActive;
        public AttackData LightAttackData
        {
            get => lightAttackData;
            set => lightAttackData = value;
        }
        public AttackData HeavyAttackData
        {
            get => heavyAttackData;
            set => heavyAttackData = value;
        }
        public DodgeData DodgeData
        {
            get => dodgeData;
            set => dodgeData = value;
        }
        public bool AllowFriendlyFire
        {
            get => allowFriendlyFire;
            set => allowFriendlyFire = value;
        }
        public bool RequireGroundedForDodge
        {
            get => requireGroundedForDodge;
            set => requireGroundedForDodge = value;
        }

        public AttackData GetAttackData(EAttackType attackType)
        {
            return attackType == EAttackType.Heavy ? heavyAttackData : lightAttackData;
        }

        public AttributeComponent GetOwnerAttributeComponent()
        {
            return GetComponent<AttributeComponent>();
        }

        public bool CanTransitionToState(ECombatState newState)
        {
            if (_currentCombatState == newState)
            {
                return true;
            }

            if (_currentCombatState == ECombatState.Dead)
            {
                return false;
            }

            if (newState == ECombatState.Dead || newState == ECombatState.Stunned)
            {
                return true;
            }

            switch (_currentCombatState)
            {
                case ECombatState.Neutral:
                    return newState == ECombatState.Attacking || newState == ECombatState.Dodging;
                case ECombatState.Attacking:
                    return newState == ECombatState.Recovering
                           || newState == ECombatState.Dodging
                           || newState == ECombatState.Neutral;
                case ECombatState.Recovering:
                    return newState == ECombatState.Neutral || newState == ECombatState.Dodging;
                case ECombatState.Dodging:
                    return newState == ECombatState.Neutral;
                case ECombatState.Stunned:
                    return newState == ECombatState.Neutral;
                default:
                    return false;
            }
        }

        public void SetCombatState(ECombatState newState)
        {
            if (_currentCombatState == newState)
            {
                return;
            }

            if (_isTransitioningState)
            {
                SSLog.LogCombat(
                    $"CombatComponent.SetCombatState recursive transition rejected: {_currentCombatState} -> {newState} on '{name}'");
                return;
            }

            if (!CanTransitionToState(newState))
            {
                SSLog.LogCombat(
                    $"CombatComponent.SetCombatState invalid transition rejected: {_currentCombatState} -> {newState} on '{name}'");
                return;
            }

            _isTransitioningState = true;

            ECombatState oldState = _currentCombatState;
            bool wasAttacking = oldState == ECombatState.Attacking;
            EAttackType endedAttackType = _activeAttackData.AttackType;
            int endedInstanceId = _currentAttackInstanceId;

            if (wasAttacking)
            {
                _hitWindowActive = false;
                _isMontageDriven = false;
                StopHitWindowCoroutine();

                if (newState != ECombatState.Recovering)
                {
                    _hitCountsThisAttack.Clear();
                }
            }
            else if (oldState == ECombatState.Recovering)
            {
                StopRecoveryCoroutine();
            }

            if (newState == ECombatState.Dead)
            {
                _hitWindowActive = false;
                _isMontageDriven = false;
                _hitCountsThisAttack.Clear();
                StopHitWindowCoroutine();
                StopRecoveryCoroutine();
            }

            _currentCombatState = newState;

            OnCombatStateChanged?.Invoke(oldState, newState);

            if (wasAttacking)
            {
                OnAttackEnded?.Invoke(endedAttackType, endedInstanceId);
            }

            _isTransitioningState = false;
        }

        public virtual bool CanPerformAttack(EAttackType attackType)
        {
            if (_currentCombatState != ECombatState.Neutral)
            {
                return false;
            }

            if (!IsOwnerAlive())
            {
                return false;
            }

            return true;
        }

        public virtual bool ExecuteAttack(EAttackType attackType)
        {
            if (!CanPerformAttack(attackType))
            {
                return false;
            }

            _activeAttackData = GetAttackData(attackType);
            _activeAttackData.AttackType = attackType;
            ++_currentAttackInstanceId;
            _hitCountsThisAttack.Clear();

            SetCombatState(ECombatState.Attacking);
            OnAttackExecuted?.Invoke(attackType);
            OnAttackStarted?.Invoke(attackType, _currentAttackInstanceId);

            // Montage/notify-driven windows deferred. Fallback: timer-driven hit window.
            _isMontageDriven = false;
            OpenHitWindow();
            if (Application.isPlaying && isActiveAndEnabled && _activeAttackData.HitWindowDuration > 0f)
            {
                _hitWindowCoroutine = StartCoroutine(HitWindowDurationRoutine(_activeAttackData.HitWindowDuration));
            }
            else if (_activeAttackData.HitWindowDuration <= 0f)
            {
                CloseHitWindow();
            }

            return true;
        }

        public void CancelAttack()
        {
            if (_currentCombatState != ECombatState.Attacking)
            {
                return;
            }

            SetCombatState(ECombatState.Neutral);
        }

        public void OpenHitWindow()
        {
            if (_currentCombatState != ECombatState.Attacking)
            {
                return;
            }

            _hitWindowActive = true;
            // Physics melee traces deferred — callers use TryApplyHit during the window.
        }

        public void CloseHitWindow()
        {
            _hitWindowActive = false;
            StopHitWindowCoroutine();

            if (_currentCombatState == ECombatState.Attacking)
            {
                float recoveryTime = _activeAttackData.RecoveryDuration;
                SetCombatState(ECombatState.Recovering);

                if (recoveryTime > 0f && Application.isPlaying && isActiveAndEnabled)
                {
                    _recoveryCoroutine = StartCoroutine(RecoveryRoutine(recoveryTime));
                }
                else
                {
                    OnRecoveryFinished();
                }
            }
        }

        public void HandleOwnerDeath()
        {
            SetCombatState(ECombatState.Dead);
        }

        public void ResetToNeutral()
        {
            if (_currentCombatState == ECombatState.Dead)
            {
                return;
            }

            if (_currentCombatState == ECombatState.Attacking)
            {
                CancelAttack();
            }
            else if (_currentCombatState == ECombatState.Recovering
                     || _currentCombatState == ECombatState.Dodging
                     || _currentCombatState == ECombatState.Stunned)
            {
                SetCombatState(ECombatState.Neutral);
            }
        }

        public virtual bool CanDamageTarget(GameObject target)
        {
            if (target == null || target == gameObject)
            {
                return false;
            }

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsAlive())
            {
                return false;
            }

            if (!allowFriendlyFire)
            {
                // Use tag string equality — CompareTag throws if TagManager lacks the tag.
                bool ownerEnemy = HasUnityTag(gameObject, "Enemy");
                bool targetEnemy = HasUnityTag(target, "Enemy");
                if (ownerEnemy && targetEnemy)
                {
                    return false;
                }

                bool ownerPlayer = HasUnityTag(gameObject, "Player");
                bool targetPlayer = HasUnityTag(target, "Player");
                if (ownerPlayer && targetPlayer)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasHitTargetThisAttack(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            return GetHitCountForTargetThisAttack(target) > 0;
        }

        public int GetHitCountForTargetThisAttack(GameObject target)
        {
            if (target == null)
            {
                return 0;
            }

            int id = target.GetInstanceID();
            return _hitCountsThisAttack.TryGetValue(id, out int count) ? count : 0;
        }

        /// <summary>
        /// Applies a hit to a target if the hit window is active and MaxHitsPerTarget allows it.
        /// Prefer this entry from future animation/detection systems (no physics sweep yet).
        /// </summary>
        public bool TryApplyHit(GameObject target, DamageInfo info)
        {
            if (!_hitWindowActive || _currentCombatState != ECombatState.Attacking)
            {
                return false;
            }

            if (!CanDamageTarget(target))
            {
                return false;
            }

            int maxHits = Mathf.Max(1, _activeAttackData.MaxHitsPerTarget);
            int currentHits = GetHitCountForTargetThisAttack(target);
            if (currentHits >= maxHits)
            {
                return false;
            }

            int id = target.GetInstanceID();
            _hitCountsThisAttack[id] = currentHits + 1;

            if (info.AttackInstanceId == 0)
            {
                info.AttackInstanceId = _currentAttackInstanceId;
            }

            if (info.Attacker == null)
            {
                info.Attacker = gameObject;
            }

            if (info.DamageCauser == null)
            {
                info.DamageCauser = gameObject;
            }

            if (info.DamageAmount <= 0f)
            {
                info.DamageAmount = _activeAttackData.Damage;
            }

            float applied = ApplyDamageToTarget(target, info);
            if (applied > 0f || info.DamageAmount > 0f)
            {
                OnTargetHit?.Invoke(target, info);
                OnHitLanded?.Invoke(target, info);
                OnDamageDealt?.Invoke(info);
            }

            return true;
        }

        /// <summary>
        /// Registers a hit count without applying damage (for custom pipelines).
        /// </summary>
        public bool RegisterHit(GameObject target)
        {
            if (target == null || _currentCombatState != ECombatState.Attacking)
            {
                return false;
            }

            int maxHits = Mathf.Max(1, _activeAttackData.MaxHitsPerTarget);
            int currentHits = GetHitCountForTargetThisAttack(target);
            if (currentHits >= maxHits)
            {
                return false;
            }

            _hitCountsThisAttack[target.GetInstanceID()] = currentHits + 1;
            return true;
        }

        /// <summary>
        /// Routes damage through IDamageable, else AttributeComponent. Does not require CharacterBase.
        /// </summary>
        public float ApplyDamageToTarget(GameObject target, DamageInfo info)
        {
            if (target == null)
            {
                return 0f;
            }

            info.DamageAmount = DamageCalculator.FinalizeAmount(info);

            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                return damageable.TakeDamage(info);
            }

            AttributeComponent attrs = target.GetComponent<AttributeComponent>();
            if (attrs != null)
            {
                return attrs.ApplyDamage(info.DamageAmount, info);
            }

            return 0f;
        }

        public void NotifyDamageReceived(DamageInfo damageInfo)
        {
            OnDamageReceived?.Invoke(damageInfo);
        }

        /// <summary>
        /// Builds a DamageInfo from the active attack for the given target/hit data.
        /// </summary>
        public DamageInfo BuildDamageInfoFromActiveAttack(
            Vector3 hitLocation = default,
            Vector3 hitNormal = default,
            Vector3 hitDirection = default)
        {
            return new DamageInfo(
                _activeAttackData.Damage,
                gameObject,
                gameObject,
                hitLocation,
                hitNormal,
                hitDirection,
                _currentAttackInstanceId);
        }

        private bool IsOwnerAlive()
        {
            IDamageable self = GetComponent<IDamageable>();
            if (self != null)
            {
                return self.IsAlive();
            }

            AttributeComponent attrs = GetOwnerAttributeComponent();
            if (attrs != null)
            {
                return attrs.IsAlive;
            }

            return true;
        }

        private void OnRecoveryFinished()
        {
            if (_currentCombatState == ECombatState.Recovering)
            {
                SetCombatState(ECombatState.Neutral);
            }
        }

        private IEnumerator HitWindowDurationRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _hitWindowCoroutine = null;
            CloseHitWindow();
        }

        private IEnumerator RecoveryRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _recoveryCoroutine = null;
            OnRecoveryFinished();
        }

        private void StopHitWindowCoroutine()
        {
            if (_hitWindowCoroutine != null)
            {
                StopCoroutine(_hitWindowCoroutine);
                _hitWindowCoroutine = null;
            }
        }

        private void StopRecoveryCoroutine()
        {
            if (_recoveryCoroutine != null)
            {
                StopCoroutine(_recoveryCoroutine);
                _recoveryCoroutine = null;
            }
        }


        private static bool HasUnityTag(GameObject go, string tagName)
        {
            return go != null && go.tag == tagName;
        }

        private void OnDisable()
        {
            StopHitWindowCoroutine();
            StopRecoveryCoroutine();
        }
    }
}
