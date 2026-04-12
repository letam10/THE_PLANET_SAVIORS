using TPS.Runtime.Interaction;
using TPS.Runtime.Spawn;
using TPS.Runtime.UI;
using UnityEngine;

namespace TPS.Runtime.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class InteriorTravelDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _doorId = "door";
        [SerializeField] private string _interactionLabel = "open";
        [SerializeField] private Transform _targetMarker;
        [SerializeField] private Transform _doorLeaf;
        [SerializeField] private float _openAngle = 82f;
        [SerializeField] private float _openSpeed = 7f;

        private Quaternion _closedRotation = Quaternion.identity;
        private Quaternion _openedRotation = Quaternion.identity;
        private bool _isOpen;

        private void Awake()
        {
            if (_doorLeaf == null)
            {
                _doorLeaf = transform;
            }

            _closedRotation = _doorLeaf.localRotation;
            _openedRotation = _closedRotation * Quaternion.Euler(0f, _openAngle, 0f);
        }

        private void Update()
        {
            if (_doorLeaf == null)
            {
                return;
            }

            Quaternion targetRotation = _isOpen ? _openedRotation : _closedRotation;
            _doorLeaf.localRotation = Quaternion.Slerp(_doorLeaf.localRotation, targetRotation, UnityEngine.Time.deltaTime * _openSpeed);
        }

        public string GetInteractionPrompt()
        {
            return $"Press [E] to {_interactionLabel}";
        }

        public void Interact(GameObject interactor)
        {
            _isOpen = !_isOpen;
            if (_targetMarker != null && PlayerSpawnSystem.Instance != null)
            {
                PlayerSpawnSystem.Instance.TeleportPlayerSafe(_targetMarker.position, _targetMarker.rotation, "Default");
                if (Phase1RuntimeHUD.Instance != null)
                {
                    Phase1RuntimeHUD.Instance.ShowMessage($"Moved through {_doorId}.");
                }
            }
        }
    }
}
