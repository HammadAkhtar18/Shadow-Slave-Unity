using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ShadowSlave.Attributes;
using UnityEngine;

namespace ShadowSlave.Items
{
    /// <summary>
    /// Data-driven ScriptableObject archetype describing immutable static item definitions.
    /// Cleanly separates static item archetype configuration from mutable runtime instances (ItemInstance).
    /// Matches UE5 UShadowSlaveItemDefinition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "ShadowSlave/Items/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string itemId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField, TextArea]
        private string description = string.Empty;

        [SerializeField]
        private int version = 1;

        [Header("Classification")]
        [SerializeField]
        private ShadowSlaveItemType itemType = ShadowSlaveItemType.Miscellaneous;

        [SerializeField]
        private ShadowSlaveEquipmentSlot equipmentSlot = ShadowSlaveEquipmentSlot.None;

        [Header("Equipment & Modifiers")]
        [SerializeField]
        private List<AttributeModifier> grantedModifiers = new List<AttributeModifier>();

        private ReadOnlyCollection<AttributeModifier> _readOnlyGrantedModifiers;
        private List<AttributeModifier> _cachedGrantedModifiersSource;

        [Header("Stacking & Metrics")]
        [SerializeField]
        private bool isStackable = false;

        [SerializeField]
        private int maxStackSize = 1;

        [SerializeField]
        private float weight = 0.1f;

        [SerializeField]
        private int baseValue = 10;

        public string ItemId => itemId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public int Version => version;
        public ShadowSlaveItemType ItemType => itemType;
        public ShadowSlaveEquipmentSlot EquipmentSlot => equipmentSlot;
        public bool IsStackable => isStackable;
        public int MaxStackSize => Mathf.Max(1, maxStackSize);
        public float Weight => weight;
        public int BaseValue => baseValue;

        public IReadOnlyList<AttributeModifier> GrantedModifiers
        {
            get
            {
                if (grantedModifiers == null)
                {
                    return Array.Empty<AttributeModifier>();
                }

                if (_readOnlyGrantedModifiers == null || _cachedGrantedModifiersSource != grantedModifiers)
                {
                    _cachedGrantedModifiersSource = grantedModifiers;
                    _readOnlyGrantedModifiers = grantedModifiers.AsReadOnly();
                }

                return _readOnlyGrantedModifiers;
            }
        }

        /* --- Compatibility Accessors --- */

        public string GetItemId() => ItemId;
        public void SetItemId(string newItemId) => itemId = newItemId ?? string.Empty;

        /// <summary>
        /// Validates definition configuration.
        /// Matches UE5 UShadowSlaveItemDefinition::IsValidDefinition.
        /// </summary>
        public bool IsValidDefinition(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                errorMessage = "ItemId cannot be null or whitespace.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errorMessage = $"Item definition '{itemId}' must have a non-empty DisplayName.";
                return false;
            }

            if (version < 1)
            {
                errorMessage = $"Item definition '{itemId}' must have Version >= 1.";
                return false;
            }

            if (isStackable && maxStackSize < 1)
            {
                errorMessage = $"Item definition '{itemId}' is stackable but has MaxStackSize < 1 ({maxStackSize}).";
                return false;
            }

            if (weight < 0f || float.IsNaN(weight) || float.IsInfinity(weight))
            {
                errorMessage = $"Item definition '{itemId}' has invalid or negative Weight ({weight}).";
                return false;
            }

            if (baseValue < 0)
            {
                errorMessage = $"Item definition '{itemId}' has negative BaseValue ({baseValue}).";
                return false;
            }

            if (grantedModifiers != null)
            {
                for (int i = 0; i < grantedModifiers.Count; i++)
                {
                    var mod = grantedModifiers[i];
                    if (float.IsNaN(mod.Value) || float.IsInfinity(mod.Value))
                    {
                        errorMessage = $"Item definition '{itemId}' has modifier with NaN or Infinity value at index {i}.";
                        return false;
                    }

                    if (float.IsNaN(mod.Duration) || float.IsInfinity(mod.Duration) || mod.Duration < 0f)
                    {
                        errorMessage = $"Item definition '{itemId}' has modifier with invalid Duration at index {i}.";
                        return false;
                    }
                }
            }

            errorMessage = null;
            return true;
        }

        public bool IsValidDefinition() => IsValidDefinition(out _);

        public bool ValidateDefinition(out string errorMessage) => IsValidDefinition(out errorMessage);
        public bool ValidateDefinition() => IsValidDefinition(out _);

        /* --- Test & Programmatic Factories --- */

        public static ItemDefinition Create(
            string itemId,
            string displayName = "Generic Item",
            string description = "",
            int version = 1,
            ShadowSlaveItemType itemType = ShadowSlaveItemType.Miscellaneous,
            ShadowSlaveEquipmentSlot equipmentSlot = ShadowSlaveEquipmentSlot.None,
            bool isStackable = false,
            int maxStackSize = 1,
            float weight = 0.1f,
            int baseValue = 10,
            IEnumerable<AttributeModifier> modifiers = null)
        {
            var def = CreateInstance<ItemDefinition>();
            def.itemId = itemId ?? string.Empty;
            def.displayName = displayName ?? string.Empty;
            def.description = description ?? string.Empty;
            def.version = version;
            def.itemType = itemType;
            def.equipmentSlot = equipmentSlot;
            def.isStackable = isStackable;
            def.maxStackSize = maxStackSize;
            def.weight = weight;
            def.baseValue = baseValue;

            if (modifiers != null)
            {
                def.grantedModifiers.AddRange(modifiers);
            }

            return def;
        }

        public static ItemDefinition CreateTestConsumableDefinition()
        {
            return Create(
                itemId: "TestConsumableItem",
                displayName: "Generic Test Draught",
                description: "A temporary development consumable for verifying stackable inventory operations.",
                version: 1,
                itemType: ShadowSlaveItemType.Consumable,
                equipmentSlot: ShadowSlaveEquipmentSlot.None,
                isStackable: true,
                maxStackSize: 10,
                weight: 0.25f,
                baseValue: 15
            );
        }

        public static ItemDefinition CreateTestQuestItemDefinition()
        {
            return Create(
                itemId: "TestQuestItem",
                displayName: "Generic Test Relic Key",
                description: "A temporary development quest item for verifying unique, non-stackable inventory slots.",
                version: 1,
                itemType: ShadowSlaveItemType.Quest,
                equipmentSlot: ShadowSlaveEquipmentSlot.None,
                isStackable: false,
                maxStackSize: 1,
                weight: 0.5f,
                baseValue: 0
            );
        }
    }
}
