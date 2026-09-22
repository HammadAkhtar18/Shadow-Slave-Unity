using System;
using UnityEngine;

namespace ShadowSlave.Interaction
{
    /// <summary>
    /// Manages the current interaction candidate and executes interactions.
    /// Mirrors essentials of UE UShadowSlaveInteractionComponent.
    /// Physics detection / periodic traces are deferred; set the target explicitly for now.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractionComponent : MonoBehaviour
    {
        [SerializeField] private bool enableDetection;

        private IInteractable _currentInteractable;
        private GameObject _currentTargetGo;
        private bool _boundToUnityObject;

        /// <summary>Fires when the candidate interactable GameObject actually changes.</summary>
        public event Action<GameObject, GameObject> OnInteractionTargetChanged;

        /// <summary>
        /// Fires after <see cref="IInteractable.Interact"/> is invoked (success or failure result).
        /// Does not fire when TryInteract rejects before calling Interact.
        /// </summary>
        public event Action<GameObject, GameObject, InteractionResult> OnInteracted;

        /// <summary>Current interactable contract, or null if none / invalid.</summary>
        public IInteractable CurrentInteractable => IsCurrentTargetValid() ? _currentInteractable : null;

        /// <summary>GameObject of the current target when bound to a Unity object; otherwise null.</summary>
        public GameObject CurrentInteractableObject =>
            IsCurrentTargetValid() && _boundToUnityObject ? _currentTargetGo : null;

        /// <summary>True when a valid interactable target is selected.</summary>
        public bool HasInteractableTarget => IsCurrentTargetValid();

        /// <summary>Whether detection is flagged enabled (no physics polling in this phase).</summary>
        public bool IsInteractionDetectionEnabled => enableDetection;

        /// <summary>
        /// Enables or disables the detection flag. Disabling clears the current target.
        /// Does not start physics traces or Update polling — detection is deferred.
        /// </summary>
        public void SetInteractionDetectionEnabled(bool enabled)
        {
            enableDetection = enabled;
            if (!enabled)
            {
                ClearCurrentInteractable();
            }
        }

        /// <summary>Sets the current target from an <see cref="IInteractable"/> (MonoBehaviour preferred).</summary>
        public void SetCurrentInteractable(IInteractable interactable)
        {
            if (interactable == null)
            {
                ClearCurrentInteractable();
                return;
            }

            if (interactable is Component component)
            {
                ApplyTarget(interactable, component.gameObject, boundToUnityObject: true);
                return;
            }

            ApplyTarget(interactable, null, boundToUnityObject: false);
        }

        /// <summary>Sets the current target from a GameObject implementing <see cref="IInteractable"/>.</summary>
        public void SetCurrentInteractable(GameObject target)
        {
            if (target == null)
            {
                ClearCurrentInteractable();
                return;
            }

            IInteractable interactable = target.GetComponent<IInteractable>();
            if (interactable == null)
            {
                ClearCurrentInteractable();
                return;
            }

            ApplyTarget(interactable, target, boundToUnityObject: true);
        }

        /// <summary>Clears the current target; fires change event only if a target was set.</summary>
        public void ClearCurrentInteractable()
        {
            ApplyTarget(null, null, boundToUnityObject: false);
        }

        /// <summary>Prompt for the current target, or empty if none.</summary>
        public string GetCurrentInteractionPrompt()
        {
            if (!IsCurrentTargetValid())
            {
                return string.Empty;
            }

            string prompt = _currentInteractable.GetInteractionPrompt(gameObject);
            return prompt ?? string.Empty;
        }

        /// <summary>
        /// Attempts to interact with the current candidate.
        /// UE TryInteract semantics (detection re-query deferred):
        /// 1. No valid target → false, no OnInteracted
        /// 2. Target invalid / lost → clear, false, no OnInteracted
        /// 3. !CanInteract(owner) → false, no OnInteracted (Interact not called)
        /// 4. result = Interact(owner)
        /// 5. OnInteracted always after Interact
        /// 6. return result.Success
        /// </summary>
        public bool TryInteract()
        {
            if (_currentInteractable == null)
            {
                return false;
            }

            if (!IsCurrentTargetValid())
            {
                ClearCurrentInteractable();
                return false;
            }

            IInteractable target = _currentInteractable;
            GameObject targetGo = _currentTargetGo;
            GameObject owner = gameObject;

            if (!target.CanInteract(owner))
            {
                return false;
            }

            InteractionResult result = target.Interact(owner);
            OnInteracted?.Invoke(owner, targetGo, result);
            return result.Success;
        }

        private void ApplyTarget(IInteractable interactable, GameObject targetGo, bool boundToUnityObject)
        {
            GameObject oldGo = _boundToUnityObject ? _currentTargetGo : null;
            GameObject newGo = boundToUnityObject ? targetGo : null;

            // Normalize destroyed Unity objects to null for comparison.
            if (oldGo == null)
            {
                oldGo = null;
            }

            if (newGo == null)
            {
                newGo = null;
            }

            bool changed = !ReferenceEquals(oldGo, newGo);

            _currentInteractable = interactable;
            _currentTargetGo = targetGo;
            _boundToUnityObject = boundToUnityObject && targetGo != null;

            if (changed)
            {
                OnInteractionTargetChanged?.Invoke(newGo, oldGo);
            }
        }

        private bool IsCurrentTargetValid()
        {
            if (_currentInteractable == null)
            {
                return false;
            }

            if (_boundToUnityObject)
            {
                // Unity fake-null: destroyed GameObject compares equal to null.
                return _currentTargetGo != null;
            }

            return true;
        }
    }
}
