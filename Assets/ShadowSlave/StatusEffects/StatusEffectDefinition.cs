using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ShadowSlave.StatusEffects
{
    /// <summary>
    /// Data-driven ScriptableObject archetype describing immutable static status effect / condition definitions.
    /// Cleanly separates static archetype configuration from mutable runtime instances (StatusEffectInstance).
    /// Matches UE5 UShadowSlaveStatusEffectDefinition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatusEffectDefinition", menuName = "ShadowSlave/Status Effects/Status Effect Definition")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string effectId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField, TextArea]
        private string description = string.Empty;

        [Header("Duration & Stacking Policies")]
        [SerializeField]
        private StatusEffectDurationPolicy durationPolicy = StatusEffectDurationPolicy.Timed;

        [SerializeField]
        private float duration = 5.0f;

        [SerializeField]
        private StatusEffectStackingPolicy stackingPolicy = StatusEffectStackingPolicy.RefreshDuration;

        [SerializeField]
        private int maxStacks = 1;

        [Header("Classification & Persistence")]
        [SerializeField]
        private StatusEffectPolarity polarity = StatusEffectPolarity.Neutral;

        [SerializeField]
        private bool persistAcrossSaveLoad = false;

        [Header("Metadata & Tags")]
        [SerializeField]
        private List<string> customTags = new List<string>();

        private ReadOnlyCollection<string> _readOnlyCustomTags;
        private List<string> _cachedCustomTagsSource;

        public string EffectId => effectId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public StatusEffectDurationPolicy DurationPolicy => durationPolicy;
        public float Duration => duration;
        public StatusEffectStackingPolicy StackingPolicy => stackingPolicy;
        public int MaxStacks => Mathf.Max(1, maxStacks);
        public StatusEffectPolarity Polarity => polarity;
        public bool PersistAcrossSaveLoad => persistAcrossSaveLoad;

        public IReadOnlyList<string> CustomTags
        {
            get
            {
                if (customTags == null)
                {
                    return Array.Empty<string>();
                }

                if (_readOnlyCustomTags == null || _cachedCustomTagsSource != customTags)
                {
                    _cachedCustomTagsSource = customTags;
                    _readOnlyCustomTags = customTags.AsReadOnly();
                }

                return _readOnlyCustomTags;
            }
        }

        /// <summary>
        /// Validates definition configuration.
        /// </summary>
        public bool IsValidDefinition(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                errorMessage = "EffectId cannot be null or whitespace.";
                return false;
            }

            if (durationPolicy == StatusEffectDurationPolicy.Timed)
            {
                if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
                {
                    errorMessage = $"Timed status effect '{effectId}' must have a positive, finite duration.";
                    return false;
                }
            }

            if (stackingPolicy == StatusEffectStackingPolicy.AddStacks && maxStacks < 1)
            {
                errorMessage = $"Status effect '{effectId}' with AddStacks policy must have MaxStacks >= 1.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// Convenience helper for tests or programmatic instantiation.
        /// </summary>
        public static StatusEffectDefinition Create(
            string effectId,
            string displayName = "",
            string description = "",
            StatusEffectDurationPolicy durationPolicy = StatusEffectDurationPolicy.Timed,
            float duration = 5f,
            StatusEffectStackingPolicy stackingPolicy = StatusEffectStackingPolicy.RefreshDuration,
            int maxStacks = 1,
            StatusEffectPolarity polarity = StatusEffectPolarity.Neutral,
            bool persistAcrossSaveLoad = false,
            IEnumerable<string> tags = null)
        {
            var def = CreateInstance<StatusEffectDefinition>();
            def.effectId = effectId ?? string.Empty;
            def.displayName = displayName ?? string.Empty;
            def.description = description ?? string.Empty;
            def.durationPolicy = durationPolicy;
            def.duration = duration;
            def.stackingPolicy = stackingPolicy;
            def.maxStacks = maxStacks;
            def.polarity = polarity;
            def.persistAcrossSaveLoad = persistAcrossSaveLoad;
            if (tags != null)
            {
                def.customTags.AddRange(tags);
            }
            return def;
        }

        /// <summary>
        /// Compatibility identity setter matching UE5 SetEffectId.
        /// </summary>
        public void SetEffectId(string newEffectId) => effectId = newEffectId ?? string.Empty;
    }
}
