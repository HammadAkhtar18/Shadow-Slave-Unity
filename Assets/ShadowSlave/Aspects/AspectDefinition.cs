using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Static content definition describing an immutable Aspect archetype in Shadow Slave.
    /// Holds verified canon classification (AspectRank), static ability definitions, and bound Flaw definition.
    /// Cleanly separates immutable static archetype data from mutable runtime character state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAspectDefinition", menuName = "ShadowSlave/Aspects/Aspect Definition")]
    public class AspectDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string aspectId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField, TextArea]
        private string description = string.Empty;

        [Header("Classification")]
        [SerializeField]
        private AspectRank aspectRank = AspectRank.Unknown;

        [Header("Abilities & Flaw")]
        [SerializeField]
        private List<AspectAbilityDefinition> abilityDefinitions = new List<AspectAbilityDefinition>();

        [SerializeField]
        private FlawDefinition flawDefinition;

        [Header("Metadata & Provenance")]
        [SerializeField]
        private List<AspectMetadataEntry> metadata = new List<AspectMetadataEntry>();

        [SerializeField]
        private string canonProvenance = string.Empty;

        public string AspectId => aspectId;
        public string DisplayName => displayName;
        public string Description => description;
        public AspectRank AspectRank => aspectRank;
        public IReadOnlyList<AspectAbilityDefinition> AbilityDefinitions => abilityDefinitions;
        public int AbilityCount => abilityDefinitions != null ? abilityDefinitions.Count : 0;
        public FlawDefinition FlawDefinition => flawDefinition;
        public IReadOnlyList<AspectMetadataEntry> Metadata => metadata;
        public string CanonProvenance => canonProvenance;

        /// <summary>
        /// Returns whether this Aspect has a known, non-unknown Aspect Rank.
        /// </summary>
        public bool HasKnownAspectRank() => aspectRank != AspectRank.Unknown;

        /// <summary>
        /// Returns whether this Aspect defines a bound Flaw.
        /// </summary>
        public bool HasFlaw() => flawDefinition != null;

        /// <summary>
        /// Finds a static ability definition by its unique AbilityId using exact ordinal comparison.
        /// Returns null if not found or if abilityId is null/empty.
        /// </summary>
        public AspectAbilityDefinition FindAbilityById(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId) || abilityDefinitions == null)
            {
                return null;
            }

            for (int i = 0; i < abilityDefinitions.Count; i++)
            {
                AspectAbilityDefinition abilityDef = abilityDefinitions[i];
                if (abilityDef != null && string.Equals(abilityDef.AbilityId, abilityId, StringComparison.Ordinal))
                {
                    return abilityDef;
                }
            }

            return null;
        }

        public void SetAspectId(string newAspectId) => aspectId = newAspectId ?? string.Empty;
        public void SetDisplayName(string newDisplayName) => displayName = newDisplayName ?? string.Empty;
        public void SetDescription(string newDescription) => description = newDescription ?? string.Empty;
        public void SetAspectRank(AspectRank rank) => aspectRank = rank;
        public void SetFlawDefinition(FlawDefinition newFlawDef) => flawDefinition = newFlawDef;
        public void SetCanonProvenance(string newProvenance) => canonProvenance = newProvenance ?? string.Empty;

        public void SetAbilityDefinitions(IEnumerable<AspectAbilityDefinition> abilities)
        {
            abilityDefinitions.Clear();
            if (abilities != null)
            {
                abilityDefinitions.AddRange(abilities);
            }
        }

        public void AddAbilityDefinition(AspectAbilityDefinition ability)
        {
            if (ability != null && !abilityDefinitions.Contains(ability))
            {
                abilityDefinitions.Add(ability);
            }
        }

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
