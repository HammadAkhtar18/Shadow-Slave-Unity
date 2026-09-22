using System;
using ShadowSlave.Attributes;
using UnityEngine;

namespace ShadowSlave.Combat
{
    /// <summary>
    /// Minimal combat test dummy for validating damage application and death
    /// without AI. Mirrors purpose of UE AShadowSlaveCombatDummy.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AttributeComponent))]
    public class CombatDummy : MonoBehaviour, IDamageable
    {
        private AttributeComponent _attributeComponent;
        private bool _isAlive = true;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDummyDied;

        public AttributeComponent AttributeComponent => _attributeComponent;

        private void Awake()
        {
            _attributeComponent = GetComponent<AttributeComponent>();
            _isAlive = true;
            if (_attributeComponent != null)
            {
                _attributeComponent.OnHealthChanged += HandleAttributeHealthChanged;
                _attributeComponent.OnDeath += HandleDeath;
            }
        }

        private void OnDestroy()
        {
            if (_attributeComponent != null)
            {
                _attributeComponent.OnHealthChanged -= HandleAttributeHealthChanged;
                _attributeComponent.OnDeath -= HandleDeath;
            }
        }

        public float TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive())
            {
                return 0f;
            }

            if (_attributeComponent == null)
            {
                return 0f;
            }

            return _attributeComponent.ApplyDamage(damageInfo.DamageAmount, damageInfo);
        }

        public bool IsAlive()
        {
            return _attributeComponent != null ? _attributeComponent.IsAlive : _isAlive;
        }

        public float GetHealthPercent()
        {
            return _attributeComponent != null ? _attributeComponent.HealthPercent : 0f;
        }

        private void HandleAttributeHealthChanged(float current, float max)
        {
            OnHealthChanged?.Invoke(current, max);
        }

        private void HandleDeath()
        {
            _isAlive = false;
            OnDummyDied?.Invoke();
        }
    }
}
