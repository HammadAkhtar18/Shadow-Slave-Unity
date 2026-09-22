using System;
using System.Collections.Generic;
using ShadowSlave.Attributes;
using ShadowSlave.Combat;
using ShadowSlave.Core;
using UnityEngine;

namespace ShadowSlave.Characters
{
    /// <summary>
    /// Foundation character base for player, companions, and enemies.
    /// Mirrors behavioural responsibilities of UE AShadowSlaveCharacterBase without requiring
    /// Combat / Equipment / StatusEffect components yet (null-safe stubs for future port).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AttributeComponent))]
    public class CharacterBase : MonoBehaviour
    {
        [SerializeField] private string characterId = string.Empty;
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;

        private AttributeComponent _attributeComponent;
        private ShadowSlaveGait _currentGait = ShadowSlaveGait.Walk;
        private bool _isAlive = true;
        private bool _canMove = true;
        private readonly HashSet<string> _movementSuppressionSources = new HashSet<string>();
        private GameObject _lastDamageAttacker;

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageInfo> OnCharacterDamaged;
        public event Action<CharacterBase, GameObject> OnCharacterDied;
        public event Action<ShadowSlaveGait, ShadowSlaveGait> OnGaitChanged;

        public string CharacterId
        {
            get => characterId;
            set => characterId = value ?? string.Empty;
        }

        public AttributeComponent AttributeComponent => _attributeComponent;
        public ShadowSlaveGait Gait => _currentGait;
        public bool IsSprinting => _currentGait == ShadowSlaveGait.Sprint;
        public bool IsMovementControlEnabled => _canMove;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;

        /// <summary>Optional future combat component — null until Combat is ported.</summary>
        public Component CombatComponent => null;

        /// <summary>Optional future equipment component — null until Equipment is ported.</summary>
        public Component EquipmentComponent => null;

        /// <summary>Optional future status-effect component — null until StatusEffects are ported.</summary>
        public Component StatusEffectComponent => null;

        private void Awake()
        {
            _attributeComponent = GetComponent<AttributeComponent>();
            WireAttributeEvents();
        }

        private void Start()
        {
            _isAlive = true;
            _movementSuppressionSources.Clear();
            UpdateMovementControlState();
            InitializeAttributes();
        }

        private void OnDestroy()
        {
            UnwireAttributeEvents();
        }

        private void WireAttributeEvents()
        {
            if (_attributeComponent == null)
            {
                return;
            }

            _attributeComponent.OnHealthChanged += HandleAttributeHealthChanged;
            _attributeComponent.OnDeath += HandleDeath;
            _attributeComponent.OnDamageReceived += HandleAttributeDamageReceived;
        }

        private void UnwireAttributeEvents()
        {
            if (_attributeComponent == null)
            {
                return;
            }

            _attributeComponent.OnHealthChanged -= HandleAttributeHealthChanged;
            _attributeComponent.OnDeath -= HandleDeath;
            _attributeComponent.OnDamageReceived -= HandleAttributeDamageReceived;
        }

        protected virtual void InitializeAttributes()
        {
            // Hook for subclasses / future attribute bootstrap.
        }

        public virtual bool IsAlive()
        {
            return _attributeComponent != null ? _attributeComponent.IsAlive : _isAlive;
        }

        public float GetCurrentHealth()
        {
            return _attributeComponent != null ? _attributeComponent.CurrentHealth : 0f;
        }

        public float GetMaxHealth()
        {
            return _attributeComponent != null ? _attributeComponent.MaximumHealth : 0f;
        }

        public virtual float TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive())
            {
                return 0f;
            }

            float actual = 0f;
            if (_attributeComponent != null)
            {
                actual = _attributeComponent.ApplyDamage(damageInfo.DamageAmount, damageInfo);
            }

            if (actual > 0f)
            {
                _lastDamageAttacker = damageInfo.Attacker;
                OnDamaged(damageInfo);
            }

            return actual;
        }

        protected virtual void OnDamaged(DamageInfo damageInfo)
        {
            OnCharacterDamaged?.Invoke(damageInfo);
        }

        private void HandleAttributeHealthChanged(float current, float max)
        {
            OnHealthChanged?.Invoke(current, max);
        }

        private void HandleAttributeDamageReceived(float applied, DamageInfo info)
        {
            // Character-level damaged event is raised from TakeDamage; keep attribute path for direct ApplyDamage.
            if (applied > 0f && info.Attacker != null)
            {
                _lastDamageAttacker = info.Attacker;
            }
        }

        private void HandleDeath()
        {
            _isAlive = false;
            SetMovementControlSuppressed("Death", true);
            OnCharacterDied?.Invoke(this, _lastDamageAttacker);
        }

        public virtual void SetGait(ShadowSlaveGait newGait)
        {
            if (_currentGait == newGait)
            {
                return;
            }

            ShadowSlaveGait old = _currentGait;
            _currentGait = newGait;
            OnGaitChanged?.Invoke(old, newGait);
        }

        public virtual bool CanSprint()
        {
            if (!IsAlive() || !_canMove)
            {
                return false;
            }

            if (_attributeComponent != null && _attributeComponent.CurrentStamina <= 0f)
            {
                return false;
            }

            return true;
        }

        public virtual void StartSprint()
        {
            if (CanSprint())
            {
                SetGait(ShadowSlaveGait.Sprint);
            }
        }

        public virtual void StopSprint()
        {
            if (_currentGait == ShadowSlaveGait.Sprint)
            {
                SetGait(ShadowSlaveGait.Walk);
            }
        }

        public virtual void SetMovementControlEnabled(bool enabled)
        {
            SetMovementControlSuppressed("Manual", !enabled);
        }

        public virtual void SetMovementControlSuppressed(string source, bool suppressed)
        {
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            if (suppressed)
            {
                _movementSuppressionSources.Add(source);
            }
            else
            {
                _movementSuppressionSources.Remove(source);
            }

            UpdateMovementControlState();
        }

        public bool IsMovementControlSuppressedBy(string source)
        {
            return !string.IsNullOrEmpty(source) && _movementSuppressionSources.Contains(source);
        }

        protected virtual void UpdateMovementControlState()
        {
            _canMove = _movementSuppressionSources.Count == 0 && _isAlive;
            if (!_canMove)
            {
                StopSprint();
            }
        }
    }
}
