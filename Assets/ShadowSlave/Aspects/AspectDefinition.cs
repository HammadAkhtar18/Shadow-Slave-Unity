using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Static content definition describing an immutable Aspect archetype in Shadow Slave.
    /// Holds verified canon classification (AspectRank), static ability definitions, and bound Flaw definition.
    /// Cleanly separates immutable static archetype data from mutable runtime character state.
    /// Authored via Unity serialized asset workflow; read-only at runtime.
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

        private ReadOnlyCollection<AspectAbilityDefinition> _readOnlyAbilityDefinitions;
        private List<AspectAbilityDefinition> _cachedAbilityDefinitionsSource;

        private ReadOnlyCollection<AspectMetadataEntry> _readOnlyMetadata;
        private List<AspectMetadataEntry> _cachedMetadataSource;

        public string AspectId => aspectId;
        public string DisplayName => displayName;
        public string Description => description;
        public AspectRank AspectRank => aspectRank;
        public IReadOnlyList<AspectAbilityDefinition> AbilityDefinitions
        {
            get
            {
                if (abilityDefinitions == null)
                {
                    return Array.Empty<AspectAbilityDefinition>();
                }

                if (_readOnlyAbilityDefinitions == null || _cachedAbilityDefinitionsSource != abilityDefinitions)
                {
                    _cachedAbilityDefinitionsSource = abilityDefinitions;
                    _readOnlyAbilityDefinitions = abilityDefinitions.AsReadOnly();
                }

                return _readOnlyAbilityDefinitions;
            }
        }
        public int AbilityCount => abilityDefinitions != null ? abilityDefinitions.Count : 0;
        public FlawDefinition FlawDefinition => flawDefinition;
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
        /// Returns whether this Aspect has a known, non-unknown Aspect Rank.
        /// </summary>
        public bool HasKnownAspectRank() => aspectRank != AspectRank.Unknown;

        /// <summary>
        /// Returns whether this Aspect defines a bound Flaw.
        /// </summary>
        public bool HasFlaw() => flawDefinition != null;

        /// <summary>
        /// Evaluates whether this Aspect Definition contains two or more abilities with identical non-empty Ability IDs.
        /// Uses exact ordinal comparison.
        /// </summary>
        public bool HasDuplicateAbilityIds()
        {
            if (abilityDefinitions == null || abilityDefinitions.Count <= 1)
            {
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < abilityDefinitions.Count; i++)
            {
                AspectAbilityDefinition abilityDef = abilityDefinitions[i];
                if (abilityDef != null && !string.IsNullOrEmpty(abilityDef.AbilityId))
                {
                    if (!seen.Add(abilityDef.AbilityId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

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

        /// <summary>
        /// Compatibility identity accessor matching UE5 SetAspectId.
        /// </summary>
        public void SetAspectId(string newAspectId) => aspectId = newAspectId ?? string.Empty;
    }
}
