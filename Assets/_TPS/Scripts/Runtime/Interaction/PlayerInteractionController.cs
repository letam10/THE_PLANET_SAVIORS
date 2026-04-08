using UnityEngine;
using UnityEngine.InputSystem;

namespace TPS.Runtime.Interaction
{
    /// <summary>
    /// Raycasts from camera to detect <see cref="IInteractable"/> objects.
    /// Shows interaction prompt via OnGUI and triggers interaction on E press.
    /// Attach to the player GameObject alongside PlayerInput.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _interactionDistance = 3f;
        [SerializeField] private Transform _rayOrigin;
        [SerializeField] private LayerMask _interactionMask = ~0;

        private PlayerInput _playerInput;
        private InputAction _interactAction;
        private IInteractable _currentTarget;
        private string _currentPrompt;

        private GUIStyle _promptStyle;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _interactAction = _playerInput.actions["Interact"];
        }

        private void Update()
        {
            ScanForInteractable();

            if (_currentTarget != null && _interactAction.WasPressedThisFrame())
            {
                _currentTarget.Interact(gameObject);
            }
        }

        private void ScanForInteractable()
        {
            _currentTarget = null;
            _currentPrompt = null;

            // Determine ray origin — prefer explicit reference, fallback to Camera.main
            Transform origin = _rayOrigin;
            if (origin == null)
            {
                Camera cam = Camera.main;
                if (cam != null) origin = cam.transform;
            }

            if (origin == null) return;

            Ray ray = new Ray(origin.position, origin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactionMask, QueryTriggerInteraction.Collide))
            {
                // Check hit object first, then parents
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponentInParent<IInteractable>();
                }

                if (interactable != null)
                {
                    _currentTarget = interactable;
                    _currentPrompt = interactable.GetInteractionPrompt();
                }
            }
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_currentPrompt)) return;

            if (_promptStyle == null)
            {
                _promptStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter
                };
                _promptStyle.normal.textColor = Color.white;
            }

            float w = 300f;
            float h = 40f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.65f;

            GUI.Box(new Rect(x, y, w, h), _currentPrompt, _promptStyle);
        }
    }
}
