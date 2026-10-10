using System;
using System.Runtime.CompilerServices;
using ShadowSlave.Items;
using UnityEngine;

[assembly: InternalsVisibleTo("ShadowSlave.Tests")]

namespace ShadowSlave.Equipment
{
    /// <summary>
    /// Origin source classification for an equipped object.
    /// Matches UE5 EShadowSlaveEquipmentSourceType.
    /// </summary>
    public enum EquipmentSourceType : byte
    {
        None = 0,
        Item = 1,
        Memory = 2
    }

    /// <summary>
    /// Lightweight runtime descriptor representing an equipped slot binding.
    /// References an authoritative instance (Inventory Item or Memory) by its unique GUID.
    /// Does not duplicate or create independent copies of item/memory state.
    /// Matches UE5 FShadowSlaveEquippedItem.
    /// </summary>
    [Serializable]
    public struct EquippedItem : IEquatable<EquippedItem>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private ShadowSlaveEquipmentSlot slot;

        [SerializeField]
        private EquipmentSourceType sourceType;

        [SerializeField]
        private string instanceIdString;

        [SerializeField]
        private string definitionId;

        private Guid _cachedGuid;

        /// <summary>The equipment slot occupied by this item.</summary>
        public ShadowSlaveEquipmentSlot Slot => slot;

        /// <summary>Source origin: Inventory Item or Memory.</summary>
        public EquipmentSourceType SourceType => sourceType;

        /// <summary>Unique stable instance GUID of the equipped item or Memory.</summary>
        public Guid InstanceId => _cachedGuid;

        /// <summary>Definition ID or asset name for debugging / inspection.</summary>
        public string DefinitionId => definitionId ?? string.Empty;

        /// <summary>True if this descriptor references a valid occupied slot, source, and instance GUID.</summary>
        public bool IsValid => slot != ShadowSlaveEquipmentSlot.None &&
                               sourceType != EquipmentSourceType.None &&
                               _cachedGuid != Guid.Empty;

        public EquippedItem(
            ShadowSlaveEquipmentSlot slot,
            EquipmentSourceType sourceType,
            Guid instanceId,
            string definitionId = "")
        {
            this.slot = slot;
            this.sourceType = sourceType;
            this._cachedGuid = instanceId;
            this.instanceIdString = instanceId != Guid.Empty ? instanceId.ToString() : string.Empty;
            this.definitionId = definitionId ?? string.Empty;
        }

        /// <summary>
        /// Clears all fields to default empty state.
        /// </summary>
        public void Reset()
        {
            slot = ShadowSlaveEquipmentSlot.None;
            sourceType = EquipmentSourceType.None;
            _cachedGuid = Guid.Empty;
            instanceIdString = string.Empty;
            definitionId = string.Empty;
        }

        public void OnBeforeSerialize()
        {
            if (_cachedGuid != Guid.Empty)
            {
                instanceIdString = _cachedGuid.ToString();
            }
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(instanceIdString) && Guid.TryParse(instanceIdString, out var parsed))
            {
                _cachedGuid = parsed;
            }
            else
            {
                _cachedGuid = Guid.Empty;
            }
        }

        public bool Equals(EquippedItem other)
        {
            return slot == other.slot &&
                   sourceType == other.sourceType &&
                   InstanceId.Equals(other.InstanceId) &&
                   string.Equals(definitionId, other.definitionId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is EquippedItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)slot;
                hash = (hash * 397) ^ (int)sourceType;
                hash = (hash * 397) ^ InstanceId.GetHashCode();
                hash = (hash * 397) ^ (definitionId != null ? StringComparer.Ordinal.GetHashCode(definitionId) : 0);
                return hash;
            }
        }

        public static bool operator ==(EquippedItem left, EquippedItem right) => left.Equals(right);
        public static bool operator !=(EquippedItem left, EquippedItem right) => !left.Equals(right);

        public override string ToString()
        {
            return $"EquippedItem({slot}, {sourceType}, {InstanceId}, '{DefinitionId}')";
        }
    }
}
