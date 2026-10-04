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

        /* --- Soul Core Foundation Tests --- */

        [Test]
        public void SoulCores_DefaultState_IsOneAndOne()
        {
            Assert.AreEqual(1, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, _progression.GetMaxSoulCores());

            SoulCoreState state = _progression.GetSoulCoreState();
            Assert.AreEqual(1, state.CurrentSoulCores);
            Assert.AreEqual(1, state.MaximumSoulCores);
            Assert.IsFalse(state.HasMultipleCores);
            Assert.IsTrue(state.IsMaxCoresReached);
        }

        [Test]
        public void SoulCores_Getters_ReturnCorrectValues()
        {
            _progression.SetMaxSoulCores(7);
            _progression.SetSoulCoreCount(4);

            Assert.AreEqual(4, _progression.GetSoulCoreCount());
            Assert.AreEqual(7, _progression.GetMaxSoulCores());

            SoulCoreState state = _progression.GetSoulCoreState();
            Assert.AreEqual(4, state.CurrentSoulCores);
            Assert.AreEqual(7, state.MaximumSoulCores);
            Assert.IsTrue(state.HasMultipleCores);
            Assert.IsFalse(state.IsMaxCoresReached);

            _progression.SetSoulCoreCount(7);
            Assert.IsTrue(_progression.GetSoulCoreState().IsMaxCoresReached);
        }

        [Test]
        public void SoulCoreState_ConstructorNormalization()
        {
            SoulCoreState state0 = new SoulCoreState(-5, 0);
            Assert.AreEqual(1, state0.MaximumSoulCores);
            Assert.AreEqual(0, state0.CurrentSoulCores);
            Assert.IsFalse(state0.HasMultipleCores);
            Assert.IsFalse(state0.IsMaxCoresReached);

            SoulCoreState state1 = new SoulCoreState(10, 4);
            Assert.AreEqual(4, state1.MaximumSoulCores);
            Assert.AreEqual(4, state1.CurrentSoulCores);
            Assert.IsTrue(state1.HasMultipleCores);
            Assert.IsTrue(state1.IsMaxCoresReached);

            SoulCoreState state2 = new SoulCoreState(3, 7);
            Assert.AreEqual(7, state2.MaximumSoulCores);
            Assert.AreEqual(3, state2.CurrentSoulCores);
            Assert.IsTrue(state2.HasMultipleCores);
            Assert.IsFalse(state2.IsMaxCoresReached);
        }

        [Test]
        public void SetSoulCoreCount_ValidChanges_FiresEvent()
        {
            _progression.SetMaxSoulCores(5);

            int eventCount = 0;
            int recordedNew = -1;
            int recordedOld = -1;

            _progression.OnSoulCoreCountChanged += (newCount, oldCount) =>
            {
                eventCount++;
                recordedNew = newCount;
                recordedOld = oldCount;
            };

            _progression.SetSoulCoreCount(3);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(3, recordedNew);
            Assert.AreEqual(1, recordedOld);
            Assert.AreEqual(3, _progression.GetSoulCoreCount());

            // 0 is valid for mundane/hollow states
            _progression.SetSoulCoreCount(0);
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(0, recordedNew);
            Assert.AreEqual(3, recordedOld);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());
        }

        [Test]
        public void SetSoulCoreCount_SameValue_IsNoOpAndDoesNotFireEvent()
        {
            _progression.SetSoulCoreCount(1);

            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            _progression.SetSoulCoreCount(1);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(1, _progression.GetSoulCoreCount());
        }

        [Test]
        public void SetSoulCoreCount_InvalidValues_ClampsToZeroAndMax()
        {
            _progression.SetMaxSoulCores(3);

            _progression.SetSoulCoreCount(-10);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());

            _progression.SetSoulCoreCount(100);
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
        }

        [Test]
        public void SetMaxSoulCores_IncreaseAndDecrease_FiresEvent()
        {
            int eventCount = 0;
            int recordedNew = -1;
            int recordedOld = -1;

            _progression.OnMaxSoulCoresChanged += (newMax, oldMax) =>
            {
                eventCount++;
                recordedNew = newMax;
                recordedOld = oldMax;
            };

            _progression.SetMaxSoulCores(4);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(4, recordedNew);
            Assert.AreEqual(1, recordedOld);
            Assert.AreEqual(4, _progression.GetMaxSoulCores());

            _progression.SetMaxSoulCores(2);
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(2, recordedNew);
            Assert.AreEqual(4, recordedOld);
            Assert.AreEqual(2, _progression.GetMaxSoulCores());
        }

        [Test]
        public void SetMaxSoulCores_SameValue_DoesNotFireEvent()
        {
            _progression.SetMaxSoulCores(3);

            int eventCount = 0;
            _progression.OnMaxSoulCoresChanged += (_, __) => eventCount++;

            _progression.SetMaxSoulCores(3);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetMaxSoulCores_InvalidValues_ClampsToOne()
        {
            _progression.SetMaxSoulCores(0);
            Assert.AreEqual(1, _progression.GetMaxSoulCores());

            _progression.SetMaxSoulCores(-5);
            Assert.AreEqual(1, _progression.GetMaxSoulCores());
        }

        [Test]
        public void SetMaxSoulCores_DecreasingBelowCurrent_ClampsCurrentAndFiresEventsInCorrectOrder()
        {
            _progression.SetMaxSoulCores(7);
            _progression.SetSoulCoreCount(5);

            var eventLog = new System.Collections.Generic.List<string>();

            _progression.OnMaxSoulCoresChanged += (newMax, oldMax) =>
            {
                eventLog.Add($"Max:{newMax},{oldMax}");
            };

            _progression.OnSoulCoreCountChanged += (newCount, oldCount) =>
            {
                eventLog.Add($"Count:{newCount},{oldCount}");
            };

            _progression.SetMaxSoulCores(3);

            Assert.AreEqual(2, eventLog.Count);
            Assert.AreEqual("Max:3,7", eventLog[0]);
            Assert.AreEqual("Count:3,5", eventLog[1]);
            Assert.AreEqual(3, _progression.GetMaxSoulCores());
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
        }

        [Test]
        public void AddSoulCore_ValidAddition_IncreasesCountAndFiresEvent()
        {
            _progression.SetMaxSoulCores(7);
            _progression.SetSoulCoreCount(1);

            int eventCount = 0;
            int recordedNew = -1;
            int recordedOld = -1;

            _progression.OnSoulCoreCountChanged += (newCount, oldCount) =>
            {
                eventCount++;
                recordedNew = newCount;
                recordedOld = oldCount;
            };

            bool success = _progression.AddSoulCore(2);
            Assert.IsTrue(success);
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(3, recordedNew);
            Assert.AreEqual(1, recordedOld);
        }

        [Test]
        public void AddSoulCore_ZeroOrNegative_ReturnsFalse_NoMutationOrEvent()
        {
            _progression.SetMaxSoulCores(5);
            _progression.SetSoulCoreCount(2);

            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            Assert.IsFalse(_progression.AddSoulCore(0));
            Assert.IsFalse(_progression.AddSoulCore(-1));
            Assert.AreEqual(2, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void AddSoulCore_ExceedingMaximum_ClampsToMaximum()
        {
            _progression.SetMaxSoulCores(4);
            _progression.SetSoulCoreCount(3);

            bool success = _progression.AddSoulCore(5);
            Assert.IsTrue(success);
            Assert.AreEqual(4, _progression.GetSoulCoreCount());

            // Adding when already at maximum should clamp to max and not fire new event
            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            bool atMaxSuccess = _progression.AddSoulCore(1);
            Assert.IsTrue(atMaxSuccess);
            Assert.AreEqual(4, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void AddSoulCore_IntMaxValue_OverflowSafe_ClampsToMaximum()
        {
            _progression.SetMaxSoulCores(5);
            _progression.SetSoulCoreCount(2);

            int eventCount = 0;
            int recordedNew = -1;
            int recordedOld = -1;

            _progression.OnSoulCoreCountChanged += (newCount, oldCount) =>
            {
                eventCount++;
                recordedNew = newCount;
                recordedOld = oldCount;
            };

            bool success = _progression.AddSoulCore(int.MaxValue);
            Assert.IsTrue(success);
            Assert.AreEqual(5, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(5, recordedNew);
            Assert.AreEqual(2, recordedOld);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaxSoulCores());

            // Adding int.MaxValue when already at maximum should not fire extra event
            bool atMaxSuccess = _progression.AddSoulCore(int.MaxValue);
            Assert.IsTrue(atMaxSuccess);
            Assert.AreEqual(5, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void RemoveSoulCore_ValidRemoval_DecreasesCountAndFiresEvent()
        {
            _progression.SetMaxSoulCores(7);
            _progression.SetSoulCoreCount(5);

            int eventCount = 0;
            int recordedNew = -1;
            int recordedOld = -1;

            _progression.OnSoulCoreCountChanged += (newCount, oldCount) =>
            {
                eventCount++;
                recordedNew = newCount;
                recordedOld = oldCount;
            };

            bool success = _progression.RemoveSoulCore(2);
            Assert.IsTrue(success);
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(3, recordedNew);
            Assert.AreEqual(5, recordedOld);
        }

        [Test]
        public void RemoveSoulCore_ZeroOrNegative_ReturnsFalse_NoMutationOrEvent()
        {
            _progression.SetSoulCoreCount(3);

            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            Assert.IsFalse(_progression.RemoveSoulCore(0));
            Assert.IsFalse(_progression.RemoveSoulCore(-2));
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void RemoveSoulCore_RemovingAllAndTooMany_ClampsToZero()
        {
            _progression.SetSoulCoreCount(2);

            bool success = _progression.RemoveSoulCore(2);
            Assert.IsTrue(success);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());

            // Removing when already at 0 clamps to 0 without firing extra events
            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            bool atZeroSuccess = _progression.RemoveSoulCore(5);
            Assert.IsTrue(atZeroSuccess);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void RemoveSoulCore_IntMaxValue_UnderflowSafe_ClampsToZero()
        {
            _progression.SetMaxSoulCores(5);
            _progression.SetSoulCoreCount(3);

            int eventCount = 0;
            int recordedNew = -1;
            int recordedOld = -1;

            _progression.OnSoulCoreCountChanged += (newCount, oldCount) =>
            {
                eventCount++;
                recordedNew = newCount;
                recordedOld = oldCount;
            };

            bool success = _progression.RemoveSoulCore(int.MaxValue);
            Assert.IsTrue(success);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(0, recordedNew);
            Assert.AreEqual(3, recordedOld);

            // Removing int.MaxValue when already at 0 remains 0 with no extra events
            bool atZeroSuccess = _progression.RemoveSoulCore(int.MaxValue);
            Assert.IsTrue(atZeroSuccess);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void Invariants_CurrentCountNeverViolatesBounds()
        {
            _progression.SetMaxSoulCores(5);
            _progression.SetSoulCoreCount(-100);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaxSoulCores());

            _progression.SetSoulCoreCount(100);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaxSoulCores());

            _progression.SetMaxSoulCores(-100);
            Assert.GreaterOrEqual(_progression.GetMaxSoulCores(), 1);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaxSoulCores());

            _progression.AddSoulCore(999);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaxSoulCores());

            _progression.RemoveSoulCore(999);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
        }

        [Test]
        public void SoulCoreOperations_DoNotMutate_CharacterRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());

            _progression.SetMaxSoulCores(5);
            _progression.SetSoulCoreCount(4);
            _progression.AddSoulCore(1);
            _progression.RemoveSoulCore(2);

            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());
        }

        [Test]
        public void SoulCoreOperations_DoNotMutate_Attributes()
        {
            GameObject testGo = new GameObject("AttributeInteractionTestActor");
            AttributeComponent attrs = testGo.AddComponent<AttributeComponent>();
            ProgressionComponent prog = testGo.AddComponent<ProgressionComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);

            prog.SetMaxSoulCores(7);
            prog.SetSoulCoreCount(7);
            prog.AddSoulCore(1);
            prog.RemoveSoulCore(3);

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);

            Object.DestroyImmediate(testGo);
        }

        /* --- Progression Metadata Tests --- */

        [Test]
        public void ProgressionMetadata_DefaultState_IsEmpty()
        {
            Assert.IsFalse(_progression.GetProgressionMetadata("Quest.Stage", out string val1));
            Assert.IsNull(val1);

            Assert.IsFalse(_progression.GetProgressionMetadata("AnyKey", out string val2));
            Assert.IsNull(val2);
        }

        [Test]
        public void SetProgressionMetadata_ThenGet_ReturnsStoredValue()
        {
            _progression.SetProgressionMetadata("Test.Key", "Value");

            bool found = _progression.GetProgressionMetadata("Test.Key", out string value);
            Assert.IsTrue(found);
            Assert.AreEqual("Value", value);
        }

        [Test]
        public void SetProgressionMetadata_ExistingKey_OverwritesValue()
        {
            _progression.SetProgressionMetadata("Test.Key", "One");
            bool found1 = _progression.GetProgressionMetadata("Test.Key", out string value1);
            Assert.IsTrue(found1);
            Assert.AreEqual("One", value1);

            _progression.SetProgressionMetadata("Test.Key", "Two");
            bool found2 = _progression.GetProgressionMetadata("Test.Key", out string value2);
            Assert.IsTrue(found2);
            Assert.AreEqual("Two", value2);
        }

        [Test]
        public void GetProgressionMetadata_MissingKey_ReturnsFalseAndDefaultOutValue()
        {
            bool found = _progression.GetProgressionMetadata("NonExistent", out string value);
            Assert.IsFalse(found);
            Assert.IsNull(value);
        }

        [Test]
        public void RemoveProgressionMetadata_ExistingKey_RemovesAndReturnsTrue()
        {
            _progression.SetProgressionMetadata("Story.Chapter", "1");
            Assert.IsTrue(_progression.GetProgressionMetadata("Story.Chapter", out _));

            bool removed = _progression.RemoveProgressionMetadata("Story.Chapter");
            Assert.IsTrue(removed);

            bool foundAfter = _progression.GetProgressionMetadata("Story.Chapter", out string value);
            Assert.IsFalse(foundAfter);
            Assert.IsNull(value);
        }

        [Test]
        public void RemoveProgressionMetadata_MissingKey_ReturnsFalse()
        {
            bool removed = _progression.RemoveProgressionMetadata("Missing.Key");
            Assert.IsFalse(removed);
        }

        [Test]
        public void ProgressionMetadata_MultipleIndependentKeys()
        {
            _progression.SetProgressionMetadata("Key.A", "Alpha");
            _progression.SetProgressionMetadata("Key.B", "Beta");
            _progression.SetProgressionMetadata("Key.C", "Gamma");

            Assert.IsTrue(_progression.GetProgressionMetadata("Key.A", out string valA));
            Assert.IsTrue(_progression.GetProgressionMetadata("Key.B", out string valB));
            Assert.IsTrue(_progression.GetProgressionMetadata("Key.C", out string valC));
            Assert.AreEqual("Alpha", valA);
            Assert.AreEqual("Beta", valB);
            Assert.AreEqual("Gamma", valC);

            bool removedA = _progression.RemoveProgressionMetadata("Key.A");
            Assert.IsTrue(removedA);

            Assert.IsFalse(_progression.GetProgressionMetadata("Key.A", out _));
            Assert.IsTrue(_progression.GetProgressionMetadata("Key.B", out string valBAfter));
            Assert.IsTrue(_progression.GetProgressionMetadata("Key.C", out string valCAfter));
            Assert.AreEqual("Beta", valBAfter);
            Assert.AreEqual("Gamma", valCAfter);
        }

        [Test]
        public void ProgressionMetadata_NullKey_SafelyRejected()
        {
            _progression.SetProgressionMetadata(null, "Ignored");
            Assert.IsFalse(_progression.GetProgressionMetadata(null, out string nullVal));
            Assert.IsNull(nullVal);
            Assert.IsFalse(_progression.RemoveProgressionMetadata(null));
        }

        [Test]
        public void ProgressionMetadata_EmptyKey_SafelyRejected()
        {
            _progression.SetProgressionMetadata("", "Ignored");
            Assert.IsFalse(_progression.GetProgressionMetadata("", out string emptyVal));
            Assert.IsNull(emptyVal);
            Assert.IsFalse(_progression.RemoveProgressionMetadata(""));
        }

        [Test]
        public void ProgressionMetadata_NullValue_SafelyRejected_DoesNotCreateEntry()
        {
            _progression.SetProgressionMetadata("NullValKey", null);
            Assert.IsFalse(_progression.GetProgressionMetadata("NullValKey", out string val));
            Assert.IsNull(val);
        }

        [Test]
        public void ProgressionMetadata_NullValue_DoesNotOverwriteExistingEntry()
        {
            _progression.SetProgressionMetadata("ExistingKey", "OriginalValue");
            Assert.IsTrue(_progression.GetProgressionMetadata("ExistingKey", out string valBefore));
            Assert.AreEqual("OriginalValue", valBefore);

            _progression.SetProgressionMetadata("ExistingKey", null);
            Assert.IsTrue(_progression.GetProgressionMetadata("ExistingKey", out string valAfter));
            Assert.AreEqual("OriginalValue", valAfter);
        }

        [Test]
        public void ProgressionMetadata_SerializationInvariant_DeduplicatesOnSet()
        {
            // Verify entry struct
            var entry1 = new ProgressionMetadataEntry("A", "1");
            var entry2 = new ProgressionMetadataEntry("A", "1");
            var entry3 = new ProgressionMetadataEntry("A", "2");
            Assert.AreEqual(entry1, entry2);
            Assert.AreNotEqual(entry1, entry3);
            Assert.AreEqual("A", entry1.Key);
            Assert.AreEqual("1", entry1.Value);

            // Test component deduplication when setting
            _progression.SetProgressionMetadata("DupKey", "Initial");
            _progression.SetProgressionMetadata("DupKey", "Updated");

            Assert.IsTrue(_progression.GetProgressionMetadata("DupKey", out string val));
            Assert.AreEqual("Updated", val);

            // Removing removes cleanly
            Assert.IsTrue(_progression.RemoveProgressionMetadata("DupKey"));
            Assert.IsFalse(_progression.GetProgressionMetadata("DupKey", out _));
        }

        [Test]
        public void ProgressionMetadata_Operations_DoNotMutate_CharacterRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Ascended);
            Assert.AreEqual(ShadowSlaveCharacterRank.Ascended, _progression.GetCharacterRank());

            _progression.SetProgressionMetadata("Quest.Stage", "5");
            _progression.GetProgressionMetadata("Quest.Stage", out _);
            _progression.RemoveProgressionMetadata("Quest.Stage");

            Assert.AreEqual(ShadowSlaveCharacterRank.Ascended, _progression.GetCharacterRank());
        }

        [Test]
        public void ProgressionMetadata_Operations_DoNotMutate_SoulCores()
        {
            _progression.SetMaxSoulCores(4);
            _progression.SetSoulCoreCount(3);

            _progression.SetProgressionMetadata("Story.Flag", "Cleared");
            _progression.GetProgressionMetadata("Story.Flag", out _);
            _progression.RemoveProgressionMetadata("Story.Flag");

            Assert.AreEqual(3, _progression.GetSoulCoreCount());
            Assert.AreEqual(4, _progression.GetMaxSoulCores());
        }

        [Test]
        public void ProgressionMetadata_Operations_DoNotMutate_Attributes()
        {
            GameObject testGo = new GameObject("MetadataAttributeTestActor");
            AttributeComponent attrs = testGo.AddComponent<AttributeComponent>();
            ProgressionComponent prog = testGo.AddComponent<ProgressionComponent>();
            attrs.InitializeAttributes(AttributeInitConfig.Default);

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);

            prog.SetProgressionMetadata("Encounter.Finished", "True");
            prog.GetProgressionMetadata("Encounter.Finished", out _);
            prog.RemoveProgressionMetadata("Encounter.Finished");

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);

            Object.DestroyImmediate(testGo);
        }
    }
}
