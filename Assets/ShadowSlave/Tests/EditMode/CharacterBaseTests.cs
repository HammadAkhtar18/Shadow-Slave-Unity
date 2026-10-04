using NUnit.Framework;
using ShadowSlave.Attributes;
using ShadowSlave.Characters;
using ShadowSlave.Combat;
using ShadowSlave.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class CharacterBaseTests
    {
        private GameObject _go;
        private CharacterBase _character;
        private AttributeComponent _attrs;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("CharacterTestActor");
            _attrs = _go.AddComponent<AttributeComponent>();
            _go.AddComponent<ProgressionComponent>();
            _character = _go.AddComponent<CharacterBase>();
            _attrs.InitializeAttributes(AttributeInitConfig.Default);
            _character.CharacterId = "test_character";
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
        public void Character_Initializes_WithAttributeComponent()
        {
            Assert.IsNotNull(_character.AttributeComponent);
            Assert.AreSame(_attrs, _character.AttributeComponent);
            Assert.AreEqual("test_character", _character.CharacterId);
            Assert.IsTrue(_character.IsAlive());
            Assert.AreEqual(100f, _character.GetCurrentHealth(), 0.001f);
            Assert.AreEqual(100f, _character.GetMaxHealth(), 0.001f);
            Assert.AreEqual(ShadowSlaveGait.Walk, _character.Gait);
        }

        [Test]
        public void Character_TakeDamage_ForwardsToAttributes_AndEvents()
        {
            float health = -1f;
            DamageInfo received = default;
            bool damaged = false;

            _character.OnHealthChanged += (c, _) => health = c;
            _character.OnCharacterDamaged += info =>
            {
                damaged = true;
                received = info;
            };

            var info = new DamageInfo(25f, _go);
            float applied = _character.TakeDamage(info);

            Assert.AreEqual(25f, applied, 0.001f);
            Assert.AreEqual(75f, _character.GetCurrentHealth(), 0.001f);
            Assert.AreEqual(75f, health, 0.001f);
            Assert.IsTrue(damaged);
            Assert.AreEqual(25f, received.DamageAmount, 0.001f);
        }

        [Test]
        public void Character_Death_SuppressesMovement_AndRaisesEvent()
        {
            CharacterBase deadChar = null;
            GameObject killer = null;
            var attacker = new GameObject("Attacker");

            _character.OnCharacterDied += (c, k) =>
            {
                deadChar = c;
                killer = k;
            };

            _character.TakeDamage(new DamageInfo(100f, attacker));

            Assert.IsFalse(_character.IsAlive());
            Assert.IsFalse(_character.IsMovementControlEnabled);
            Assert.IsTrue(_character.IsMovementControlSuppressedBy("Death"));
            Assert.AreSame(_character, deadChar);
            Assert.AreSame(attacker, killer);

            Object.DestroyImmediate(attacker);
        }

        [Test]
        public void Gait_Sprint_RequiresStaminaAndAlive()
        {
            bool gaitChanged = false;
            _character.OnGaitChanged += (_, __) => gaitChanged = true;

            _character.StartSprint();
            Assert.AreEqual(ShadowSlaveGait.Sprint, _character.Gait);
            Assert.IsTrue(gaitChanged);

            _character.StopSprint();
            Assert.AreEqual(ShadowSlaveGait.Walk, _character.Gait);

            _attrs.SetStamina(0f);
            _character.StartSprint();
            Assert.AreEqual(ShadowSlaveGait.Walk, _character.Gait);
        }

        [Test]
        public void MovementSuppression_ByNamedSource()
        {
            _character.SetMovementControlSuppressed("Stun", true);
            Assert.IsFalse(_character.IsMovementControlEnabled);
            Assert.IsTrue(_character.IsMovementControlSuppressedBy("Stun"));

            _character.StartSprint();
            Assert.AreEqual(ShadowSlaveGait.Walk, _character.Gait);

            _character.SetMovementControlSuppressed("Stun", false);
            Assert.IsTrue(_character.IsMovementControlEnabled);
        }

        [Test]
        public void FutureComponents_AreNullSafe()
        {
            Assert.IsNull(_character.CombatComponent);
            Assert.IsNull(_character.EquipmentComponent);
            Assert.IsNull(_character.StatusEffectComponent);
        }
    }
}
