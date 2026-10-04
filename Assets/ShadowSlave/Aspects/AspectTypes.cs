using System;
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
}
