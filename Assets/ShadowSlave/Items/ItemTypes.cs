using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("ShadowSlave.Tests")]

namespace ShadowSlave.Items
{
    /// <summary>
    /// Generic item classification.
    /// Matches UE5 EShadowSlaveItemType.
    /// </summary>
    public enum ShadowSlaveItemType : byte
    {
        Miscellaneous = 0,
        Consumable = 1,
        Equipment = 2,
        Memory = 3,
        Quest = 4,
        Material = 5
    }

    /// <summary>
    /// Generic equipment slot classification.
    /// Matches UE5 EShadowSlaveEquipmentSlot.
    /// </summary>
    public enum ShadowSlaveEquipmentSlot : byte
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
        Charm = 3,
        Ring = 4,
        Relic = 5
    }

    /// <summary>
    /// Represents an individual item instance owned by an inventory or actor.
    /// Cleanly separates immutable item definition data (what the item is)
    /// from mutable instance-specific runtime data (quantity, unique GUID, dynamic properties).
    /// Matches UE5 FShadowSlaveItemInstance.
    /// </summary>
    public class ItemInstance
    {
        private readonly Dictionary<string, string> _dynamicProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly ReadOnlyDictionary<string, string> _dynamicPropertiesReadOnly;

        public Guid InstanceId { get; }
        public ItemDefinition ItemDefinition { get; }
        public int Quantity { get; internal set; }
        public IReadOnlyDictionary<string, string> DynamicProperties => _dynamicPropertiesReadOnly;

        public bool IsValid => ItemDefinition != null && Quantity > 0;

        public ItemInstance(
            ItemDefinition definition,
            int quantity = 1,
            Guid instanceId = default,
            IDictionary<string, string> dynamicProperties = null)
        {
            InstanceId = instanceId != Guid.Empty ? instanceId : Guid.NewGuid();
            ItemDefinition = definition;
            Quantity = Mathf.Max(1, quantity);
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

        /// <summary>
        /// Checks whether this item instance can combine into a stack with another.
        /// Matches UE5 FShadowSlaveItemInstance::CanStackWith.
        /// </summary>
        public bool CanStackWith(ItemInstance other)
        {
            if (other == null || !IsValid || !other.IsValid)
            {
                return false;
            }

            if (ItemDefinition != other.ItemDefinition)
            {
                return false;
            }

            if (!ItemDefinition.IsStackable)
            {
                return false;
            }

            // Must match dynamic instance properties to safely stack
            if (_dynamicProperties.Count != other._dynamicProperties.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> pair in _dynamicProperties)
            {
                if (!other._dynamicProperties.TryGetValue(pair.Key, out string otherVal) ||
                    !string.Equals(pair.Value, otherVal, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public void SetDynamicProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _dynamicProperties[key] = value ?? string.Empty;
        }

        public string GetDynamicProperty(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return _dynamicProperties.TryGetValue(key, out string val) ? val : null;
        }
    }
}
