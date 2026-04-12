using TPS.Runtime.Core;
using TPS.Runtime.Interaction;
using TPS.Runtime.Spawn;
using TPS.Runtime.UI;
using UnityEngine;

namespace TPS.Runtime.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class SceneTravelAnchor : MonoBehaviour, IInteractable, IStateResolvable
    {
        [SerializeField] private string _travelId = "travel_anchor";
        [SerializeField] private string _targetSceneName = "ZN_Town_AsterHarbor";
        [SerializeField] private string _targetSpawnId = "Default";
        [SerializeField] private string _interactionLabel = "travel";
        [SerializeField] private TPS.Runtime.Conditions.ConditionResolver _availabilityConditions = new TPS.Runtime.Conditions.ConditionResolver();
        [SerializeField] private bool _hideWhenUnavailable = false;

        private bool _isAvailable = true;

        public string GetInteractionPrompt()
        {
            return _isAvailable ? $"Press [E] to {_interactionLabel}" : "Travel unavailable";
        }

        public void Interact(GameObject interactor)
        {
            if (!_isAvailable || SceneLoader.Instance == null)
            {
                return;
            }

            if (PlayerSpawnSystem.Instance != null)
            {
                PlayerSpawnSystem.Instance.SetPendingSpawnId(_targetSpawnId);
            }

            if (RuntimeMenuCanvasController.Instance != null)
            {
                RuntimeMenuCanvasController.Instance.PrepareForSceneTransition($"Traveling to {_targetSceneName}...");
            }
            else if (Phase1RuntimeHUD.Instance != null)
            {
                Phase1RuntimeHUD.Instance.CloseShop();
                Phase1RuntimeHUD.Instance.ShowMessage($"Traveling to {_targetSceneName}...");
            }

            SceneLoader.Instance.StartCoroutine(SceneLoader.Instance.LoadContentSceneAsync(_targetSceneName));
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
            _isAvailable = _availabilityConditions == null || _availabilityConditions.EvaluateAll();

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = _isAvailable;
            }

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = !_hideWhenUnavailable || _isAvailable;
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetBool($"travel.{_travelId}.available", _isAvailable);
                GameStateManager.Instance.SetString($"travel.{_travelId}.target_scene", _targetSceneName);
                GameStateManager.Instance.SetString($"travel.{_travelId}.target_spawn", _targetSpawnId);
            }
        }
    }
}
