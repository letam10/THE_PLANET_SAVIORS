using UnityEngine;

namespace TPS.Runtime.Interaction
{
    /// <summary>
    /// Interface for any object the player can interact with via E key.
    /// </summary>
    public interface IInteractable
    {
        string GetInteractionPrompt();
        void Interact(GameObject interactor);
    }
}
