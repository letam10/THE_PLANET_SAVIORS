using UnityEngine;
using UnityEngine.InputSystem;
using TPS.Runtime.UI;

namespace TPS.Runtime.Player
{
    /// <summary>
    /// Handles player movement (walk, sprint, jump, gravity) using CharacterController.
    /// Reads input directly from PlayerInput.actions via polling.
    /// Movement direction is relative to camera orientation on the horizontal plane.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;

        [Header("Jump & Gravity")]
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -15f;

        private CharacterController _cc;
        private PlayerInput _playerInput;
        private Vector3 _verticalVelocity;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _moveAction = _playerInput.actions["Move"];
            _jumpAction = _playerInput.actions["Jump"];
            _sprintAction = _playerInput.actions["Sprint"];
        }

        private void Update()
        {
            bool uiFocused = RuntimeUiInputState.IsUiFocused;
            Vector2 input = uiFocused ? Vector2.zero : _moveAction.ReadValue<Vector2>();

            Camera cam = Camera.main;
            Vector3 moveDir = Vector3.zero;
            if (cam != null)
            {
                Vector3 forward = cam.transform.forward;
                Vector3 right = cam.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDir = (forward * input.y) + (right * input.x);
            }

            bool isSprinting = !uiFocused && _sprintAction.IsPressed();
            float speed = isSprinting ? _sprintSpeed : _walkSpeed;

            // Jump & Gravity
            if (_cc.isGrounded)
            {
                if (_verticalVelocity.y < 0f)
                {
                    _verticalVelocity.y = -2f; // Small downward force to keep grounded
                }

                if (!uiFocused && _jumpAction.WasPressedThisFrame())
                {
                    _verticalVelocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                }
            }

            _verticalVelocity.y += _gravity * UnityEngine.Time.deltaTime;

            // Combine and apply
            Vector3 finalMove = (moveDir * speed) + _verticalVelocity;
            _cc.Move(finalMove * UnityEngine.Time.deltaTime);
        }
    }
}
