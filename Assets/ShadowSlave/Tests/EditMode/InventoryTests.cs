using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShadowSlave.Attributes;
using ShadowSlave.Characters;
using ShadowSlave.Items;
using ShadowSlave.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class InventoryTests
    {
        private GameObject _go;
        private InventoryComponent _inventoryComp;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("InventoryTestActor");
            _inventoryComp = _go.AddComponent<InventoryComponent>();
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
        public void EmptyInventoryInitialState_MatchesExpectedDefaults()
        {
            _inventoryComp.SetCapacity(8);

            Assert.AreEqual(8, _inventoryComp.GetCapacity());
            Assert.AreEqual(8, _inventoryComp.Capacity);
            Assert.AreEqual(8, _inventoryComp.MaxSlots);
            Assert.AreEqual(0, _inventoryComp.GetUsedSlotCount());
            Assert.AreEqual(0, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(8, _inventoryComp.GetFreeSlotCount());
            Assert.AreEqual(8, _inventoryComp.FreeSlotCount);
            Assert.IsFalse(_inventoryComp.IsFull);
            Assert.IsFalse(_inventoryComp.IsInventoryFull());
            Assert.AreEqual(0, _inventoryComp.Slots.Count);
            Assert.AreEqual(0, _inventoryComp.GetSlots().Count);
        }

        [Test]
        public void AddItemAndQuantityTracking_SingleItemTracksCorrectly()
        {
            _inventoryComp.SetCapacity(5);

            var singleItemDef = ItemDefinition.Create(
                itemId: "Single_Item",
                displayName: "Single Item",
                isStackable: false,
                maxStackSize: 1
            );

            bool added = _inventoryComp.AddItem(singleItemDef, 1, out int remainder);

            Assert.IsTrue(added);
            Assert.AreEqual(0, remainder);
            Assert.AreEqual(1, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(1, _inventoryComp.GetTotalItemCount(singleItemDef));
            Assert.IsTrue(_inventoryComp.HasItem(singleItemDef, 1));
            Assert.IsFalse(_inventoryComp.HasItem(singleItemDef, 2));
        }

        [Test]
        public void StackingBehavior_MergesIntoSameSlotUpToMaxStack()
        {
            _inventoryComp.SetCapacity(5);

            var stackDef = ItemDefinition.Create(
                itemId: "Stackable_Potion",
                displayName: "Stackable Potion",
                isStackable: true,
                maxStackSize: 10
            );

            Assert.IsTrue(_inventoryComp.AddItem(stackDef, 4, out int remainder1));
            Assert.AreEqual(0, remainder1);
            Assert.AreEqual(1, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(4, _inventoryComp.GetTotalItemCount(stackDef));

            Assert.IsTrue(_inventoryComp.AddItem(stackDef, 5, out int remainder2));
            Assert.AreEqual(0, remainder2);
            Assert.AreEqual(1, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(9, _inventoryComp.GetTotalItemCount(stackDef));
        }

        [Test]
        public void CapacityAndRemainderHandling_CapsAtMaxSlotsAndOutputsRemainder()
        {
            _inventoryComp.SetCapacity(2);

            var stackDef = ItemDefinition.Create(
                itemId: "Stackable_Herb",
                displayName: "Stackable Herb",
                isStackable: true,
                maxStackSize: 5
            );

            // Adding 13 units to 2 slots of max stack 5 (capacity 10 total)
            bool added = _inventoryComp.AddItem(stackDef, 13, out int remainder);

            Assert.IsTrue(added);
            Assert.AreEqual(3, remainder);
            Assert.AreEqual(10, _inventoryComp.GetTotalItemCount(stackDef));
            Assert.AreEqual(2, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(0, _inventoryComp.FreeSlotCount);
            Assert.IsTrue(_inventoryComp.IsFull);
        }

        [Test]
        public void NonStackableCapacityAndRemainderHandling_CapsCorrectly()
        {
            _inventoryComp.SetCapacity(2);

            var questDef = ItemDefinition.CreateTestQuestItemDefinition();

            bool added = _inventoryComp.AddItem(questDef, 3, out int remainder);

            Assert.IsTrue(added);
            Assert.AreEqual(1, remainder);
            Assert.AreEqual(2, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(0, _inventoryComp.FreeSlotCount);
            Assert.IsTrue(_inventoryComp.IsFull);
        }

        [Test]
        public void AtomicRemovalWhenUnavailable_LeavesInventoryUntouched()
        {
            _inventoryComp.SetCapacity(5);

            var itemDef = ItemDefinition.Create(
                itemId: "Shard_Item",
                displayName: "Shard Item",
                isStackable: true,
                maxStackSize: 20
            );

            _inventoryComp.AddItem(itemDef, 5, out _);
            Assert.AreEqual(5, _inventoryComp.GetTotalItemCount(itemDef));

            bool removed = _inventoryComp.RemoveItem(itemDef, 10);

            Assert.IsFalse(removed);
            Assert.AreEqual(5, _inventoryComp.GetTotalItemCount(itemDef));
            Assert.AreEqual(1, _inventoryComp.UsedSlotCount);
        }

        [Test]
        public void SuccessfulRemovalAndInstanceId_DecrementsAndRemovesSlots()
        {
            _inventoryComp.SetCapacity(5);

            var itemDef = ItemDefinition.Create(
                itemId: "Ore_Item",
                displayName: "Ore Item",
                isStackable: true,
                maxStackSize: 20
            );

            _inventoryComp.AddItem(itemDef, 8, out _);

            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out ItemInstance foundInstance));
            Assert.AreNotEqual(Guid.Empty, foundInstance.InstanceId);
            Assert.IsTrue(_inventoryComp.HasItemByInstanceId(foundInstance.InstanceId));

            Assert.IsTrue(_inventoryComp.RemoveItem(itemDef, 3));
            Assert.AreEqual(5, _inventoryComp.GetTotalItemCount(itemDef));

            Assert.IsTrue(_inventoryComp.RemoveItemByInstanceId(foundInstance.InstanceId, 5));
            Assert.AreEqual(0, _inventoryComp.GetTotalItemCount(itemDef));
            Assert.IsFalse(_inventoryComp.HasItemByInstanceId(foundInstance.InstanceId));
            Assert.AreEqual(0, _inventoryComp.UsedSlotCount);
        }

        [Test]
        public void RemoveItemByInstanceId_FailsWhenQuantityExceedsSlot()
        {
            _inventoryComp.SetCapacity(5);

            var itemDef = ItemDefinition.Create(
                itemId: "Crystal_Item",
                displayName: "Crystal Item",
                isStackable: true,
                maxStackSize: 20
            );

            _inventoryComp.AddItem(itemDef, 4, out _);
            Assert.IsTrue(_inventoryComp.FindItem(itemDef, out ItemInstance foundInstance));

            bool removed = _inventoryComp.RemoveItemByInstanceId(foundInstance.InstanceId, 10);
            Assert.IsFalse(removed);
            Assert.AreEqual(4, _inventoryComp.GetTotalItemCount(itemDef));
        }

        [Test]
        public void ClearInventoryEmptiesAllSlots_CorrectlyResetsState()
        {
            _inventoryComp.SetCapacity(10);

            var itemA = ItemDefinition.Create("Item_A", "Item A");
            var itemB = ItemDefinition.Create("Item_B", "Item B");

            _inventoryComp.AddItem(itemA, 1, out _);
            _inventoryComp.AddItem(itemB, 1, out _);
            Assert.AreEqual(2, _inventoryComp.UsedSlotCount);

            _inventoryComp.ClearInventory();

            Assert.AreEqual(0, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(10, _inventoryComp.FreeSlotCount);
            Assert.AreEqual(0, _inventoryComp.Slots.Count);
        }

        [Test]
        public void SetCapacity_ClampsToAtLeastOneAndNotifies()
        {
            bool changedFired = false;
            _inventoryComp.OnInventoryChanged += () => changedFired = true;

            _inventoryComp.SetCapacity(-5);
            Assert.AreEqual(1, _inventoryComp.Capacity);
            Assert.IsTrue(changedFired);

            changedFired = false;
            _inventoryComp.SetCapacity(1); // Same capacity, should not fire again
            Assert.IsFalse(changedFired);
        }

        [Test]
        public void ItemDefinitionValidation_ValidatesCorrectly()
        {
            var validDef = ItemDefinition.Create(
                itemId: "Valid_Item",
                displayName: "Valid Item",
                version: 1,
                isStackable: true,
                maxStackSize: 10,
                weight: 0.5f,
                baseValue: 25
            );

            Assert.IsTrue(validDef.IsValidDefinition(out string error));
            Assert.IsNull(error);
            Assert.IsTrue(validDef.ValidateDefinition());

            var emptyIdDef = ItemDefinition.Create("", "Display");
            Assert.IsFalse(emptyIdDef.IsValidDefinition(out error));
            Assert.IsNotNull(error);

            var emptyNameDef = ItemDefinition.Create("Valid_Id", "");
            Assert.IsFalse(emptyNameDef.IsValidDefinition(out error));

            var badVersionDef = ItemDefinition.Create("Valid_Id", "Display", version: 0);
            Assert.IsFalse(badVersionDef.IsValidDefinition(out error));

            var badStackDef = ItemDefinition.Create("Valid_Id", "Display", isStackable: true, maxStackSize: 0);
            Assert.IsFalse(badStackDef.IsValidDefinition(out error));

            var badWeightDef = ItemDefinition.Create("Valid_Id", "Display", weight: -1f);
            Assert.IsFalse(badWeightDef.IsValidDefinition(out error));

            var badValueDef = ItemDefinition.Create("Valid_Id", "Display", baseValue: -1);
            Assert.IsFalse(badValueDef.IsValidDefinition(out error));
        }

        [Test]
        public void ItemDefinitionFactories_ProduceValidDefinitions()
        {
            var consumable = ItemDefinition.CreateTestConsumableDefinition();
            Assert.IsNotNull(consumable);
            Assert.AreEqual("TestConsumableItem", consumable.ItemId);
            Assert.AreEqual(ShadowSlaveItemType.Consumable, consumable.ItemType);
            Assert.IsTrue(consumable.IsStackable);
            Assert.IsTrue(consumable.IsValidDefinition(out _));

            var quest = ItemDefinition.CreateTestQuestItemDefinition();
            Assert.IsNotNull(quest);
            Assert.AreEqual("TestQuestItem", quest.ItemId);
            Assert.AreEqual(ShadowSlaveItemType.Quest, quest.ItemType);
            Assert.IsFalse(quest.IsStackable);
            Assert.IsTrue(quest.IsValidDefinition(out _));
        }

        [Test]
        public void ItemInstance_DynamicPropertiesAndCanStackWith_Validated()
        {
            var stackDef = ItemDefinition.Create("Stack_Item", "Stack Item", isStackable: true, maxStackSize: 10);
            var nonStackDef = ItemDefinition.Create("Single_Item", "Single Item", isStackable: false, maxStackSize: 1);

            var propsA = new Dictionary<string, string> { { "Quality", "Rare" } };
            var propsB = new Dictionary<string, string> { { "Quality", "Rare" } };
            var propsC = new Dictionary<string, string> { { "Quality", "Epic" } };

            var instA = new ItemInstance(stackDef, 2, Guid.NewGuid(), propsA);
            var instB = new ItemInstance(stackDef, 3, Guid.NewGuid(), propsB);
            var instC = new ItemInstance(stackDef, 1, Guid.NewGuid(), propsC);
            var instPlain = new ItemInstance(stackDef, 1);
            var instNonStack = new ItemInstance(nonStackDef, 1);

            Assert.IsTrue(instA.CanStackWith(instB));
            Assert.IsFalse(instA.CanStackWith(instC));
            Assert.IsFalse(instA.CanStackWith(instPlain));
            Assert.IsFalse(instA.CanStackWith(instNonStack));
            Assert.IsFalse(instA.CanStackWith(null));

            Assert.AreEqual("Rare", instA.GetDynamicProperty("Quality"));
            instA.SetDynamicProperty("Enchanted", "True");
            Assert.AreEqual("True", instA.GetDynamicProperty("Enchanted"));
            Assert.IsNull(instA.GetDynamicProperty("NonExistent"));
        }

        [Test]
        public void AddItemInstance_RespectsDynamicPropertiesWhenStacking()
        {
            _inventoryComp.SetCapacity(5);

            var stackDef = ItemDefinition.Create("Mod_Item", "Mod Item", isStackable: true, maxStackSize: 10);

            var props1 = new Dictionary<string, string> { { "Gem", "Ruby" } };
            var props2 = new Dictionary<string, string> { { "Gem", "Sapphire" } };

            var inst1 = new ItemInstance(stackDef, 2, Guid.NewGuid(), props1);
            var inst2 = new ItemInstance(stackDef, 3, Guid.NewGuid(), props2);
            var inst1More = new ItemInstance(stackDef, 4, Guid.NewGuid(), props1);

            Assert.IsTrue(_inventoryComp.AddItemInstance(inst1, out int rem1));
            Assert.AreEqual(0, rem1);
            Assert.AreEqual(1, _inventoryComp.UsedSlotCount);

            // Different properties: must allocate a new slot
            Assert.IsTrue(_inventoryComp.AddItemInstance(inst2, out int rem2));
            Assert.AreEqual(0, rem2);
            Assert.AreEqual(2, _inventoryComp.UsedSlotCount);

            // Same properties as inst1: should stack into first slot
            Assert.IsTrue(_inventoryComp.AddItemInstance(inst1More, out int rem3));
            Assert.AreEqual(0, rem3);
            Assert.AreEqual(2, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(6, _inventoryComp.Slots[0].Quantity);
        }

        [Test]
        public void InventoryEvents_FireWithCorrectParameters()
        {
            _inventoryComp.SetCapacity(5);

            var stackDef = ItemDefinition.Create("Event_Item", "Event Item", isStackable: true, maxStackSize: 10);

            int addEventCount = 0;
            int lastAddedQty = 0;
            int removeEventCount = 0;
            int lastRemovedQty = 0;
            int changedCount = 0;

            _inventoryComp.OnItemAdded += (inst, qty) =>
            {
                addEventCount++;
                lastAddedQty = qty;
            };

            _inventoryComp.OnItemRemoved += (inst, qty) =>
            {
                removeEventCount++;
                lastRemovedQty = qty;
            };

            _inventoryComp.OnInventoryChanged += () => changedCount++;

            _inventoryComp.AddItem(stackDef, 3, out _);
            Assert.AreEqual(1, addEventCount);
            Assert.AreEqual(3, lastAddedQty);
            Assert.AreEqual(1, changedCount);

            _inventoryComp.AddItem(stackDef, 2, out _);
            Assert.AreEqual(2, addEventCount);
            Assert.AreEqual(2, lastAddedQty);
            Assert.AreEqual(2, changedCount);

            _inventoryComp.RemoveItem(stackDef, 4);
            Assert.AreEqual(1, removeEventCount);
            Assert.AreEqual(4, lastRemovedQty);
            Assert.AreEqual(3, changedCount);
        }

        [Test]
        public void InventoryEvents_SubscriberExceptionDoesNotAbortOperation()
        {
            _inventoryComp.SetCapacity(5);

            var itemDef = ItemDefinition.Create("Safe_Item", "Safe Item", isStackable: true, maxStackSize: 10);

            _inventoryComp.OnItemAdded += (inst, qty) => throw new InvalidOperationException("Subscriber failure!");
            _inventoryComp.OnItemRemoved += (inst, qty) => throw new InvalidOperationException("Subscriber failure!");
            _inventoryComp.OnInventoryChanged += () => throw new InvalidOperationException("Subscriber failure!");

            Assert.DoesNotThrow(() =>
            {
                bool added = _inventoryComp.AddItem(itemDef, 5, out _);
                Assert.IsTrue(added);
            });

            Assert.AreEqual(5, _inventoryComp.GetTotalItemCount(itemDef));

            Assert.DoesNotThrow(() =>
            {
                bool removed = _inventoryComp.RemoveItem(itemDef, 2);
                Assert.IsTrue(removed);
            });

            Assert.AreEqual(3, _inventoryComp.GetTotalItemCount(itemDef));
        }

        [Test]
        public void InventoryReentrancyGuard_PreventsRecursiveMutations()
        {
            _inventoryComp.SetCapacity(5);

            var itemDef = ItemDefinition.Create("Reentrant_Item", "Reentrant Item", isStackable: true, maxStackSize: 10);
            var extraDef = ItemDefinition.Create("Extra_Item", "Extra Item");

            bool reentrantAttemptMade = false;
            bool reentrantAddResult = true;

            _inventoryComp.OnItemAdded += (inst, qty) =>
            {
                if (!reentrantAttemptMade)
                {
                    reentrantAttemptMade = true;
                    // Attempt recursive mutation from inside event callback
                    reentrantAddResult = _inventoryComp.AddItem(extraDef, 1, out _);
                }
            };

            bool initialAdd = _inventoryComp.AddItem(itemDef, 3, out _);

            Assert.IsTrue(initialAdd);
            Assert.IsTrue(reentrantAttemptMade);
            Assert.IsFalse(reentrantAddResult); // Reentrant call must be rejected
            Assert.AreEqual(1, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(3, _inventoryComp.GetTotalItemCount(itemDef));
            Assert.AreEqual(0, _inventoryComp.GetTotalItemCount(extraDef));
        }

        [Test]
        public void RestoreInventory_ReplacesSlotsAndPreservesInstanceData()
        {
            _inventoryComp.SetCapacity(5);

            var itemDef = ItemDefinition.Create("Saved_Item", "Saved Item", isStackable: true, maxStackSize: 10);
            _inventoryComp.AddItem(itemDef, 2, out _);

            var savedGuid1 = Guid.NewGuid();
            var savedGuid2 = Guid.NewGuid();
            var savedProps = new Dictionary<string, string> { { "Bound", "Soul" } };

            var restoredList = new List<ItemInstance>
            {
                new ItemInstance(itemDef, 7, savedGuid1, savedProps),
                new ItemInstance(itemDef, 3, savedGuid2)
            };

            bool changedFired = false;
            _inventoryComp.OnInventoryChanged += () => changedFired = true;

            _inventoryComp.RestoreInventory(restoredList, 15);

            Assert.IsTrue(changedFired);
            Assert.AreEqual(15, _inventoryComp.Capacity);
            Assert.AreEqual(2, _inventoryComp.UsedSlotCount);
            Assert.AreEqual(10, _inventoryComp.GetTotalItemCount(itemDef));

            Assert.IsTrue(_inventoryComp.FindItemByInstanceId(savedGuid1, out ItemInstance found1));
            Assert.AreEqual(7, found1.Quantity);
            Assert.AreEqual("Soul", found1.GetDynamicProperty("Bound"));

            Assert.IsTrue(_inventoryComp.FindItemByInstanceId(savedGuid2, out ItemInstance found2));
            Assert.AreEqual(3, found2.Quantity);
        }

        [Test]
        public void GetItemsByType_FiltersCorrectly()
        {
            _inventoryComp.SetCapacity(10);

            var consumableDef = ItemDefinition.Create("Cons_Item", "Cons", itemType: ShadowSlaveItemType.Consumable);
            var equipDef = ItemDefinition.Create("Equip_Item", "Equip", itemType: ShadowSlaveItemType.Equipment);
            var questDef = ItemDefinition.Create("Quest_Item", "Quest", itemType: ShadowSlaveItemType.Quest);

            _inventoryComp.AddItem(consumableDef, 1, out _);
            _inventoryComp.AddItem(equipDef, 1, out _);
            _inventoryComp.AddItem(questDef, 1, out _);

            var consumables = _inventoryComp.GetItemsByType(ShadowSlaveItemType.Consumable);
            var equips = _inventoryComp.GetItemsByType(ShadowSlaveItemType.Equipment);
            var materials = _inventoryComp.GetItemsByType(ShadowSlaveItemType.Material);

            Assert.AreEqual(1, consumables.Count);
            Assert.AreEqual("Cons_Item", consumables[0].ItemDefinition.ItemId);

            Assert.AreEqual(1, equips.Count);
            Assert.AreEqual("Equip_Item", equips[0].ItemDefinition.ItemId);

            Assert.AreEqual(0, materials.Count);
        }

        [Test]
        public void EdgeCases_NullParametersAndEmptyGuids_HandledSafely()
        {
            Assert.IsFalse(_inventoryComp.AddItem(null, 1, out int rem));
            Assert.AreEqual(1, rem);

            Assert.IsFalse(_inventoryComp.AddItem(_inventoryComp.Slots.Count > 0 ? _inventoryComp.Slots[0].ItemDefinition : null, 0, out rem));
            Assert.AreEqual(0, rem);

            Assert.IsFalse(_inventoryComp.RemoveItem(null, 1));
            Assert.IsFalse(_inventoryComp.RemoveItemByInstanceId(Guid.Empty, 1));
            Assert.IsFalse(_inventoryComp.HasItem(null));
            Assert.IsFalse(_inventoryComp.HasItemByInstanceId(Guid.Empty));
            Assert.IsFalse(_inventoryComp.FindItem(null, out _));
            Assert.IsFalse(_inventoryComp.FindItemByInstanceId(Guid.Empty, out _));
            Assert.AreEqual(0, _inventoryComp.GetTotalItemCount(null));

            // AddItemSimple convenience
            var simpleDef = ItemDefinition.Create("Simple_Item", "Simple Item");
            Assert.IsTrue(_inventoryComp.AddItemSimple(simpleDef, 1));
            Assert.AreEqual(1, _inventoryComp.GetTotalItemCount(simpleDef));
        }

        [Test]
        public void CharacterBase_Integration_InventoryComponentWiredCorrectly()
        {
            var charGo = new GameObject("TestCharacter");
            charGo.AddComponent<AttributeComponent>();
            charGo.AddComponent<ProgressionComponent>();
            var invOnChar = charGo.AddComponent<InventoryComponent>();
            var character = charGo.AddComponent<CharacterBase>();

            // Awake is executed upon AddComponent in Unity edit mode, resolving components
            Assert.IsNotNull(character.InventoryComponent);
            Assert.AreSame(invOnChar, character.InventoryComponent);

            Object.DestroyImmediate(charGo);
        }

        [Test]
        public void DowncastingSlotsCollection_ProtectedAgainstMutation()
        {
            var stackDef = ItemDefinition.Create("Protected_Item", "Protected Item");
            _inventoryComp.AddItem(stackDef, 1, out _);

            Assert.Throws<InvalidCastException>(() =>
            {
                var mutableList = (List<ItemInstance>)_inventoryComp.Slots;
                mutableList.Clear();
            });

            Assert.Throws<InvalidCastException>(() =>
            {
                var mutableList = (List<ItemInstance>)_inventoryComp.GetSlots();
                mutableList.Clear();
            });

            Assert.AreEqual(1, _inventoryComp.UsedSlotCount);
        }
    }
}
