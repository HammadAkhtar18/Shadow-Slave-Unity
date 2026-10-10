using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ShadowSlave.Core;
using UnityEngine;

namespace ShadowSlave.Items
{
    /// <summary>
    /// Reusable component managing an actor's inventory slots, stacking, and capacity.
    /// Operates purely event-driven without tick overhead.
    /// Usable by player, companions, containers, and enemies.
    /// Protected by a reentrancy transition guard against recursive mutations.
    /// Matches UE5 UShadowSlaveInventoryComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryComponent : MonoBehaviour
    {
        [SerializeField, Tooltip("Maximum number of inventory slots.")]
        private int maxSlots = 20;

        private readonly List<ItemInstance> _slots = new List<ItemInstance>();
        private readonly ReadOnlyCollection<ItemInstance> _readOnlySlots;

        private bool _isProcessingInventoryTransition;

        /* --- Delegates --- */
        /// <summary>Invoked when an item stack is added or incremented. (ItemInstance, AddedQuantity)</summary>
        public event Action<ItemInstance, int> OnItemAdded;

        /// <summary>Invoked when an item stack is removed or decremented. (ItemInstance snapshot, RemovedQuantity)</summary>
        public event Action<ItemInstance, int> OnItemRemoved;

        /// <summary>Invoked when inventory contents or capacity change.</summary>
        public event Action OnInventoryChanged;

        public int MaxSlots => Mathf.Max(1, maxSlots);
        public int Capacity => MaxSlots;
        public IReadOnlyList<ItemInstance> Slots => _readOnlySlots;
        public int UsedSlotCount => _slots.Count;
        public int FreeSlotCount => Mathf.Max(0, MaxSlots - _slots.Count);
        public bool IsFull => _slots.Count >= MaxSlots;

        public InventoryComponent()
        {
            _readOnlySlots = _slots.AsReadOnly();
        }

        private void OnDestroy()
        {
            _slots.Clear();
        }

        /* --- Capacity API --- */

        public int GetCapacity() => MaxSlots;

        public void SetCapacity(int newCapacity)
        {
            if (_isProcessingInventoryTransition)
            {
                SSLog.Warning(SSLog.CategoryItems, "InventoryComponent.SetCapacity - Re-entrant call prevented.");
                return;
            }

            int clampedCapacity = Mathf.Max(1, newCapacity);
            if (maxSlots != clampedCapacity)
            {
                _isProcessingInventoryTransition = true;
                try
                {
                    maxSlots = clampedCapacity;
                    FireInventoryChanged();
                }
                finally
                {
                    _isProcessingInventoryTransition = false;
                }
            }
        }

        public int GetUsedSlotCount() => UsedSlotCount;
        public int GetFreeSlotCount() => FreeSlotCount;
        public bool IsInventoryFull() => IsFull;
        public IReadOnlyList<ItemInstance> GetSlots() => Slots;

        /* --- Inventory Operations --- */

        /// <summary>
        /// Adds items to the inventory, respecting stackability and capacity.
        /// Returns true if at least one item was added.
        /// OutRemainder outputs any items that could not fit due to capacity.
        /// </summary>
        public bool AddItem(ItemDefinition itemDef, int quantity, out int remainder)
        {
            if (itemDef == null || quantity <= 0)
            {
                remainder = Mathf.Max(0, quantity);
                return false;
            }

            if (_isProcessingInventoryTransition)
            {
                SSLog.Warning(SSLog.CategoryItems, "InventoryComponent.AddItem - Re-entrant call prevented.");
                remainder = quantity;
                return false;
            }

            _isProcessingInventoryTransition = true;
            try
            {
                int remainingToAdd = quantity;

                if (itemDef.IsStackable)
                {
                    int maxStack = itemDef.MaxStackSize;

                    // Phase 1: Try filling existing partially filled stacks (matching itemDef and no custom dynamic properties)
                    for (int i = 0; i < _slots.Count; i++)
                    {
                        ItemInstance slot = _slots[i];
                        if (slot.ItemDefinition == itemDef && slot.DynamicProperties.Count == 0 && slot.Quantity < maxStack)
                        {
                            int spaceAvailable = maxStack - slot.Quantity;
                            int amountToFill = Mathf.Min(remainingToAdd, spaceAvailable);

                            slot.Quantity += amountToFill;
                            remainingToAdd -= amountToFill;

                            FireItemAdded(slot, amountToFill);

                            if (remainingToAdd <= 0)
                            {
                                break;
                            }
                        }
                    }

                    // Phase 2: Create new stacks in empty slots as long as capacity permits
                    while (remainingToAdd > 0 && _slots.Count < MaxSlots)
                    {
                        int amountInNewSlot = Mathf.Min(remainingToAdd, maxStack);
                        ItemInstance newInstance = new ItemInstance(itemDef, amountInNewSlot);

                        _slots.Add(newInstance);
                        remainingToAdd -= amountInNewSlot;

                        FireItemAdded(newInstance, amountInNewSlot);
                    }
                }
                else
                {
                    // Non-stackable item: each individual unit requires its own slot
                    while (remainingToAdd > 0 && _slots.Count < MaxSlots)
                    {
                        ItemInstance newInstance = new ItemInstance(itemDef, 1);

                        _slots.Add(newInstance);
                        remainingToAdd -= 1;

                        FireItemAdded(newInstance, 1);
                    }
                }

                remainder = remainingToAdd;
                int totalAdded = quantity - remainingToAdd;

                if (totalAdded > 0)
                {
                    FireInventoryChanged();
                    return true;
                }

                return false;
            }
            finally
            {
                _isProcessingInventoryTransition = false;
            }
        }

        /// <summary>
        /// Adds a specific item instance into the inventory, respecting stackability and capacity.
        /// Preserves dynamic properties.
        /// </summary>
        public bool AddItemInstance(ItemInstance instance, out int remainder)
        {
            if (instance == null || !instance.IsValid)
            {
                remainder = instance != null ? Mathf.Max(0, instance.Quantity) : 0;
                return false;
            }

            if (_isProcessingInventoryTransition)
            {
                SSLog.Warning(SSLog.CategoryItems, "InventoryComponent.AddItemInstance - Re-entrant call prevented.");
                remainder = instance.Quantity;
                return false;
            }

            _isProcessingInventoryTransition = true;
            try
            {
                int remainingToAdd = instance.Quantity;
                ItemDefinition itemDef = instance.ItemDefinition;

                if (itemDef.IsStackable)
                {
                    int maxStack = itemDef.MaxStackSize;

                    // Phase 1: Try filling existing partially filled stacks that can stack with this instance
                    for (int i = 0; i < _slots.Count; i++)
                    {
                        ItemInstance slot = _slots[i];
                        if (slot.CanStackWith(instance) && slot.Quantity < maxStack)
                        {
                            int spaceAvailable = maxStack - slot.Quantity;
                            int amountToFill = Mathf.Min(remainingToAdd, spaceAvailable);

                            slot.Quantity += amountToFill;
                            remainingToAdd -= amountToFill;

                            FireItemAdded(slot, amountToFill);

                            if (remainingToAdd <= 0)
                            {
                                break;
                            }
                        }
                    }

                    // Phase 2: Create new stacks as long as capacity permits
                    while (remainingToAdd > 0 && _slots.Count < MaxSlots)
                    {
                        int amountInNewSlot = Mathf.Min(remainingToAdd, maxStack);
                        ItemInstance newInstance = new ItemInstance(
                            itemDef,
                            amountInNewSlot,
                            remainingToAdd == instance.Quantity ? instance.InstanceId : Guid.NewGuid(),
                            instance.DynamicProperties
                        );

                        _slots.Add(newInstance);
                        remainingToAdd -= amountInNewSlot;

                        FireItemAdded(newInstance, amountInNewSlot);
                    }
                }
                else
                {
                    // Non-stackable: add instance directly if slot available
                    while (remainingToAdd > 0 && _slots.Count < MaxSlots)
                    {
                        ItemInstance newInstance = remainingToAdd == 1 && instance.Quantity == 1
                            ? instance
                            : new ItemInstance(itemDef, 1, Guid.NewGuid(), instance.DynamicProperties);

                        _slots.Add(newInstance);
                        remainingToAdd -= 1;

                        FireItemAdded(newInstance, 1);
                    }
                }

                remainder = remainingToAdd;
                int totalAdded = instance.Quantity - remainingToAdd;

                if (totalAdded > 0)
                {
                    FireInventoryChanged();
                    return true;
                }

                return false;
            }
            finally
            {
                _isProcessingInventoryTransition = false;
            }
        }

        /// <summary>
        /// Convenience wrapper without remainder parameter.
        /// </summary>
        public bool AddItemSimple(ItemDefinition itemDef, int quantity = 1)
        {
            return AddItem(itemDef, quantity, out _);
        }

        /// <summary>
        /// Removes Quantity of ItemDef from the inventory.
        /// Returns true if requested quantity was completely removed.
        /// If inventory contains fewer items than Quantity, operation fails safely with zero removal (atomic).
        /// </summary>
        public bool RemoveItem(ItemDefinition itemDef, int quantity = 1)
        {
            if (itemDef == null || quantity <= 0)
            {
                return false;
            }

            if (_isProcessingInventoryTransition)
            {
                SSLog.Warning(SSLog.CategoryItems, "InventoryComponent.RemoveItem - Re-entrant call prevented.");
                return false;
            }

            _isProcessingInventoryTransition = true;
            try
            {
                if (GetTotalItemCount(itemDef) < quantity)
                {
                    return false;
                }

                int remainingToRemove = quantity;

                for (int i = _slots.Count - 1; i >= 0; i--)
                {
                    ItemInstance slot = _slots[i];
                    if (slot.ItemDefinition == itemDef)
                    {
                        int amountFromSlot = Mathf.Min(remainingToRemove, slot.Quantity);
                        slot.Quantity -= amountFromSlot;
                        remainingToRemove -= amountFromSlot;

                        ItemInstance removedSnapshot = new ItemInstance(
                            slot.ItemDefinition,
                            amountFromSlot,
                            slot.InstanceId,
                            slot.DynamicProperties
                        );

                        if (slot.Quantity <= 0)
                        {
                            _slots.RemoveAt(i);
                        }

                        FireItemRemoved(removedSnapshot, amountFromSlot);

                        if (remainingToRemove <= 0)
                        {
                            break;
                        }
                    }
                }

                FireInventoryChanged();
                return true;
            }
            finally
            {
                _isProcessingInventoryTransition = false;
            }
        }

        /// <summary>
        /// Removes Quantity from a specific item instance stack by GUID.
        /// Returns true if successfully removed.
        /// </summary>
        public bool RemoveItemByInstanceId(Guid instanceId, int quantity = 1)
        {
            if (instanceId == Guid.Empty || quantity <= 0)
            {
                return false;
            }

            if (_isProcessingInventoryTransition)
            {
                SSLog.Warning(SSLog.CategoryItems, "InventoryComponent.RemoveItemByInstanceId - Re-entrant call prevented.");
                return false;
            }

            _isProcessingInventoryTransition = true;
            try
            {
                for (int i = 0; i < _slots.Count; i++)
                {
                    ItemInstance slot = _slots[i];
                    if (slot.InstanceId == instanceId)
                    {
                        if (slot.Quantity < quantity)
                        {
                            return false;
                        }

                        slot.Quantity -= quantity;

                        ItemInstance removedSnapshot = new ItemInstance(
                            slot.ItemDefinition,
                            quantity,
                            slot.InstanceId,
                            slot.DynamicProperties
                        );

                        if (slot.Quantity <= 0)
                        {
                            _slots.RemoveAt(i);
                        }

                        FireItemRemoved(removedSnapshot, quantity);
                        FireInventoryChanged();
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                _isProcessingInventoryTransition = false;
            }
        }

        /* --- Query API --- */

        public bool HasItem(ItemDefinition itemDef, int quantity = 1)
        {
            if (itemDef == null || quantity <= 0)
            {
                return false;
            }

            return GetTotalItemCount(itemDef) >= quantity;
        }

        public bool FindItem(ItemDefinition itemDef, out ItemInstance outInstance)
        {
            outInstance = null;
            if (itemDef == null)
            {
                return false;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ItemDefinition == itemDef)
                {
                    outInstance = _slots[i];
                    return true;
                }
            }

            return false;
        }

        public bool FindItemByInstanceId(Guid instanceId, out ItemInstance outInstance)
        {
            outInstance = null;
            if (instanceId == Guid.Empty)
            {
                return false;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].InstanceId == instanceId)
                {
                    outInstance = _slots[i];
                    return true;
                }
            }

            return false;
        }

        public bool HasItemByInstanceId(Guid instanceId)
        {
            if (instanceId == Guid.Empty)
            {
                return false;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].InstanceId == instanceId)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetTotalItemCount(ItemDefinition itemDef)
        {
            if (itemDef == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ItemDefinition == itemDef)
                {
                    total += _slots[i].Quantity;
                }
            }

            return total;
        }

        public List<ItemInstance> GetItemsByType(ShadowSlaveItemType itemType)
        {
            List<ItemInstance> filtered = new List<ItemInstance>();
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ItemDefinition != null && _slots[i].ItemDefinition.ItemType == itemType)
                {
                    filtered.Add(_slots[i]);
                }
            }
            return filtered;
        }

        /* --- Clear & Persistence Restoration API --- */

        public void ClearInventory()
        {
            if (_slots.Count == 0 || _isProcessingInventoryTransition)
            {
                return;
            }

            _isProcessingInventoryTransition = true;
            try
            {
                _slots.Clear();
                FireInventoryChanged();
            }
            finally
            {
                _isProcessingInventoryTransition = false;
            }
        }

        /// <summary>
        /// Dedicated persistence restoration API: replaces current inventory slots with saved instances.
        /// Preserves original instance GUIDs, quantities, and dynamic properties without triggering gameplay acquisition rules.
        /// </summary>
        public void RestoreInventory(IEnumerable<ItemInstance> inInstances, int inCapacity)
        {
            if (_isProcessingInventoryTransition)
            {
                SSLog.Warning(SSLog.CategoryItems, "InventoryComponent.RestoreInventory - Re-entrant call prevented.");
                return;
            }

            _isProcessingInventoryTransition = true;
            try
            {
                maxSlots = Mathf.Max(1, inCapacity);
                _slots.Clear();

                if (inInstances != null)
                {
                    foreach (ItemInstance item in inInstances)
                    {
                        if (item != null && item.IsValid)
                        {
                            _slots.Add(item);
                        }
                    }
                }

                FireInventoryChanged();
            }
            finally
            {
                _isProcessingInventoryTransition = false;
            }
        }

        /* --- Debug API --- */

        public void LogInventoryContents()
        {
            string ownerName = gameObject != null ? gameObject.name : "None";
            SSLog.LogItems($"[{ownerName}] Inventory ({_slots.Count}/{MaxSlots} slots occupied):");

            if (_slots.Count == 0)
            {
                SSLog.LogItems("  (Empty)");
                return;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                ItemInstance slot = _slots[i];
                string itemName = slot.ItemDefinition != null ? slot.ItemDefinition.DisplayName : "Invalid";
                string stackable = (slot.ItemDefinition != null && slot.ItemDefinition.IsStackable) ? "Stackable" : "Single";

                SSLog.LogItems($"  Slot [{i}]: {itemName} x{slot.Quantity} ({stackable}, GUID: {slot.InstanceId})");
            }
        }

        /* --- Event Helper Invocations (Exception-Safe) --- */

        private void FireItemAdded(ItemInstance instance, int addedQuantity)
        {
            try
            {
                OnItemAdded?.Invoke(instance, addedQuantity);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryItems, $"Exception in OnItemAdded subscriber: {ex}");
            }
        }

        private void FireItemRemoved(ItemInstance instance, int removedQuantity)
        {
            try
            {
                OnItemRemoved?.Invoke(instance, removedQuantity);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryItems, $"Exception in OnItemRemoved subscriber: {ex}");
            }
        }

        private void FireInventoryChanged()
        {
            try
            {
                OnInventoryChanged?.Invoke();
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryItems, $"Exception in OnInventoryChanged subscriber: {ex}");
            }
        }
    }
}
