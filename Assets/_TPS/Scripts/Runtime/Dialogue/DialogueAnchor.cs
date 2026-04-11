using TPS.Runtime.Core;
using TPS.Runtime.Interaction;
using TPS.Runtime.UI;
using UnityEngine;

namespace TPS.Runtime.Dialogue
{
    public sealed class DialogueAnchor : MonoBehaviour, IInteractable, IStateResolvable
    {
        [SerializeField] private string _anchorId = "dialogue_anchor";
        [SerializeField] private DialogueDefinition _dialogueDefinition;
        [SerializeField] private string _interactionLabel = "Talk";

        public string GetInteractionPrompt()
        {
            return $"Press [E] to {_interactionLabel}";
        }

        public void Interact(GameObject interactor)
        {
            if (DialogueStateService.Instance == null || _dialogueDefinition == null)
            {
                return;
            }

            string body = DialogueStateService.Instance.Interact(_dialogueDefinition);
            if (Phase1RuntimeHUD.Instance != null)
            {
                Phase1RuntimeHUD.Instance.ShowMessage(body);
            }

            ResolveState();
        }

        private void OnEnable()
        {
            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.Unregister(this);
            }
        }

        public void ResolveState()
        {
            if (DialogueStateService.Instance == null || GameStateManager.Instance == null || _dialogueDefinition == null)
            {
                return;
            }

            DialogueVariant currentVariant = DialogueStateService.Instance.ResolveCurrentVariant(_dialogueDefinition);
            GameStateManager.Instance.SetString($"dialogue.{_anchorId}.active_variant", currentVariant != null ? currentVariant.VariantId : "none");
        }
    }
}
