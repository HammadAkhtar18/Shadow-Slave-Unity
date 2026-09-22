using System;
using System.Collections.Generic;

namespace ShadowSlave.Core
{
    /// <summary>
    /// Mutable set of gameplay tags with hierarchical HasTag / HasAny / HasAll matching.
    /// Mirrors UE FGameplayTagContainer query semantics used by Shadow Slave systems.
    /// </summary>
    [Serializable]
    public class GameplayTagContainer
    {
        private readonly List<GameplayTag> _tags = new List<GameplayTag>();

        public IReadOnlyList<GameplayTag> Tags => _tags;

        public int Count => _tags.Count;

        public void Add(GameplayTag tag)
        {
            if (!tag.IsValid)
            {
                return;
            }

            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Equals(tag))
                {
                    return;
                }
            }

            _tags.Add(tag);
        }

        public void Add(string tag) => Add(new GameplayTag(tag));

        public bool Remove(GameplayTag tag)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Equals(tag))
                {
                    _tags.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool Remove(string tag) => Remove(new GameplayTag(tag));

        public void Clear() => _tags.Clear();

        /// <summary>
        /// True if any owned tag matches <paramref name="tag"/> hierarchically
        /// (exact or owned child of the query tag).
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            if (!tag.IsValid)
            {
                return false;
            }

            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Matches(tag))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasTag(string tag) => HasTag(new GameplayTag(tag));

        public bool HasAny(GameplayTagContainer other)
        {
            if (other == null || other.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < other._tags.Count; i++)
            {
                if (HasTag(other._tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAny(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < tags.Length; i++)
            {
                if (HasTag(tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAll(GameplayTagContainer other)
        {
            if (other == null || other.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < other._tags.Count; i++)
            {
                if (!HasTag(other._tags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAll(params GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < tags.Length; i++)
            {
                if (!HasTag(tags[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
