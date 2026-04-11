using UnityEngine;
using UnityEngine.InputSystem;
using TPS.Runtime.UI;

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
            if (RuntimeUiInputState.IsUiFocused)
            {
                _currentTarget = null;
                _currentPrompt = null;
                return;
            }

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

            // Origin at player's chest level
            Vector3 startPos = transform.position + Vector3.up * 1.0f;
            Vector3 direction = transform.forward;
            float radius = 0.5f;

            // We use SphereCastAll to have a thick "forgiving" raycast that ignores missing exact aims.
            RaycastHit[] hits = Physics.SphereCastAll(startPos, radius, direction, _interactionDistance, _interactionMask, QueryTriggerInteraction.Collide);
            
            // Sort by distance to find the closest object first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // Ignore the player's own colliders
                if (hit.collider.transform.root == this.transform.root) continue;

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
                    return; // Found the closest valid interactable
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
