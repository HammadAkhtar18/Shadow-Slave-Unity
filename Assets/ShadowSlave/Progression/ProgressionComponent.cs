using System;
using UnityEngine;

namespace ShadowSlave.Progression
{
    /// <summary>
    /// Owns the character's progression rank.
    /// Does not tick or poll; state changes occur explicitly via method calls.
    /// Character rank is independent from Aspect rank.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressionComponent : MonoBehaviour
    {
        [SerializeField]
        private ShadowSlaveCharacterRank currentRank = ShadowSlaveCharacterRank.Unknown;

        /// <summary>
        /// Fires when the character rank changes. Arguments are (newRank, oldRank).
        /// Does not fire when setting the same rank.
        /// </summary>
        public event Action<ShadowSlaveCharacterRank, ShadowSlaveCharacterRank> OnCharacterRankChanged;

        /// <summary>
        /// Current character progression rank.
        /// </summary>
        public ShadowSlaveCharacterRank CurrentRank => currentRank;

        /// <summary>
        /// Gets the current character progression rank.
        /// </summary>
        public ShadowSlaveCharacterRank GetCharacterRank()
        {
            return currentRank;
        }

        /// <summary>
        /// Returns true if the current rank is initialized to a known rank (Dormant through Divine).
        /// Returns false for Unknown.
        /// </summary>
        public bool HasKnownRank()
        {
            return currentRank >= ShadowSlaveCharacterRank.Dormant &&
                   currentRank <= ShadowSlaveCharacterRank.Divine;
        }

        /// <summary>
        /// Explicitly sets the character rank.
        /// If the new rank equals the current rank, no state changes and no event fires.
        /// Otherwise, updates the rank and fires <see cref="OnCharacterRankChanged"/>.
        /// Intermediate ranks are not automatically advanced.
        /// </summary>
        public void SetCharacterRank(ShadowSlaveCharacterRank newRank)
        {
            if (newRank == currentRank)
            {
                return;
            }

            ShadowSlaveCharacterRank oldRank = currentRank;
            currentRank = newRank;
            OnCharacterRankChanged?.Invoke(newRank, oldRank);
        }

        /// <summary>
        /// Evaluates whether the character can advance to the target rank.
        /// Requirements:
        /// - Current rank must be a known rank (not Unknown).
        /// - Target rank must be a valid known rank (Dormant through Divine).
        /// - Target rank must be strictly greater than current rank.
        /// Does not mutate state or fire events.
        /// </summary>
        public bool CanAdvanceRank(ShadowSlaveCharacterRank targetRank)
        {
            if (!HasKnownRank())
            {
                return false;
            }

            if (targetRank < ShadowSlaveCharacterRank.Dormant ||
                targetRank > ShadowSlaveCharacterRank.Divine)
            {
                return false;
            }

            return targetRank > currentRank;
        }

        /// <summary>
        /// Advances the character rank to the specified target rank if valid.
        /// Returns false without mutation or event if <see cref="CanAdvanceRank"/> fails.
        /// Returns true and updates rank with exactly one event if valid.
        /// </summary>
        public bool AdvanceRank(ShadowSlaveCharacterRank targetRank)
        {
            if (!CanAdvanceRank(targetRank))
            {
                return false;
            }

            SetCharacterRank(targetRank);
            return true;
        }
    }
}
