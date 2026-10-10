using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShadowSlave.Attributes;
using ShadowSlave.Characters;
using ShadowSlave.Core;
using ShadowSlave.Equipment;
using ShadowSlave.Items;
using ShadowSlave.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class EquipmentTests
    {
        private GameObject _go;
        private InventoryComponent _inventoryComp;
        private AttributeComponent _attributeComp;
        private EquipmentComponent _equipmentComp;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("EquipmentTestActor");
            _inventoryComp = _go.AddComponent<InventoryComponent>();
            _attributeComp = _go.AddComponent<AttributeComponent>();
            _equipmentComp = _go.AddComponent<EquipmentComponent>();

            var config = new AttributeInitConfig
            {
                BaseMaxHealth = 100f,
                BaseMaxStamina = 100f,
                BaseMaxEssence = 100f,
                EnableStaminaRegen = false,
                StaminaRegenRate = 0f,
                StaminaRegenDelay = 0f
            };
            _attributeComp.Initialize(config);
            _inventoryComp.SetCapacity(10);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void Scenario01_EmptyInitialState_SlotQueriesReturnExpectedDefaults()
        {
            Assert.AreEqual(0, _equipmentComp.EquippedCount);
            Assert.AreEqual(0, _equipmentComp.EquippedSlots.Count);
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.None));

            Assert.IsFalse(_equipmentComp.GetEquippedItemInSlot(ShadowSlaveEquipmentSlot.Weapon, out var item));
            Assert.IsFalse(item.IsValid);

            var randomGuid = Guid.NewGuid();
            Assert.IsFalse(_equipmentComp.IsInstanceEquipped(randomGuid));
            Assert.AreEqual(ShadowSlaveEquipmentSlot.None, _equipmentComp.GetSlotForInstance(randomGuid));

            var allEquipped = _equipmentComp.GetAllEquippedItems();
            Assert.IsNotNull(allEquipped);
            Assert.AreEqual(0, allEquipped.Count);
        }

        [Test]
        public void Scenario02_SuccessfulEquip_ByValidInventoryInstanceId()
        {
            var itemDef = ItemDefinition.Create(
                itemId: "Sword_01",
                displayName: "Iron Sword",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon);

            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var instance));

            bool equipped = _equipmentComp.EquipItem(instance.InstanceId);
            Assert.IsTrue(equipped);

            Assert.AreEqual(1, _equipmentComp.EquippedCount);
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));
            Assert.IsTrue(_equipmentComp.IsInstanceEquipped(instance.InstanceId));
            Assert.AreEqual(ShadowSlaveEquipmentSlot.Weapon, _equipmentComp.GetSlotForInstance(instance.InstanceId));

            Assert.IsTrue(_equipmentComp.GetEquippedItemInSlot(ShadowSlaveEquipmentSlot.Weapon, out var equippedItem));
            Assert.IsTrue(equippedItem.IsValid);
            Assert.AreEqual(ShadowSlaveEquipmentSlot.Weapon, equippedItem.Slot);
            Assert.AreEqual(EquipmentSourceType.Item, equippedItem.SourceType);
            Assert.AreEqual(instance.InstanceId, equippedItem.InstanceId);
            Assert.AreEqual("Sword_01", equippedItem.DefinitionId);

            Assert.IsTrue(_equipmentComp.GetEquippedItemInstance(ShadowSlaveEquipmentSlot.Weapon, out var foundInstance));
            Assert.AreEqual(instance.InstanceId, foundInstance.InstanceId);
        }

        [Test]
        public void Scenario03_InvalidOrMissingInstance_RejectsSafely()
        {
            Assert.IsFalse(_equipmentComp.EquipItem(Guid.Empty));
            Assert.IsFalse(_equipmentComp.EquipItem(Guid.NewGuid()));
            Assert.AreEqual(0, _equipmentComp.EquippedCount);
            Assert.IsFalse(_equipmentComp.EquipItemInstance(null));
        }

        [Test]
        public void Scenario04_DefaultSlotSelection_AndExplicitSlotOverride()
        {
            var ringDef = ItemDefinition.Create(
                itemId: "Ring_01",
                displayName: "Silver Ring",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Ring);

            Assert.IsTrue(_inventoryComp.AddItem(ringDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(ringDef, out var ringInstance));

            // Default slot selection: should equip to Ring slot
            Assert.IsTrue(_equipmentComp.EquipItem(ringInstance.InstanceId));
            Assert.AreEqual(ShadowSlaveEquipmentSlot.Ring, _equipmentComp.GetSlotForInstance(ringInstance.InstanceId));
            Assert.IsTrue(_equipmentComp.UnequipSlot(ShadowSlaveEquipmentSlot.Ring));

            // Explicit slot override: equip to Charm slot instead
            Assert.IsTrue(_equipmentComp.EquipItem(ringInstance.InstanceId, ShadowSlaveEquipmentSlot.Charm));
            Assert.AreEqual(ShadowSlaveEquipmentSlot.Charm, _equipmentComp.GetSlotForInstance(ringInstance.InstanceId));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Ring));

            // Item with None slot and no override fails
            var miscDef = ItemDefinition.Create(
                itemId: "Misc_01",
                displayName: "Pebble",
                itemType: ShadowSlaveItemType.Miscellaneous,
                equipmentSlot: ShadowSlaveEquipmentSlot.None);
            Assert.IsTrue(_inventoryComp.AddItem(miscDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(miscDef, out var miscInstance));
            Assert.IsFalse(_equipmentComp.EquipItem(miscInstance.InstanceId));
        }

        [Test]
        public void Scenario05_OccupiedSlotReplacement_ReplacesOldOccupantAndRemovesOldModifiers()
        {
            var swordDef = ItemDefinition.Create(
                itemId: "Sword_Basic",
                displayName: "Basic Sword",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon,
                modifiers: new[]
                {
                    new AttributeModifier("HealthBoost_Sword", AttributeType.MaxHealth, AttributeModifierType.Flat, 20f)
                });

            var axeDef = ItemDefinition.Create(
                itemId: "Axe_Heavy",
                displayName: "Heavy Axe",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon,
                modifiers: new[]
                {
                    new AttributeModifier("HealthBoost_Axe", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(swordDef, 1, out _));
            Assert.IsTrue(_inventoryComp.AddItem(axeDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(swordDef, out var swordInstance));
            Assert.IsTrue(_inventoryComp.FindItem(axeDef, out var axeInstance));

            // Equip sword: 100 + 20 = 120 MaxHealth
            Assert.IsTrue(_equipmentComp.EquipItem(swordInstance.InstanceId));
            Assert.AreEqual(120f, _attributeComp.EffectiveMaxHealth);
            Assert.IsTrue(_equipmentComp.IsInstanceEquipped(swordInstance.InstanceId));

            // Equip axe in same slot: old sword is replaced and unequipped, new axe modifiers active: 100 + 50 = 150
            Assert.IsTrue(_equipmentComp.EquipItem(axeInstance.InstanceId));
            Assert.AreEqual(150f, _attributeComp.EffectiveMaxHealth);
            Assert.IsFalse(_equipmentComp.IsInstanceEquipped(swordInstance.InstanceId));
            Assert.IsTrue(_equipmentComp.IsInstanceEquipped(axeInstance.InstanceId));
            Assert.AreEqual(1, _equipmentComp.EquippedCount);
        }

        [Test]
        public void Scenario06_IdempotentRepeatedEquip_DoesNotDuplicateOrReapply()
        {
            var itemDef = ItemDefinition.Create(
                itemId: "Shield_01",
                displayName: "Wooden Shield",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Armor,
                modifiers: new[]
                {
                    new AttributeModifier("Shield_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 30f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var instance));

            int equipEventCount = 0;
            _equipmentComp.OnEquipmentItemEquipped += (slot, item) => equipEventCount++;

            Assert.IsTrue(_equipmentComp.EquipItem(instance.InstanceId));
            Assert.AreEqual(1, equipEventCount);
            Assert.AreEqual(130f, _attributeComp.EffectiveMaxHealth);

            // Re-equip identical item in same slot: idempotent success
            Assert.IsTrue(_equipmentComp.EquipItem(instance.InstanceId));
            Assert.AreEqual(1, equipEventCount); // No duplicate event fired
            Assert.AreEqual(130f, _attributeComp.EffectiveMaxHealth); // No duplicate modifiers
        }

        [Test]
        public void Scenario07_Unequip_BySlotAndInstanceId()
        {
            var itemDef = ItemDefinition.Create(
                itemId: "Armor_01",
                displayName: "Leather Armor",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Armor,
                modifiers: new[]
                {
                    new AttributeModifier("Armor_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 40f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var instance));

            Assert.IsTrue(_equipmentComp.EquipItem(instance.InstanceId));
            Assert.AreEqual(140f, _attributeComp.EffectiveMaxHealth);

            // Unequip by slot
            Assert.IsTrue(_equipmentComp.UnequipSlot(ShadowSlaveEquipmentSlot.Armor));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
            Assert.AreEqual(100f, _attributeComp.EffectiveMaxHealth);
            Assert.IsFalse(_equipmentComp.UnequipSlot(ShadowSlaveEquipmentSlot.Armor));

            // Re-equip and unequip by instance ID
            Assert.IsTrue(_equipmentComp.EquipItem(instance.InstanceId));
            Assert.AreEqual(140f, _attributeComp.EffectiveMaxHealth);
            Assert.IsTrue(_equipmentComp.UnequipInstance(instance.InstanceId));
            Assert.IsFalse(_equipmentComp.IsInstanceEquipped(instance.InstanceId));
            Assert.AreEqual(100f, _attributeComp.EffectiveMaxHealth);
        }

        [Test]
        public void Scenario08_UnequipAll_ClearsAllSlotsAndStripsModifiers()
        {
            var swordDef = ItemDefinition.Create(
                itemId: "Sword_01",
                displayName: "Sword",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon,
                modifiers: new[]
                {
                    new AttributeModifier("Sword_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 15f)
                });

            var armorDef = ItemDefinition.Create(
                itemId: "Armor_01",
                displayName: "Armor",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Armor,
                modifiers: new[]
                {
                    new AttributeModifier("Armor_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 25f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(swordDef, 1, out _));
            Assert.IsTrue(_inventoryComp.AddItem(armorDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(swordDef, out var sInst));
            Assert.IsTrue(_inventoryComp.FindItem(armorDef, out var aInst));

            Assert.IsTrue(_equipmentComp.EquipItem(sInst.InstanceId));
            Assert.IsTrue(_equipmentComp.EquipItem(aInst.InstanceId));
            Assert.AreEqual(2, _equipmentComp.EquippedCount);
            Assert.AreEqual(140f, _attributeComp.EffectiveMaxHealth);

            _equipmentComp.UnequipAll();
            Assert.AreEqual(0, _equipmentComp.EquippedCount);
            Assert.AreEqual(100f, _attributeComp.EffectiveMaxHealth);
        }

        [Test]
        public void Scenario09_ModifierLifecycle_SourceIsolation_PreservesUnrelatedModifiers()
        {
            var aspectGuid = Guid.NewGuid();
            var aspectMod = new AttributeModifier("Aspect_Passive", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 0f, aspectGuid);
            _attributeComp.AddModifier(aspectMod);
            Assert.AreEqual(150f, _attributeComp.EffectiveMaxHealth);

            var itemDef = ItemDefinition.Create(
                itemId: "Staff_01",
                displayName: "Magic Staff",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon,
                modifiers: new[]
                {
                    new AttributeModifier("Staff_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 25f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var inst));

            Assert.IsTrue(_equipmentComp.EquipItem(inst.InstanceId));
            Assert.AreEqual(175f, _attributeComp.EffectiveMaxHealth);

            _equipmentComp.UnequipSlot(ShadowSlaveEquipmentSlot.Weapon);
            // Weapon modifier removed, but aspect modifier remains!
            Assert.AreEqual(150f, _attributeComp.EffectiveMaxHealth);
            Assert.IsTrue(_attributeComp.HasModifierFromSourceId(aspectGuid));
            Assert.IsFalse(_attributeComp.HasModifierFromSourceId(inst.InstanceId));
        }

        [Test]
        public void Scenario10_FailedModifierApplication_PreservesStateConsistency()
        {
            var dummyGo = new GameObject("DummyNoAttr");
            var dummyInv = dummyGo.AddComponent<InventoryComponent>();
            var dummyEquip = dummyGo.AddComponent<EquipmentComponent>();
            dummyInv.SetCapacity(5);

            var itemWithMod = ItemDefinition.Create(
                itemId: "Helm_Mod",
                displayName: "Helm",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Armor,
                modifiers: new[]
                {
                    new AttributeModifier("Helm_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 10f)
                });

            dummyInv.AddItem(itemWithMod, 1, out _);
            dummyInv.FindItem(itemWithMod, out var inst);

            // Cannot equip item with modifiers when owner has no AttributeComponent
            bool equipped = dummyEquip.EquipItem(inst.InstanceId);
            Assert.IsFalse(equipped);
            Assert.AreEqual(0, dummyEquip.EquippedCount);
            Assert.IsFalse(dummyEquip.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));

            Object.DestroyImmediate(dummyGo);
        }

        [Test]
        public void Scenario11_InventoryRemovalAndClearing_ReconcilesEquipmentState()
        {
            var itemDef = ItemDefinition.Create(
                itemId: "Boots_01",
                displayName: "Boots",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Armor,
                modifiers: new[]
                {
                    new AttributeModifier("Boots_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 15f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var inst));

            Assert.IsTrue(_equipmentComp.EquipItem(inst.InstanceId));
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
            Assert.AreEqual(115f, _attributeComp.EffectiveMaxHealth);

            // Removing item from inventory automatically reconciles and unequips
            Assert.IsTrue(_inventoryComp.RemoveItemByInstanceId(inst.InstanceId, 1));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
            Assert.AreEqual(100f, _attributeComp.EffectiveMaxHealth);

            // Re-add and clear inventory
            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var inst2));
            Assert.IsTrue(_equipmentComp.EquipItem(inst2.InstanceId));
            Assert.AreEqual(115f, _attributeComp.EffectiveMaxHealth);

            _inventoryComp.Clear();
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
            Assert.AreEqual(100f, _attributeComp.EffectiveMaxHealth);
        }

        [Test]
        public void Scenario12_PartialStackRemoval_RetainsEquipmentWhileInstanceRemains()
        {
            var stackDef = ItemDefinition.Create(
                itemId: "Throwing_Knives",
                displayName: "Throwing Knives",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon,
                isStackable: true,
                maxStackSize: 10);

            Assert.IsTrue(_inventoryComp.AddItem(stackDef, 5, out _));
            Assert.IsTrue(_inventoryComp.FindItem(stackDef, out var inst));
            Assert.AreEqual(5, inst.Quantity);

            Assert.IsTrue(_equipmentComp.EquipItem(inst.InstanceId));
            Assert.IsTrue(_equipmentComp.IsInstanceEquipped(inst.InstanceId));

            // Remove 2 units: stack still has 3 units remaining
            Assert.IsTrue(_inventoryComp.RemoveItemByInstanceId(inst.InstanceId, 2));
            Assert.AreEqual(3, inst.Quantity);
            // Item MUST still be equipped!
            Assert.IsTrue(_equipmentComp.IsInstanceEquipped(inst.InstanceId));
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));

            // Remove remaining 3 units: depleted
            Assert.IsTrue(_inventoryComp.RemoveItemByInstanceId(inst.InstanceId, 3));
            Assert.IsFalse(_equipmentComp.IsInstanceEquipped(inst.InstanceId));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));
        }

        [Test]
        public void Scenario13_MissingCompanionComponents_RejectsSafelyWithoutThrowing()
        {
            var lonelyGo = new GameObject("LonelyActor");
            var lonelyEquip = lonelyGo.AddComponent<EquipmentComponent>();

            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(lonelyEquip.EquipItem(Guid.NewGuid()));
                Assert.IsFalse(lonelyEquip.UnequipSlot(ShadowSlaveEquipmentSlot.Weapon));
                Assert.IsFalse(lonelyEquip.UnequipInstance(Guid.NewGuid()));
                lonelyEquip.UnequipAll();
                Assert.IsFalse(lonelyEquip.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));
                Assert.AreEqual(0, lonelyEquip.EquippedCount);
                Assert.IsFalse(lonelyEquip.GetEquippedItemInstance(ShadowSlaveEquipmentSlot.Weapon, out _));
            });

            Object.DestroyImmediate(lonelyGo);
        }

        [Test]
        public void Scenario14_EventParametersAndNotificationOrder_FiresInExpectedSequence()
        {
            var itemDef = ItemDefinition.Create(
                itemId: "Dagger_01",
                displayName: "Dagger",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon);

            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var inst));

            var eventLog = new List<string>();
            _equipmentComp.OnEquipmentItemEquipped += (slot, item) => eventLog.Add($"Equipped:{slot}:{item.DefinitionId}");
            _equipmentComp.OnEquipmentItemUnequipped += (slot, item) => eventLog.Add($"Unequipped:{slot}:{item.DefinitionId}");
            _equipmentComp.OnEquipmentSlotChanged += slot => eventLog.Add($"SlotChanged:{slot}");
            _equipmentComp.OnEquipmentChanged += () => eventLog.Add("EquipmentChanged");

            _equipmentComp.EquipItem(inst.InstanceId);
            Assert.AreEqual(3, eventLog.Count);
            Assert.AreEqual("Equipped:Weapon:Dagger_01", eventLog[0]);
            Assert.AreEqual("SlotChanged:Weapon", eventLog[1]);
            Assert.AreEqual("EquipmentChanged", eventLog[2]);

            eventLog.Clear();
            _equipmentComp.UnequipSlot(ShadowSlaveEquipmentSlot.Weapon);
            Assert.AreEqual(3, eventLog.Count);
            Assert.AreEqual("Unequipped:Weapon:Dagger_01", eventLog[0]);
            Assert.AreEqual("SlotChanged:Weapon", eventLog[1]);
            Assert.AreEqual("EquipmentChanged", eventLog[2]);
        }

        [Test]
        public void Scenario15_SubscriberExceptions_DoNotCorruptInternalState()
        {
            var itemDef = ItemDefinition.Create(
                itemId: "Trinket_01",
                displayName: "Trinket",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Charm);

            Assert.IsTrue(_inventoryComp.AddItem(itemDef, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out var inst));

            _equipmentComp.OnEquipmentItemEquipped += (slot, item) =>
            {
                throw new InvalidOperationException("Simulated subscriber exception");
            };

            bool equipped = _equipmentComp.EquipItem(inst.InstanceId);
            Assert.IsTrue(equipped);
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Charm));
        }

        [Test]
        public void Scenario16_ReentrantMutationAttempts_RejectedSafely()
        {
            var itemA = ItemDefinition.Create(
                itemId: "ItemA",
                displayName: "Item A",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon);

            var itemB = ItemDefinition.Create(
                itemId: "ItemB",
                displayName: "Item B",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Armor);

            Assert.IsTrue(_inventoryComp.AddItem(itemA, 1, out _));
            Assert.IsTrue(_inventoryComp.AddItem(itemB, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemA, out var instA));
            Assert.IsTrue(_inventoryComp.FindItem(itemB, out var instB));

            bool reentrantResult = true;
            _equipmentComp.OnEquipmentItemEquipped += (slot, item) =>
            {
                // Reentrant call during equip transition
                reentrantResult = _equipmentComp.EquipItem(instB.InstanceId);
            };

            Assert.IsTrue(_equipmentComp.EquipItem(instA.InstanceId));
            Assert.IsFalse(reentrantResult); // Must have been rejected!
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
        }

        [Test]
        public void Scenario17_ReadOnlyCollectionEncapsulation_CannotBeMutatedOrDowncast()
        {
            var dict = _equipmentComp.EquippedSlots;
            Assert.IsNotNull(dict);
            Assert.Throws<InvalidCastException>(() =>
            {
                var mutable = (Dictionary<ShadowSlaveEquipmentSlot, EquippedItem>)dict;
            });
        }

        [Test]
        public void Scenario18_CharacterBase_EquipmentComponentIntegration_ResolvesCorrectly()
        {
            var charGo = new GameObject("TestCharacter");
            charGo.AddComponent<AttributeComponent>();
            charGo.AddComponent<ProgressionComponent>();
            var equipOnChar = charGo.AddComponent<EquipmentComponent>();
            var charBase = charGo.AddComponent<CharacterBase>();

            // Trigger Awake via reflection or active state
            charGo.SetActive(true);

            Assert.IsNotNull(charBase.EquipmentComponent);
            Assert.AreSame(equipOnChar, charBase.EquipmentComponent);

            Object.DestroyImmediate(charGo);
        }

        [Test]
        public void Scenario19_MovingItemBetweenSlots_VacatesOldSlotAndOccupiesNewSlot()
        {
            var flexItem = ItemDefinition.Create(
                itemId: "FlexRing",
                displayName: "Flexible Ring",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Ring,
                modifiers: new[]
                {
                    new AttributeModifier("Ring_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 10f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(flexItem, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(flexItem, out var inst));

            // Equip into Ring slot
            Assert.IsTrue(_equipmentComp.EquipItem(inst.InstanceId, ShadowSlaveEquipmentSlot.Ring));
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Ring));
            Assert.AreEqual(110f, _attributeComp.EffectiveMaxHealth);

            // Move same item into Charm slot
            Assert.IsTrue(_equipmentComp.EquipItem(inst.InstanceId, ShadowSlaveEquipmentSlot.Charm));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Ring));
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Charm));
            Assert.AreEqual(1, _equipmentComp.EquippedCount);
            // Modifiers must not duplicate: 100 + 10 = 110f
            Assert.AreEqual(110f, _attributeComp.EffectiveMaxHealth);
        }

        [Test]
        public void Scenario20_EquippedItemSerialization_PreservesGuidAndHandlesInvalidStrings()
        {
            var validGuid = Guid.NewGuid();
            var item = new EquippedItem(ShadowSlaveEquipmentSlot.Weapon, EquipmentSourceType.Item, validGuid, "TestDef");

            item.OnBeforeSerialize();
            Assert.AreEqual(validGuid, item.InstanceId);
            Assert.IsTrue(item.IsValid);

            // Test OnAfterDeserialize roundtrip
            item.OnAfterDeserialize();
            Assert.AreEqual(validGuid, item.InstanceId);
            Assert.IsTrue(item.IsValid);

            // Test Reset and OnBeforeSerialize clears string
            item.Reset();
            Assert.AreEqual(Guid.Empty, item.InstanceId);
            Assert.IsFalse(item.IsValid);
            item.OnBeforeSerialize();
            item.OnAfterDeserialize();
            Assert.AreEqual(Guid.Empty, item.InstanceId);
            Assert.IsFalse(item.IsValid);
        }

        [Test]
        public void Scenario21_ReentrantInventoryRemovalDuringEquip_DefersAndReconcilesPostTransition()
        {
            var itemA = ItemDefinition.Create(
                itemId: "Sword_A",
                displayName: "Sword A",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon,
                modifiers: new[]
                {
                    new AttributeModifier("Sword_A_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 20f)
                });

            var itemB = ItemDefinition.Create(
                itemId: "Armor_B",
                displayName: "Armor B",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Armor,
                modifiers: new[]
                {
                    new AttributeModifier("Armor_B_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 30f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(itemA, 1, out _));
            Assert.IsTrue(_inventoryComp.AddItem(itemB, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemA, out var instA));
            Assert.IsTrue(_inventoryComp.FindItem(itemB, out var instB));

            // Equip item B in Armor slot
            Assert.IsTrue(_equipmentComp.EquipItem(instB.InstanceId));
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
            Assert.AreEqual(130f, _attributeComp.EffectiveMaxHealth);

            // Reentrant hook: when item A is equipped, remove item B from inventory
            _equipmentComp.OnEquipmentItemEquipped += (slot, item) =>
            {
                if (slot == ShadowSlaveEquipmentSlot.Weapon)
                {
                    _inventoryComp.RemoveItemByInstanceId(instB.InstanceId, 1);
                }
            };

            // Equip item A: triggers transition, fires OnEquipmentItemEquipped, removes item B
            Assert.IsTrue(_equipmentComp.EquipItem(instA.InstanceId));

            // Post-transition verification:
            // 1. Item A is equipped in Weapon slot with +20 modifier
            Assert.IsTrue(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));
            Assert.IsTrue(_equipmentComp.IsInstanceEquipped(instA.InstanceId));

            // 2. Item B was removed from inventory during the transition, so post-transition reconciliation unequipped it!
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Armor));
            Assert.IsFalse(_equipmentComp.IsInstanceEquipped(instB.InstanceId));
            Assert.AreEqual(1, _equipmentComp.EquippedCount);

            // 3. Modifiers: 100 base + 20 (Sword A) = 120 (Armor B modifier of +30 is completely removed!)
            Assert.AreEqual(120f, _attributeComp.EffectiveMaxHealth);
            Assert.IsFalse(_attributeComp.HasModifierFromSourceId(instB.InstanceId));
        }

        [Test]
        public void Scenario22_ReentrantInventoryClearDuringEquip_DefersAndReconcilesPostTransition()
        {
            var itemA = ItemDefinition.Create(
                itemId: "Dagger_A",
                displayName: "Dagger A",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Weapon,
                modifiers: new[]
                {
                    new AttributeModifier("Dagger_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 15f)
                });

            var itemB = ItemDefinition.Create(
                itemId: "Ring_B",
                displayName: "Ring B",
                itemType: ShadowSlaveItemType.Equipment,
                equipmentSlot: ShadowSlaveEquipmentSlot.Ring,
                modifiers: new[]
                {
                    new AttributeModifier("Ring_Mod", AttributeType.MaxHealth, AttributeModifierType.Flat, 25f)
                });

            Assert.IsTrue(_inventoryComp.AddItem(itemA, 1, out _));
            Assert.IsTrue(_inventoryComp.AddItem(itemB, 1, out _));
            Assert.IsTrue(_inventoryComp.FindItem(itemA, out var instA));
            Assert.IsTrue(_inventoryComp.FindItem(itemB, out var instB));

            Assert.IsTrue(_equipmentComp.EquipItem(instB.InstanceId));
            Assert.AreEqual(125f, _attributeComp.EffectiveMaxHealth);

            // Reentrant hook: clearing inventory during EquipItem
            _equipmentComp.OnEquipmentItemEquipped += (slot, item) =>
            {
                if (slot == ShadowSlaveEquipmentSlot.Weapon)
                {
                    _inventoryComp.ClearInventory();
                }
            };

            Assert.IsTrue(_equipmentComp.EquipItem(instA.InstanceId));

            // Since inventory was cleared, deferred post-transition reconciliation unequipped all items!
            Assert.AreEqual(0, _equipmentComp.EquippedCount);
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Weapon));
            Assert.IsFalse(_equipmentComp.IsSlotOccupied(ShadowSlaveEquipmentSlot.Ring));
            Assert.AreEqual(100f, _attributeComp.EffectiveMaxHealth);
        }
    }
}
