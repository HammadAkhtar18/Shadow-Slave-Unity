using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ShadowSlave.Progression;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Static content definition describing an immutable static Aspect Ability archetype.
    /// Holds static identity, Character Rank prerequisite, and base Essence cost parameters.
    /// Runtime activation, state, cooldowns, and essence consumption are handled in later runtime systems.
    /// Authored via Unity serialized asset workflow; read-only at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAspectAbilityDefinition", menuName = "ShadowSlave/Aspects/Aspect Ability Definition")]
    public class AspectAbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string abilityId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField, TextArea]
        private string description = string.Empty;

        [Header("Requirements & Parameters")]
        [SerializeField]
        private ShadowSlaveCharacterRank requiredCharacterRank = ShadowSlaveCharacterRank.Unknown;

        [SerializeField]
        private float baseEssenceCost = 0f;

        [Header("Metadata & Provenance")]
        [SerializeField]
        private List<AspectMetadataEntry> metadata = new List<AspectMetadataEntry>();

        [SerializeField]
        private string canonProvenance = string.Empty;

        private ReadOnlyCollection<AspectMetadataEntry> _readOnlyMetadata;
        private List<AspectMetadataEntry> _cachedMetadataSource;

        public string AbilityId => abilityId;
        public string DisplayName => displayName;
        public string Description => description;
        public ShadowSlaveCharacterRank RequiredCharacterRank => requiredCharacterRank;
        public float BaseEssenceCost => baseEssenceCost;
        public IReadOnlyList<AspectMetadataEntry> Metadata
        {
            get
            {
                if (metadata == null)
                {
                    return Array.Empty<AspectMetadataEntry>();
                }

                if (_readOnlyMetadata == null || _cachedMetadataSource != metadata)
                {
                    _cachedMetadataSource = metadata;
                    _readOnlyMetadata = metadata.AsReadOnly();
                }

                return _readOnlyMetadata;
            }
        }
        public string CanonProvenance => canonProvenance;

        /// <summary>
        /// Returns whether this ability enforces a specific minimum Character Rank prerequisite.
        /// </summary>
        public bool HasRankRequirement() => requiredCharacterRank != ShadowSlaveCharacterRank.Unknown;

        /// <summary>
        /// Compatibility identity accessor matching UE5 SetAbilityId.
        /// </summary>
        public void SetAbilityId(string newAbilityId) => abilityId = newAbilityId ?? string.Empty;
    }
}
