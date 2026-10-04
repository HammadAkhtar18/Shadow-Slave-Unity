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
        private SoulCoreState soulCoreState = new SoulCoreState(1, 1);

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
        /// Returns current active soul core count.
        /// </summary>
        public int GetSoulCoreCount()
        {
            return soulCoreState.CurrentSoulCores;
        }

        /// <summary>
        /// Returns maximum soul cores cultivatable by this entity.
        /// </summary>
        public int GetMaxSoulCores()
        {
            return soulCoreState.MaximumSoulCores;
        }

        /// <summary>
        /// Returns copy of current soul core state struct.
        /// </summary>
        public SoulCoreState GetSoulCoreState()
        {
            return soulCoreState;
        }

        /// <summary>
        /// Sets soul core count clamped between 0 and MaximumSoulCores.
        /// Broadcasts <see cref="OnSoulCoreCountChanged"/> if the value changed.
        /// Returns true.
        /// </summary>
        public bool SetSoulCoreCount(int newCount)
        {
            int clampedCount = Mathf.Clamp(newCount, 0, soulCoreState.MaximumSoulCores);
            if (soulCoreState.CurrentSoulCores == clampedCount)
            {
                return true;
            }

            int oldCount = soulCoreState.CurrentSoulCores;
            soulCoreState = soulCoreState.WithCurrent(clampedCount);
            OnSoulCoreCountChanged?.Invoke(clampedCount, oldCount);
            return true;
        }

        /// <summary>
        /// Sets maximum soul core limit (clamped to at least 1).
        /// Broadcasts <see cref="OnMaxSoulCoresChanged"/> if the value changed.
        /// If current cores exceed the new maximum, clamps current cores down and broadcasts <see cref="OnSoulCoreCountChanged"/>.
        /// Returns true.
        /// </summary>
        public bool SetMaxSoulCores(int newMax)
        {
            int clampedMax = Mathf.Max(1, newMax);
            if (soulCoreState.MaximumSoulCores == clampedMax)
            {
                return true;
            }

            int oldMax = soulCoreState.MaximumSoulCores;
            soulCoreState = soulCoreState.WithMaximum(clampedMax);
            OnMaxSoulCoresChanged?.Invoke(clampedMax, oldMax);

            if (soulCoreState.CurrentSoulCores > clampedMax)
            {
                SetSoulCoreCount(clampedMax);
            }

            return true;
        }

        /// <summary>
        /// Increments soul core count by count (clamped to MaximumSoulCores).
        /// Overflow-safe against arbitrarily large inputs such as int.MaxValue.
        /// Rejects non-positive count (returns false without mutation or event).
        /// </summary>
        public bool AddSoulCore(int count = 1)
        {
            if (count <= 0)
            {
                return false;
            }

            int current = soulCoreState.CurrentSoulCores;
            int max = soulCoreState.MaximumSoulCores;
            int targetCount = (count >= max - current) ? max : current + count;
            return SetSoulCoreCount(targetCount);
        }

        /// <summary>
        /// Decrements soul core count by count (clamped to at least 0).
        /// Underflow-safe against arbitrarily large inputs such as int.MaxValue.
        /// Rejects non-positive count (returns false without mutation or event).
        /// </summary>
        public bool RemoveSoulCore(int count = 1)
        {
            if (count <= 0)
            {
                return false;
            }

            int current = soulCoreState.CurrentSoulCores;
            int targetCount = (count >= current) ? 0 : current - count;
            return SetSoulCoreCount(targetCount);
        }
    }
}
