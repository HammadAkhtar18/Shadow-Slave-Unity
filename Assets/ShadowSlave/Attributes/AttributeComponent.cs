using System;
using System.Collections;
using System.Collections.Generic;
using ShadowSlave.Combat;
using ShadowSlave.Core;
using UnityEngine;

namespace ShadowSlave.Attributes
{
    /// <summary>
    /// Manages Health, Stamina, and Soul Essence. Behaviour mirrors UE UShadowSlaveAttributeComponent:
    /// no Update tick; stamina regen via coroutines; modifiers recalculate effective maxima.
    /// </summary>
    [DisallowMultipleComponent]
    public class AttributeComponent : MonoBehaviour
    {
        [SerializeField] private AttributeInitConfig attributeConfig = AttributeInitConfig.Default;

        private float _currentHealth = 100f;
        private float _effectiveMaxHealth = 100f;
        private float _currentStamina = 100f;
        private float _effectiveMaxStamina = 100f;
        private float _currentEssence = 100f;
        private float _effectiveMaxEssence = 100f;
        private bool _isDead;

        private readonly List<AttributeModifier> _activeModifiers = new List<AttributeModifier>();
        private readonly Dictionary<string, Coroutine> _modifierTimers = new Dictionary<string, Coroutine>();
        private Coroutine _staminaRegenDelayCoroutine;
        private Coroutine _staminaRegenTickCoroutine;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<float, float> OnEssenceChanged;
        public event Action<float, DamageInfo> OnDamageReceived;
        public event Action<float> OnHealReceived;
        public event Action OnDeath;

        public float CurrentHealth => _currentHealth;
        public float MaximumHealth => _effectiveMaxHealth;
        public float BaseMaxHealth => attributeConfig.BaseMaxHealth;
        public float HealthPercent => _effectiveMaxHealth > 0f ? _currentHealth / _effectiveMaxHealth : 0f;

        public float CurrentStamina => _currentStamina;
        public float MaximumStamina => _effectiveMaxStamina;
        public float BaseMaxStamina => attributeConfig.BaseMaxStamina;
        public float StaminaPercent => _effectiveMaxStamina > 0f ? _currentStamina / _effectiveMaxStamina : 0f;

        public float CurrentEssence => _currentEssence;
        public float MaximumEssence => _effectiveMaxEssence;
        public float BaseMaxEssence => attributeConfig.BaseMaxEssence;
        public float EssencePercent => _effectiveMaxEssence > 0f ? _currentEssence / _effectiveMaxEssence : 0f;

        public bool IsDead => _isDead;
        public bool IsAlive => !_isDead;
        public bool IsStaminaRegenEnabled => attributeConfig.EnableStaminaRegen;
        public IReadOnlyList<AttributeModifier> ActiveModifiers => _activeModifiers;
        public AttributeInitConfig AttributeConfig => attributeConfig;

        private void Awake()
        {
            _effectiveMaxHealth = attributeConfig.BaseMaxHealth;
            _currentHealth = _effectiveMaxHealth;
            _effectiveMaxStamina = attributeConfig.BaseMaxStamina;
            _currentStamina = _effectiveMaxStamina;
            _effectiveMaxEssence = attributeConfig.BaseMaxEssence;
            _currentEssence = _effectiveMaxEssence;
        }

        private void Start()
        {
            RecalculateMaxAttributes();
            _currentHealth = _effectiveMaxHealth;
            _currentStamina = _effectiveMaxStamina;
            _currentEssence = _effectiveMaxEssence;
            _isDead = false;
        }

        private void OnDisable()
        {
            StopRegenTimers();
            ClearModifierTimers();
        }

        /* --- Health --- */

        public float ApplyDamage(float amount, DamageInfo damageInfo = default)
        {
            if (_isDead || amount <= 0f)
            {
                return 0f;
            }

            float damageApplied = Mathf.Clamp(amount, 0f, _currentHealth);
            _currentHealth -= damageApplied;

            OnHealthChanged?.Invoke(_currentHealth, _effectiveMaxHealth);
            OnDamageReceived?.Invoke(damageApplied, damageInfo);

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                _isDead = true;
                StopRegenTimers();
                OnDeath?.Invoke();
            }

            return damageApplied;
        }

        public float Heal(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return 0f;
            }

            float missing = Mathf.Max(_effectiveMaxHealth - _currentHealth, 0f);
            float actual = Mathf.Min(amount, missing);

            if (actual > 0f)
            {
                _currentHealth += actual;
                OnHealthChanged?.Invoke(_currentHealth, _effectiveMaxHealth);
                OnHealReceived?.Invoke(actual);
            }

            return actual;
        }

        public void SetHealth(float newHealth)
        {
            if (_isDead)
            {
                return;
            }

            _currentHealth = Mathf.Clamp(newHealth, 0f, _effectiveMaxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _effectiveMaxHealth);

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                _isDead = true;
                StopRegenTimers();
                OnDeath?.Invoke();
            }
        }

        /* --- Stamina --- */

        public bool ConsumeStamina(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return false;
            }

            if (_currentStamina < amount)
            {
                return false;
            }

            _currentStamina -= amount;
            OnStaminaChanged?.Invoke(_currentStamina, _effectiveMaxStamina);

            if (attributeConfig.EnableStaminaRegen && isActiveAndEnabled)
            {
                if (_staminaRegenTickCoroutine != null)
                {
                    StopCoroutine(_staminaRegenTickCoroutine);
                    _staminaRegenTickCoroutine = null;
                }

                if (_staminaRegenDelayCoroutine != null)
                {
                    StopCoroutine(_staminaRegenDelayCoroutine);
                }

                _staminaRegenDelayCoroutine = StartCoroutine(StaminaRegenDelayRoutine());
            }

            return true;
        }

        public float RestoreStamina(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return 0f;
            }

            float missing = Mathf.Max(_effectiveMaxStamina - _currentStamina, 0f);
            float restored = Mathf.Min(amount, missing);

            if (restored > 0f)
            {
                _currentStamina += restored;
                OnStaminaChanged?.Invoke(_currentStamina, _effectiveMaxStamina);
            }

            return restored;
        }

        public void SetStamina(float newStamina)
        {
            _currentStamina = Mathf.Clamp(newStamina, 0f, _effectiveMaxStamina);
            OnStaminaChanged?.Invoke(_currentStamina, _effectiveMaxStamina);
        }

        public void SetStaminaRegenEnabled(bool enabled)
        {
            attributeConfig.EnableStaminaRegen = enabled;

            if (!enabled)
            {
                StopRegenTimers();
            }
            else if (_currentStamina < _effectiveMaxStamina && !_isDead && isActiveAndEnabled)
            {
                StartStaminaRegenTick();
            }
        }

        /* --- Essence --- */

        public bool ConsumeEssence(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return false;
            }

            if (_currentEssence < amount)
            {
                return false;
            }

            _currentEssence -= amount;
            try
            {
                OnEssenceChanged?.Invoke(_currentEssence, _effectiveMaxEssence);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryAttributes, $"Exception in OnEssenceChanged subscriber during ConsumeEssence: {ex}");
            }
            return true;
        }

        public float RestoreEssence(float amount)
        {
            if (_isDead || amount <= 0f)
            {
                return 0f;
            }

            float missing = Mathf.Max(_effectiveMaxEssence - _currentEssence, 0f);
            float restored = Mathf.Min(amount, missing);

            if (restored > 0f)
            {
                _currentEssence += restored;
                try
                {
                    OnEssenceChanged?.Invoke(_currentEssence, _effectiveMaxEssence);
                }
                catch (Exception ex)
                {
                    SSLog.Error(SSLog.CategoryAttributes, $"Exception in OnEssenceChanged subscriber during RestoreEssence: {ex}");
                }
            }

            return restored;
        }

        public void SetEssence(float newEssence)
        {
            _currentEssence = Mathf.Clamp(newEssence, 0f, _effectiveMaxEssence);
            try
            {
                OnEssenceChanged?.Invoke(_currentEssence, _effectiveMaxEssence);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryAttributes, $"Exception in OnEssenceChanged subscriber during SetEssence: {ex}");
            }
        }

        /* --- Modifiers --- */

        public void AddModifier(AttributeModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.ModifierId))
            {
                return;
            }

            RemoveModifier(modifier.ModifierId);
            _activeModifiers.Add(modifier);

            if (modifier.Duration > 0f && isActiveAndEnabled)
            {
                Coroutine timer = StartCoroutine(ModifierExpiryRoutine(modifier.ModifierId, modifier.Duration));
                _modifierTimers[modifier.ModifierId] = timer;
            }

            RecalculateMaxAttributes();
        }

        public bool RemoveModifier(string modifierId)
        {
            if (string.IsNullOrEmpty(modifierId))
            {
                return false;
            }

            int removed = _activeModifiers.RemoveAll(m => m.ModifierId == modifierId);

            if (_modifierTimers.TryGetValue(modifierId, out Coroutine timer))
            {
                if (timer != null)
                {
                    StopCoroutine(timer);
                }

                _modifierTimers.Remove(modifierId);
            }

            if (removed > 0)
            {
                RecalculateMaxAttributes();
                return true;
            }

            return false;
        }

        public void RemoveModifiersFromSourceId(Guid sourceId)
        {
            if (sourceId == Guid.Empty)
            {
                return;
            }

            List<string> ids = new List<string>();
            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_activeModifiers[i].SourceId == sourceId)
                {
                    ids.Add(_activeModifiers[i].ModifierId);
                }
            }

            for (int i = 0; i < ids.Count; i++)
            {
                RemoveModifier(ids[i]);
            }
        }

        public bool HasModifierFromSourceId(Guid sourceId)
        {
            if (sourceId == Guid.Empty)
            {
                return false;
            }

            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                if (_activeModifiers[i].SourceId == sourceId)
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearAllModifiers()
        {
            ClearModifierTimers();
            _activeModifiers.Clear();
            RecalculateMaxAttributes();
        }

        public void InitializeAttributes(AttributeInitConfig newConfig)
        {
            attributeConfig = newConfig;
            RecalculateMaxAttributes();
            _currentHealth = _effectiveMaxHealth;
            _currentStamina = _effectiveMaxStamina;
            _currentEssence = _effectiveMaxEssence;
            _isDead = false;
        }

        public void RecalculateMaxAttributes()
        {
            float flatHealth = 0f, percentHealth = 0f;
            float flatStamina = 0f, percentStamina = 0f;
            float flatEssence = 0f, percentEssence = 0f;

            for (int i = 0; i < _activeModifiers.Count; i++)
            {
                AttributeModifier mod = _activeModifiers[i];
                switch (mod.TargetAttribute)
                {
                    case AttributeType.MaxHealth:
                    case AttributeType.Health:
                        if (mod.ModifierType == AttributeModifierType.Flat) flatHealth += mod.Value;
                        else percentHealth += mod.Value;
                        break;
                    case AttributeType.MaxStamina:
                    case AttributeType.Stamina:
                        if (mod.ModifierType == AttributeModifierType.Flat) flatStamina += mod.Value;
                        else percentStamina += mod.Value;
                        break;
                    case AttributeType.MaxEssence:
                    case AttributeType.Essence:
                        if (mod.ModifierType == AttributeModifierType.Flat) flatEssence += mod.Value;
                        else percentEssence += mod.Value;
                        break;
                }
            }

            _effectiveMaxHealth = Mathf.Max(1f, (attributeConfig.BaseMaxHealth + flatHealth) * (1f + percentHealth));
            _effectiveMaxStamina = Mathf.Max(0f, (attributeConfig.BaseMaxStamina + flatStamina) * (1f + percentStamina));
            _effectiveMaxEssence = Mathf.Max(0f, (attributeConfig.BaseMaxEssence + flatEssence) * (1f + percentEssence));

            _currentHealth = Mathf.Clamp(_currentHealth, 0f, _effectiveMaxHealth);
            _currentStamina = Mathf.Clamp(_currentStamina, 0f, _effectiveMaxStamina);
            _currentEssence = Mathf.Clamp(_currentEssence, 0f, _effectiveMaxEssence);

            OnHealthChanged?.Invoke(_currentHealth, _effectiveMaxHealth);
            OnStaminaChanged?.Invoke(_currentStamina, _effectiveMaxStamina);
            OnEssenceChanged?.Invoke(_currentEssence, _effectiveMaxEssence);
        }

        public void LogAttributeStatus()
        {
            SSLog.LogAttributes(
                $"[{gameObject.name}] Health: {_currentHealth:F1}/{_effectiveMaxHealth:F1}, " +
                $"Stamina: {_currentStamina:F1}/{_effectiveMaxStamina:F1}, " +
                $"Essence: {_currentEssence:F1}/{_effectiveMaxEssence:F1}, Dead: {_isDead}");
        }

        /* --- Regen (coroutines; no Update) --- */

        private IEnumerator StaminaRegenDelayRoutine()
        {
            yield return new WaitForSeconds(attributeConfig.StaminaRegenDelay);
            _staminaRegenDelayCoroutine = null;
            StartStaminaRegenTick();
        }

        private void StartStaminaRegenTick()
        {
            if (_isDead || !attributeConfig.EnableStaminaRegen || !isActiveAndEnabled)
            {
                return;
            }

            if (_staminaRegenTickCoroutine != null)
            {
                StopCoroutine(_staminaRegenTickCoroutine);
            }

            _staminaRegenTickCoroutine = StartCoroutine(StaminaRegenTickRoutine());
        }

        private IEnumerator StaminaRegenTickRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(attributeConfig.StaminaRegenTickInterval);

            while (!_isDead && attributeConfig.EnableStaminaRegen)
            {
                if (_currentStamina >= _effectiveMaxStamina)
                {
                    _currentStamina = _effectiveMaxStamina;
                    break;
                }

                float step = attributeConfig.StaminaRegenRate * attributeConfig.StaminaRegenTickInterval;
                _currentStamina = Mathf.Min(_currentStamina + step, _effectiveMaxStamina);
                OnStaminaChanged?.Invoke(_currentStamina, _effectiveMaxStamina);

                if (_currentStamina >= _effectiveMaxStamina)
                {
                    break;
                }

                yield return wait;
            }

            _staminaRegenTickCoroutine = null;
        }

        private IEnumerator ModifierExpiryRoutine(string modifierId, float duration)
        {
            yield return new WaitForSeconds(duration);
            _modifierTimers.Remove(modifierId);
            RemoveModifier(modifierId);
        }

        private void StopRegenTimers()
        {
            if (_staminaRegenDelayCoroutine != null)
            {
                StopCoroutine(_staminaRegenDelayCoroutine);
                _staminaRegenDelayCoroutine = null;
            }

            if (_staminaRegenTickCoroutine != null)
            {
                StopCoroutine(_staminaRegenTickCoroutine);
                _staminaRegenTickCoroutine = null;
            }
        }

        private void ClearModifierTimers()
        {
            foreach (KeyValuePair<string, Coroutine> pair in _modifierTimers)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }

            _modifierTimers.Clear();
        }
    }
}
