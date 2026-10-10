using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ShadowSlave.Attributes;
using ShadowSlave.Core;
using ShadowSlave.Items;
using UnityEngine;

namespace ShadowSlave.Equipment
{
    /// <summary>
    /// Reusable component managing an actor's equipped items and Memories across equipment slots.
    /// Coordinates between InventoryComponent and AttributeComponent.
    /// Operates purely event-driven without tick or Update() overhead.
    /// Usable by player, companions, and enemies.
    /// Protected by a reentrancy transition guard against recursive mutations.
    /// Matches UE5 UShadowSlaveEquipmentComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public class EquipmentComponent : MonoBehaviour
    {
        private readonly Dictionary<ShadowSlaveEquipmentSlot, EquippedItem> _equippedSlots =
            new Dictionary<ShadowSlaveEquipmentSlot, EquippedItem>();

        private readonly ReadOnlyDictionary<ShadowSlaveEquipmentSlot, EquippedItem> _readOnlyEquippedSlots;

        private bool _isProcessingEquipmentTransition;

        /* --- Delegates / Events --- */

        /// <summary>Invoked when an item or Memory is equipped in a slot. (Slot, EquippedItem)</summary>
        public event Action<ShadowSlaveEquipmentSlot, EquippedItem> OnEquipmentItemEquipped;

        /// <summary>Invoked when an item or Memory is unequipped from a slot. (Slot, UnequippedItem)</summary>
        public event Action<ShadowSlaveEquipmentSlot, EquippedItem> OnEquipmentItemUnequipped;

        /// <summary>Invoked when a specific equipment slot changes occupancy. (Slot)</summary>
        public event Action<ShadowSlaveEquipmentSlot> OnEquipmentSlotChanged;

        /// <summary>Invoked when any equipment change occurs.</summary>
        public event Action OnEquipmentChanged;

        /* --- Public Accessors --- */

        /// <summary>Read-only view of currently occupied equipment slots and their descriptors.</summary>
        public IReadOnlyDictionary<ShadowSlaveEquipmentSlot, EquippedItem> EquippedSlots => _readOnlyEquippedSlots;

        /// <summary>Total number of currently equipped slots.</summary>
        public int EquippedCount => _equippedSlots.Count;

        public EquipmentComponent()
        {
            _readOnlyEquippedSlots = new ReadOnlyDictionary<ShadowSlaveEquipmentSlot, EquippedItem>(_equippedSlots);
        }

        /* --- Unity Lifecycle --- */

        private void OnEnable()
        {
            BindToCompanionComponents();
            HandleInventoryChanged();
        }

        private void OnDisable()
        {
            UnbindFromCompanionComponents();
        }

        private void OnDestroy()
        {
            UnbindFromCompanionComponents();
            // Remove all equipment-derived attribute modifiers before destruction
            foreach (var pair in _equippedSlots)
            {
                RemoveModifiersForSource(pair.Value.InstanceId);
            }
            _equippedSlots.Clear();
        }

        /* --- Companion Component Resolution --- */

        /// <summary>
        /// Retrieves the AttributeComponent on the same owner GameObject, if present.
        /// </summary>
        public AttributeComponent GetAttributeComponent()
        {
            return GetComponent<AttributeComponent>();
        }

        /// <summary>
        /// Retrieves the InventoryComponent on the same owner GameObject, if present.
        /// </summary>
        public InventoryComponent GetInventoryComponent()
        {
            return GetComponent<InventoryComponent>();
        }

        private void BindToCompanionComponents()
        {
            var invComp = GetInventoryComponent();
            if (invComp != null)
            {
                invComp.OnItemRemoved -= HandleInventoryItemRemoved;
                invComp.OnItemRemoved += HandleInventoryItemRemoved;
                invComp.OnInventoryChanged -= HandleInventoryChanged;
                invComp.OnInventoryChanged += HandleInventoryChanged;
            }
        }

        private void UnbindFromCompanionComponents()
        {
            var invComp = GetInventoryComponent();
            if (invComp != null)
            {
                invComp.OnItemRemoved -= HandleInventoryItemRemoved;
                invComp.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        /* --- Equipment Operations --- */

        /// <summary>
        /// Equips an inventory-backed item by its unique instance GUID.
        /// If slot is None, attempts to use the item definition's designated EquipmentSlot.
        /// Returns true if successfully equipped.
        /// </summary>
        public bool EquipItem(Guid instanceId, ShadowSlaveEquipmentSlot slot = ShadowSlaveEquipmentSlot.None)
        {
            if (instanceId == Guid.Empty)
            {
                return false;
            }

            if (_isProcessingEquipmentTransition)
            {
                SSLog.Warning(SSLog.CategoryEquipment, "EquipmentComponent.EquipItem - Reentrant transition rejected.");
                return false;
            }

            var invComp = GetInventoryComponent();
            if (invComp == null)
            {
                SSLog.Warning(SSLog.CategoryEquipment, $"EquipmentComponent.EquipItem: Owner '{name}' has no InventoryComponent.");
                return false;
            }

            if (!invComp.FindItemByInstanceId(instanceId, out var itemInstance) || itemInstance == null || !itemInstance.IsValid)
            {
                SSLog.Warning(SSLog.CategoryEquipment, $"EquipmentComponent.EquipItem: InstanceId '{instanceId}' not found in inventory.");
                return false;
            }

            if (itemInstance.ItemDefinition == null)
            {
                return false;
            }

            var targetSlot = (slot != ShadowSlaveEquipmentSlot.None)
                ? slot
                : itemInstance.ItemDefinition.EquipmentSlot;

            if (targetSlot == ShadowSlaveEquipmentSlot.None)
            {
                SSLog.Warning(SSLog.CategoryEquipment, $"EquipmentComponent.EquipItem: Item '{itemInstance.ItemDefinition.DisplayName}' has no valid equipment slot defined or specified.");
                return false;
            }

            // Validate attribute component availability if item grants modifiers
            var modifiers = itemInstance.ItemDefinition.GrantedModifiers;
            if (modifiers != null && modifiers.Count > 0 && GetAttributeComponent() == null)
            {
                SSLog.Warning(SSLog.CategoryEquipment, $"EquipmentComponent.EquipItem: Item '{itemInstance.ItemDefinition.DisplayName}' grants modifiers but owner has no AttributeComponent.");
                return false;
            }

            // Idempotency check: already equipped in this exact slot
            if (_equippedSlots.TryGetValue(targetSlot, out var existingInTarget))
            {
                if (existingInTarget.InstanceId == instanceId && existingInTarget.SourceType == EquipmentSourceType.Item)
                {
                    return true;
                }
            }

            _isProcessingEquipmentTransition = true;
            try
            {
                // Snapshot existing state for atomic transition & potential rollback
                var existingSlot = GetSlotForInstance(instanceId);
                bool hasExistingSlot = (existingSlot != ShadowSlaveEquipmentSlot.None && existingSlot != targetSlot);
                EquippedItem existingSlotOccupant = default;
                if (hasExistingSlot)
                {
                    existingSlotOccupant = _equippedSlots[existingSlot];
                }

                bool targetSlotOccupied = _equippedSlots.TryGetValue(targetSlot, out var oldTargetOccupant);

                // 1. Establish new equipment modifiers
                if (!ApplyModifiersForSource(instanceId, modifiers))
                {
                    RemoveModifiersForSource(instanceId);
                    return false;
                }

                // 2. If instance was equipped in another slot, vacate it
                if (hasExistingSlot)
                {
                    _equippedSlots.Remove(existingSlot);
                    FireItemUnequipped(existingSlot, existingSlotOccupant);
                    FireSlotChanged(existingSlot);
                }

                // 3. If target slot was occupied by a different item, unequip old occupant
                if (targetSlotOccupied && oldTargetOccupant.InstanceId != instanceId)
                {
                    RemoveModifiersForSource(oldTargetOccupant.InstanceId);
                    FireItemUnequipped(targetSlot, oldTargetOccupant);
                }

                // 4. Commit new equipped descriptor
                string defId = !string.IsNullOrEmpty(itemInstance.ItemDefinition.ItemId)
                    ? itemInstance.ItemDefinition.ItemId
                    : itemInstance.ItemDefinition.name;

                var newEquipped = new EquippedItem(targetSlot, EquipmentSourceType.Item, instanceId, defId);
                _equippedSlots[targetSlot] = newEquipped;

                // 5. Dispatch success notifications
                FireItemEquipped(targetSlot, newEquipped);
                FireSlotChanged(targetSlot);
                FireEquipmentChanged();

                SSLog.LogEquipment($"Equipped Item '{defId}' in slot '{targetSlot}' on '{name}'.");
                return true;
            }
            finally
            {
                _isProcessingEquipmentTransition = false;
            }
        }

        /// <summary>
        /// Convenience helper to equip an ItemInstance directly.
        /// </summary>
        public bool EquipItemInstance(ItemInstance itemInstance, ShadowSlaveEquipmentSlot slot = ShadowSlaveEquipmentSlot.None)
        {
            if (itemInstance == null || !itemInstance.IsValid)
            {
                return false;
            }

            return EquipItem(itemInstance.InstanceId, slot);
        }

        /// <summary>
        /// Unequips whatever is currently occupying the specified equipment slot.
        /// Removes associated attribute modifiers and clears slot state.
        /// Returns true if an item was equipped and unequipped.
        /// </summary>
        public bool UnequipSlot(ShadowSlaveEquipmentSlot slot)
        {
            if (slot == ShadowSlaveEquipmentSlot.None)
            {
                return false;
            }

            if (_isProcessingEquipmentTransition)
            {
                SSLog.Warning(SSLog.CategoryEquipment, "EquipmentComponent.UnequipSlot - Reentrant transition rejected.");
                return false;
            }

            if (!_equippedSlots.TryGetValue(slot, out var removedItem))
            {
                return false;
            }

            _isProcessingEquipmentTransition = true;
            try
            {
                _equippedSlots.Remove(slot);

                // Remove all attribute modifiers originating from this instance
                RemoveModifiersForSource(removedItem.InstanceId);

                // Dispatch event notifications
                FireItemUnequipped(slot, removedItem);
                FireSlotChanged(slot);
                FireEquipmentChanged();

                SSLog.LogEquipment($"Unequipped slot '{slot}' on '{name}'.");
                return true;
            }
            finally
            {
                _isProcessingEquipmentTransition = false;
            }
        }

        /// <summary>
        /// Unequips the specified instance by its unique GUID regardless of which slot it occupies.
        /// Returns true if found and unequipped.
        /// </summary>
        public bool UnequipInstance(Guid instanceId)
        {
            if (instanceId == Guid.Empty)
            {
                return false;
            }

            var slot = GetSlotForInstance(instanceId);
            if (slot != ShadowSlaveEquipmentSlot.None)
            {
                return UnequipSlot(slot);
            }

            // Ensure any orphaned modifiers with this SourceId are also removed
            RemoveModifiersForSource(instanceId);
            return false;
        }

        /// <summary>
        /// Unequips all currently equipped slots and strips all equipment-derived modifiers.
        /// </summary>
        public void UnequipAll()
        {
            if (_equippedSlots.Count == 0)
            {
                return;
            }

            if (_isProcessingEquipmentTransition)
            {
                SSLog.Warning(SSLog.CategoryEquipment, "EquipmentComponent.UnequipAll - Reentrant transition rejected.");
                return;
            }

            var occupiedSlots = new List<ShadowSlaveEquipmentSlot>(_equippedSlots.Keys);
            for (int i = 0; i < occupiedSlots.Count; i++)
            {
                UnequipSlot(occupiedSlots[i]);
            }
        }

        /* --- Queries --- */

        /// <summary>Returns true if the specified equipment slot is currently occupied.</summary>
        public bool IsSlotOccupied(ShadowSlaveEquipmentSlot slot)
        {
            if (slot == ShadowSlaveEquipmentSlot.None)
            {
                return false;
            }

            return _equippedSlots.ContainsKey(slot);
        }

        /// <summary>Retrieves the descriptor for the item equipped in the given slot.</summary>
        public bool GetEquippedItemInSlot(ShadowSlaveEquipmentSlot slot, out EquippedItem outEquippedItem)
        {
            if (slot != ShadowSlaveEquipmentSlot.None && _equippedSlots.TryGetValue(slot, out outEquippedItem))
            {
                return true;
            }

            outEquippedItem = default;
            return false;
        }

        /// <summary>Returns true if the given instance GUID is currently equipped in any slot.</summary>
        public bool IsInstanceEquipped(Guid instanceId)
        {
            return GetSlotForInstance(instanceId) != ShadowSlaveEquipmentSlot.None;
        }

        /// <summary>Returns the equipment slot occupied by the given instance GUID, or None if not equipped.</summary>
        public ShadowSlaveEquipmentSlot GetSlotForInstance(Guid instanceId)
        {
            if (instanceId == Guid.Empty)
            {
                return ShadowSlaveEquipmentSlot.None;
            }

            foreach (var pair in _equippedSlots)
            {
                if (pair.Value.InstanceId == instanceId)
                {
                    return pair.Key;
                }
            }

            return ShadowSlaveEquipmentSlot.None;
        }

        /// <summary>Returns a snapshot list of all currently equipped item descriptors.</summary>
        public IReadOnlyList<EquippedItem> GetAllEquippedItems()
        {
            if (_equippedSlots.Count == 0)
            {
                return Array.Empty<EquippedItem>();
            }

            var result = new EquippedItem[_equippedSlots.Count];
            int i = 0;
            foreach (var pair in _equippedSlots)
            {
                result[i++] = pair.Value;
            }
            return result;
        }

        /// <summary>Retrieves the underlying Inventory Item instance for an equipped slot, if backed by inventory.</summary>
        public bool GetEquippedItemInstance(ShadowSlaveEquipmentSlot slot, out ItemInstance outInstance)
        {
            outInstance = null;
            if (!GetEquippedItemInSlot(slot, out var equipped) || equipped.SourceType != EquipmentSourceType.Item)
            {
                return false;
            }

            var invComp = GetInventoryComponent();
            if (invComp != null)
            {
                return invComp.FindItemByInstanceId(equipped.InstanceId, out outInstance);
            }

            return false;
        }

        /* --- Attribute Modifier Lifecycle --- */

        private bool ApplyModifiersForSource(Guid sourceId, IReadOnlyList<AttributeModifier> modifiers)
        {
            if (sourceId == Guid.Empty)
            {
                return false;
            }

            if (modifiers == null || modifiers.Count == 0)
            {
                return true;
            }

            var attrComp = GetAttributeComponent();
            if (attrComp == null)
            {
                SSLog.Warning(SSLog.CategoryEquipment, $"EquipmentComponent.ApplyModifiersForSource failed: Owner '{name}' has no AttributeComponent.");
                return false;
            }

            // Remove any existing modifiers with this SourceId to prevent duplicates
            attrComp.RemoveModifiersFromSourceId(sourceId);

            try
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var baseMod = modifiers[i];
                    string baseName = !string.IsNullOrEmpty(baseMod.ModifierId) ? baseMod.ModifierId : "EquipMod";
                    string generatedId = $"{baseName}_{sourceId:N}_{i}";

                    var appliedMod = new AttributeModifier(
                        generatedId,
                        baseMod.TargetAttribute,
                        baseMod.ModifierType,
                        baseMod.Value,
                        baseMod.Duration,
                        sourceId
                    );

                    attrComp.AddModifier(appliedMod);
                }

                return true;
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryEquipment, $"Exception while applying modifiers for source {sourceId}: {ex}");
                // Rollback any modifiers applied for this source
                attrComp.RemoveModifiersFromSourceId(sourceId);
                return false;
            }
        }

        private void RemoveModifiersForSource(Guid sourceId)
        {
            if (sourceId == Guid.Empty)
            {
                return;
            }

            var attrComp = GetAttributeComponent();
            if (attrComp != null)
            {
                attrComp.RemoveModifiersFromSourceId(sourceId);
            }
        }

        /* --- Inventory Event Handlers --- */

        private void HandleInventoryItemRemoved(ItemInstance itemInstance, int quantityRemoved)
        {
            if (itemInstance == null || itemInstance.InstanceId == Guid.Empty)
            {
                return;
            }

            if (IsInstanceEquipped(itemInstance.InstanceId))
            {
                // Check if the item stack was fully depleted or remains in inventory
                var invComp = GetInventoryComponent();
                if (invComp != null)
                {
                    if (!invComp.FindItemByInstanceId(itemInstance.InstanceId, out var foundInstance) || foundInstance.Quantity <= 0)
                    {
                        UnequipInstance(itemInstance.InstanceId);
                    }
                }
                else
                {
                    UnequipInstance(itemInstance.InstanceId);
                }
            }
        }

        /// <summary>
        /// Reconciles equipped slot state against authoritative inventory contents.
        /// Unequips any item whose instance GUID no longer exists in inventory.
        /// </summary>
        public void HandleInventoryChanged()
        {
            var invComp = GetInventoryComponent();
            if (invComp == null)
            {
                return;
            }

            List<ShadowSlaveEquipmentSlot> slotsToUnequip = null;
            foreach (var pair in _equippedSlots)
            {
                if (pair.Value.SourceType == EquipmentSourceType.Item)
                {
                    if (!invComp.HasItemByInstanceId(pair.Value.InstanceId))
                    {
                        if (slotsToUnequip == null)
                        {
                            slotsToUnequip = new List<ShadowSlaveEquipmentSlot>();
                        }
                        slotsToUnequip.Add(pair.Key);
                    }
                }
            }

            if (slotsToUnequip != null)
            {
                for (int i = 0; i < slotsToUnequip.Count; i++)
                {
                    UnequipSlot(slotsToUnequip[i]);
                }
            }
        }

        /* --- Event Helper Invocations (Exception-Safe) --- */

        private void FireItemEquipped(ShadowSlaveEquipmentSlot slot, EquippedItem equipped)
        {
            try
            {
                OnEquipmentItemEquipped?.Invoke(slot, equipped);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryEquipment, $"Exception in OnEquipmentItemEquipped subscriber: {ex}");
            }
        }

        private void FireItemUnequipped(ShadowSlaveEquipmentSlot slot, EquippedItem unequipped)
        {
            try
            {
                OnEquipmentItemUnequipped?.Invoke(slot, unequipped);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryEquipment, $"Exception in OnEquipmentItemUnequipped subscriber: {ex}");
            }
        }

        private void FireSlotChanged(ShadowSlaveEquipmentSlot slot)
        {
            try
            {
                OnEquipmentSlotChanged?.Invoke(slot);
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryEquipment, $"Exception in OnEquipmentSlotChanged subscriber: {ex}");
            }
        }

        private void FireEquipmentChanged()
        {
            try
            {
                OnEquipmentChanged?.Invoke();
            }
            catch (Exception ex)
            {
                SSLog.Error(SSLog.CategoryEquipment, $"Exception in OnEquipmentChanged subscriber: {ex}");
            }
        }
    }
}
