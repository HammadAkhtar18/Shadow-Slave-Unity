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
            Assert.AreEqual(1, _progression.CurrentSoulCores);
            Assert.AreEqual(1, _progression.GetMaximumSoulCores());
            Assert.AreEqual(1, _progression.GetMaxSoulCores());
            Assert.AreEqual(1, _progression.MaximumSoulCores);
            Assert.IsFalse(_progression.HasMultipleCores());
            Assert.IsTrue(_progression.IsMaxCoresReached());

            SoulCoreState state = _progression.GetSoulCoreState();
            Assert.AreEqual(1, state.CurrentSoulCores);
            Assert.AreEqual(1, state.MaximumSoulCores);
            Assert.IsFalse(state.HasMultipleCores);
            Assert.IsTrue(state.IsMaxCoresReached);
        }

        [Test]
        public void SoulCores_Getters_ReturnCorrectValues()
        {
            _progression.SetMaximumSoulCores(7);
            _progression.SetSoulCoreCount(4);

            Assert.AreEqual(4, _progression.GetSoulCoreCount());
            Assert.AreEqual(4, _progression.CurrentSoulCores);
            Assert.AreEqual(7, _progression.GetMaximumSoulCores());
            Assert.AreEqual(7, _progression.GetMaxSoulCores());
            Assert.AreEqual(7, _progression.MaximumSoulCores);
            Assert.IsTrue(_progression.HasMultipleCores());
            Assert.IsFalse(_progression.IsMaxCoresReached());

            _progression.SetSoulCoreCount(7);
            Assert.IsTrue(_progression.IsMaxCoresReached());
        }

        [Test]
        public void SetSoulCoreCount_ValidChanges_FiresEvent()
        {
            _progression.SetMaximumSoulCores(5);

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
            _progression.SetMaximumSoulCores(3);

            _progression.SetSoulCoreCount(-10);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());

            _progression.SetSoulCoreCount(100);
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
        }

        [Test]
        public void SetMaximumSoulCores_IncreaseAndDecrease_FiresEvent()
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

            _progression.SetMaximumSoulCores(4);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(4, recordedNew);
            Assert.AreEqual(1, recordedOld);
            Assert.AreEqual(4, _progression.GetMaximumSoulCores());

            _progression.SetMaximumSoulCores(2);
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(2, recordedNew);
            Assert.AreEqual(4, recordedOld);
            Assert.AreEqual(2, _progression.GetMaximumSoulCores());
        }

        [Test]
        public void SetMaximumSoulCores_SameValue_DoesNotFireEvent()
        {
            _progression.SetMaximumSoulCores(3);

            int eventCount = 0;
            _progression.OnMaxSoulCoresChanged += (_, __) => eventCount++;

            _progression.SetMaximumSoulCores(3);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetMaximumSoulCores_InvalidValues_ClampsToOne()
        {
            _progression.SetMaximumSoulCores(0);
            Assert.AreEqual(1, _progression.GetMaximumSoulCores());

            _progression.SetMaximumSoulCores(-5);
            Assert.AreEqual(1, _progression.GetMaximumSoulCores());
        }

        [Test]
        public void SetMaximumSoulCores_DecreasingBelowCurrent_ClampsCurrentAndFiresEventsInCorrectOrder()
        {
            _progression.SetMaximumSoulCores(7);
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

            _progression.SetMaximumSoulCores(3);

            Assert.AreEqual(2, eventLog.Count);
            Assert.AreEqual("Max:3,7", eventLog[0]);
            Assert.AreEqual("Count:3,5", eventLog[1]);
            Assert.AreEqual(3, _progression.GetMaximumSoulCores());
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
        }

        [Test]
        public void AddSoulCores_ValidAddition_IncreasesCountAndFiresEvent()
        {
            _progression.SetMaximumSoulCores(7);
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

            bool success = _progression.AddSoulCores(2);
            Assert.IsTrue(success);
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(3, recordedNew);
            Assert.AreEqual(1, recordedOld);
        }

        [Test]
        public void AddSoulCores_ZeroOrNegative_ReturnsFalse_NoMutationOrEvent()
        {
            _progression.SetMaximumSoulCores(5);
            _progression.SetSoulCoreCount(2);

            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            Assert.IsFalse(_progression.AddSoulCores(0));
            Assert.IsFalse(_progression.AddSoulCores(-1));
            Assert.AreEqual(2, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void AddSoulCores_ExceedingMaximum_ClampsToMaximum()
        {
            _progression.SetMaximumSoulCores(4);
            _progression.SetSoulCoreCount(3);

            bool success = _progression.AddSoulCores(5);
            Assert.IsTrue(success);
            Assert.AreEqual(4, _progression.GetSoulCoreCount());

            // Adding when already at maximum should clamp to max and not fire new event
            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            bool atMaxSuccess = _progression.AddSoulCores(1);
            Assert.IsTrue(atMaxSuccess);
            Assert.AreEqual(4, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void RemoveSoulCores_ValidRemoval_DecreasesCountAndFiresEvent()
        {
            _progression.SetMaximumSoulCores(7);
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

            bool success = _progression.RemoveSoulCores(2);
            Assert.IsTrue(success);
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(3, recordedNew);
            Assert.AreEqual(5, recordedOld);
        }

        [Test]
        public void RemoveSoulCores_ZeroOrNegative_ReturnsFalse_NoMutationOrEvent()
        {
            _progression.SetSoulCoreCount(3);

            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            Assert.IsFalse(_progression.RemoveSoulCores(0));
            Assert.IsFalse(_progression.RemoveSoulCores(-2));
            Assert.AreEqual(3, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void RemoveSoulCores_RemovingAllAndTooMany_ClampsToZero()
        {
            _progression.SetSoulCoreCount(2);

            bool success = _progression.RemoveSoulCores(2);
            Assert.IsTrue(success);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());

            // Removing when already at 0 clamps to 0 without firing extra events
            int eventCount = 0;
            _progression.OnSoulCoreCountChanged += (_, __) => eventCount++;

            bool atZeroSuccess = _progression.RemoveSoulCores(5);
            Assert.IsTrue(atZeroSuccess);
            Assert.AreEqual(0, _progression.GetSoulCoreCount());
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Invariants_CurrentCountNeverViolatesBounds()
        {
            _progression.SetMaximumSoulCores(5);
            _progression.SetSoulCoreCount(-100);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaximumSoulCores());

            _progression.SetSoulCoreCount(100);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaximumSoulCores());

            _progression.SetMaximumSoulCores(-100);
            Assert.GreaterOrEqual(_progression.GetMaximumSoulCores(), 1);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaximumSoulCores());

            _progression.AddSoulCores(999);
            Assert.LessOrEqual(_progression.GetSoulCoreCount(), _progression.GetMaximumSoulCores());

            _progression.RemoveSoulCores(999);
            Assert.GreaterOrEqual(_progression.GetSoulCoreCount(), 0);
        }

        [Test]
        public void SoulCoreOperations_DoNotMutate_CharacterRank()
        {
            _progression.SetCharacterRank(ShadowSlaveCharacterRank.Awakened);
            Assert.AreEqual(ShadowSlaveCharacterRank.Awakened, _progression.GetCharacterRank());

            _progression.SetMaximumSoulCores(5);
            _progression.SetSoulCoreCount(4);
            _progression.AddSoulCores(1);
            _progression.RemoveSoulCores(2);

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

            prog.SetMaximumSoulCores(7);
            prog.SetSoulCoreCount(7);
            prog.AddSoulCores(1);
            prog.RemoveSoulCores(3);

            Assert.AreEqual(100f, attrs.CurrentHealth, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentStamina, 0.001f);
            Assert.AreEqual(100f, attrs.CurrentEssence, 0.001f);

            Object.DestroyImmediate(testGo);
        }

        [Test]
        public void Aliases_GetMaxSoulCores_AddSoulCore_RemoveSoulCore_MatchExactBehavior()
        {
            _progression.SetMaxSoulCores(5);
            Assert.AreEqual(5, _progression.GetMaxSoulCores());

            _progression.SetSoulCoreCount(1);
            Assert.IsTrue(_progression.AddSoulCore(2));
            Assert.AreEqual(3, _progression.GetSoulCoreCount());

            Assert.IsTrue(_progression.RemoveSoulCore(1));
            Assert.AreEqual(2, _progression.GetSoulCoreCount());

            Assert.IsFalse(_progression.AddSoulCore(0));
            Assert.IsFalse(_progression.RemoveSoulCore(-1));
        }
    }
}
