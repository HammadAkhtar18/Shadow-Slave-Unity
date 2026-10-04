using System;
using System.Collections.Generic;
using ShadowSlave.Progression;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Static content definition describing an immutable static Aspect Ability archetype.
    /// Holds static identity, Character Rank prerequisite, and base Essence cost parameters.
    /// Runtime activation, state, cooldowns, and essence consumption are handled in later runtime systems.
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

        public string AbilityId => abilityId;
        public string DisplayName => displayName;
        public string Description => description;
        public ShadowSlaveCharacterRank RequiredCharacterRank => requiredCharacterRank;
        public float BaseEssenceCost => baseEssenceCost;
        public IReadOnlyList<AspectMetadataEntry> Metadata => metadata;
        public string CanonProvenance => canonProvenance;

        /// <summary>
        /// Returns whether this ability enforces a specific minimum Character Rank prerequisite.
        /// </summary>
        public bool HasRankRequirement() => requiredCharacterRank != ShadowSlaveCharacterRank.Unknown;

        public void SetAbilityId(string newAbilityId) => abilityId = newAbilityId ?? string.Empty;
        public void SetDisplayName(string newDisplayName) => displayName = newDisplayName ?? string.Empty;
        public void SetDescription(string newDescription) => description = newDescription ?? string.Empty;
        public void SetRequiredCharacterRank(ShadowSlaveCharacterRank rank) => requiredCharacterRank = rank;
        public void SetBaseEssenceCost(float cost) => baseEssenceCost = Mathf.Max(0f, cost);
        public void SetCanonProvenance(string newProvenance) => canonProvenance = newProvenance ?? string.Empty;

        public void SetMetadata(IEnumerable<AspectMetadataEntry> entries)
        {
            metadata.Clear();
            if (entries != null)
            {
                metadata.AddRange(entries);
            }
        }
    }
}
