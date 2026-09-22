using NUnit.Framework;
using ShadowSlave.Attributes;
using ShadowSlave.Characters;
using ShadowSlave.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class CombatTests
    {
        private GameObject _attackerGo;
        private GameObject _targetGo;
        private CombatComponent _combat;
        private AttributeComponent _attackerAttrs;
        private AttributeComponent _targetAttrs;
        private CombatDummy _dummy;

        [SetUp]
        public void SetUp()
        {
            _attackerGo = new GameObject("Attacker");
            _attackerAttrs = _attackerGo.AddComponent<AttributeComponent>();
            _attackerAttrs.InitializeAttributes(AttributeInitConfig.Default);
            _combat = _attackerGo.AddComponent<CombatComponent>();

            _targetGo = new GameObject("DummyTarget");
            _targetAttrs = _targetGo.AddComponent<AttributeComponent>();
            _targetAttrs.InitializeAttributes(AttributeInitConfig.Default);
            _dummy = _targetGo.AddComponent<CombatDummy>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_attackerGo != null) Object.DestroyImmediate(_attackerGo);
            if (_targetGo != null) Object.DestroyImmediate(_targetGo);
        }

        [Test]
        public void DamageInfo_Construction_PreservesFields()
        {
            var attacker = _attackerGo;
            var causer = _attackerGo;
            var info = new DamageInfo(
                42f,
                attacker,
                causer,
                new Vector3(1f, 2f, 3f),
                Vector3.up,
                Vector3.forward,
                7);

            Assert.AreEqual(42f, info.DamageAmount, 0.001f);
            Assert.AreSame(attacker, info.Attacker);
            Assert.AreSame(causer, info.DamageCauser);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), info.HitLocation);
            Assert.AreEqual(Vector3.up, info.HitNormal);
            Assert.AreEqual(Vector3.forward, info.HitDirection);
            Assert.AreEqual(7, info.AttackInstanceId);
        }

        [Test]
        public void DamageInfo_Default_HasZeroAttackInstanceId()
        {
            var info = new DamageInfo(10f);
            Assert.AreEqual(0, info.AttackInstanceId);
            Assert.AreEqual(10f, info.DamageAmount, 0.001f);
        }

        [Test]
        public void DamageCalculator_Finalize_ClampsNegativeAndPassThrough()
        {
            Assert.AreEqual(0f, DamageCalculator.FinalizeAmount(new DamageInfo(-5f)), 0.001f);
            Assert.AreEqual(12f, DamageCalculator.FinalizeAmount(new DamageInfo(12f)), 0.001f);
            Assert.AreEqual(50f, DamageCalculator.ClampToRemainingHealth(80f, 50f), 0.001f);
            Assert.AreEqual(0f, DamageCalculator.ClampToRemainingHealth(10f, 0f), 0.001f);
            Assert.AreEqual(25f, DamageCalculator.CalculateApplied(new DamageInfo(25f), 100f), 0.001f);
        }

        [Test]
        public void IDamageable_Dummy_TakeDamage_ReducesHealth()
        {
            IDamageable damageable = _dummy;
            Assert.IsTrue(damageable.IsAlive());

            float applied = damageable.TakeDamage(new DamageInfo(30f, _attackerGo));
            Assert.AreEqual(30f, applied, 0.001f);
            Assert.AreEqual(70f, _targetAttrs.CurrentHealth, 0.001f);
            Assert.IsTrue(damageable.IsAlive());
        }

        [Test]
        public void IDamageable_Dummy_LethalDamage_Kills()
        {
            bool died = false;
            _dummy.OnDummyDied += () => died = true;

            float applied = _dummy.TakeDamage(new DamageInfo(200f, _attackerGo));
            Assert.AreEqual(100f, applied, 0.001f);
            Assert.IsFalse(_dummy.IsAlive());
            Assert.IsTrue(died);
            Assert.AreEqual(0f, _targetAttrs.CurrentHealth, 0.001f);
        }

        [Test]
        public void InvalidOrZeroDamage_DoesNotChangeHealth()
        {
            float before = _targetAttrs.CurrentHealth;
            Assert.AreEqual(0f, _dummy.TakeDamage(new DamageInfo(0f)), 0.001f);
            Assert.AreEqual(0f, _dummy.TakeDamage(new DamageInfo(-10f)), 0.001f);
            Assert.AreEqual(before, _targetAttrs.CurrentHealth, 0.001f);
        }

        [Test]
        public void MultipleDamageSources_Accumulate()
        {
            var sourceA = new GameObject("SourceA");
            var sourceB = new GameObject("SourceB");
            try
            {
                _dummy.TakeDamage(new DamageInfo(20f, sourceA));
                _dummy.TakeDamage(new DamageInfo(15f, sourceB));
                Assert.AreEqual(65f, _targetAttrs.CurrentHealth, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(sourceA);
                Object.DestroyImmediate(sourceB);
            }
        }

        [Test]
        public void CharacterBase_ImplementsIDamageable()
        {
            var go = new GameObject("Char");
            var attrs = go.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            var character = go.AddComponent<CharacterBase>();

            IDamageable damageable = character;
            Assert.IsTrue(damageable.IsAlive());
            float applied = damageable.TakeDamage(new DamageInfo(10f, _attackerGo));
            Assert.AreEqual(10f, applied, 0.001f);
            Assert.AreEqual(90f, character.GetCurrentHealth(), 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CombatComponent_ExecuteAttack_ChangesState_AndFiresEvents()
        {
            ECombatState oldState = ECombatState.Dead;
            ECombatState newState = ECombatState.Dead;
            EAttackType startedType = EAttackType.Heavy;
            int startedId = -1;
            bool executed = false;

            _combat.OnCombatStateChanged += (o, n) =>
            {
                oldState = o;
                newState = n;
            };
            _combat.OnAttackExecuted += t => executed = true;
            _combat.OnAttackStarted += (t, id) =>
            {
                startedType = t;
                startedId = id;
            };

            Assert.IsTrue(_combat.ExecuteAttack(EAttackType.Light));
            Assert.AreEqual(ECombatState.Neutral, oldState);
            Assert.AreEqual(ECombatState.Attacking, newState);
            Assert.AreEqual(ECombatState.Attacking, _combat.CombatState);
            Assert.IsTrue(executed);
            Assert.AreEqual(EAttackType.Light, startedType);
            Assert.Greater(startedId, 0);
            Assert.IsTrue(_combat.IsHitWindowActive);
        }

        [Test]
        public void CombatComponent_TryApplyHit_DamagesTarget_AndFiresHitEvents()
        {
            Assert.IsTrue(_combat.ExecuteAttack(EAttackType.Light));

            GameObject hitTarget = null;
            DamageInfo dealt = default;
            bool hitLanded = false;
            bool damageDealt = false;

            _combat.OnTargetHit += (t, info) => hitTarget = t;
            _combat.OnHitLanded += (_, __) => hitLanded = true;
            _combat.OnDamageDealt += info =>
            {
                damageDealt = true;
                dealt = info;
            };

            DamageInfo payload = _combat.BuildDamageInfoFromActiveAttack();
            Assert.IsTrue(_combat.TryApplyHit(_targetGo, payload));

            Assert.AreSame(_targetGo, hitTarget);
            Assert.IsTrue(hitLanded);
            Assert.IsTrue(damageDealt);
            Assert.AreEqual(25f, dealt.DamageAmount, 0.001f);
            Assert.AreEqual(75f, _targetAttrs.CurrentHealth, 0.001f);
            Assert.IsTrue(_combat.HasHitTargetThisAttack(_targetGo));
            Assert.AreEqual(1, _combat.GetHitCountForTargetThisAttack(_targetGo));
        }

        [Test]
        public void CombatComponent_MaxHitsPerTarget_BlocksRepeatHit()
        {
            _combat.LightAttackData = AttackData.DefaultLight;
            Assert.IsTrue(_combat.ExecuteAttack(EAttackType.Light));

            Assert.IsTrue(_combat.TryApplyHit(_targetGo, _combat.BuildDamageInfoFromActiveAttack()));
            Assert.IsFalse(_combat.TryApplyHit(_targetGo, _combat.BuildDamageInfoFromActiveAttack()));
            Assert.AreEqual(75f, _targetAttrs.CurrentHealth, 0.001f);
        }

        [Test]
        public void CombatComponent_CancelAttack_ReturnsToNeutral_AndEndsAttack()
        {
            bool ended = false;
            _combat.OnAttackEnded += (_, __) => ended = true;

            Assert.IsTrue(_combat.ExecuteAttack(EAttackType.Heavy));
            _combat.CancelAttack();

            Assert.AreEqual(ECombatState.Neutral, _combat.CombatState);
            Assert.IsTrue(ended);
            Assert.IsFalse(_combat.IsHitWindowActive);
        }

        [Test]
        public void CombatComponent_ApplyDamageToTarget_ViaAttributeWithoutIDamageable()
        {
            var bare = new GameObject("BareAttrs");
            var attrs = bare.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);

            float applied = _combat.ApplyDamageToTarget(bare, new DamageInfo(40f, _attackerGo));
            Assert.AreEqual(40f, applied, 0.001f);
            Assert.AreEqual(60f, attrs.CurrentHealth, 0.001f);

            Object.DestroyImmediate(bare);
        }

        [Test]
        public void CharacterBase_WithCombat_NotifiesDamageReceived_AndDeathLocksState()
        {
            var go = new GameObject("Fighter");
            var attrs = go.AddComponent<AttributeComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);
            var combat = go.AddComponent<CombatComponent>();
            var character = go.AddComponent<CharacterBase>();

            Assert.AreSame(combat, character.CombatComponent);

            DamageInfo received = default;
            bool got = false;
            combat.OnDamageReceived += info =>
            {
                got = true;
                received = info;
            };

            character.TakeDamage(new DamageInfo(20f, _attackerGo));
            Assert.IsTrue(got);
            Assert.AreEqual(20f, received.DamageAmount, 0.001f);

            character.TakeDamage(new DamageInfo(200f, _attackerGo));
            Assert.AreEqual(ECombatState.Dead, combat.CombatState);
            Assert.IsFalse(combat.CanPerformAttack(EAttackType.Light));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CombatState_RejectsAttackWhileNotNeutral()
        {
            Assert.IsTrue(_combat.ExecuteAttack(EAttackType.Light));
            Assert.IsFalse(_combat.CanPerformAttack(EAttackType.Heavy));
            Assert.IsFalse(_combat.ExecuteAttack(EAttackType.Heavy));
        }

        [Test]
        public void Healing_RemainsOnAttributes_NotCombat()
        {
            _dummy.TakeDamage(new DamageInfo(40f));
            float healed = _targetAttrs.Heal(15f);
            Assert.AreEqual(15f, healed, 0.001f);
            Assert.AreEqual(75f, _targetAttrs.CurrentHealth, 0.001f);
        }
    }
}
