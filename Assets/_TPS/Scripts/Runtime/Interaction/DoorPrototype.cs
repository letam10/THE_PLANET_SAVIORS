using System.Collections;
using UnityEngine;

namespace TPS.Runtime.Interaction
{
    /// <summary>
    /// Simple door that toggles open/close when interacted with.
    /// Rotates <see cref="_doorHinge"/> by <see cref="_openAngle"/> degrees on Y axis.
    /// </summary>
    public sealed class DoorPrototype : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform _doorHinge;
        [SerializeField] private float _openAngle = 90f;
        [SerializeField] private float _openSpeed = 3f;

        private bool _isOpen;
        private bool _isAnimating;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;

        private void Awake()
        {
            if (_doorHinge != null)
            {
                _closedRotation = _doorHinge.localRotation;
                _openRotation = _closedRotation * Quaternion.Euler(0f, _openAngle, 0f);
            }
        }

        public string GetInteractionPrompt()
        {
            return _isOpen ? "Press [E] to Close" : "Press [E] to Open";
        }

        public void Interact(GameObject interactor)
        {
            if (_isAnimating || _doorHinge == null) return;
            StartCoroutine(AnimateDoor(!_isOpen));
        }

        private IEnumerator AnimateDoor(bool open)
        {
            _isAnimating = true;
            Quaternion from = _doorHinge.localRotation;
            Quaternion to = open ? _openRotation : _closedRotation;
            float t = 0f;

            while (t < 1f)
            {
                t += UnityEngine.Time.deltaTime * _openSpeed;
                _doorHinge.localRotation = Quaternion.Slerp(from, to, Mathf.Clamp01(t));
                yield return null;
            }

            _doorHinge.localRotation = to;
            _isOpen = open;
            _isAnimating = false;
        }
    }
}
