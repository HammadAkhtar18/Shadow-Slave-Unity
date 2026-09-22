using System.Collections.Generic;
using NUnit.Framework;
using ShadowSlave.Interaction;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadowSlave.Tests.EditMode
{
    public class InteractionTests
    {
        private GameObject _ownerGo;
        private InteractionComponent _interaction;

        [SetUp]
        public void SetUp()
        {
            _ownerGo = new GameObject("Interactor");
            _interaction = _ownerGo.AddComponent<InteractionComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_ownerGo != null)
            {
                Object.DestroyImmediate(_ownerGo);
            }
        }

        [Test]
        public void TryInteract_NoTarget_ReturnsFalse_NoOnInteracted()
        {
            int interactedCount = 0;
            _interaction.OnInteracted += (_, __, ___) => interactedCount++;

            Assert.IsFalse(_interaction.HasInteractableTarget);
            Assert.IsFalse(_interaction.TryInteract());
            Assert.AreEqual(0, interactedCount);
        }

        [Test]
        public void TryInteract_CannotInteract_DoesNotCallInteract_NoOnInteracted()
        {
            var stub = new TrackingInteractableStub { AllowInteract = false };
            int interactedCount = 0;
            _interaction.OnInteracted += (_, __, ___) => interactedCount++;

            _interaction.SetCurrentInteractable(stub);
            Assert.IsTrue(_interaction.HasInteractableTarget);
            Assert.IsFalse(_interaction.TryInteract());
            Assert.AreEqual(0, stub.InteractCallCount);
            Assert.AreEqual(0, interactedCount);
        }

        [Test]
        public void TryInteract_Successful_CallsInteractOnce_FiresOnInteractedOnce()
        {
            var targetGo = new GameObject("Target");
            var behaviour = targetGo.AddComponent<InteractableBehaviour>();
            behaviour.InteractionId = "open_door";
            behaviour.InteractionPrompt = "Open";

            GameObject gotOwner = null;
            GameObject gotTarget = null;
            InteractionResult gotResult = default;
            int interactedCount = 0;
            _interaction.OnInteracted += (owner, target, result) =>
            {
                interactedCount++;
                gotOwner = owner;
                gotTarget = target;
                gotResult = result;
            };

            _interaction.SetCurrentInteractable(targetGo);
            Assert.IsTrue(_interaction.TryInteract());
            Assert.AreEqual(1, interactedCount);
            Assert.AreSame(_ownerGo, gotOwner);
            Assert.AreSame(targetGo, gotTarget);
            Assert.IsTrue(gotResult.Success);
            Assert.AreEqual("open_door", gotResult.InteractionId);
            Assert.AreEqual("Open", _interaction.GetCurrentInteractionPrompt());

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void TryInteract_FailedResult_StillFiresOnInteracted_ReturnsFalse()
        {
            var stub = new TrackingInteractableStub
            {
                AllowInteract = true,
                InteractResult = InteractionResult.Failure("locked", "chest")
            };

            int interactedCount = 0;
            InteractionResult gotResult = default;
            _interaction.OnInteracted += (_, __, result) =>
            {
                interactedCount++;
                gotResult = result;
            };

            _interaction.SetCurrentInteractable(stub);
            Assert.IsFalse(_interaction.TryInteract());
            Assert.AreEqual(1, stub.InteractCallCount);
            Assert.AreEqual(1, interactedCount);
            Assert.IsFalse(gotResult.Success);
            Assert.AreEqual("locked", gotResult.FailureReason);
            Assert.AreEqual("chest", gotResult.InteractionId);
        }

        [Test]
        public void TargetChanged_FiresOnlyOnActualChange()
        {
            var a = new GameObject("A");
            a.AddComponent<InteractableBehaviour>();
            var b = new GameObject("B");
            b.AddComponent<InteractableBehaviour>();

            var changes = new List<(GameObject neu, GameObject old)>();
            _interaction.OnInteractionTargetChanged += (neu, old) => changes.Add((neu, old));

            _interaction.SetCurrentInteractable(a);
            Assert.AreEqual(1, changes.Count);
            Assert.AreSame(a, changes[0].neu);
            Assert.IsNull(changes[0].old);

            _interaction.SetCurrentInteractable(a);
            Assert.AreEqual(1, changes.Count);

            _interaction.SetCurrentInteractable(b);
            Assert.AreEqual(2, changes.Count);
            Assert.AreSame(b, changes[1].neu);
            Assert.AreSame(a, changes[1].old);

            _interaction.ClearCurrentInteractable();
            Assert.AreEqual(3, changes.Count);
            Assert.IsNull(changes[2].neu);
            Assert.AreSame(b, changes[2].old);

            _interaction.ClearCurrentInteractable();
            Assert.AreEqual(3, changes.Count);

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void TargetRevalidation_LaterCanInteractFalse_RejectsWithoutInteract()
        {
            var stub = new TrackingInteractableStub { AllowInteract = true };
            int interactedCount = 0;
            _interaction.OnInteracted += (_, __, ___) => interactedCount++;

            _interaction.SetCurrentInteractable(stub);
            Assert.IsTrue(_interaction.TryInteract());
            Assert.AreEqual(1, stub.InteractCallCount);
            Assert.AreEqual(1, interactedCount);

            stub.AllowInteract = false;
            Assert.IsFalse(_interaction.TryInteract());
            Assert.AreEqual(1, stub.InteractCallCount);
            Assert.AreEqual(1, interactedCount);
        }

        [Test]
        public void InteractionResult_SuccessAndFailure_DataAndMetadata()
        {
            var ok = InteractionResult.Succeeded("id_ok");
            Assert.IsTrue(ok.Success);
            Assert.AreEqual(string.Empty, ok.FailureReason);
            Assert.AreEqual("id_ok", ok.InteractionId);
            Assert.IsNull(ok.Metadata);

            var fail = InteractionResult.Failure("busy", "id_fail");
            Assert.IsFalse(fail.Success);
            Assert.AreEqual("busy", fail.FailureReason);
            Assert.AreEqual("id_fail", fail.InteractionId);

            var withMeta = new InteractionResult(
                true,
                string.Empty,
                "meta_id",
                new Dictionary<string, string> { { "quest", "q1" } });
            Assert.IsTrue(withMeta.Success);
            Assert.AreEqual("q1", withMeta.Metadata["quest"]);
            Assert.AreEqual("meta_id", withMeta.InteractionId);

            var defaults = InteractionResult.Succeeded();
            Assert.AreEqual(string.Empty, defaults.InteractionId);
        }

        [Test]
        public void Priority_ExposedCorrectly_NoRanking()
        {
            var targetGo = new GameObject("PrioTarget");
            var behaviour = targetGo.AddComponent<InteractableBehaviour>();
            behaviour.InteractionPriority = 7;

            Assert.AreEqual(7, behaviour.GetInteractionPriority(_ownerGo));

            var stub = new TrackingInteractableStub { Priority = 3 };
            Assert.AreEqual(3, stub.GetInteractionPriority(_ownerGo));

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void InteractableBehaviour_Disabled_CanInteractFalse_InteractReturnsFailure()
        {
            var targetGo = new GameObject("Disabled");
            var behaviour = targetGo.AddComponent<InteractableBehaviour>();
            behaviour.InteractionEnabled = false;
            behaviour.InteractionId = "x";

            Assert.IsFalse(behaviour.CanInteract(_ownerGo));
            InteractionResult result = behaviour.Interact(_ownerGo);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("x", result.InteractionId);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void SetCurrentInteractable_ByInterface_UsesMonoBehaviourGameObject()
        {
            var targetGo = new GameObject("ViaInterface");
            var behaviour = targetGo.AddComponent<InteractableBehaviour>();
            behaviour.InteractionPrompt = "Talk";

            _interaction.SetCurrentInteractable((IInteractable)behaviour);
            Assert.IsTrue(_interaction.HasInteractableTarget);
            Assert.AreSame(behaviour, _interaction.CurrentInteractable);
            Assert.AreEqual("Talk", _interaction.GetCurrentInteractionPrompt());

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void SetInteractionDetectionEnabled_False_ClearsTarget_DoesNotPoll()
        {
            var targetGo = new GameObject("DetectFlag");
            targetGo.AddComponent<InteractableBehaviour>();
            _interaction.SetCurrentInteractable(targetGo);
            Assert.IsTrue(_interaction.HasInteractableTarget);

            _interaction.SetInteractionDetectionEnabled(false);
            Assert.IsFalse(_interaction.IsInteractionDetectionEnabled);
            Assert.IsFalse(_interaction.HasInteractableTarget);

            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void TryInteract_DestroyedTarget_ClearsAndReturnsFalse()
        {
            var targetGo = new GameObject("Temp");
            targetGo.AddComponent<InteractableBehaviour>();
            _interaction.SetCurrentInteractable(targetGo);

            int interactedCount = 0;
            _interaction.OnInteracted += (_, __, ___) => interactedCount++;

            Object.DestroyImmediate(targetGo);

            Assert.IsFalse(_interaction.TryInteract());
            Assert.AreEqual(0, interactedCount);
            Assert.IsFalse(_interaction.HasInteractableTarget);
        }

        /// <summary>Plain C# IInteractable stub for Edit Mode tests (no MonoBehaviour required).</summary>
        private sealed class TrackingInteractableStub : IInteractable
        {
            public bool AllowInteract = true;
            public int Priority;
            public string Prompt = "Stub";
            public InteractionResult InteractResult = InteractionResult.Succeeded("stub");
            public int InteractCallCount { get; private set; }

            public bool CanInteract(GameObject interactor)
            {
                return AllowInteract && interactor != null;
            }

            public string GetInteractionPrompt(GameObject interactor)
            {
                return Prompt;
            }

            public InteractionResult Interact(GameObject interactor)
            {
                InteractCallCount++;
                return InteractResult;
            }

            public int GetInteractionPriority(GameObject interactor)
            {
                return Priority;
            }
        }
    }
}
