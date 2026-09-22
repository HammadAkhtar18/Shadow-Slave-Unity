using System;

namespace ShadowSlave.Core
{
    /// <summary>
    /// Hierarchical gameplay tag (e.g. "State.Active"), mirroring UE FGameplayTag behaviour.
    /// Matching is case-sensitive and supports parent/child relationships via '.' separators.
    /// </summary>
    [Serializable]
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        public readonly string Value;

        public GameplayTag(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(GameplayTag other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is GameplayTag other && Equals(other);

        public override int GetHashCode() =>
            Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;

        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(GameplayTag left, GameplayTag right) => left.Equals(right);
        public static bool operator !=(GameplayTag left, GameplayTag right) => !left.Equals(right);

        public static implicit operator GameplayTag(string value) => new GameplayTag(value);

        /// <summary>
        /// Returns true if this tag equals <paramref name="other"/> or is a hierarchical child of it.
        /// Example: "State.Active".Matches("State") == true; "State".Matches("State.Active") == false.
        /// </summary>
        public bool Matches(GameplayTag other)
        {
            if (!IsValid || !other.IsValid)
            {
                return false;
            }

            if (string.Equals(Value, other.Value, StringComparison.Ordinal))
            {
                return true;
            }

            // Child matches parent: "State.Active" matches request for "State"
            return Value.StartsWith(other.Value + ".", StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if this tag equals <paramref name="other"/> or is an ancestor of it.
        /// Example: "State".MatchesExactOrParentOf("State.Active") == true.
        /// </summary>
        public bool MatchesExactOrParentOf(GameplayTag other)
        {
            if (!IsValid || !other.IsValid)
            {
                return false;
            }

            if (string.Equals(Value, other.Value, StringComparison.Ordinal))
            {
                return true;
            }

            return other.Value.StartsWith(Value + ".", StringComparison.Ordinal);
        }
    }
}
