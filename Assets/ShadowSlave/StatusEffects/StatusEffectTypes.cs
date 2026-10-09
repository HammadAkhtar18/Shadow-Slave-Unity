using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ShadowSlave.Tests")]

namespace ShadowSlave.StatusEffects
{
    /// <summary>
    /// Duration policy defining how a status effect expires or persists over time.
    /// Matches UE5 EStatusEffectDurationPolicy.
    /// </summary>
    public enum StatusEffectDurationPolicy : byte
    {
        Instant = 0,    // One-off application, not retained in active collection
        Timed = 1,      // Finite duration in seconds; expires via lifecycle timer
        Persistent = 2  // Remains active until explicitly removed by gameplay logic
    }

    /// <summary>
    /// Stacking policy defining behavior when an effect with the same definition is reapplied.
    /// Matches UE5 EStatusEffectStackingPolicy.
    /// </summary>
    public enum StatusEffectStackingPolicy : byte
    {
        IgnoreNew = 0,       // Keep existing instance; discard new application
        RefreshDuration = 1, // Reset remaining duration to full without changing stacks
        AddStacks = 2,       // Increment stack count up to MaxStacks and refresh duration
        Replace = 3          // Remove older instance and apply new instance
    }

    /// <summary>
    /// Generic polarity classification for status effects (informational and filtering metadata).
    /// Matches UE5 EStatusEffectPolarity.
    /// </summary>
    public enum StatusEffectPolarity : byte
    {
        Neutral = 0,
        Beneficial = 1,
        Harmful = 2
    }

    /// <summary>
    /// Attribution source identifying what caused or applied this status effect.
    /// Matches UE5 FShadowSlaveStatusEffectSource.
    /// </summary>
    [Serializable]
    public struct StatusEffectSource : IEquatable<StatusEffectSource>
    {
        [SerializeField] private string sourceIdString;
        [SerializeField] private string sourceName;
        [SerializeField] private GameObject sourceActor;

        private Guid _sourceId;

        public Guid SourceId
        {
            get
            {
                if (_sourceId == Guid.Empty && !string.IsNullOrEmpty(sourceIdString))
                {
                    Guid.TryParse(sourceIdString, out _sourceId);
                }
                return _sourceId;
            }
            set
            {
                _sourceId = value;
                sourceIdString = value.ToString("D");
            }
        }

        public string SourceName
        {
            get => sourceName ?? string.Empty;
            set => sourceName = value ?? string.Empty;
        }

        public GameObject SourceActor
        {
            get => sourceActor;
            set => sourceActor = value;
        }

        public StatusEffectSource(Guid sourceId, string sourceName = null, GameObject sourceActor = null)
        {
            _sourceId = sourceId;
            sourceIdString = sourceId != Guid.Empty ? sourceId.ToString("D") : string.Empty;
            this.sourceName = sourceName ?? string.Empty;
            this.sourceActor = sourceActor;
        }

        public bool Equals(StatusEffectSource other)
        {
            return SourceId == other.SourceId &&
                   string.Equals(SourceName, other.SourceName, StringComparison.Ordinal) &&
                   sourceActor == other.sourceActor;
        }

        public override bool Equals(object obj)
        {
            return obj is StatusEffectSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = SourceId.GetHashCode();
                hash = (hash * 397) ^ (SourceName != null ? StringComparer.Ordinal.GetHashCode(SourceName) : 0);
                hash = (hash * 397) ^ (sourceActor != null ? sourceActor.GetHashCode() : 0);
                return hash;
            }
        }

        public static bool operator ==(StatusEffectSource left, StatusEffectSource right) => left.Equals(right);
        public static bool operator !=(StatusEffectSource left, StatusEffectSource right) => !left.Equals(right);
    }

    /// <summary>
    /// Runtime instance of an active status effect on an actor.
    /// Holds mutable runtime state; static data (definition) is NOT duplicated.
    /// Matches UE5 FShadowSlaveStatusEffectInstance.
    /// </summary>
    public class StatusEffectInstance
    {
        private readonly Dictionary<string, string> _dynamicProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly ReadOnlyDictionary<string, string> _dynamicPropertiesReadOnly;

        public Guid InstanceId { get; }
        public StatusEffectDefinition EffectDefinition { get; }
        public int CurrentStacks { get; internal set; }
        public float TotalDuration { get; internal set; }
        public StatusEffectSource Source { get; }
        public IReadOnlyDictionary<string, string> DynamicProperties => _dynamicPropertiesReadOnly;

        // Authoritative runtime expiration state
        public float ExpirationTime { get; internal set; }
        public int Generation { get; internal set; }
        internal Coroutine TimerCoroutine { get; set; }

        public bool IsValid => InstanceId != Guid.Empty && EffectDefinition != null;

        public StatusEffectInstance(
            StatusEffectDefinition definition,
            StatusEffectSource source = default,
            IDictionary<string, string> dynamicProperties = null)
        {
            InstanceId = Guid.NewGuid();
            EffectDefinition = definition;
            CurrentStacks = 1;
            TotalDuration = 0f;
            Source = source;
            _dynamicPropertiesReadOnly = new ReadOnlyDictionary<string, string>(_dynamicProperties);

            if (dynamicProperties != null)
            {
                foreach (KeyValuePair<string, string> pair in dynamicProperties)
                {
                    if (!string.IsNullOrEmpty(pair.Key))
                    {
                        _dynamicProperties[pair.Key] = pair.Value ?? string.Empty;
                    }
                }
            }
        }

        internal void SetDynamicProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _dynamicProperties[key] = value ?? string.Empty;
        }

        internal string GetDynamicProperty(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return _dynamicProperties.TryGetValue(key, out string val) ? val : null;
        }
    }
}
