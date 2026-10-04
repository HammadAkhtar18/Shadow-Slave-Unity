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
    public struct SoulCoreState
    {
        [SerializeField]
        private int currentSoulCores;

        [SerializeField]
        private int maximumSoulCores;

        public int CurrentSoulCores => currentSoulCores;
        public int MaximumSoulCores => maximumSoulCores;

        public SoulCoreState(int current, int max)
        {
            maximumSoulCores = Mathf.Max(1, max);
            currentSoulCores = Mathf.Clamp(current, 0, maximumSoulCores);
        }

        /// <summary>True if entity possesses more than one active soul core (Monster class or higher).</summary>
        public bool HasMultipleCores => currentSoulCores > 1;

        /// <summary>True if current soul cores have reached maximum capacity.</summary>
        public bool IsMaxCoresReached => currentSoulCores >= maximumSoulCores;
    }
}
