using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ShadowSlave.Tests")]

namespace ShadowSlave.Attributes
{
    public enum AttributeType
    {
        Health,
        MaxHealth,
        Stamina,
        MaxStamina,
        Essence,
        MaxEssence
    }

    public enum AttributeModifierType
    {
        Flat,
        Percent
    }

    [Serializable]
    public struct AttributeModifier
    {
        public string ModifierId;
        public AttributeType TargetAttribute;
        public AttributeModifierType ModifierType;
        public float Value;
        /// <summary>Duration in seconds. 0 = indefinite until removed.</summary>
        public float Duration;
        public Guid SourceId;

        public AttributeModifier(
            string modifierId,
            AttributeType targetAttribute,
            AttributeModifierType modifierType,
            float value,
            float duration = 0f,
            Guid sourceId = default)
        {
            ModifierId = modifierId;
            TargetAttribute = targetAttribute;
            ModifierType = modifierType;
            Value = value;
            Duration = duration;
            SourceId = sourceId;
        }
    }

    [Serializable]
    public struct AttributeInitConfig
    {
        [Min(1f)] public float BaseMaxHealth;
        [Min(0f)] public float BaseMaxStamina;
        [Min(0f)] public float BaseMaxEssence;
        public bool EnableStaminaRegen;
        [Min(0f)] public float StaminaRegenRate;
        [Min(0f)] public float StaminaRegenDelay;
        [Min(0.02f)] public float StaminaRegenTickInterval;

        public static AttributeInitConfig Default => new AttributeInitConfig
        {
            BaseMaxHealth = 100f,
            BaseMaxStamina = 100f,
            BaseMaxEssence = 100f,
            EnableStaminaRegen = true,
            StaminaRegenRate = 20f,
            StaminaRegenDelay = 1f,
            StaminaRegenTickInterval = 0.1f
        };
    }
}
