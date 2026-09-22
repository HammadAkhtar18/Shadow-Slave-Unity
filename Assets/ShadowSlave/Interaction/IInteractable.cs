using UnityEngine;

namespace ShadowSlave.Interaction
{
    /// <summary>
    /// Interface for any world object capable of interaction.
    /// Mirrors UE IShadowSlaveInteractableInterface (C# interface instead of UInterface).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Whether the given interactor is currently permitted to interact with this object.</summary>
        bool CanInteract(GameObject interactor);

        /// <summary>User-facing prompt text when targeting this object (e.g. "Open", "Talk").</summary>
        string GetInteractionPrompt(GameObject interactor);

        /// <summary>Executes the interaction on behalf of the interactor. Returns outcome payload.</summary>
        InteractionResult Interact(GameObject interactor);

        /// <summary>Candidate selection priority (higher = preferred). Ranking deferred with detection.</summary>
        int GetInteractionPriority(GameObject interactor);
    }
}
