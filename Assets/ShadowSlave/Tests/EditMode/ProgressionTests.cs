using System;
using NUnit.Framework;
using ShadowSlave.Attributes;
using ShadowSlave.Characters;
using ShadowSlave.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class ProgressionTests
    {
        private GameObject _go;
        private ProgressionComponent _progression;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ProgressionTestActor");
            _progression = _go.AddComponent<ProgressionComponent>();
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
        public void RankEnum_ExactOrdinalOrdering()
        {
            Assert.AreEqual(0, (int)ShadowSlaveCharacterRank.Unknown);
            Assert.AreEqual(1, (int)ShadowSlaveCharacterRank.Dormant);
            Assert.AreEqual(2, (int)ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(3, (int)ShadowSlaveCharacterRank.Ascended);
            Assert.AreEqual(4, (int)ShadowSlaveCharacterRank.Transcendent);
            Assert.AreEqual(5, (int)ShadowSlaveCharacterRank.Supreme);
            Assert.AreEqual(6, (int)ShadowSlaveCharacterRank.Sacred);
            Assert.AreEqual(7, (int)ShadowSlaveCharacterRank.Divine);

            Assert.Less(ShadowSlaveCharacterRank.Unknown, ShadowSlaveCharacterRank.Dormant);
            Assert.Less(ShadowSlaveCharacterRank.Dormant, ShadowSlaveCharacterRank.Awakened);
            Assert.Less(ShadowSlaveCharacterRank.Awakened, ShadowSlaveCharacterRank.Ascended);
            Assert.Less(ShadowSlaveCharacterRank.Ascended, ShadowSlaveCharacterRank.Transcendent);
            Assert.Less(ShadowSlaveCharacterRank.Transcendent, ShadowSlaveCharacterRank.Supreme);
            Assert.Less(ShadowSlaveCharacterRank.Supreme, ShadowSlaveCharacterRank.Sacred);
            Assert.Less(ShadowSlaveCharacterRank.Sacred, ShadowSlaveCharacterRank.Divine);
        }

        [Test]
        public void ProgressionComponent_DefaultRank_IsUnknown()
        {
            Assert.AreEqual(ShadowSlaveCharacterRank.Unknown, _progression.GetCharacterRank());
            Assert.AreEqual(ShadowSlaveCharacterRank.Unknown, _progression.CurrentRank);
        }

        [Test]
        public void GetCharacterRank_ReturnsCurrentRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);
            Assert.AreEqual(ShadowSlaveCharacterRank.Dormant, _progression.GetCharacterRank());

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Transcendent);
            Assert.AreEqual(ShadowSlaveCharacterRank.Transcendent, _progression.GetCharacterRank());
        }

        [Test]
        public void HasKnownRank_IsFalseForUnknown()
        {
            Assert.IsFalse(_progression.HasKnownRank());

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.IsTrue(_progression.HasKnownRank());

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Unknown);
            Assert.IsFalse(_progression.HasKnownRank());
        }

        [Test]
        public void HasKnownRank_IsTrueForKnownRanks()
        {
            ShadowSlaveCharacterRank[] knownRanks =
            {
                ShadowSlaveCharacterRank.Dormant,
                ShadowSlaveCharacterRank.Awakened,
                ShadowSlaveCharacterRank.Ascended,
                ShadowSlaveCharacterRank.Transcendent,
                ShadowSlaveCharacterRank.Supreme,
                ShadowSlaveCharacterRank.Sacred,
                ShadowSlaveCharacterRank.Divine
            };

            foreach (ShadowSlaveCharacterRank rank in knownRanks)
            {
                _progression.SetCharacterRank(rank);
                Assert.IsTrue(_progression.HasKnownRank(), $"Rank {rank} should be recognized as known.");
            }
        }

        [Test]
        public void SetCharacterRank_ChangesRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Supreme);
            Assert.AreEqual(ShadowSlaveCharacterRank.Supreme, _progression.GetCharacterRank());
        }

        [Test]
        public void SetCharacterRank_FiresEvent_WithNewAndOldRank()
        {
            int eventCount = 0;
            ShadowSlaveCharacterRank recordedNew = ShadowSlaveCharacterRank.Unknown;
            ShadowSlaveCharacterRank recordedOld = ShadowSlaveCharacterRank.Unknown;

            _progression.OnCharacterRankChanged += (newRank, oldRank) =>
            {
                eventCount++;
                recordedNew = newRank;
                recordedOld = oldRank;
            };

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(ShadowSlaveCharacterRank.Dormant, recordedNew);
            Assert.AreEqual(ShadowSlaveCharacterRank.Unknown, recordedOld);

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, recordedNew);
            Assert.AreEqual(ShadowSlaveCharacterRank.Dormant, recordedOld);
        }

        [Test]
        public void SetCharacterRank_SameRank_IsNoOp()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());
        }

        [Test]
        public void SetCharacterRank_SameRank_DoesNotFireEvent()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            int eventCount = 0;
            _progression.OnCharacterRankChanged += (_, __) => eventCount++;

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void CanAdvanceRank_IsFalse_FromUnknown()
        {
            Assert.AreEqual(ShadowSlaveCharacterRank.Unknown, _progression.GetCharacterRank());
            Assert.IsFalse(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Dormant));
            Assert.IsFalse(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Awakened));
            Assert.IsFalse(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Divine));
        }

        [Test]
        public void CanAdvanceRank_IsTrue_ForStrictlyHigherKnownRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);
            Assert.IsTrue(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Awakened));
            Assert.IsTrue(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Ascended));
            Assert.IsTrue(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Divine));
        }

        [Test]
        public void CanAdvanceRank_IsFalse_ForSameRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.IsFalse(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Awakened));
        }

        [Test]
        public void CanAdvanceRank_IsFalse_ForLowerRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Ascended);
            Assert.IsFalse(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Awakened));
            Assert.IsFalse(_progression.CanAdvanceRank(ShadowSlaveCharacterRank.Dormant));
        }

        [Test]
        public void CanAdvanceRank_DoesNotMutateState()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Ascended);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());

            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Dormant);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());

            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());

            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Unknown);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());
        }

        [Test]
        public void CanAdvanceRank_DoesNotFireEvent()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            int eventCount = 0;
            _progression.OnCharacterRankChanged += (_, __) => eventCount++;

            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Ascended);
            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Dormant);
            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Awakened);
            _progression.CanAdvanceRank(ShadowSlaveCharacterRank.Unknown);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void AdvanceRank_Succeeds_ForValidHigherRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);
            bool success = _progression.AdvanceRank(ShadowSlaveCharacterRank.Awakened);
            Assert.IsTrue(success);
        }

        [Test]
        public void AdvanceRank_ChangesRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);
            _progression.AdvanceRank(ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());
        }

        [Test]
        public void AdvanceRank_FiresExactlyOneEvent()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Dormant);

            int eventCount = 0;
            ShadowSlaveCharacterRank recordedNew = ShadowSlaveCharacterRank.Unknown;
            ShadowSlaveCharacterRank recordedOld = ShadowSlaveCharacterRank.Unknown;

            _progression.OnCharacterRankChanged += (newRank, oldRank) =>
            {
                eventCount++;
                recordedNew = newRank;
                recordedOld = oldRank;
            };

            bool success = _progression.AdvanceRank(ShadowSlaveCharacterRank.Awakened);
            Assert.IsTrue(success);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, recordedNew);
            Assert.AreEqual(ShadowSlaveCharacterRank.Dormant, recordedOld);
        }

        [Test]
        public void AdvanceRank_Fails_ForInvalidOrNonAdvancingTarget()
        {
            // From Unknown
            Assert.IsFalse(_progression.AdvanceRank(ShadowSlaveCharacterRank.Dormant));

            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            // Same rank
            Assert.IsFalse(_progression.AdvanceRank(ShadowSlaveCharacterRank.Awakened));

            // Lower rank
            Assert.IsFalse(_progression.AdvanceRank(ShadowSlaveCharacterRank.Dormant));

            // Target is Unknown
            Assert.IsFalse(_progression.AdvanceRank(ShadowSlaveCharacterRank.Unknown));

            // Out-of-range / invalid enum values
            Assert.IsFalse(_progression.AdvanceRank((ShadowSlaveCharacterRank)99));
            Assert.IsFalse(_progression.AdvanceRank((ShadowSlaveCharacterRank)(-1)));
        }

        [Test]
        public void AdvanceRank_Failed_DoesNotMutateState_OrFireEvent()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);

            int eventCount = 0;
            _progression.OnCharacterRankChanged += (_, __) => eventCount++;

            Assert.IsFalse(_progression.AdvanceRank(ShadowSlaveCharacterRank.Dormant));
            Assert.IsFalse(_progression.AdvanceRank(ShadowSlaveCharacterRank.Awakened));
            Assert.IsFalse(_progression.AdvanceRank(ShadowSlaveCharacterRank.Unknown));
            Assert.IsFalse(_progression.AdvanceRank((ShadowSlaveCharacterRank)99));

            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void CharacterBase_Requires_ProgressionComponent()
        {
            RequireComponent[] reqAttrs = (RequireComponent[])typeof(CharacterBase).GetCustomAttributes(typeof(RequireComponent), true);
            bool foundProgressionRequirement = false;

            foreach (RequireComponent req in reqAttrs)
            {
                if (req.m_Type0 == typeof(ProgressionComponent) ||
                    req.m_Type1 == typeof(ProgressionComponent) ||
                    req.m_Type2 == typeof(ProgressionComponent))
                {
                    foundProgressionRequirement = true;
                    break;
                }
            }

            Assert.IsTrue(foundProgressionRequirement, "CharacterBase must declare RequireComponent for ProgressionComponent.");
        }

        [Test]
        public void CharacterBase_CanAccess_ProgressionComponent()
        {
            GameObject testGo = new GameObject("CharacterProgressionTestActor");
            testGo.AddComponent<AttributeComponent>();
            ProgressionComponent prog = testGo.AddComponent<ProgressionComponent>();
            CharacterBase character = testGo.AddComponent<CharacterBase>();

            Assert.IsNotNull(character.ProgressionComponent);
            Assert.AreSame(prog, character.ProgressionComponent);

            Object.DestroyImmediate(testGo);
        }
    }
}
