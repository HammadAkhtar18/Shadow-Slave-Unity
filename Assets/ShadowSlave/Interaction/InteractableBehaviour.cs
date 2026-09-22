using UnityEngine;

namespace ShadowSlave.Interaction
{
    /// <summary>
    /// Optional reusable MonoBehaviour base for interactable world objects.
    /// Mirrors essentials of UE AShadowSlaveInteractableActor without mesh/save/concrete types.
    /// Primary interaction event bus remains <see cref="InteractionComponent"/> — no local OnInteracted.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool interactionEnabled = true;
        [SerializeField] private string interactionPrompt = "Interact";
        [SerializeField] private int interactionPriority;
        [SerializeField] private string interactionId = string.Empty;

        public bool InteractionEnabled
        {
            get => interactionEnabled;
            set => interactionEnabled = value;
        }

        public string InteractionPrompt
        {
            get => interactionPrompt;
            set => interactionPrompt = value ?? string.Empty;
        }

        public int InteractionPriority
        {
            get => interactionPriority;
            set => interactionPriority = value;
        }

        public string InteractionId
        {
            get => interactionId;
            set => interactionId = value ?? string.Empty;
        }

        public virtual bool CanInteract(GameObject interactor)
        {
            return interactionEnabled && interactor != null;
        }

        public virtual string GetInteractionPrompt(GameObject interactor)
        {
            return interactionPrompt ?? string.Empty;
        }

        public virtual int GetInteractionPriority(GameObject interactor)
        {
            return interactionPriority;
        }

        public virtual InteractionResult Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return InteractionResult.Failure("Interaction is not available.", interactionId);
            }

            return ExecuteInteraction(interactor);
        }

        /// <summary>Core extensibility hook for derived behaviours. Base succeeds with InteractionId.</summary>
        protected virtual InteractionResult ExecuteInteraction(GameObject interactor)
        {
            return InteractionResult.Succeeded(interactionId);
        }
    }
}
