using System;
using NUnit.Framework;
using ShadowSlave.Attributes;
using ShadowSlave.Combat;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class AttributeComponentTests
    {
        private GameObject _go;
        private AttributeComponent _attrs;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("AttributeTestActor");
            _attrs = _go.AddComponent<AttributeComponent>();
            _attrs.InitializeAttributes(AttributeInitConfig.Default);
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
        public void InitializeAttributes_SetsMaxAndCurrent()
        {
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(100f, _attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, _attrs.MaximumStamina, 0.001f);
            Assert.AreEqual(100f, _attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, _attrs.MaximumEssence, 0.001f);
            Assert.AreEqual(100f, _attrs.CurrentEssence, 0.001f);
            Assert.IsTrue(_attrs.IsAlive);
            Assert.IsFalse(_attrs.IsDead);
        }

        [Test]
        public void ApplyDamage_ReducesHealth_AndClamps()
        {
            float applied = _attrs.ApplyDamage(40f);
            Assert.AreEqual(40f, applied, 0.001f);
            Assert.AreEqual(60f, _attrs.CurrentHealth, 0.001f);

            applied = _attrs.ApplyDamage(999f);
            Assert.AreEqual(60f, applied, 0.001f);
            Assert.AreEqual(0f, _attrs.CurrentHealth, 0.001f);
            Assert.IsTrue(_attrs.IsDead);
        }

        [Test]
        public void ApplyDamage_IgnoresNonPositive_AndDead()
        {
            Assert.AreEqual(0f, _attrs.ApplyDamage(0f));
            Assert.AreEqual(0f, _attrs.ApplyDamage(-10f));
            _attrs.ApplyDamage(100f);
            Assert.IsTrue(_attrs.IsDead);
            Assert.AreEqual(0f, _attrs.ApplyDamage(10f));
        }

        [Test]
        public void Heal_Restores_Clamps_AndIgnoresWhenDead()
        {
            _attrs.ApplyDamage(40f);
            float healed = _attrs.Heal(25f);
            Assert.AreEqual(25f, healed, 0.001f);
            Assert.AreEqual(85f, _attrs.CurrentHealth, 0.001f);

            Assert.AreEqual(15f, _attrs.Heal(100f), 0.001f);
            Assert.AreEqual(100f, _attrs.CurrentHealth, 0.001f);

            _attrs.ApplyDamage(100f);
            Assert.AreEqual(0f, _attrs.Heal(50f));
        }

        [Test]
        public void SetHealth_Clamps_AndCanKill()
        {
            _attrs.SetHealth(150f);
            Assert.AreEqual(100f, _attrs.CurrentHealth, 0.001f);

            _attrs.SetHealth(-5f);
            Assert.AreEqual(0f, _attrs.CurrentHealth, 0.001f);
            Assert.IsTrue(_attrs.IsDead);
        }

        [Test]
        public void ConsumeAndRestoreStamina()
        {
            Assert.IsTrue(_attrs.ConsumeStamina(30f));
            Assert.AreEqual(70f, _attrs.CurrentStamina, 0.001f);
            Assert.IsFalse(_attrs.ConsumeStamina(80f));
            Assert.AreEqual(70f, _attrs.CurrentStamina, 0.001f);

            Assert.AreEqual(20f, _attrs.RestoreStamina(20f), 0.001f);
            Assert.AreEqual(90f, _attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(10f, _attrs.RestoreStamina(50f), 0.001f);
        }

        [Test]
        public void ConsumeAndRestoreEssence()
        {
            Assert.IsTrue(_attrs.ConsumeEssence(40f));
            Assert.AreEqual(60f, _attrs.CurrentEssence, 0.001f);
            Assert.IsFalse(_attrs.ConsumeEssence(61f));
            Assert.AreEqual(25f, _attrs.RestoreEssence(25f), 0.001f);
            Assert.AreEqual(85f, _attrs.CurrentEssence, 0.001f);
        }

        [Test]
        public void InvalidResourceAmounts_AreRejected()
        {
            Assert.IsFalse(_attrs.ConsumeStamina(0f));
            Assert.IsFalse(_attrs.ConsumeStamina(-1f));
            Assert.IsFalse(_attrs.ConsumeEssence(0f));
            Assert.AreEqual(0f, _attrs.RestoreStamina(-5f));
            Assert.AreEqual(0f, _attrs.RestoreEssence(-5f));
            Assert.AreEqual(0f, _attrs.Heal(-1f));
        }

        [Test]
        public void Events_FireOnChange()
        {
            float healthCurrent = -1f, healthMax = -1f;
            float damageApplied = -1f;
            float healAmount = -1f;
            bool died = false;

            _attrs.OnHealthChanged += (c, m) => { healthCurrent = c; healthMax = m; };
            _attrs.OnDamageReceived += (a, _) => { damageApplied = a; };
            _attrs.OnHealReceived += a => { healAmount = a; };
            _attrs.OnDeath += () => { died = true; };

            _attrs.ApplyDamage(30f);
            Assert.AreEqual(70f, healthCurrent, 0.001f);
            Assert.AreEqual(100f, healthMax, 0.001f);
            Assert.AreEqual(30f, damageApplied, 0.001f);

            _attrs.Heal(10f);
            Assert.AreEqual(10f, healAmount, 0.001f);

            _attrs.ApplyDamage(100f);
            Assert.IsTrue(died);
            Assert.AreEqual(0f, healthCurrent, 0.001f);
        }

        [Test]
        public void LethalDamage_RaisesDeath_Once()
        {
            int deathCount = 0;
            _attrs.OnDeath += () => deathCount++;
            _attrs.ApplyDamage(100f);
            _attrs.ApplyDamage(10f);
            Assert.AreEqual(1, deathCount);
            Assert.IsTrue(_attrs.IsDead);
        }

        [Test]
        public void Modifiers_RecalculateEffectiveMax()
        {
            _attrs.AddModifier(new AttributeModifier(
                "flat_hp",
                AttributeType.MaxHealth,
                AttributeModifierType.Flat,
                50f));

            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);

            _attrs.AddModifier(new AttributeModifier(
                "pct_hp",
                AttributeType.MaxHealth,
                AttributeModifierType.Percent,
                0.1f));

            // (100 + 50) * 1.1 = 165
            Assert.AreEqual(165f, _attrs.MaximumHealth, 0.001f);

            Assert.IsTrue(_attrs.RemoveModifier("flat_hp"));
            // (100 + 0) * 1.1 = 110
            Assert.AreEqual(110f, _attrs.MaximumHealth, 0.001f);

            _attrs.ClearAllModifiers();
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
        }

        [Test]
        public void CustomInitConfig_AppliesBaselines()
        {
            var config = AttributeInitConfig.Default;
            config.BaseMaxHealth = 200f;
            config.BaseMaxStamina = 50f;
            config.BaseMaxEssence = 75f;
            _attrs.InitializeAttributes(config);

            Assert.AreEqual(200f, _attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(50f, _attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(75f, _attrs.CurrentEssence, 0.001f);
        }

        [Test]
        public void TimedModifier_ExpiresNormally()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));

            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);
            Assert.IsTrue(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(10f, _attrs.GetRemainingDuration("buff"), 0.001f);

            currentTime = 105f;
            Assert.AreEqual(5f, _attrs.GetRemainingDuration("buff"), 0.001f);

            currentTime = 110f;
            _attrs.ReconcileTimedModifiers();

            Assert.IsFalse(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
        }

        [Test]
        public void TimedModifier_RemainsActiveWhenComponentDisabled()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));
            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);

            _attrs.OnDisable();

            Assert.IsTrue(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);

            currentTime = 104f;
            Assert.AreEqual(6f, _attrs.GetRemainingDuration("buff"), 0.001f);
        }

        [Test]
        public void TimedModifier_ReEnabledBeforeExpiration_PreservesRemainingDuration()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));
            _attrs.OnDisable();

            currentTime = 104f;
            _attrs.OnEnable();

            Assert.IsTrue(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(6f, _attrs.GetRemainingDuration("buff"), 0.001f);
        }

        [Test]
        public void TimedModifier_ReEnabledAfterExpiration_PrunesModifierImmediately()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));
            _attrs.OnDisable();

            currentTime = 115f;
            Assert.AreEqual(0f, _attrs.GetRemainingDuration("buff"), 0.001f);

            _attrs.OnEnable();

            Assert.IsFalse(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(0, _attrs.ActiveModifiers.Count);
        }

        [Test]
        public void TimedModifier_MultipleModifiers_IndependentExpiration()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff_short", AttributeType.MaxHealth, AttributeModifierType.Flat, 20f, 5f));
            _attrs.AddModifier(new AttributeModifier("buff_long", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 15f));

            Assert.AreEqual(170f, _attrs.MaximumHealth, 0.001f);

            _attrs.OnDisable();

            currentTime = 108f;
            _attrs.OnEnable();

            Assert.IsFalse(_attrs.HasTimedModifier("buff_short"));
            Assert.IsTrue(_attrs.HasTimedModifier("buff_long"));
            Assert.AreEqual(7f, _attrs.GetRemainingDuration("buff_long"), 0.001f);
            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);

            currentTime = 116f;
            _attrs.ReconcileTimedModifiers();

            Assert.IsFalse(_attrs.HasTimedModifier("buff_long"));
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
        }

        [Test]
        public void TimedModifier_ManualRemoval_PreventsLaterExpirationFromRemovingAgain()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));
            currentTime = 102f;

            Assert.IsTrue(_attrs.RemoveModifier("buff"));
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
            Assert.IsFalse(_attrs.HasTimedModifier("buff"));
            Assert.IsFalse(_attrs.RemoveModifier("buff"));

            currentTime = 115f;
            _attrs.OnModifierExpired("buff");
            _attrs.ReconcileTimedModifiers();

            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
        }

        [Test]
        public void TimedModifier_ReEnable_DoesNotDuplicateExpiration()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            int healthChangeCount = 0;
            _attrs.OnHealthChanged += (_, _) => healthChangeCount++;

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));
            _attrs.OnDisable();
            _attrs.OnEnable();
            _attrs.OnDisable();
            _attrs.OnEnable();

            Assert.IsTrue(_attrs.HasTimedModifier("buff"));

            currentTime = 115f;
            _attrs.OnEnable();
            Assert.IsFalse(_attrs.HasTimedModifier("buff"));

            int countAfterExpiry = healthChangeCount;
            _attrs.OnEnable();
            Assert.AreEqual(countAfterExpiry, healthChangeCount);
        }

        [Test]
        public void TimedModifier_ExpirationOccursExactlyOnce()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            int healthChangeCount = 0;
            _attrs.OnHealthChanged += (_, _) => healthChangeCount++;

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));

            currentTime = 110f;
            _attrs.OnModifierExpired("buff");
            int countAfterFirstExpiry = healthChangeCount;

            _attrs.OnModifierExpired("buff");
            _attrs.ReconcileTimedModifiers();

            Assert.AreEqual(countAfterFirstExpiry, healthChangeCount);
            Assert.IsFalse(_attrs.HasTimedModifier("buff"));
        }

        [Test]
        public void TimedModifier_NonTimedModifiers_RemainUnaffectedByLifecycle()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("perm_flat", AttributeType.MaxHealth, AttributeModifierType.Flat, 30f, 0f));
            _attrs.AddModifier(new AttributeModifier("timed_buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 40f, 5f));

            Assert.AreEqual(170f, _attrs.MaximumHealth, 0.001f);

            _attrs.OnDisable();
            currentTime = 110f;
            _attrs.OnEnable();

            Assert.IsFalse(_attrs.HasTimedModifier("timed_buff"));
            Assert.AreEqual(130f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(1, _attrs.ActiveModifiers.Count);
            Assert.AreEqual("perm_flat", _attrs.ActiveModifiers[0].ModifierId);
        }

        [Test]
        public void TimedModifier_StackingAndFormulas_RemainAccurate()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("flat_1", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f));
            _attrs.AddModifier(new AttributeModifier("pct_timed", AttributeType.MaxHealth, AttributeModifierType.Percent, 0.2f, 10f));

            // (100 + 50) * 1.2 = 180
            Assert.AreEqual(180f, _attrs.MaximumHealth, 0.001f);

            currentTime = 110f;
            _attrs.ReconcileTimedModifiers();

            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);
        }

        [Test]
        public void TimedModifier_EventSubscriberThrows_StateRemainsConsistent()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 10f));

            Action<float, float> throwingSubscriber = (_, _) => throw new InvalidOperationException("Simulated listener exception");
            _attrs.OnHealthChanged += throwingSubscriber;

            currentTime = 110f;
            Assert.DoesNotThrow(() => _attrs.ReconcileTimedModifiers());

            Assert.IsFalse(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(0, _attrs.ActiveModifiers.Count);

            _attrs.OnHealthChanged -= throwingSubscriber;
        }

        [Test]
        public void TimedModifier_StaleGenerationHandle_DoesNotRemoveNewInstance()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 30f, 5f));

            currentTime = 102f;
            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 50f, 15f));

            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(15f, _attrs.GetRemainingDuration("buff"), 0.001f);

            currentTime = 105f;
            _attrs.OnModifierExpired("buff", generation: 1);

            Assert.IsTrue(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(150f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(12f, _attrs.GetRemainingDuration("buff"), 0.001f);

            currentTime = 117f;
            _attrs.OnModifierExpired("buff", generation: 2);

            Assert.IsFalse(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
        }

        [Test]
        public void TimedModifier_AddedWhileDisabled_ReconcilesUponEnable()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.OnDisable();
            _attrs.AddModifier(new AttributeModifier("buff", AttributeType.MaxHealth, AttributeModifierType.Flat, 40f, 10f));

            Assert.AreEqual(140f, _attrs.MaximumHealth, 0.001f);

            currentTime = 103f;
            _attrs.OnEnable();

            Assert.IsTrue(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(7f, _attrs.GetRemainingDuration("buff"), 0.001f);

            currentTime = 112f;
            _attrs.ReconcileTimedModifiers();

            Assert.IsFalse(_attrs.HasTimedModifier("buff"));
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
        }

        [Test]
        public void TimedModifier_ClearAllModifiers_ClearsTimedState()
        {
            float currentTime = 100f;
            _attrs.SetTimeProvider(() => currentTime);

            _attrs.AddModifier(new AttributeModifier("timed1", AttributeType.MaxHealth, AttributeModifierType.Flat, 20f, 10f));
            _attrs.AddModifier(new AttributeModifier("timed2", AttributeType.MaxStamina, AttributeModifierType.Flat, 30f, 15f));

            _attrs.ClearAllModifiers();

            Assert.IsFalse(_attrs.HasTimedModifier("timed1"));
            Assert.IsFalse(_attrs.HasTimedModifier("timed2"));
            Assert.AreEqual(0, _attrs.ActiveModifiers.Count);
            Assert.AreEqual(100f, _attrs.MaximumHealth, 0.001f);
            Assert.AreEqual(100f, _attrs.MaximumStamina, 0.001f);
        }
    }
}
