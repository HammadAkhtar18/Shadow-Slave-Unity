using System;
using System.Collections.Generic;
using NUnit.Framework;
using ShadowSlave.Characters;
using ShadowSlave.StatusEffects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class StatusEffectTests
    {
        private GameObject _go;
        private StatusEffectComponent _statusComp;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("StatusEffectTestActor");
            _statusComp = _go.AddComponent<StatusEffectComponent>();
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
        public void StatusEffect_ApplicationAndQueries_Success()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create(
                effectId: "test_buff",
                displayName: "Test Buff",
                description: "A test buff.",
                durationPolicy: StatusEffectDurationPolicy.Timed,
                duration: 10f,
                polarity: StatusEffectPolarity.Beneficial);

            var sourceGuid = Guid.NewGuid();
            var source = new StatusEffectSource(sourceGuid, "TestSource", _go);
            var props = new Dictionary<string, string> { { "Intensity", "High" } };

            Guid appliedId = _statusComp.ApplyEffect(def, source, props);

            Assert.AreNotEqual(Guid.Empty, appliedId);
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsTrue(_statusComp.HasEffect(def));
            Assert.IsTrue(_statusComp.HasEffectById("test_buff"));
            Assert.IsTrue(_statusComp.HasEffectByInstanceId(appliedId));

            Assert.IsTrue(_statusComp.FindEffect(def, out StatusEffectInstance foundByDef));
            Assert.AreEqual(appliedId, foundByDef.InstanceId);
            Assert.AreEqual(1, foundByDef.CurrentStacks);
            Assert.AreEqual(sourceGuid, foundByDef.Source.SourceId);
            Assert.AreEqual("TestSource", foundByDef.Source.SourceName);
            Assert.AreEqual(_go, foundByDef.Source.SourceActor);
            Assert.AreEqual("High", foundByDef.DynamicProperties["Intensity"]);

            Assert.IsTrue(_statusComp.FindEffectById("test_buff", out StatusEffectInstance foundById));
            Assert.AreEqual(appliedId, foundById.InstanceId);

            Assert.IsTrue(_statusComp.FindEffectByInstanceId(appliedId, out StatusEffectInstance foundByGuid));
            Assert.AreEqual(appliedId, foundByGuid.InstanceId);

            List<StatusEffectInstance> allList = new List<StatusEffectInstance>();
            _statusComp.GetAllEffectsByDefinition(def, allList);
            Assert.AreEqual(1, allList.Count);
            Assert.AreEqual(appliedId, allList[0].InstanceId);

            Assert.AreEqual(1, _statusComp.GetStackCount(def));
            Assert.AreEqual(10f, _statusComp.GetRemainingDuration(appliedId), 0.001f);
        }

        [Test]
        public void StatusEffect_StackingPolicy_IgnoreNew()
        {
            var def = StatusEffectDefinition.Create(
                effectId: "test_ignore_new",
                durationPolicy: StatusEffectDurationPolicy.Timed,
                duration: 5f,
                stackingPolicy: StatusEffectStackingPolicy.IgnoreNew);

            Guid firstId = _statusComp.ApplyEffectSimple(def);
            Assert.AreNotEqual(Guid.Empty, firstId);
            Assert.AreEqual(1, _statusComp.EffectCount);

            Guid secondId = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(firstId, secondId);
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.AreEqual(1, _statusComp.GetStackCount(def));
        }

        [Test]
        public void StatusEffect_StackingPolicy_RefreshDuration()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create(
                effectId: "test_refresh",
                durationPolicy: StatusEffectDurationPolicy.Timed,
                duration: 5f,
                stackingPolicy: StatusEffectStackingPolicy.RefreshDuration);

            Guid id = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(5f, _statusComp.GetRemainingDuration(id), 0.001f);

            currentTime = 103f;
            Assert.AreEqual(2f, _statusComp.GetRemainingDuration(id), 0.001f);

            Guid refreshedId = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(id, refreshedId);
            Assert.AreEqual(5f, _statusComp.GetRemainingDuration(id), 0.001f);
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.AreEqual(1, _statusComp.GetStackCount(def));
        }

        [Test]
        public void StatusEffect_StackingPolicy_AddStacks_ClampedAtMax()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create(
                effectId: "test_stacks",
                durationPolicy: StatusEffectDurationPolicy.Timed,
                duration: 5f,
                stackingPolicy: StatusEffectStackingPolicy.AddStacks,
                maxStacks: 3);

            int stackChangedCount = 0;
            int lastOldStacks = -1;
            _statusComp.OnStatusEffectStackChanged += (_, oldStacks) =>
            {
                stackChangedCount++;
                lastOldStacks = oldStacks;
            };

            Guid id1 = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(1, _statusComp.GetStackCount(def));
            Assert.AreEqual(0, stackChangedCount);

            currentTime = 102f;
            Guid id2 = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(id1, id2);
            Assert.AreEqual(2, _statusComp.GetStackCount(def));
            Assert.AreEqual(1, stackChangedCount);
            Assert.AreEqual(1, lastOldStacks);
            Assert.AreEqual(5f, _statusComp.GetRemainingDuration(id1), 0.001f);

            Guid id3 = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(3, _statusComp.GetStackCount(def));
            Assert.AreEqual(2, stackChangedCount);
            Assert.AreEqual(2, lastOldStacks);

            // 4th application should be clamped at maxStacks (3)
            Guid id4 = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(3, _statusComp.GetStackCount(def));
            Assert.AreEqual(2, stackChangedCount); // Did not change stacks
        }

        [Test]
        public void StatusEffect_StackingPolicy_Replace()
        {
            var def = StatusEffectDefinition.Create(
                effectId: "test_replace",
                durationPolicy: StatusEffectDurationPolicy.Timed,
                duration: 5f,
                stackingPolicy: StatusEffectStackingPolicy.Replace);

            bool removedFired = false;
            Guid removedId = Guid.Empty;
            _statusComp.OnStatusEffectRemoved += inst =>
            {
                removedFired = true;
                removedId = inst.InstanceId;
            };

            Guid firstId = _statusComp.ApplyEffectSimple(def);
            Assert.AreNotEqual(Guid.Empty, firstId);
            Assert.AreEqual(1, _statusComp.EffectCount);

            Guid secondId = _statusComp.ApplyEffectSimple(def);
            Assert.AreNotEqual(firstId, secondId);
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsTrue(removedFired);
            Assert.AreEqual(firstId, removedId);
        }

        [Test]
        public void StatusEffect_DurationPolicy_Instant_ExecutesAndDoesNotPersist()
        {
            var def = StatusEffectDefinition.Create(
                effectId: "test_instant",
                durationPolicy: StatusEffectDurationPolicy.Instant);

            bool appliedFired = false;
            bool removedFired = false;
            _statusComp.OnStatusEffectApplied += _ => appliedFired = true;
            _statusComp.OnStatusEffectRemoved += _ => removedFired = true;

            Guid id = _statusComp.ApplyEffectSimple(def);

            Assert.AreNotEqual(Guid.Empty, id);
            Assert.IsTrue(appliedFired);
            Assert.IsTrue(removedFired);
            Assert.AreEqual(0, _statusComp.EffectCount);
            Assert.IsFalse(_statusComp.HasEffect(def));
        }

        [Test]
        public void StatusEffect_DurationPolicy_Persistent_DoesNotExpireOverTime()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create(
                effectId: "test_persistent",
                durationPolicy: StatusEffectDurationPolicy.Persistent);

            Guid id = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.AreEqual(-1.0f, _statusComp.GetRemainingDuration(id), 0.001f);

            currentTime = 1000f;
            _statusComp.ReconcileTimedEffects();

            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsTrue(_statusComp.HasEffect(def));
        }

        [Test]
        public void StatusEffect_TimedExpiration_PrunesEffect()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create(
                effectId: "test_timed",
                durationPolicy: StatusEffectDurationPolicy.Timed,
                duration: 5f);

            bool expiredFired = false;
            bool removedFired = false;
            _statusComp.OnStatusEffectExpired += _ => expiredFired = true;
            _statusComp.OnStatusEffectRemoved += _ => removedFired = true;

            Guid id = _statusComp.ApplyEffectSimple(def);
            Assert.AreEqual(1, _statusComp.EffectCount);

            currentTime = 102f;
            Assert.AreEqual(3f, _statusComp.GetRemainingDuration(id), 0.001f);

            currentTime = 105f;
            _statusComp.ReconcileTimedEffects();

            Assert.IsTrue(expiredFired);
            Assert.IsTrue(removedFired);
            Assert.AreEqual(0, _statusComp.EffectCount);
            Assert.IsFalse(_statusComp.HasEffect(def));
        }

        [Test]
        public void StatusEffect_ManualRemoval_ByInstanceId()
        {
            var def1 = StatusEffectDefinition.Create("effect_1");
            var def2 = StatusEffectDefinition.Create("effect_2");

            Guid id1 = _statusComp.ApplyEffectSimple(def1);
            Guid id2 = _statusComp.ApplyEffectSimple(def2);
            Assert.AreEqual(2, _statusComp.EffectCount);

            Assert.IsTrue(_statusComp.RemoveEffect(id1));
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsFalse(_statusComp.HasEffect(def1));
            Assert.IsTrue(_statusComp.HasEffect(def2));

            Assert.IsFalse(_statusComp.RemoveEffect(id1));
            Assert.IsFalse(_statusComp.RemoveEffect(Guid.NewGuid()));
        }

        [Test]
        public void StatusEffect_ManualRemoval_ByDefinition()
        {
            var def1 = StatusEffectDefinition.Create("effect_1");
            var def2 = StatusEffectDefinition.Create("effect_2");

            _statusComp.ApplyEffectSimple(def1);
            _statusComp.ApplyEffectSimple(def2);

            Assert.IsTrue(_statusComp.RemoveEffectByDefinition(def1));
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsFalse(_statusComp.HasEffect(def1));
            Assert.IsTrue(_statusComp.HasEffect(def2));

            Assert.IsFalse(_statusComp.RemoveEffectByDefinition(def1));
        }

        [Test]
        public void StatusEffect_RemoveAllEffects_AndClear()
        {
            var def1 = StatusEffectDefinition.Create("effect_1");
            var def2 = StatusEffectDefinition.Create("effect_2");

            _statusComp.ApplyEffectSimple(def1);
            _statusComp.ApplyEffectSimple(def2);
            Assert.AreEqual(2, _statusComp.EffectCount);

            int removed = _statusComp.RemoveAllEffects();
            Assert.AreEqual(2, removed);
            Assert.AreEqual(0, _statusComp.EffectCount);

            _statusComp.ApplyEffectSimple(def1);
            Assert.AreEqual(1, _statusComp.EffectCount);
            _statusComp.ClearEffects();
            Assert.AreEqual(0, _statusComp.EffectCount);
        }

        [Test]
        public void StatusEffect_DisableAndReEnable_PreservesRemainingDuration()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create("buff", durationPolicy: StatusEffectDurationPolicy.Timed, duration: 10f);
            Guid id = _statusComp.ApplyEffectSimple(def);

            _statusComp.OnDisable();

            currentTime = 104f;
            Assert.AreEqual(6f, _statusComp.GetRemainingDuration(id), 0.001f);

            _statusComp.OnEnable();

            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsTrue(_statusComp.HasEffect(def));
            Assert.AreEqual(6f, _statusComp.GetRemainingDuration(id), 0.001f);
        }

        [Test]
        public void StatusEffect_ReEnableAfterExpiration_PrunesExpiredEffectImmediately()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create("buff", durationPolicy: StatusEffectDurationPolicy.Timed, duration: 5f);
            Guid id = _statusComp.ApplyEffectSimple(def);

            _statusComp.OnDisable();

            currentTime = 110f;
            Assert.AreEqual(0f, _statusComp.GetRemainingDuration(id), 0.001f);

            _statusComp.OnEnable();

            Assert.AreEqual(0, _statusComp.EffectCount);
            Assert.IsFalse(_statusComp.HasEffect(def));
        }

        [Test]
        public void StatusEffect_ReentrancyProtection_RejectsMutationDuringTransition()
        {
            var def1 = StatusEffectDefinition.Create("effect_1");
            var def2 = StatusEffectDefinition.Create("effect_2");

            Guid reentrantResult = Guid.NewGuid();
            _statusComp.OnStatusEffectApplied += _ =>
            {
                reentrantResult = _statusComp.ApplyEffectSimple(def2);
            };

            Guid id = _statusComp.ApplyEffectSimple(def1);

            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreEqual(Guid.Empty, reentrantResult);
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsTrue(_statusComp.HasEffect(def1));
            Assert.IsFalse(_statusComp.HasEffect(def2));
        }

        [Test]
        public void StatusEffect_EventSubscriberException_StateRemainsCommitted()
        {
            var def = StatusEffectDefinition.Create("effect_error");

            Action<StatusEffectInstance> throwingListener = _ => throw new InvalidOperationException("Test exception");
            _statusComp.OnStatusEffectApplied += throwingListener;

            Guid id = Guid.Empty;
            Assert.DoesNotThrow(() =>
            {
                id = _statusComp.ApplyEffectSimple(def);
            });

            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsTrue(_statusComp.HasEffect(def));

            _statusComp.OnStatusEffectApplied -= throwingListener;
        }

        [Test]
        public void StatusEffect_CollectionEncapsulation_CannotBeMutatedDirectly()
        {
            var def = StatusEffectDefinition.Create("effect_encap");
            _statusComp.ApplyEffectSimple(def);

            IReadOnlyList<StatusEffectInstance> list = _statusComp.ActiveEffects;
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);
            Assert.IsFalse(list is List<StatusEffectInstance>);
        }

        [Test]
        public void StatusEffect_StaleGenerationHandle_DoesNotRemoveNewInstance()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var def = StatusEffectDefinition.Create(
                "buff_gen",
                durationPolicy: StatusEffectDurationPolicy.Timed,
                duration: 5f,
                stackingPolicy: StatusEffectStackingPolicy.RefreshDuration);

            Guid id = _statusComp.ApplyEffectSimple(def);

            currentTime = 102f;
            _statusComp.ApplyEffectSimple(def); // Refreshes duration, bumps generation to 2

            // Simulate stale generation 1 waking up at t = 105f
            currentTime = 105f;
            _statusComp.HandleEffectExpired(id, generation: 1);

            Assert.AreEqual(1, _statusComp.EffectCount);
            Assert.IsTrue(_statusComp.HasEffect(def));

            // Genuine generation 2 expiring at t = 107f
            currentTime = 107f;
            _statusComp.HandleEffectExpired(id, generation: 2);

            Assert.AreEqual(0, _statusComp.EffectCount);
            Assert.IsFalse(_statusComp.HasEffect(def));
        }

        [Test]
        public void StatusEffect_DefinitionValidation()
        {
            var valid = StatusEffectDefinition.Create("valid_id", durationPolicy: StatusEffectDurationPolicy.Timed, duration: 3f);
            Assert.IsTrue(valid.IsValidDefinition(out string error));
            Assert.IsNull(error);

            var invalidId = StatusEffectDefinition.Create("");
            Assert.IsFalse(invalidId.IsValidDefinition(out error));
            Assert.IsNotNull(error);

            var invalidDuration = StatusEffectDefinition.Create("neg_dur", durationPolicy: StatusEffectDurationPolicy.Timed, duration: -1f);
            Assert.IsFalse(invalidDuration.IsValidDefinition(out error));
            Assert.IsNotNull(error);

            var invalidStacks = StatusEffectDefinition.Create("zero_stacks", stackingPolicy: StatusEffectStackingPolicy.AddStacks, maxStacks: 0);
            Assert.IsFalse(invalidStacks.IsValidDefinition(out error));
            Assert.IsNotNull(error);
        }

        [Test]
        public void StatusEffect_CharacterBaseIntegration()
        {
            var charGo = new GameObject("CharacterWithStatus");
            charGo.AddComponent<ShadowSlave.Attributes.AttributeComponent>();
            charGo.AddComponent<ShadowSlave.Progression.ProgressionComponent>();
            var status = charGo.AddComponent<StatusEffectComponent>();
            var character = charGo.AddComponent<CharacterBase>();

            Assert.IsNotNull(character.StatusEffectComponent);
            Assert.AreSame(status, character.StatusEffectComponent);

            Object.DestroyImmediate(charGo);
        }

        [Test]
        public void StatusEffect_RestoreEffects_HandlesPersistenceAndTimers()
        {
            float currentTime = 100f;
            _statusComp.SetTimeProvider(() => currentTime);

            var defTimed = StatusEffectDefinition.Create("timed_persist", durationPolicy: StatusEffectDurationPolicy.Timed, duration: 10f);
            var defExpired = StatusEffectDefinition.Create("timed_expired", durationPolicy: StatusEffectDurationPolicy.Timed, duration: 5f);
            var defPersistent = StatusEffectDefinition.Create("permanent", durationPolicy: StatusEffectDurationPolicy.Persistent);

            var inst1 = new StatusEffectInstance(defTimed)
            {
                TotalDuration = 10f,
                ExpirationTime = 108f // 8 seconds remaining
            };
            var inst2 = new StatusEffectInstance(defExpired)
            {
                TotalDuration = 5f,
                ExpirationTime = 95f // Already expired
            };
            var inst3 = new StatusEffectInstance(defPersistent);

            _statusComp.RestoreEffects(new[] { inst1, inst2, inst3 });

            Assert.AreEqual(2, _statusComp.EffectCount);
            Assert.IsTrue(_statusComp.HasEffect(defTimed));
            Assert.IsFalse(_statusComp.HasEffect(defExpired));
            Assert.IsTrue(_statusComp.HasEffect(defPersistent));
            Assert.AreEqual(8f, _statusComp.GetRemainingDuration(inst1.InstanceId), 0.001f);
        }
    }
}
