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
    }
}
