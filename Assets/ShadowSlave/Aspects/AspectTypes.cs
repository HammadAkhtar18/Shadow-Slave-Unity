using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ShadowSlave.Aspects
{
    /// <summary>
    /// Canon and technical Aspect Rank representing the intrinsic rarity/potency tier of an Aspect.
    /// NOTE: Completely independent of Character Rank (e.g. Sunny can be a Dormant sleeper with a Divine Aspect).
    /// Distinct enum from ShadowSlaveCharacterRank.
    /// </summary>
    public enum AspectRank : byte
    {
        Unknown = 0,
        Dormant = 1,
        Awakened = 2,
        Ascended = 3,
        Transcendent = 4,
        Supreme = 5,
        Sacred = 6,
        Divine = 7
    }

    /// <summary>
    /// Serializable key-value pair for static content metadata and provenance tracking.
    /// Kept strictly separate from runtime Progression Metadata.
    /// </summary>
    [Serializable]
    public struct AspectMetadataEntry : IEquatable<AspectMetadataEntry>
    {
        [SerializeField]
        private string key;

        [SerializeField]
        private string value;

        public string Key => key;
        public string Value => value;

        public AspectMetadataEntry(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public bool Equals(AspectMetadataEntry other)
        {
            return string.Equals(key, other.key, StringComparison.Ordinal) &&
                   string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is AspectMetadataEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((key != null ? StringComparer.Ordinal.GetHashCode(key) : 0) * 397) ^
                       (value != null ? StringComparer.Ordinal.GetHashCode(value) : 0);
            }
        }
    }

    /// <summary>
    /// Runtime state representing an individual Aspect Ability on a character.
    /// Separates immutable archetype data (<see cref="AspectAbilityDefinition"/>)
    /// from mutable runtime state (IsUnlocked, IsActive).
    /// Owned by <see cref="AspectComponent"/>; strictly runtime state, not a ScriptableObject.
    /// </summary>
    [Serializable]
    public class AspectAbilityInstance
    {
        [SerializeField]
        private AspectAbilityDefinition abilityDefinition;

        [SerializeField]
        private bool isUnlocked;

        [SerializeField]
        private bool isActive;

        private Dictionary<string, string> dynamicProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        private ReadOnlyDictionary<string, string> _readOnlyDynamicProperties;

        /// <summary>
        /// Reference to the immutable static definition for this ability archetype.
        /// </summary>
        public AspectAbilityDefinition AbilityDefinition => abilityDefinition;

        /// <summary>
        /// Unique technical identifier from the underlying ability definition, or empty if unbound.
        /// </summary>
        public string AbilityId => abilityDefinition != null ? abilityDefinition.AbilityId : string.Empty;

        /// <summary>
        /// Whether this ability has been unlocked/awakened for this character.
        /// </summary>
        public bool IsUnlocked
        {
            get => isUnlocked;
            internal set => isUnlocked = value;
        }

        /// <summary>
        /// Whether this ability is currently active or sustained.
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            internal set => isActive = value;
        }

        /// <summary>
        /// Returns whether this runtime instance is bound to a valid static ability definition.
        /// </summary>
        public bool IsValid => abilityDefinition != null;

        /// <summary>
        /// Read-only view of runtime dynamic instance properties.
        /// Direct external mutation is prevented; mutations must go through <see cref="AspectComponent.SetAbilityDynamicProperty"/>.
        /// Uses deterministic ordinal key comparison.
        /// Backed by a ReadOnlyDictionary wrapper preventing cast recovery of mutable backing dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> DynamicProperties
        {
            get
            {
                Dictionary<string, string> dict = GetOrCreateDynamicProperties();
                if (_readOnlyDynamicProperties == null)
                {
                    _readOnlyDynamicProperties = new ReadOnlyDictionary<string, string>(dict);
                }
                return _readOnlyDynamicProperties;
            }
        }

        internal bool SetDynamicProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            GetOrCreateDynamicProperties()[key] = value ?? string.Empty;
            return true;
        }

        internal bool TryGetDynamicProperty(string key, out string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                value = null;
                return false;
            }

            return GetOrCreateDynamicProperties().TryGetValue(key, out value);
        }

        private Dictionary<string, string> GetOrCreateDynamicProperties()
        {
            if (dynamicProperties == null)
            {
                dynamicProperties = new Dictionary<string, string>(StringComparer.Ordinal);
            }
            return dynamicProperties;
        }

        public AspectAbilityInstance()
        {
            abilityDefinition = null;
            isUnlocked = false;
            isActive = false;
            dynamicProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public AspectAbilityInstance(AspectAbilityDefinition definition, bool isUnlocked = false)
        {
            abilityDefinition = definition;
            this.isUnlocked = isUnlocked;
            isActive = false;
            dynamicProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
