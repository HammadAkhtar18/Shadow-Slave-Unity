using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ShadowSlave.Aspects;
using ShadowSlave.Attributes;
using ShadowSlave.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class AspectTests
    {
        private GameObject _actor;
        private AspectComponent _aspectComponent;
        private readonly List<Object> _createdAssets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _actor = new GameObject("AspectTestActor");
            _aspectComponent = _actor.AddComponent<AspectComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_actor != null)
            {
                Object.DestroyImmediate(_actor);
            }

            for (int i = 0; i < _createdAssets.Count; i++)
            {
                if (_createdAssets[i] != null)
                {
                    Object.DestroyImmediate(_createdAssets[i]);
                }
            }
            _createdAssets.Clear();
        }

        private T CreateTestAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _createdAssets.Add(asset);
            return asset;
        }

        /// <summary>
        /// Test helper simulating Unity deserialization/Inspector authoring for serialized backing fields.
        /// Prevents polluting production ScriptableObject APIs with general runtime mutation setters.
        /// </summary>
        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Serialized field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }

        /* --- Aspect Rank Tests --- */

        [Test]
        public void AspectRank_EnumValues_AreDistinctAndOrderedCorrectly()
        {
            Assert.AreEqual(0, (byte)AspectRank.Unknown);
            Assert.AreEqual(1, (byte)AspectRank.Dormant);
            Assert.AreEqual(2, (byte)AspectRank.Awakened);
            Assert.AreEqual(3, (byte)AspectRank.Ascended);
            Assert.AreEqual(4, (byte)AspectRank.Transcendent);
            Assert.AreEqual(5, (byte)AspectRank.Supreme);
            Assert.AreEqual(6, (byte)AspectRank.Sacred);
            Assert.AreEqual(7, (byte)AspectRank.Divine);

            Assert.IsTrue(AspectRank.Unknown < AspectRank.Dormant);
            Assert.IsTrue(AspectRank.Dormant < AspectRank.Awakened);
            Assert.IsTrue(AspectRank.Awakened < AspectRank.Ascended);
            Assert.IsTrue(AspectRank.Ascended < AspectRank.Transcendent);
            Assert.IsTrue(AspectRank.Transcendent < AspectRank.Supreme);
            Assert.IsTrue(AspectRank.Supreme < AspectRank.Sacred);
            Assert.IsTrue(AspectRank.Sacred < AspectRank.Divine);
        }

        [Test]
        public void AspectRank_IsDistinctAndIndependent_FromCharacterRank()
        {
            // Verifies that AspectRank and ShadowSlaveCharacterRank are separate types
            AspectRank aspectRank = AspectRank.Divine;
            ShadowSlaveCharacterRank charRank = ShadowSlaveCharacterRank.Dormant;

            // Sunny in early novel: Dormant character rank, Divine aspect rank
            Assert.AreNotEqual(typeof(AspectRank), typeof(ShadowSlaveCharacterRank));
            Assert.AreEqual(7, (byte)aspectRank);
            Assert.AreEqual(1, (byte)charRank);
        }

        /* --- Aspect Definition Tests --- */

        [Test]
        public void AspectDefinition_DefaultState_HasValidCleanIdentity()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();

            Assert.AreEqual(string.Empty, def.AspectId);
            Assert.AreEqual(string.Empty, def.DisplayName);
            Assert.AreEqual(string.Empty, def.Description);
            Assert.AreEqual(AspectRank.Unknown, def.AspectRank);
            Assert.IsFalse(def.HasKnownAspectRank());
            Assert.IsNotNull(def.AbilityDefinitions);
            Assert.AreEqual(0, def.AbilityCount);
            Assert.IsNull(def.FlawDefinition);
            Assert.IsFalse(def.HasFlaw());
            Assert.IsNotNull(def.Metadata);
            Assert.AreEqual(0, def.Metadata.Count);
            Assert.AreEqual(string.Empty, def.CanonProvenance);
        }

        [Test]
        public void AspectDefinition_AspectIdAndRank_CanBeStoredAndRetrieved()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_shadow_slave");
            SetField(def, "displayName", "Shadow Slave");
            SetField(def, "description", "Slave of shadows, master of nothing.");
            SetField(def, "aspectRank", AspectRank.Divine);

            Assert.AreEqual("aspect_shadow_slave", def.AspectId);
            Assert.AreEqual("Shadow Slave", def.DisplayName);
            Assert.AreEqual("Slave of shadows, master of nothing.", def.Description);
            Assert.AreEqual(AspectRank.Divine, def.AspectRank);
            Assert.IsTrue(def.HasKnownAspectRank());
        }

        [Test]
        public void AspectDefinition_AbilityDefinitions_AssignmentAndLookup()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();

            AspectAbilityDefinition ability1 = CreateTestAsset<AspectAbilityDefinition>();
            ability1.SetAbilityId("ability_shadow_control");
            SetField(ability1, "displayName", "Shadow Control");
            SetField(ability1, "requiredCharacterRank", ShadowSlaveCharacterRank.Dormant);
            SetField(ability1, "baseEssenceCost", 5f);

            AspectAbilityDefinition ability2 = CreateTestAsset<AspectAbilityDefinition>();
            ability2.SetAbilityId("ability_shadow_step");
            SetField(ability2, "displayName", "Shadow Step");
            SetField(ability2, "requiredCharacterRank", ShadowSlaveCharacterRank.Awakened);
            SetField(ability2, "baseEssenceCost", 15f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { ability1, ability2 });

            Assert.AreEqual(2, def.AbilityCount);
            Assert.AreSame(ability1, def.AbilityDefinitions[0]);
            Assert.AreSame(ability2, def.AbilityDefinitions[1]);

            // Lookup existing
            AspectAbilityDefinition found1 = def.FindAbilityById("ability_shadow_control");
            Assert.IsNotNull(found1);
            Assert.AreSame(ability1, found1);

            AspectAbilityDefinition found2 = def.FindAbilityById("ability_shadow_step");
            Assert.IsNotNull(found2);
            Assert.AreSame(ability2, found2);
        }

        [Test]
        public void AspectDefinition_FindAbilityById_MissingReturnsNull()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();

            AspectAbilityDefinition ability = CreateTestAsset<AspectAbilityDefinition>();
            ability.SetAbilityId("ability_shadow_manifestation");
            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { ability });

            Assert.IsNull(def.FindAbilityById("non_existent"));
            Assert.IsNull(def.FindAbilityById(null));
            Assert.IsNull(def.FindAbilityById(""));
        }

        [Test]
        public void AspectDefinition_OptionalFlawDefinition_Works()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            Assert.IsNull(def.FlawDefinition);
            Assert.IsFalse(def.HasFlaw());

            FlawDefinition flaw = CreateTestAsset<FlawDefinition>();
            flaw.SetFlawId("flaw_clear_conscience");
            SetField(flaw, "displayName", "Clear Conscience");
            SetField(flaw, "description", "Cannot tell a lie.");

            SetField(def, "flawDefinition", flaw);

            Assert.IsNotNull(def.FlawDefinition);
            Assert.IsTrue(def.HasFlaw());
            Assert.AreSame(flaw, def.FlawDefinition);
            Assert.AreEqual("flaw_clear_conscience", def.FlawDefinition.FlawId);
            Assert.AreEqual("Clear Conscience", def.FlawDefinition.DisplayName);
        }

        [Test]
        public void AspectAbilityDefinition_PropertiesAndRankRequirement()
        {
            AspectAbilityDefinition ability = CreateTestAsset<AspectAbilityDefinition>();
            ability.SetAbilityId("ability_shadow_manifestation");
            SetField(ability, "displayName", "Shadow Manifestation");
            SetField(ability, "description", "Solidify shadows into physical objects.");
            SetField(ability, "requiredCharacterRank", ShadowSlaveCharacterRank.Ascended);
            SetField(ability, "baseEssenceCost", 25f);

            Assert.AreEqual("ability_shadow_manifestation", ability.AbilityId);
            Assert.AreEqual("Shadow Manifestation", ability.DisplayName);
            Assert.AreEqual("Solidify shadows into physical objects.", ability.Description);
            Assert.AreEqual(ShadowSlaveCharacterRank.Ascended, ability.RequiredCharacterRank);
            Assert.AreEqual(25f, ability.BaseEssenceCost, 0.001f);
            Assert.IsTrue(ability.HasRankRequirement());

            // Unknown rank requirement means no prerequisite
            AspectAbilityDefinition innateAbility = CreateTestAsset<AspectAbilityDefinition>();
            SetField(innateAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);
            Assert.IsFalse(innateAbility.HasRankRequirement());
        }

        /* --- Boundary & Static Integrity Tests --- */

        [Test]
        public void AspectComponent_Operations_DoNotMutate_StaticDefinitionData()
        {
            // Prepare static definition data
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_shadow_slave");
            SetField(def, "displayName", "Shadow Slave");
            SetField(def, "description", "Slave of shadows, master of nothing.");
            SetField(def, "aspectRank", AspectRank.Divine);
            SetField(def, "canonProvenance", "Novel Chapter 13");

            FlawDefinition flaw = CreateTestAsset<FlawDefinition>();
            flaw.SetFlawId("flaw_clear_conscience");
            SetField(flaw, "displayName", "Clear Conscience");
            SetField(def, "flawDefinition", flaw);

            AspectAbilityDefinition ability = CreateTestAsset<AspectAbilityDefinition>();
            ability.SetAbilityId("ability_shadow_control");
            SetField(ability, "displayName", "Shadow Control");
            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { ability });

            AspectDefinition otherDef = CreateTestAsset<AspectDefinition>();
            otherDef.SetAspectId("aspect_other");
            SetField(otherDef, "aspectRank", AspectRank.Awakened);

            // Execute component operations
            _aspectComponent.SetAspectDefinition(def);
            Assert.IsTrue(_aspectComponent.HasAspect());
            Assert.AreEqual(AspectRank.Divine, _aspectComponent.GetAspectRank());
            Assert.AreSame(def, _aspectComponent.GetAspectDefinition());

            // Rebind to other definition and clear
            _aspectComponent.SetAspectDefinition(otherDef);
            _aspectComponent.SetAspectDefinition(null);

            // Assert that the static definition asset was not mutated in any way
            Assert.AreEqual("aspect_shadow_slave", def.AspectId);
            Assert.AreEqual("Shadow Slave", def.DisplayName);
            Assert.AreEqual("Slave of shadows, master of nothing.", def.Description);
            Assert.AreEqual(AspectRank.Divine, def.AspectRank);
            Assert.AreEqual("Novel Chapter 13", def.CanonProvenance);
            Assert.AreSame(flaw, def.FlawDefinition);
            Assert.AreEqual("flaw_clear_conscience", def.FlawDefinition.FlawId);
            Assert.AreEqual(1, def.AbilityCount);
            Assert.AreSame(ability, def.AbilityDefinitions[0]);
            Assert.AreEqual("ability_shadow_control", def.AbilityDefinitions[0].AbilityId);
        }

        [Test]
        public void AspectDefinitions_DoNotExpose_UnintendedPublicSetters()
        {
            // Verify AspectDefinition properties are read-only
            Assert.IsFalse(typeof(AspectDefinition).GetProperty("DisplayName")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectDefinition).GetProperty("Description")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectDefinition).GetProperty("AspectRank")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectDefinition).GetProperty("AbilityDefinitions")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectDefinition).GetProperty("FlawDefinition")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectDefinition).GetProperty("Metadata")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectDefinition).GetProperty("CanonProvenance")?.CanWrite ?? true);

            // Verify AspectAbilityDefinition properties are read-only
            Assert.IsFalse(typeof(AspectAbilityDefinition).GetProperty("DisplayName")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectAbilityDefinition).GetProperty("Description")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectAbilityDefinition).GetProperty("RequiredCharacterRank")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectAbilityDefinition).GetProperty("BaseEssenceCost")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectAbilityDefinition).GetProperty("Metadata")?.CanWrite ?? true);
            Assert.IsFalse(typeof(AspectAbilityDefinition).GetProperty("CanonProvenance")?.CanWrite ?? true);

            // Verify FlawDefinition properties are read-only
            Assert.IsFalse(typeof(FlawDefinition).GetProperty("DisplayName")?.CanWrite ?? true);
            Assert.IsFalse(typeof(FlawDefinition).GetProperty("Description")?.CanWrite ?? true);
            Assert.IsFalse(typeof(FlawDefinition).GetProperty("Metadata")?.CanWrite ?? true);
            Assert.IsFalse(typeof(FlawDefinition).GetProperty("CanonProvenance")?.CanWrite ?? true);

            // Verify public "Set*" methods: only explicit UE5 identity compatibility setters are exposed
            var aspectSetMethods = typeof(AspectDefinition)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("Set", StringComparison.Ordinal) && !m.IsSpecialName)
                .Select(m => m.Name)
                .ToList();
            CollectionAssert.AreEqual(new[] { "SetAspectId" }, aspectSetMethods);

            var abilitySetMethods = typeof(AspectAbilityDefinition)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("Set", StringComparison.Ordinal) && !m.IsSpecialName)
                .Select(m => m.Name)
                .ToList();
            CollectionAssert.AreEqual(new[] { "SetAbilityId" }, abilitySetMethods);

            var flawSetMethods = typeof(FlawDefinition)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("Set", StringComparison.Ordinal) && !m.IsSpecialName)
                .Select(m => m.Name)
                .ToList();
            CollectionAssert.AreEqual(new[] { "SetFlawId" }, flawSetMethods);
        }

        /* --- Aspect Component Binding Tests --- */

        [Test]
        public void AspectComponent_DefaultState_HasNoAspect()
        {
            Assert.IsFalse(_aspectComponent.HasAspect());
            Assert.IsNull(_aspectComponent.GetAspectDefinition());
            Assert.AreEqual(AspectRank.Unknown, _aspectComponent.GetAspectRank());
            Assert.IsNotNull(_aspectComponent.GetAbilityInstances());
            Assert.AreEqual(0, _aspectComponent.GetAbilityInstances().Count);
        }

        [Test]
        public void AspectComponent_SetAspectDefinition_BindsDefinitionCorrectly()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_shadow_slave");
            SetField(def, "aspectRank", AspectRank.Divine);

            int eventCount = 0;
            AspectDefinition recordedNew = null;
            AspectDefinition recordedOld = null;

            _aspectComponent.OnAspectChanged += (newDef, oldDef) =>
            {
                eventCount++;
                recordedNew = newDef;
                recordedOld = oldDef;
            };

            bool success = _aspectComponent.SetAspectDefinition(def);
            Assert.IsTrue(success);
            Assert.IsTrue(_aspectComponent.HasAspect());
            Assert.AreSame(def, _aspectComponent.GetAspectDefinition());
            Assert.AreEqual(AspectRank.Divine, _aspectComponent.GetAspectRank());

            Assert.AreEqual(1, eventCount);
            Assert.AreSame(def, recordedNew);
            Assert.IsNull(recordedOld);
        }

        [Test]
        public void AspectComponent_SetAspectDefinition_SameDefinition_DoesNotFireEvent()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            _aspectComponent.SetAspectDefinition(def);

            int eventCount = 0;
            _aspectComponent.OnAspectChanged += (_, __) => eventCount++;

            bool success = _aspectComponent.SetAspectDefinition(def);
            Assert.IsTrue(success);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void AspectComponent_SetAspectDefinition_ReplacingAspect_UpdatesCorrectlyAndFiresEvent()
        {
            AspectDefinition aspectA = CreateTestAsset<AspectDefinition>();
            aspectA.SetAspectId("aspect_a");
            SetField(aspectA, "aspectRank", AspectRank.Awakened);

            AspectDefinition aspectB = CreateTestAsset<AspectDefinition>();
            aspectB.SetAspectId("aspect_b");
            SetField(aspectB, "aspectRank", AspectRank.Ascended);

            _aspectComponent.SetAspectDefinition(aspectA);
            Assert.AreEqual(AspectRank.Awakened, _aspectComponent.GetAspectRank());

            int eventCount = 0;
            AspectDefinition recordedNew = null;
            AspectDefinition recordedOld = null;

            _aspectComponent.OnAspectChanged += (newDef, oldDef) =>
            {
                eventCount++;
                recordedNew = newDef;
                recordedOld = oldDef;
            };

            bool success = _aspectComponent.SetAspectDefinition(aspectB);
            Assert.IsTrue(success);
            Assert.AreSame(aspectB, _aspectComponent.GetAspectDefinition());
            Assert.AreEqual(AspectRank.Ascended, _aspectComponent.GetAspectRank());

            Assert.AreEqual(1, eventCount);
            Assert.AreSame(aspectB, recordedNew);
            Assert.AreSame(aspectA, recordedOld);
        }

        [Test]
        public void AspectComponent_SetAspectDefinition_SettingNull_ClearsAspect()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            SetField(def, "aspectRank", AspectRank.Transcendent);
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsTrue(_aspectComponent.HasAspect());

            bool success = _aspectComponent.SetAspectDefinition(null);
            Assert.IsTrue(success);
            Assert.IsFalse(_aspectComponent.HasAspect());
            Assert.IsNull(_aspectComponent.GetAspectDefinition());
            Assert.AreEqual(AspectRank.Unknown, _aspectComponent.GetAspectRank());
        }

        /* --- Phase 2: Runtime Ability Instance Tests --- */

        private (AspectDefinition aspect, AspectAbilityDefinition ability1, AspectAbilityDefinition ability2) CreateTwoAbilityAspect()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_shadow_slave");
            SetField(def, "aspectRank", AspectRank.Divine);

            AspectAbilityDefinition ability1 = CreateTestAsset<AspectAbilityDefinition>();
            ability1.SetAbilityId("ability_shadow_control");
            SetField(ability1, "displayName", "Shadow Control");
            SetField(ability1, "requiredCharacterRank", ShadowSlaveCharacterRank.Dormant);
            SetField(ability1, "baseEssenceCost", 5f);

            AspectAbilityDefinition ability2 = CreateTestAsset<AspectAbilityDefinition>();
            ability2.SetAbilityId("ability_shadow_step");
            SetField(ability2, "displayName", "Shadow Step");
            SetField(ability2, "requiredCharacterRank", ShadowSlaveCharacterRank.Awakened);
            SetField(ability2, "baseEssenceCost", 15f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { ability1, ability2 });
            return (def, ability1, ability2);
        }

        [Test]
        public void AspectBinding_CreatesOneRuntimeInstance_PerAbilityDefinition()
        {
            var (def, ability1, ability2) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            IReadOnlyList<AspectAbilityInstance> instances = _aspectComponent.GetAbilityInstances();
            Assert.IsNotNull(instances);
            Assert.AreEqual(2, instances.Count);
            Assert.AreSame(ability1, instances[0].AbilityDefinition);
            Assert.AreSame(ability2, instances[1].AbilityDefinition);
            Assert.AreEqual("ability_shadow_control", instances[0].AbilityId);
            Assert.AreEqual("ability_shadow_step", instances[1].AbilityId);
            Assert.IsTrue(instances[0].IsValid);
            Assert.IsTrue(instances[1].IsValid);
        }

        [Test]
        public void NewRuntimeInstances_StartLocked()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            IReadOnlyList<AspectAbilityInstance> instances = _aspectComponent.GetAbilityInstances();
            Assert.AreEqual(2, instances.Count);
            Assert.IsFalse(instances[0].IsUnlocked);
            Assert.IsFalse(instances[1].IsUnlocked);
        }

        [Test]
        public void NewRuntimeInstances_StartInactive()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            IReadOnlyList<AspectAbilityInstance> instances = _aspectComponent.GetAbilityInstances();
            Assert.AreEqual(2, instances.Count);
            Assert.IsFalse(instances[0].IsActive);
            Assert.IsFalse(instances[1].IsActive);
        }

        [Test]
        public void FindAbilityInstance_FindsExistingAbility()
        {
            var (def, ability1, ability2) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            AspectAbilityInstance instance1 = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.IsNotNull(instance1);
            Assert.AreEqual("ability_shadow_control", instance1.AbilityId);
            Assert.AreSame(ability1, instance1.AbilityDefinition);

            AspectAbilityInstance instance2 = _aspectComponent.FindAbilityInstance("ability_shadow_step");
            Assert.IsNotNull(instance2);
            Assert.AreEqual("ability_shadow_step", instance2.AbilityId);
            Assert.AreSame(ability2, instance2.AbilityDefinition);
        }

        [Test]
        public void FindAbilityInstance_MissingReturnsNullSafely()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsNull(_aspectComponent.FindAbilityInstance("non_existent"));
            Assert.IsNull(_aspectComponent.FindAbilityInstance(null));
            Assert.IsNull(_aspectComponent.FindAbilityInstance(string.Empty));
        }

        [Test]
        public void IsAbilityUnlocked_ReturnsFalseInitially()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_step"));
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("non_existent"));
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked(null));
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked(string.Empty));
        }

        [Test]
        public void IsAbilityActive_ReturnsFalseInitially()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"));
            Assert.IsFalse(_aspectComponent.IsAbilityActive("non_existent"));
            Assert.IsFalse(_aspectComponent.IsAbilityActive(null));
            Assert.IsFalse(_aspectComponent.IsAbilityActive(string.Empty));
        }

        [Test]
        public void UnlockAbility_ChangesRuntimeState()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            bool success = _aspectComponent.UnlockAbility("ability_shadow_control");
            Assert.IsTrue(success);
            Assert.IsTrue(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
            Assert.IsTrue(_aspectComponent.FindAbilityInstance("ability_shadow_control").IsUnlocked);

            // Other ability remains locked
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_step"));

            // Non-existent ability fails safely
            Assert.IsFalse(_aspectComponent.UnlockAbility("non_existent"));
            Assert.IsFalse(_aspectComponent.UnlockAbility(null));
            Assert.IsFalse(_aspectComponent.UnlockAbility(string.Empty));
        }

        [Test]
        public void UnlockAbility_FiresEventOnce_OnActualTransition()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            int eventCount = 0;
            AspectAbilityInstance recordedInstance = null;

            _aspectComponent.OnAbilityUnlocked += inst =>
            {
                eventCount++;
                recordedInstance = inst;
            };

            bool success = _aspectComponent.UnlockAbility("ability_shadow_control");
            Assert.IsTrue(success);
            Assert.AreEqual(1, eventCount);
            Assert.IsNotNull(recordedInstance);
            Assert.AreEqual("ability_shadow_control", recordedInstance.AbilityId);
        }

        [Test]
        public void UnlockAbility_AlreadyUnlocked_DoesNotFireEventAgain()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            // Initial unlock
            bool firstUnlock = _aspectComponent.UnlockAbility("ability_shadow_control");
            Assert.IsTrue(firstUnlock);

            int eventCount = 0;
            _aspectComponent.OnAbilityUnlocked += _ => eventCount++;

            // Redundant unlock
            bool secondUnlock = _aspectComponent.UnlockAbility("ability_shadow_control");
            Assert.IsFalse(secondUnlock, "Redundant unlock should return false because no state transition occurred.");
            Assert.AreEqual(0, eventCount, "OnAbilityUnlocked must not fire for an already unlocked ability.");
            Assert.IsTrue(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
        }

        [Test]
        public void DeactivateAbility_ClearsActiveStateSafely()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.IsNotNull(instance);

            // Simulate active state for testing deactivation transition
            SetField(instance, "isActive", true);
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_control"));

            int eventCount = 0;
            AspectAbilityInstance recordedDeactivated = null;
            _aspectComponent.OnAbilityDeactivated += inst =>
            {
                eventCount++;
                recordedDeactivated = inst;
            };

            bool deactivated = _aspectComponent.DeactivateAbility("ability_shadow_control");
            Assert.IsTrue(deactivated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.AreEqual(1, eventCount);
            Assert.AreSame(instance, recordedDeactivated);

            // Redundant deactivation safely succeeds and does not refire event
            bool redundantDeactivate = _aspectComponent.DeactivateAbility("ability_shadow_control");
            Assert.IsTrue(redundantDeactivate, "DeactivateAbility safely succeeds when the ability exists.");
            Assert.AreEqual(1, eventCount, "OnAbilityDeactivated must not refire if already inactive.");

            // Missing ability fails safely
            Assert.IsFalse(_aspectComponent.DeactivateAbility("non_existent"));
            Assert.IsFalse(_aspectComponent.DeactivateAbility(null));
            Assert.IsFalse(_aspectComponent.DeactivateAbility(string.Empty));
        }

        [Test]
        public void ReplacingAspect_RebuildsRuntimeInstanceCollection()
        {
            var (aspectA, _, _) = CreateTwoAbilityAspect();

            AspectDefinition aspectB = CreateTestAsset<AspectDefinition>();
            aspectB.SetAspectId("aspect_b");
            AspectAbilityDefinition abilityB = CreateTestAsset<AspectAbilityDefinition>();
            abilityB.SetAbilityId("ability_solar_flare");
            SetField(aspectB, "abilityDefinitions", new List<AspectAbilityDefinition> { abilityB });

            _aspectComponent.SetAspectDefinition(aspectA);
            _aspectComponent.UnlockAbility("ability_shadow_control");
            Assert.AreEqual(2, _aspectComponent.GetAbilityInstances().Count);
            Assert.IsTrue(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));

            // Replace aspect
            _aspectComponent.SetAspectDefinition(aspectB);

            IReadOnlyList<AspectAbilityInstance> instances = _aspectComponent.GetAbilityInstances();
            Assert.AreEqual(1, instances.Count);
            Assert.AreEqual("ability_solar_flare", instances[0].AbilityId);
            Assert.IsFalse(instances[0].IsUnlocked, "Abilities on a newly bound Aspect must start locked.");
            Assert.IsFalse(instances[0].IsActive, "Abilities on a newly bound Aspect must start inactive.");

            // Old ability instances are completely discarded
            Assert.IsNull(_aspectComponent.FindAbilityInstance("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
        }

        [Test]
        public void ClearingAspect_RemovesAllRuntimeInstances()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            Assert.AreEqual(2, _aspectComponent.GetAbilityInstances().Count);

            bool cleared = _aspectComponent.SetAspectDefinition(null);
            Assert.IsTrue(cleared);
            Assert.AreEqual(0, _aspectComponent.GetAbilityInstances().Count);
            Assert.IsNull(_aspectComponent.FindAbilityInstance("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
        }

        [Test]
        public void AspectRuntimeOperations_DoNotMutate_StaticAbilityDefinitions()
        {
            var (def, ability1, ability2) = CreateTwoAbilityAspect();

            string snapId1 = ability1.AbilityId;
            string snapName1 = ability1.DisplayName;
            string snapDesc1 = ability1.Description;
            ShadowSlaveCharacterRank snapRank1 = ability1.RequiredCharacterRank;
            float snapCost1 = ability1.BaseEssenceCost;

            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");
            _aspectComponent.DeactivateAbility("ability_shadow_control");

            AspectDefinition otherDef = CreateTestAsset<AspectDefinition>();
            _aspectComponent.SetAspectDefinition(otherDef);
            _aspectComponent.SetAspectDefinition(null);

            // Verify ability definition asset remains completely untouched
            Assert.AreEqual(snapId1, ability1.AbilityId);
            Assert.AreEqual(snapName1, ability1.DisplayName);
            Assert.AreEqual(snapDesc1, ability1.Description);
            Assert.AreEqual(snapRank1, ability1.RequiredCharacterRank);
            Assert.AreEqual(snapCost1, ability1.BaseEssenceCost, 0.001f);
        }

        /* --- Boundary Tests with other Components --- */

        [Test]
        public void AspectOperations_DoNotMutate_CharacterRank()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Ascended);

            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");
            _aspectComponent.DeactivateAbility("ability_shadow_control");

            Assert.AreEqual(ShadowSlaveCharacterRank.Ascended, prog.GetCharacterRank());
            Assert.AreEqual(AspectRank.Divine, _aspectComponent.GetAspectRank());
        }

        [Test]
        public void AspectOperations_DoNotMutate_SoulCores()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetMaxSoulCores(4);
            prog.SetSoulCoreCount(3);

            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");
            _aspectComponent.DeactivateAbility("ability_shadow_control");

            Assert.AreEqual(3, prog.GetSoulCoreCount());
            Assert.AreEqual(4, prog.GetMaxSoulCores());
        }

        [Test]
        public void AspectOperations_DoNotMutate_Attributes()
        {
            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);

            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");
            _aspectComponent.DeactivateAbility("ability_shadow_control");

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);
        }

        /* --- Phase 3: Ability Activation Prerequisites Tests --- */

        [Test]
        public void CanActivateAbility_MissingAbility_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.CanActivateAbility("non_existent"));
            Assert.IsFalse(_aspectComponent.CanActivateAbility(null));
            Assert.IsFalse(_aspectComponent.CanActivateAbility(string.Empty));
        }

        [Test]
        public void CanActivateAbility_LockedAbility_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            // Initially locked
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_shadow_control"));
        }

        [Test]
        public void CanActivateAbility_UnlockedZeroCostAbility_ReturnsTrue()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition freeAbility = CreateTestAsset<AspectAbilityDefinition>();
            freeAbility.SetAbilityId("ability_free");
            SetField(freeAbility, "baseEssenceCost", 0f);
            SetField(freeAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { freeAbility });
            _aspectComponent.SetAspectDefinition(def);

            _aspectComponent.UnlockAbility("ability_free");
            Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_free"));
        }

        [Test]
        public void CanActivateAbility_NegativeEssenceCost_ReturnsFalse()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition badAbility = CreateTestAsset<AspectAbilityDefinition>();
            badAbility.SetAbilityId("ability_negative_cost");
            SetField(badAbility, "baseEssenceCost", -10f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { badAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_negative_cost");

            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_negative_cost"));
        }

        [Test]
        public void CanActivateAbility_NonFiniteEssenceCost_ReturnsFalse()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition infAbility = CreateTestAsset<AspectAbilityDefinition>();
            infAbility.SetAbilityId("ability_infinity_cost");
            SetField(infAbility, "baseEssenceCost", float.PositiveInfinity);

            AspectAbilityDefinition nanAbility = CreateTestAsset<AspectAbilityDefinition>();
            nanAbility.SetAbilityId("ability_nan_cost");
            SetField(nanAbility, "baseEssenceCost", float.NaN);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { infAbility, nanAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_infinity_cost");
            _aspectComponent.UnlockAbility("ability_nan_cost");

            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_infinity_cost"));
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_nan_cost"));
        }

        [Test]
        public void CanActivateAbility_RankRequirementSatisfied_ReturnsTrue()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition rankAbility = CreateTestAsset<AspectAbilityDefinition>();
            rankAbility.SetAbilityId("ability_awakened_req");
            SetField(rankAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Awakened);
            SetField(rankAbility, "baseEssenceCost", 0f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { rankAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_awakened_req");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();

            // Equal rank satisfies prerequisite
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_awakened_req"));

            // Higher rank satisfies prerequisite
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Ascended);
            Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_awakened_req"));
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Divine);
            Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_awakened_req"));
        }

        [Test]
        public void CanActivateAbility_RankRequirementNotSatisfied_ReturnsFalse()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition rankAbility = CreateTestAsset<AspectAbilityDefinition>();
            rankAbility.SetAbilityId("ability_ascended_req");
            SetField(rankAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Ascended);
            SetField(rankAbility, "baseEssenceCost", 0f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { rankAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_ascended_req");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();

            // Lower rank fails prerequisite
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_ascended_req"));

            prog.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_ascended_req"));
        }

        [Test]
        public void CanActivateAbility_RankRequirement_WithMissingProgression_ReturnsFalse()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition rankAbility = CreateTestAsset<AspectAbilityDefinition>();
            rankAbility.SetAbilityId("ability_awakened_req");
            SetField(rankAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Awakened);
            SetField(rankAbility, "baseEssenceCost", 0f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { rankAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_awakened_req");

            // No ProgressionComponent on _actor
            Assert.IsNull(_actor.GetComponent<ProgressionComponent>());
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_awakened_req"));
        }

        [Test]
        public void CanActivateAbility_RankRequirement_WithUnknownCharacterRank_ReturnsFalse()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition rankAbility = CreateTestAsset<AspectAbilityDefinition>();
            rankAbility.SetAbilityId("ability_dormant_req");
            SetField(rankAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Dormant);
            SetField(rankAbility, "baseEssenceCost", 0f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { rankAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_dormant_req");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            Assert.AreEqual(ShadowSlaveCharacterRank.Unknown, prog.GetCharacterRank());
            Assert.IsFalse(prog.HasKnownRank());

            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_dormant_req"));
        }

        [Test]
        public void CanActivateAbility_SufficientEssence_ReturnsTrue()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition essenceAbility = CreateTestAsset<AspectAbilityDefinition>();
            essenceAbility.SetAbilityId("ability_essence_cost");
            SetField(essenceAbility, "baseEssenceCost", 25f);
            SetField(essenceAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { essenceAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_essence_cost");

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);

            // More than sufficient
            attrs.SetEssence(50f);
            Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_essence_cost"));

            // Exactly equal
            attrs.SetEssence(25f);
            Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_essence_cost"));
        }

        [Test]
        public void CanActivateAbility_InsufficientEssence_ReturnsFalse()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition essenceAbility = CreateTestAsset<AspectAbilityDefinition>();
            essenceAbility.SetAbilityId("ability_essence_cost");
            SetField(essenceAbility, "baseEssenceCost", 25f);
            SetField(essenceAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { essenceAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_essence_cost");

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);

            attrs.SetEssence(24.9f);
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_essence_cost"));

            attrs.SetEssence(0f);
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_essence_cost"));
        }

        [Test]
        public void CanActivateAbility_PositiveCost_WithMissingAttributes_ReturnsFalse()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition essenceAbility = CreateTestAsset<AspectAbilityDefinition>();
            essenceAbility.SetAbilityId("ability_essence_cost");
            SetField(essenceAbility, "baseEssenceCost", 10f);
            SetField(essenceAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { essenceAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_essence_cost");

            // No AttributeComponent on _actor
            Assert.IsNull(_aspectComponent.GetAttributeComponent());
            Assert.IsFalse(_aspectComponent.CanActivateAbility("ability_essence_cost"));
        }

        [Test]
        public void CanActivateAbility_ZeroCost_DoesNotRequireAttributes()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition freeAbility = CreateTestAsset<AspectAbilityDefinition>();
            freeAbility.SetAbilityId("ability_zero_cost");
            SetField(freeAbility, "baseEssenceCost", 0f);
            SetField(freeAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { freeAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_zero_cost");

            // No AttributeComponent on _actor
            Assert.IsNull(_actor.GetComponent<AttributeComponent>());
            Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_zero_cost"));
        }

        [Test]
        public void CanActivateAbility_DoesNotConsumeEssence()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition costAbility = CreateTestAsset<AspectAbilityDefinition>();
            costAbility.SetAbilityId("ability_cost_check");
            SetField(costAbility, "baseEssenceCost", 30f);
            SetField(costAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { costAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_cost_check");

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(100f);

            bool canActivate = _aspectComponent.CanActivateAbility("ability_cost_check");
            Assert.IsTrue(canActivate);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);

            // Repeat calls
            _aspectComponent.CanActivateAbility("ability_cost_check");
            _aspectComponent.CanActivateAbility("ability_cost_check");
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);
        }

        [Test]
        public void CanActivateAbility_DoesNotChangeUnlockedState()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            // Locked ability
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
            _aspectComponent.CanActivateAbility("ability_shadow_control");
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));

            // Unlocked ability
            _aspectComponent.UnlockAbility("ability_shadow_control");
            Assert.IsTrue(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
            _aspectComponent.CanActivateAbility("ability_shadow_control");
            Assert.IsTrue(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));
        }

        [Test]
        public void CanActivateAbility_DoesNotChangeActiveState()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            _aspectComponent.CanActivateAbility("ability_shadow_control");
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
        }

        [Test]
        public void CanActivateAbility_DoesNotModifyCharacterRank()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Ascended);

            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            _aspectComponent.CanActivateAbility("ability_shadow_control");
            _aspectComponent.CanActivateAbility("ability_shadow_step");
            Assert.AreEqual(ShadowSlaveCharacterRank.Ascended, prog.GetCharacterRank());
        }

        [Test]
        public void CanActivateAbility_DoesNotModifySoulCores()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetMaxSoulCores(5);
            prog.SetSoulCoreCount(3);

            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            _aspectComponent.CanActivateAbility("ability_shadow_control");
            Assert.AreEqual(3, prog.GetSoulCoreCount());
            Assert.AreEqual(5, prog.GetMaxSoulCores());
        }

        [Test]
        public void CanActivateAbility_DoesNotModifyStaticDefinition()
        {
            var (def, ability1, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            string snapId = ability1.AbilityId;
            string snapName = ability1.DisplayName;
            string snapDesc = ability1.Description;
            ShadowSlaveCharacterRank snapRank = ability1.RequiredCharacterRank;
            float snapCost = ability1.BaseEssenceCost;

            _aspectComponent.CanActivateAbility("ability_shadow_control");
            _aspectComponent.CanActivateAbility("ability_shadow_control");

            Assert.AreEqual(snapId, ability1.AbilityId);
            Assert.AreEqual(snapName, ability1.DisplayName);
            Assert.AreEqual(snapDesc, ability1.Description);
            Assert.AreEqual(snapRank, ability1.RequiredCharacterRank);
            Assert.AreEqual(snapCost, ability1.BaseEssenceCost, 0.001f);
        }

        [Test]
        public void CanActivateAbility_RepeatedCalls_AreStableAndSideEffectFree()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition ability = CreateTestAsset<AspectAbilityDefinition>();
            ability.SetAbilityId("ability_shadow_step");
            SetField(ability, "requiredCharacterRank", ShadowSlaveCharacterRank.Awakened);
            SetField(ability, "baseEssenceCost", 15f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { ability });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(_aspectComponent.CanActivateAbility("ability_shadow_step"));
                Assert.AreEqual(50f, attrs.CurrentEssence, 0.001f);
                Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, prog.GetCharacterRank());
                Assert.IsTrue(_aspectComponent.IsAbilityUnlocked("ability_shadow_step"));
                Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"));
            }
        }

        /* --- Phase 4: Ability Activation State Transition Tests --- */

        [Test]
        public void ActivateAbility_NullOrEmptyId_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            Assert.IsFalse(_aspectComponent.ActivateAbility(null));
            Assert.IsFalse(_aspectComponent.ActivateAbility(string.Empty));
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void ActivateAbility_NonExistentAbilityId_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            Assert.IsFalse(_aspectComponent.ActivateAbility("non_existent_ability_id"));
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void ActivateAbility_LockedAbility_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            // Ability is not unlocked
            Assert.IsFalse(_aspectComponent.IsAbilityUnlocked("ability_shadow_control"));

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            Assert.IsFalse(_aspectComponent.ActivateAbility("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void ActivateAbility_PrerequisitesSatisfied_ActivatesSuccessfully()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(activated);
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_control"));
        }

        [Test]
        public void ActivateAbility_FiresOnAbilityActivated_ExactlyOnceWithCorrectInstance()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            int eventCount = 0;
            AspectAbilityInstance capturedInstance = null;
            _aspectComponent.OnAbilityActivated += inst =>
            {
                eventCount++;
                capturedInstance = inst;
            };

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(activated);
            Assert.AreEqual(1, eventCount);
            Assert.IsNotNull(capturedInstance);
            Assert.AreEqual("ability_shadow_control", capturedInstance.AbilityId);
            Assert.IsTrue(capturedInstance.IsActive);
        }

        [Test]
        public void ActivateAbility_ConsumesExactEssenceCost_FromAttributeComponent()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            // ability_shadow_control BaseEssenceCost is 5f
            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(activated);
            Assert.AreEqual(45f, attrs.CurrentEssence, 0.001f);
        }

        [Test]
        public void ActivateAbility_ZeroCostAbility_DoesNotConsumeEssence()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition freeAbility = CreateTestAsset<AspectAbilityDefinition>();
            freeAbility.SetAbilityId("ability_free");
            SetField(freeAbility, "baseEssenceCost", 0f);
            SetField(freeAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { freeAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_free");

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool activated = _aspectComponent.ActivateAbility("ability_free");
            Assert.IsTrue(activated);
            Assert.AreEqual(50f, attrs.CurrentEssence, 0.001f);
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_free"));
        }

        [Test]
        public void ActivateAbility_ZeroCostAbility_SucceedsWithoutAttributeComponent()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition freeAbility = CreateTestAsset<AspectAbilityDefinition>();
            freeAbility.SetAbilityId("ability_free");
            SetField(freeAbility, "baseEssenceCost", 0f);
            SetField(freeAbility, "requiredCharacterRank", ShadowSlaveCharacterRank.Unknown);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { freeAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_free");

            // No AttributeComponent on _actor
            Assert.IsNull(_aspectComponent.GetAttributeComponent());

            bool activated = _aspectComponent.ActivateAbility("ability_free");
            Assert.IsTrue(activated);
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_free"));
        }

        [Test]
        public void ActivateAbility_InsufficientEssence_FailsAndRemainsInactive()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(10f); // ability_shadow_step requires 15f

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_step");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"));
            Assert.AreEqual(10f, attrs.CurrentEssence, 0.001f);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void ActivateAbility_MissingAttributeComponent_FailsWhenCostGreaterThanZero()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            // No AttributeComponent
            Assert.IsNull(_aspectComponent.GetAttributeComponent());

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void ActivateAbility_NegativeOrInvalidCost_FailsActivation()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_test");

            AspectAbilityDefinition badAbility = CreateTestAsset<AspectAbilityDefinition>();
            badAbility.SetAbilityId("ability_bad_cost");
            SetField(badAbility, "baseEssenceCost", -10f);

            SetField(def, "abilityDefinitions", new List<AspectAbilityDefinition> { badAbility });
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_bad_cost");

            bool activated = _aspectComponent.ActivateAbility("ability_bad_cost");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_bad_cost"));
        }

        [Test]
        public void ActivateAbility_AlreadyActive_ReturnsTrueWithoutConsumingEssenceOrFiringEvent()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            // First activation
            bool first = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(first);
            Assert.AreEqual(45f, attrs.CurrentEssence, 0.001f);

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            // Second activation while already active
            bool second = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(second);
            Assert.AreEqual(45f, attrs.CurrentEssence, 0.001f, "Essence must not be consumed again on already active ability.");
            Assert.AreEqual(0, eventCount, "OnAbilityActivated must not fire on already active ability.");
        }

        [Test]
        public void ActivateAbility_RepeatedCalls_AreStableAndIdempotent()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            Assert.IsTrue(_aspectComponent.ActivateAbility("ability_shadow_control"));
            Assert.AreEqual(45f, attrs.CurrentEssence, 0.001f);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(_aspectComponent.ActivateAbility("ability_shadow_control"));
                Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_control"));
                Assert.AreEqual(45f, attrs.CurrentEssence, 0.001f);
            }
        }

        [Test]
        public void ActivateAbility_FailureAtomicity_WhenPrerequisitesFail()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step"); // Requires Awakened, cost 15f

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant); // Rank too low

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_step");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"));
            Assert.AreEqual(50f, attrs.CurrentEssence, 0.001f, "Essence must remain untouched on failure.");
            Assert.AreEqual(0, eventCount, "No event must be fired on failure.");
        }

        [Test]
        public void ActivateAbility_FailureAtomicity_WhenEssenceConsumptionFails()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control"); // cost 5f

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            // Kill character so ConsumeEssence returns false
            attrs.SetHealth(0f);
            Assert.IsTrue(attrs.IsDead);

            int eventCount = 0;
            _aspectComponent.OnAbilityActivated += _ => eventCount++;

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.AreEqual(0, eventCount);
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);
        }

        [Test]
        public void ActivateAbility_RankPrerequisite_InsufficientRank_Fails()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step"); // requires Awakened

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_step");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"));
            Assert.AreEqual(50f, attrs.CurrentEssence, 0.001f);
        }

        [Test]
        public void ActivateAbility_RankPrerequisite_MissingProgressionComponent_Fails()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step"); // requires Awakened

            // No ProgressionComponent on _actor
            Assert.IsNull(_actor.GetComponent<ProgressionComponent>());

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_step");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"));
        }

        [Test]
        public void ActivateAbility_RankPrerequisite_UnknownRank_Fails()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step"); // requires Awakened

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            // Rank defaults to Unknown
            Assert.AreEqual(ShadowSlaveCharacterRank.Unknown, prog.GetCharacterRank());

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_step");
            Assert.IsFalse(activated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"));
        }

        [Test]
        public void ActivateAbility_RankPrerequisite_EqualOrHigherRank_Succeeds()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step"); // requires Awakened

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Transcendent); // Higher than Awakened

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_step");
            Assert.IsTrue(activated);
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_step"));
            Assert.AreEqual(35f, attrs.CurrentEssence, 0.001f);
        }

        [Test]
        public void ActivateAbility_TransitionGuard_ReentrantActivation_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");
            _aspectComponent.UnlockAbility("ability_shadow_step");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool reentrantActivationAttempted = false;
            bool reentrantActivationResult = true;

            _aspectComponent.OnAbilityActivated += inst =>
            {
                if (inst.AbilityId == "ability_shadow_control")
                {
                    reentrantActivationAttempted = true;
                    Assert.IsTrue(_aspectComponent.IsProcessingAbilityTransition, "Transition guard must be active during event dispatch.");
                    reentrantActivationResult = _aspectComponent.ActivateAbility("ability_shadow_step");
                }
            };

            bool primaryActivated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(primaryActivated);
            Assert.IsTrue(reentrantActivationAttempted);
            Assert.IsFalse(reentrantActivationResult, "Reentrant activation must be rejected while transition is in progress.");
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_step"), "Reentrant ability must remain inactive.");
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition, "Transition guard must clear after completion.");
        }

        [Test]
        public void ActivateAbility_TransitionGuard_ReentrantDeactivation_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool reentrantDeactivateAttempted = false;
            bool reentrantDeactivateResult = true;

            _aspectComponent.OnAbilityActivated += inst =>
            {
                reentrantDeactivateAttempted = true;
                reentrantDeactivateResult = _aspectComponent.DeactivateAbility("ability_shadow_control");
            };

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(activated);
            Assert.IsTrue(reentrantDeactivateAttempted);
            Assert.IsFalse(reentrantDeactivateResult, "Reentrant deactivation must be rejected while activation transition is in progress.");
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);
        }

        [Test]
        public void ActivateAbility_TransitionGuard_ReentrantSetAspectDefinition_ReturnsFalse()
        {
            var (defA, _, _) = CreateTwoAbilityAspect();
            AspectDefinition defB = CreateTestAsset<AspectDefinition>();
            defB.SetAspectId("aspect_b");

            _aspectComponent.SetAspectDefinition(defA);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool reentrantSetAspectResult = true;

            _aspectComponent.OnAbilityActivated += _ =>
            {
                reentrantSetAspectResult = _aspectComponent.SetAspectDefinition(defB);
            };

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(activated);
            Assert.IsFalse(reentrantSetAspectResult, "Reentrant SetAspectDefinition must be rejected during activation transition.");
            Assert.AreEqual("aspect_shadow_slave", _aspectComponent.GetAspectDefinition().AspectId);
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);
        }

        [Test]
        public void ActivateAbility_TransitionGuard_ClearsAfterActivation()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);
            _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);

            // Can now deactivate normally
            bool deactivated = _aspectComponent.DeactivateAbility("ability_shadow_control");
            Assert.IsTrue(deactivated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);
        }

        [Test]
        public void ActivateAbility_TransitionGuard_ClearsEvenIfHandlerThrows()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            Action<AspectAbilityInstance> faultyHandler = _ => throw new InvalidOperationException("Simulated listener exception");
            _aspectComponent.OnAbilityActivated += faultyHandler;

            Assert.Throws<InvalidOperationException>(() => _aspectComponent.ActivateAbility("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition, "Transition guard must clear even when listener throws.");

            _aspectComponent.OnAbilityActivated -= faultyHandler;

            // Subsequent deactivation works normally without being blocked by transition guard
            bool deactivated = _aspectComponent.DeactivateAbility("ability_shadow_control");
            Assert.IsTrue(deactivated);
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
        }

        [Test]
        public void DeactivateAbility_TransitionGuard_ReentrantActivation_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_control"));

            bool reentrantActivateAttempted = false;
            bool reentrantActivateResult = true;

            _aspectComponent.OnAbilityDeactivated += _ =>
            {
                reentrantActivateAttempted = true;
                Assert.IsTrue(_aspectComponent.IsProcessingAbilityTransition, "Transition guard must be active during deactivation event dispatch.");
                reentrantActivateResult = _aspectComponent.ActivateAbility("ability_shadow_control");
            };

            bool deactivated = _aspectComponent.DeactivateAbility("ability_shadow_control");
            Assert.IsTrue(deactivated);
            Assert.IsTrue(reentrantActivateAttempted);
            Assert.IsFalse(reentrantActivateResult, "Reentrant activation must be rejected while deactivation transition is in progress.");
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);
        }

        [Test]
        public void DeactivateAbility_AlreadyInactive_ReturnsTrueWithoutFiringEvent()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));

            int eventCount = 0;
            _aspectComponent.OnAbilityDeactivated += _ => eventCount++;

            bool result = _aspectComponent.DeactivateAbility("ability_shadow_control");
            Assert.IsTrue(result);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void DeactivateAbility_NullOrEmptyId_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.DeactivateAbility(null));
            Assert.IsFalse(_aspectComponent.DeactivateAbility(string.Empty));
        }

        [Test]
        public void SetAspectDefinition_TransitionGuard_PreventsReentrantCall()
        {
            var (defA, _, _) = CreateTwoAbilityAspect();
            AspectDefinition defB = CreateTestAsset<AspectDefinition>();
            defB.SetAspectId("aspect_second");

            _aspectComponent.SetAspectDefinition(defA);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_control"));

            bool reentrantActivateRejected = false;
            bool reentrantSetAspectRejected = false;

            Action<AspectAbilityInstance> deactivationListener = _ =>
            {
                reentrantActivateRejected = !_aspectComponent.ActivateAbility("ability_shadow_control");
                reentrantSetAspectRejected = !_aspectComponent.SetAspectDefinition(defA);
            };

            _aspectComponent.OnAbilityDeactivated += deactivationListener;
            _aspectComponent.SetAspectDefinition(defB);
            _aspectComponent.OnAbilityDeactivated -= deactivationListener;

            Assert.IsTrue(reentrantActivateRejected, "Reentrant ActivateAbility during SetAspectDefinition must be rejected.");
            Assert.IsTrue(reentrantSetAspectRejected, "Reentrant SetAspectDefinition during SetAspectDefinition must be rejected.");
            Assert.IsFalse(_aspectComponent.IsProcessingAbilityTransition);
            Assert.AreEqual("aspect_second", _aspectComponent.GetAspectDefinition().AspectId);
        }

        [Test]
        public void ActivateAbility_DoesNotModifyProgressionComponent()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            prog.SetMaxSoulCores(7);
            prog.SetSoulCoreCount(3);
            prog.SetProgressionMetadata("quest_step", "five");

            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_step");

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_step");
            Assert.IsTrue(activated);

            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, prog.GetCharacterRank());
            Assert.AreEqual(3, prog.GetSoulCoreCount());
            Assert.AreEqual(7, prog.GetMaxSoulCores());
            Assert.IsTrue(prog.GetProgressionMetadata("quest_step", out string val));
            Assert.AreEqual("five", val);
        }

        [Test]
        public void ActivateAbility_DoesNotModifyStaticDefinitions()
        {
            var (def, ability1, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            string snapId = ability1.AbilityId;
            string snapName = ability1.DisplayName;
            string snapDesc = ability1.Description;
            ShadowSlaveCharacterRank snapRank = ability1.RequiredCharacterRank;
            float snapCost = ability1.BaseEssenceCost;

            string snapAspectId = def.AspectId;
            string snapAspectName = def.DisplayName;
            AspectRank snapAspectRank = def.AspectRank;

            bool activated = _aspectComponent.ActivateAbility("ability_shadow_control");
            Assert.IsTrue(activated);

            // Re-verify static definition assets remain completely unaltered
            Assert.AreEqual(snapId, ability1.AbilityId);
            Assert.AreEqual(snapName, ability1.DisplayName);
            Assert.AreEqual(snapDesc, ability1.Description);
            Assert.AreEqual(snapRank, ability1.RequiredCharacterRank);
            Assert.AreEqual(snapCost, ability1.BaseEssenceCost, 0.001f);

            Assert.AreEqual(snapAspectId, def.AspectId);
            Assert.AreEqual(snapAspectName, def.DisplayName);
            Assert.AreEqual(snapAspectRank, def.AspectRank);
        }

        /* --- Phase 5: Ability Dynamic Properties Tests --- */

        [Test]
        public void SetAbilityDynamicProperty_NullOrEmptyAbilityId_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.SetAbilityDynamicProperty(null, "test_key", "test_val"));
            Assert.IsFalse(_aspectComponent.SetAbilityDynamicProperty(string.Empty, "test_key", "test_val"));
        }

        [Test]
        public void SetAbilityDynamicProperty_NullOrEmptyKey_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", null, "test_val"));
            Assert.IsFalse(_aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", string.Empty, "test_val"));
        }

        [Test]
        public void SetAbilityDynamicProperty_NonExistentAbility_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.SetAbilityDynamicProperty("non_existent_ability", "test_key", "test_val"));
        }

        [Test]
        public void SetAbilityDynamicProperty_ValidProperty_ReturnsTrue()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            bool result = _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "mode", "stealth");
            Assert.IsTrue(result);

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.IsNotNull(instance);
            Assert.AreEqual(1, instance.DynamicProperties.Count);
            Assert.AreEqual("stealth", instance.DynamicProperties["mode"]);
        }

        [Test]
        public void GetAbilityDynamicProperty_NullOrEmptyAbilityId_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "key", "val");

            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty(null, "key", out string v1));
            Assert.IsNull(v1);

            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty(string.Empty, "key", out string v2));
            Assert.IsNull(v2);
        }

        [Test]
        public void GetAbilityDynamicProperty_NullOrEmptyKey_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "key", "val");

            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", null, out string v1));
            Assert.IsNull(v1);

            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", string.Empty, out string v2));
            Assert.IsNull(v2);
        }

        [Test]
        public void GetAbilityDynamicProperty_NonExistentAbility_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty("non_existent_ability", "key", out string v));
            Assert.IsNull(v);
        }

        [Test]
        public void GetAbilityDynamicProperty_MissingKey_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "existing_key", "existing_val");

            bool result = _aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "missing_key", out string v);
            Assert.IsFalse(result);
            Assert.IsNull(v);
        }

        [Test]
        public void GetAbilityDynamicProperty_ReturnsStoredValue()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "target_count", "4");

            bool result = _aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "target_count", out string val);
            Assert.IsTrue(result);
            Assert.AreEqual("4", val);
        }

        [Test]
        public void SetAbilityDynamicProperty_OverwritesExistingKey_WithoutDuplicateEntries()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsTrue(_aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "combo", "1"));
            Assert.IsTrue(_aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "combo", "2"));

            bool result = _aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "combo", out string val);
            Assert.IsTrue(result);
            Assert.AreEqual("2", val);

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.AreEqual(1, instance.DynamicProperties.Count);
        }

        [Test]
        public void SetAbilityDynamicProperty_MultipleKeysOnSameAbility_CoexistIndependently()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "prop_a", "alpha");
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "prop_b", "beta");

            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "prop_a", out string valA));
            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "prop_b", out string valB));

            Assert.AreEqual("alpha", valA);
            Assert.AreEqual("beta", valB);

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.AreEqual(2, instance.DynamicProperties.Count);
        }

        [Test]
        public void SetAbilityDynamicProperty_IsolatedBetweenAbilities()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "shared_name", "from_control");

            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "shared_name", out string val1));
            Assert.AreEqual("from_control", val1);

            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_step", "shared_name", out string val2));
            Assert.IsNull(val2);

            AspectAbilityInstance instance2 = _aspectComponent.FindAbilityInstance("ability_shadow_step");
            Assert.AreEqual(0, instance2.DynamicProperties.Count);
        }

        [Test]
        public void DynamicProperties_SurviveActivationAndDeactivation()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.UnlockAbility("ability_shadow_control");

            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);

            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "stance", "shadow_blade");

            // Activate ability
            Assert.IsTrue(_aspectComponent.ActivateAbility("ability_shadow_control"));
            Assert.IsTrue(_aspectComponent.IsAbilityActive("ability_shadow_control"));

            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "stance", out string valAfterActivate));
            Assert.AreEqual("shadow_blade", valAfterActivate);

            // Deactivate ability
            Assert.IsTrue(_aspectComponent.DeactivateAbility("ability_shadow_control"));
            Assert.IsFalse(_aspectComponent.IsAbilityActive("ability_shadow_control"));

            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "stance", out string valAfterDeactivate));
            Assert.AreEqual("shadow_blade", valAfterDeactivate);
        }

        [Test]
        public void DynamicProperties_DiscardedWhenAspectReplaced()
        {
            var (defA, _, _) = CreateTwoAbilityAspect();
            AspectDefinition defB = CreateTestAsset<AspectDefinition>();
            defB.SetAspectId("aspect_b");

            AspectAbilityDefinition abilityB = CreateTestAsset<AspectAbilityDefinition>();
            abilityB.SetAbilityId("ability_solar_beam");
            SetField(defB, "abilityDefinitions", new List<AspectAbilityDefinition> { abilityB });

            _aspectComponent.SetAspectDefinition(defA);
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "persistent_flag", "active");

            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "persistent_flag", out string _));

            // Replace aspect
            _aspectComponent.SetAspectDefinition(defB);

            // Old ability instance is gone
            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "persistent_flag", out string _));

            // New ability instance starts clean
            AspectAbilityInstance newInstance = _aspectComponent.FindAbilityInstance("ability_solar_beam");
            Assert.IsNotNull(newInstance);
            Assert.AreEqual(0, newInstance.DynamicProperties.Count);
        }

        [Test]
        public void DynamicProperties_DiscardedWhenAspectCleared()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "temp_state", "123");

            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "temp_state", out string _));

            // Clear aspect
            _aspectComponent.SetAspectDefinition(null);

            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "temp_state", out string v));
            Assert.IsNull(v);
        }

        [Test]
        public void DynamicProperties_NewRuntimeInstances_StartClean()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            IReadOnlyList<AspectAbilityInstance> instances = _aspectComponent.GetAbilityInstances();
            Assert.AreEqual(2, instances.Count);
            for (int i = 0; i < instances.Count; i++)
            {
                Assert.IsNotNull(instances[i].DynamicProperties);
                Assert.AreEqual(0, instances[i].DynamicProperties.Count);
            }
        }

        [Test]
        public void SetAbilityDynamicProperty_DoesNotMutateStaticDefinitions()
        {
            var (def, ability1, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            string snapId = ability1.AbilityId;
            string snapName = ability1.DisplayName;
            string snapDesc = ability1.Description;
            ShadowSlaveCharacterRank snapRank = ability1.RequiredCharacterRank;
            float snapCost = ability1.BaseEssenceCost;

            string snapAspectId = def.AspectId;
            string snapAspectName = def.DisplayName;
            AspectRank snapAspectRank = def.AspectRank;

            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "meta1", "val1");
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "meta2", "val2");

            Assert.AreEqual(snapId, ability1.AbilityId);
            Assert.AreEqual(snapName, ability1.DisplayName);
            Assert.AreEqual(snapDesc, ability1.Description);
            Assert.AreEqual(snapRank, ability1.RequiredCharacterRank);
            Assert.AreEqual(snapCost, ability1.BaseEssenceCost, 0.001f);

            Assert.AreEqual(snapAspectId, def.AspectId);
            Assert.AreEqual(snapAspectName, def.DisplayName);
            Assert.AreEqual(snapAspectRank, def.AspectRank);
        }

        [Test]
        public void SetAbilityDynamicProperty_DoesNotMutateProgressionOrAttributeComponents()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            prog.SetMaxSoulCores(7);
            prog.SetSoulCoreCount(3);
            prog.SetProgressionMetadata("quest_step", "five");

            AttributeComponent attrs = _actor.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            attrs.SetEssence(50f);
            attrs.SetHealth(80f);
            attrs.SetStamina(90f);

            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "temp_buff", "swift");
            _aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "temp_buff", out string _);

            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, prog.GetCharacterRank());
            Assert.AreEqual(3, prog.GetSoulCoreCount());
            Assert.AreEqual(7, prog.GetMaxSoulCores());
            Assert.IsTrue(prog.GetProgressionMetadata("quest_step", out string val));
            Assert.AreEqual("five", val);

            Assert.AreEqual(50f, attrs.CurrentEssence, 0.001f);
            Assert.AreEqual(80f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(90f, attrs.CurrentStamina, 0.001f);
        }

        [Test]
        public void GetAbilityDynamicProperty_GetterPurity_DoesNotMutateState()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.AreEqual(0, instance.DynamicProperties.Count);

            // Repeated calls for absent property
            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "absent_prop", out string v));
                Assert.IsNull(v);
                Assert.AreEqual(0, instance.DynamicProperties.Count);
            }

            // Set property and repeat calls for present property
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "present_prop", "constant");
            Assert.AreEqual(1, instance.DynamicProperties.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "present_prop", out string v));
                Assert.AreEqual("constant", v);
                Assert.AreEqual(1, instance.DynamicProperties.Count);
            }
        }

        [Test]
        public void DynamicProperties_KeySemantics_AreOrdinalAndCaseSensitive()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "CaseKey", "Uppercase");
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "casekey", "Lowercase");

            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "CaseKey", out string upper));
            Assert.IsTrue(_aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "casekey", out string lower));

            Assert.AreEqual("Uppercase", upper);
            Assert.AreEqual("Lowercase", lower);

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.AreEqual(2, instance.DynamicProperties.Count);
        }

        [Test]
        public void DynamicProperties_EmptyValue_StoredAndRetrievedSuccessfully()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            bool setResult = _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "flag", string.Empty);
            Assert.IsTrue(setResult);

            bool getResult = _aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "flag", out string val);
            Assert.IsTrue(getResult);
            Assert.AreEqual(string.Empty, val);
        }

        [Test]
        public void DynamicProperties_NullValue_NormalizedToEmptyString()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            bool setResult = _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "flag_null", null);
            Assert.IsTrue(setResult);

            bool getResult = _aspectComponent.GetAbilityDynamicProperty("ability_shadow_control", "flag_null", out string val);
            Assert.IsTrue(getResult);
            Assert.AreEqual(string.Empty, val, "Null value must normalize to empty string matching UE5 FString semantics.");
        }

        [Test]
        public void DynamicProperties_BelongExclusivelyToAspectAbilityInstance_NotStaticDefinitions()
        {
            // Verify static content definitions do not define DynamicProperties
            Type defType = typeof(AspectDefinition);
            Type abilityDefType = typeof(AspectAbilityDefinition);
            Type instanceType = typeof(AspectAbilityInstance);

            Assert.IsNull(defType.GetProperty("DynamicProperties"), "AspectDefinition must not possess DynamicProperties.");
            Assert.IsNull(defType.GetField("dynamicProperties", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), "AspectDefinition must not possess dynamicProperties field.");

            Assert.IsNull(abilityDefType.GetProperty("DynamicProperties"), "AspectAbilityDefinition must not possess DynamicProperties.");
            Assert.IsNull(abilityDefType.GetField("dynamicProperties", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), "AspectAbilityDefinition must not possess dynamicProperties field.");

            Assert.IsNotNull(instanceType.GetProperty("DynamicProperties"), "AspectAbilityInstance must possess DynamicProperties.");

            // Verify runtime storage on instance
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "instance_only", "verified");

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.DynamicProperties.ContainsKey("instance_only"));
            Assert.AreEqual("verified", instance.DynamicProperties["instance_only"]);
        }

        [Test]
        public void DynamicProperties_InvalidInstance_ReturnsFalse()
        {
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);

            AspectAbilityInstance invalidInstance = new AspectAbilityInstance();
            Assert.IsFalse(invalidInstance.IsValid);

            // Adding invalid instance to list directly via reflection for defensive test
            FieldInfo instancesField = typeof(AspectComponent).GetField("abilityInstances", BindingFlags.Instance | BindingFlags.NonPublic);
            List<AspectAbilityInstance> list = (List<AspectAbilityInstance>)instancesField.GetValue(_aspectComponent);
            list.Add(invalidInstance);

            Assert.IsFalse(_aspectComponent.SetAbilityDynamicProperty(string.Empty, "key", "val"));
            Assert.IsFalse(_aspectComponent.GetAbilityDynamicProperty(string.Empty, "key", out string _));
        }

        [Test]
        public void DynamicProperties_IsEncapsulatedAsReadOnly_AndNotDirectlyMutable()
        {
            Type instanceType = typeof(AspectAbilityInstance);
            PropertyInfo prop = instanceType.GetProperty("DynamicProperties");
            Assert.IsNotNull(prop, "AspectAbilityInstance must possess a DynamicProperties property.");
            Assert.AreEqual(typeof(IReadOnlyDictionary<string, string>), prop.PropertyType,
                "DynamicProperties must be exposed as IReadOnlyDictionary to prevent direct external mutation.");
            Assert.IsFalse(prop.CanWrite, "DynamicProperties must not expose a public setter.");
            Assert.IsNull(prop.PropertyType.GetMethod("Add", new[] { typeof(string), typeof(string) }),
                "DynamicProperties property type must not expose an Add method.");
            Assert.IsNull(prop.PropertyType.GetMethod("Clear", Type.EmptyTypes),
                "DynamicProperties property type must not expose a Clear method.");
            Assert.IsNull(prop.PropertyType.GetMethod("Remove", new[] { typeof(string) }),
                "DynamicProperties property type must not expose a Remove method.");

            // Verify that instances expose read-only view and cannot be mutated except via AspectComponent
            var (def, _, _) = CreateTwoAbilityAspect();
            _aspectComponent.SetAspectDefinition(def);
            _aspectComponent.SetAbilityDynamicProperty("ability_shadow_control", "encap_test", "readonly_verified");

            AspectAbilityInstance instance = _aspectComponent.FindAbilityInstance("ability_shadow_control");
            Assert.IsNotNull(instance);
            IReadOnlyDictionary<string, string> readOnlyView = instance.DynamicProperties;
            Assert.IsNotNull(readOnlyView);
            Assert.AreEqual(1, readOnlyView.Count);
            Assert.AreEqual("readonly_verified", readOnlyView["encap_test"]);
            Assert.IsTrue(readOnlyView.ContainsKey("encap_test"));
        }
    }
}
