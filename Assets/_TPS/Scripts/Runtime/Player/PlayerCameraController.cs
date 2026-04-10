using UnityEngine;
using UnityEngine.InputSystem;
using TPS.Runtime.UI;

namespace TPS.Runtime.Player
{
    /// <summary>
    /// Third-person camera controller. Yaw rotates the player, pitch rotates a camera pivot.
    /// Locks and hides cursor on enable. Reads Look input via polling.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Camera _playerCamera;

        [Header("Sensitivity")]
        [SerializeField] private float _mouseSensitivity = 0.15f;

        [Header("Pitch Clamp")]
        [SerializeField] private float _minPitch = -35f;
        [SerializeField] private float _maxPitch = 75f;

        private PlayerInput _playerInput;
        private InputAction _lookAction;
        private float _pitch;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _lookAction = _playerInput.actions["Look"];
            RuntimeUiInputState.SetUiFocused(false);
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (_lookAction == null || RuntimeUiInputState.IsUiFocused) return;

            Vector2 look = _lookAction.ReadValue<Vector2>();

            // Yaw — rotate the entire player around Y axis
            float yaw = look.x * _mouseSensitivity;
            transform.Rotate(Vector3.up, yaw);

            // Pitch — rotate camera pivot around local X axis
            _pitch -= look.y * _mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            if (_cameraPivot != null)
            {
                _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }
    }
}
