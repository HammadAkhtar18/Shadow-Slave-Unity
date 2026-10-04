using System.Collections.Generic;
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
        }

        [Test]
        public void AspectDefinition_AspectIdAndRank_CanBeStoredAndRetrieved()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_shadow_slave");
            def.SetDisplayName("Shadow Slave");
            def.SetDescription("Slave of shadows, master of nothing.");
            def.SetAspectRank(AspectRank.Divine);

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
            ability1.SetDisplayName("Shadow Control");
            ability1.SetRequiredCharacterRank(ShadowSlaveCharacterRank.Dormant);
            ability1.SetBaseEssenceCost(5f);

            AspectAbilityDefinition ability2 = CreateTestAsset<AspectAbilityDefinition>();
            ability2.SetAbilityId("ability_shadow_step");
            ability2.SetDisplayName("Shadow Step");
            ability2.SetRequiredCharacterRank(ShadowSlaveCharacterRank.Awakened);
            ability2.SetBaseEssenceCost(15f);

            def.SetAbilityDefinitions(new[] { ability1, ability2 });

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
            def.AddAbilityDefinition(ability);

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
            flaw.SetDisplayName("Clear Conscience");
            flaw.SetDescription("Cannot tell a lie.");

            def.SetFlawDefinition(flaw);

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
            ability.SetDisplayName("Shadow Manifestation");
            ability.SetDescription("Solidify shadows into physical objects.");
            ability.SetRequiredCharacterRank(ShadowSlaveCharacterRank.Ascended);
            ability.SetBaseEssenceCost(25f);

            Assert.AreEqual("ability_shadow_manifestation", ability.AbilityId);
            Assert.AreEqual("Shadow Manifestation", ability.DisplayName);
            Assert.AreEqual("Solidify shadows into physical objects.", ability.Description);
            Assert.AreEqual(ShadowSlaveCharacterRank.Ascended, ability.RequiredCharacterRank);
            Assert.AreEqual(25f, ability.BaseEssenceCost, 0.001f);
            Assert.IsTrue(ability.HasRankRequirement());

            // Unknown rank requirement means no prerequisite
            AspectAbilityDefinition innateAbility = CreateTestAsset<AspectAbilityDefinition>();
            innateAbility.SetRequiredCharacterRank(ShadowSlaveCharacterRank.Unknown);
            Assert.IsFalse(innateAbility.HasRankRequirement());
        }

        /* --- Aspect Component Tests --- */

        [Test]
        public void AspectComponent_DefaultState_HasNoAspect()
        {
            Assert.IsFalse(_aspectComponent.HasAspect());
            Assert.IsNull(_aspectComponent.GetAspectDefinition());
            Assert.AreEqual(AspectRank.Unknown, _aspectComponent.GetAspectRank());
        }

        [Test]
        public void AspectComponent_SetAspectDefinition_BindsDefinitionCorrectly()
        {
            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectId("aspect_shadow_slave");
            def.SetAspectRank(AspectRank.Divine);

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
            aspectA.SetAspectRank(AspectRank.Awakened);

            AspectDefinition aspectB = CreateTestAsset<AspectDefinition>();
            aspectB.SetAspectId("aspect_b");
            aspectB.SetAspectRank(AspectRank.Ascended);

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
            def.SetAspectRank(AspectRank.Transcendent);
            _aspectComponent.SetAspectDefinition(def);

            Assert.IsTrue(_aspectComponent.HasAspect());

            bool success = _aspectComponent.SetAspectDefinition(null);
            Assert.IsTrue(success);
            Assert.IsFalse(_aspectComponent.HasAspect());
            Assert.IsNull(_aspectComponent.GetAspectDefinition());
            Assert.AreEqual(AspectRank.Unknown, _aspectComponent.GetAspectRank());
        }

        /* --- Boundary Tests --- */

        [Test]
        public void AspectOperations_DoNotMutate_CharacterRank()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetCharacterRank(ShadowSlaveCharacterRank.Ascended);

            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectRank(AspectRank.Divine);

            _aspectComponent.SetAspectDefinition(def);

            Assert.AreEqual(ShadowSlaveCharacterRank.Ascended, prog.GetCharacterRank());
            Assert.AreEqual(AspectRank.Divine, _aspectComponent.GetAspectRank());
        }

        [Test]
        public void AspectOperations_DoNotMutate_SoulCores()
        {
            ProgressionComponent prog = _actor.AddComponent<ProgressionComponent>();
            prog.SetMaxSoulCores(4);
            prog.SetSoulCoreCount(3);

            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectRank(AspectRank.Sacred);

            _aspectComponent.SetAspectDefinition(def);

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

            AspectDefinition def = CreateTestAsset<AspectDefinition>();
            def.SetAspectRank(AspectRank.Divine);

            _aspectComponent.SetAspectDefinition(def);

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);
        }
    }
}
