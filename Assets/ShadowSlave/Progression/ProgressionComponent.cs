using System;
using UnityEngine;

namespace ShadowSlave.Progression
{
    /// <summary>
    /// Owns the character's progression state: Nightmare Spell character rank and soul cores.
    /// Does not tick or poll; state changes occur explicitly via method calls.
    /// Character rank and soul core capacity are independent progression axes.
    /// Resource storage (Health, Stamina, Essence) remains strictly within AttributeComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressionComponent : MonoBehaviour
    {
        [Header("Rank")]
        [SerializeField]
        private ShadowSlaveCharacterRank currentRank = ShadowSlaveCharacterRank.Unknown;

        [Header("Soul Cores")]
        [SerializeField]
        private int currentSoulCores = 1;

        [SerializeField]
        private int maximumSoulCores = 1;

        /// <summary>
        /// Fires when the character rank changes. Arguments are (newRank, oldRank).
        /// Does not fire when setting the same rank.
        /// </summary>
        public event Action<ShadowSlaveCharacterRank, ShadowSlaveCharacterRank> OnCharacterRankChanged;

        /// <summary>
        /// Fires when active soul core count changes. Arguments are (newCount, oldCount).
        /// Does not fire when setting the same clamped count.
        /// </summary>
        public event Action<int, int> OnSoulCoreCountChanged;

        /// <summary>
        /// Fires when maximum soul core capacity changes. Arguments are (newMax, oldMax).
        /// Does not fire when setting the same clamped maximum.
        /// </summary>
        public event Action<int, int> OnMaxSoulCoresChanged;

        /* --- Character Rank API --- */

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

        /* --- Soul Core API --- */

        /// <summary>
        /// Current number of formed soul cores.
        /// </summary>
        public int CurrentSoulCores => currentSoulCores;

        /// <summary>
        /// Maximum number of soul cores this entity can cultivate or form.
        /// </summary>
        public int MaximumSoulCores => maximumSoulCores;

        /// <summary>
        /// Returns current active soul core count.
        /// </summary>
        public int GetSoulCoreCount()
        {
            return currentSoulCores;
        }

        /// <summary>
        /// Returns maximum soul cores cultivatable by this entity.
        /// </summary>
        public int GetMaximumSoulCores()
        {
            return maximumSoulCores;
        }

        /// <summary>
        /// Alias for <see cref="GetMaximumSoulCores"/> matching UE5 naming.
        /// </summary>
        public int GetMaxSoulCores()
        {
            return maximumSoulCores;
        }

        /// <summary>
        /// Returns copy of current soul core state struct.
        /// </summary>
        public SoulCoreState GetSoulCoreState()
        {
            return new SoulCoreState(currentSoulCores, maximumSoulCores);
        }

        /// <summary>
        /// Returns true if this entity possesses more than one active soul core (Monster class or higher).
        /// </summary>
        public bool HasMultipleCores()
        {
            return currentSoulCores > 1;
        }

        /// <summary>
        /// Returns whether soul core count is at maximum capacity.
        /// </summary>
        public bool IsMaxCoresReached()
        {
            return currentSoulCores >= maximumSoulCores;
        }

        /// <summary>
        /// Sets soul core count clamped between 0 and MaximumSoulCores.
        /// Broadcasts <see cref="OnSoulCoreCountChanged"/> if the value changed.
        /// Returns true.
        /// </summary>
        public bool SetSoulCoreCount(int newCount)
        {
            int clampedCount = Mathf.Clamp(newCount, 0, maximumSoulCores);
            if (currentSoulCores == clampedCount)
            {
                return true;
            }

            int oldCount = currentSoulCores;
            currentSoulCores = clampedCount;
            OnSoulCoreCountChanged?.Invoke(clampedCount, oldCount);
            return true;
        }

        /// <summary>
        /// Sets maximum soul core limit (clamped to at least 1).
        /// Broadcasts <see cref="OnMaxSoulCoresChanged"/> if the value changed.
        /// If current cores exceed the new maximum, clamps current cores down and broadcasts <see cref="OnSoulCoreCountChanged"/>.
        /// Returns true.
        /// </summary>
        public bool SetMaximumSoulCores(int newMax)
        {
            int clampedMax = Mathf.Max(1, newMax);
            if (maximumSoulCores == clampedMax)
            {
                return true;
            }

            int oldMax = maximumSoulCores;
            maximumSoulCores = clampedMax;
            OnMaxSoulCoresChanged?.Invoke(clampedMax, oldMax);

            if (currentSoulCores > clampedMax)
            {
                SetSoulCoreCount(clampedMax);
            }

            return true;
        }

        /// <summary>
        /// Alias for <see cref="SetMaximumSoulCores"/> matching UE5 naming.
        /// </summary>
        public bool SetMaxSoulCores(int newMax)
        {
            return SetMaximumSoulCores(newMax);
        }

        /// <summary>
        /// Increments soul core count by count (clamped to MaximumSoulCores).
        /// Rejects non-positive count (returns false without mutation or event).
        /// </summary>
        public bool AddSoulCores(int count = 1)
        {
            if (count <= 0)
            {
                return false;
            }

            return SetSoulCoreCount(currentSoulCores + count);
        }

        /// <summary>
        /// Alias for <see cref="AddSoulCores"/> matching UE5 naming.
        /// </summary>
        public bool AddSoulCore(int count = 1)
        {
            return AddSoulCores(count);
        }

        /// <summary>
        /// Decrements soul core count by count (clamped to at least 0).
        /// Rejects non-positive count (returns false without mutation or event).
        /// </summary>
        public bool RemoveSoulCores(int count = 1)
        {
            if (count <= 0)
            {
                return false;
            }

            return SetSoulCoreCount(currentSoulCores - count);
        }

        /// <summary>
        /// Alias for <see cref="RemoveSoulCores"/> matching UE5 naming.
        /// </summary>
        public bool RemoveSoulCore(int count = 1)
        {
            return RemoveSoulCores(count);
        }
    }
}
