using System;
using UnityEngine;

namespace ShadowSlave.Progression
{
    /// <summary>
    /// Character progression ranks in Shadow Slave.
    /// Ordered ordinally from Unknown through Divine.
    /// Independent of future Aspect rank.
    /// </summary>
    public enum ShadowSlaveCharacterRank
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
    /// Represents an entity's soul core configuration state.
    /// Tracks current formed cores and maximum cultivatable core capacity.
    /// Supports zero cores (mundane humans/hollows), single-core standard Awakened,
    /// and multi-core divine aspects / nightmare creatures up to Titan class (7).
    /// </summary>
    [Serializable]
    public struct SoulCoreState : IEquatable<SoulCoreState>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private int currentSoulCores;

        [SerializeField]
        private int maximumSoulCores;

        public int CurrentSoulCores => Mathf.Clamp(currentSoulCores, 0, MaximumSoulCores);
        public int MaximumSoulCores => Mathf.Max(1, maximumSoulCores);

        public SoulCoreState(int current, int max)
        {
            maximumSoulCores = Mathf.Max(1, max);
            currentSoulCores = Mathf.Clamp(current, 0, maximumSoulCores);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Normalize();
        }

        /// <summary>
        /// Normalizes internal state to guarantee invariants: current >= 0, maximum >= 1, current <= maximum.
        /// </summary>
        public void Normalize()
        {
            maximumSoulCores = Mathf.Max(1, maximumSoulCores);
            currentSoulCores = Mathf.Clamp(currentSoulCores, 0, maximumSoulCores);
        }

        internal SoulCoreState WithCurrent(int current)
        {
            SoulCoreState copy = this;
            copy.maximumSoulCores = Mathf.Max(1, copy.maximumSoulCores);
            copy.currentSoulCores = Mathf.Clamp(current, 0, copy.maximumSoulCores);
            return copy;
        }

        internal SoulCoreState WithMaximum(int max)
        {
            SoulCoreState copy = this;
            copy.maximumSoulCores = Mathf.Max(1, max);
            copy.currentSoulCores = Mathf.Clamp(copy.currentSoulCores, 0, copy.maximumSoulCores);
            return copy;
        }

        /// <summary>True if entity possesses more than one active soul core (Monster class or higher).</summary>
        public bool HasMultipleCores => CurrentSoulCores > 1;

        /// <summary>True if current soul cores have reached maximum capacity.</summary>
        public bool IsMaxCoresReached => CurrentSoulCores >= MaximumSoulCores;

        public bool Equals(SoulCoreState other)
        {
            return CurrentSoulCores == other.CurrentSoulCores &&
                   MaximumSoulCores == other.MaximumSoulCores;
        }

        public override bool Equals(object obj)
        {
            return obj is SoulCoreState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CurrentSoulCores * 397) ^ MaximumSoulCores;
            }
        }
    }

    /// <summary>
    /// Represents a serializable key-value pair for progression metadata.
    /// Used for quest, story, and advancement tracking state.
    /// </summary>
    [Serializable]
    public struct ProgressionMetadataEntry : IEquatable<ProgressionMetadataEntry>
    {
        [SerializeField]
        private string key;

        [SerializeField]
        private string value;

        public string Key => key;
        public string Value => value;

        public ProgressionMetadataEntry(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public bool Equals(ProgressionMetadataEntry other)
        {
            return string.Equals(key, other.key, StringComparison.Ordinal) &&
                   string.Equals(value, other.value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionMetadataEntry other && Equals(other);
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
