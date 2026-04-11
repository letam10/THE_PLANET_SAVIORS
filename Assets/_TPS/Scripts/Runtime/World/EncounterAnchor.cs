using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Interaction;
using TPS.Runtime.UI;
using UnityEngine;

namespace TPS.Runtime.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class EncounterAnchor : MonoBehaviour, IInteractable, IStateResolvable
    {
        [SerializeField] private string _anchorId = "encounter_anchor";
        [SerializeField] private string _zoneId = "aster_harbor";
        [SerializeField] private EncounterDefinition _directEncounter;
        [SerializeField] private bool _useZoneEncounterTable = false;
        [SerializeField] private bool _triggerOnEnter = true;
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private bool _hideWhenCleared = false;
        [SerializeField] private string _returnSpawnIdOverride = "";
        [SerializeField] private TPS.Runtime.Conditions.ConditionResolver _availabilityConditions = new TPS.Runtime.Conditions.ConditionResolver();

        private bool _hasTriggered;
        private bool _isAvailable = true;

        public string GetInteractionPrompt()
        {
            return _isAvailable ? "Press [E] to challenge" : "Encounter unavailable";
        }

        public void Interact(GameObject interactor)
        {
            TryTriggerEncounter(interactor != null ? interactor.transform : null);
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

        private void OnTriggerEnter(Collider other)
        {
            if (!_triggerOnEnter || other == null || !other.CompareTag("Player"))
            {
                return;
            }

            TryTriggerEncounter(other.transform);
        }

        public void ResolveState()
        {
            EncounterDefinition resolvedEncounter = ResolveEncounterDefinition();
            bool cleared = resolvedEncounter != null && EncounterService.Instance != null && EncounterService.Instance.IsEncounterCleared(resolvedEncounter.EncounterId);
            _isAvailable = (_availabilityConditions == null || _availabilityConditions.EvaluateAll()) && (!_hideWhenCleared || !cleared);

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetBool($"encounter.{_anchorId}.available", _isAvailable);
                GameStateManager.Instance.SetString($"encounter.{_anchorId}.resolved_id", resolvedEncounter != null ? resolvedEncounter.EncounterId : "none");
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = _isAvailable;
            }

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = _isAvailable;
            }
        }

        private void TryTriggerEncounter(Transform sourceTransform)
        {
            if ((_triggerOnce && _hasTriggered) || !_isAvailable || EncounterService.Instance == null || SceneLoader.Instance == null)
            {
                return;
            }

            EncounterDefinition encounterDefinition = ResolveEncounterDefinition();
            if (encounterDefinition == null)
            {
                return;
            }

            Vector3 position = sourceTransform != null ? sourceTransform.position : transform.position;
            Quaternion rotation = sourceTransform != null ? sourceTransform.rotation : transform.rotation;
            EncounterService.Instance.BeginEncounter(
                encounterDefinition,
                SceneLoader.Instance.CurrentContentScene,
                _returnSpawnIdOverride,
                position,
                rotation);
            _hasTriggered = true;

            if (Phase1RuntimeHUD.Instance != null)
            {
                Phase1RuntimeHUD.Instance.CloseShop();
                Phase1RuntimeHUD.Instance.ShowMessage($"Entering {encounterDefinition.DisplayName}...");
            }

            SceneLoader.Instance.StartCoroutine(SceneLoader.Instance.LoadContentSceneAsync(encounterDefinition.BattleSceneName));
        }

        private EncounterDefinition ResolveEncounterDefinition()
        {
            if (_useZoneEncounterTable && EncounterService.Instance != null)
            {
                return EncounterService.Instance.RollEncounterForZone(_zoneId);
            }

            return _directEncounter;
        }
    }
}
