using UnityEngine;

namespace TPS.Runtime.World
{
    public enum SimplePlaceholderMotionMode
    {
        None = 0,
        Bob = 1,
        Sway = 2,
        Pace = 3
    }

    public sealed class SimplePlaceholderMotion : MonoBehaviour
    {
        [SerializeField] private SimplePlaceholderMotionMode _mode = SimplePlaceholderMotionMode.Bob;
        [SerializeField] private float _speed = 1.2f;
        [SerializeField] private float _amplitude = 0.12f;
        [SerializeField] private Vector3 _paceOffset = new Vector3(0.45f, 0f, 0f);

        private Vector3 _originPosition;
        private Quaternion _originRotation;
        private float _phaseOffset;

        private void Awake()
        {
            _originPosition = transform.localPosition;
            _originRotation = transform.localRotation;
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float t = UnityEngine.Time.time * Mathf.Max(0.05f, _speed) + _phaseOffset;
            switch (_mode)
            {
                case SimplePlaceholderMotionMode.Bob:
                    transform.localPosition = _originPosition + new Vector3(0f, Mathf.Sin(t) * _amplitude, 0f);
                    break;
                case SimplePlaceholderMotionMode.Sway:
                    transform.localPosition = _originPosition;
                    transform.localRotation = _originRotation * Quaternion.Euler(0f, Mathf.Sin(t) * (_amplitude * 30f), 0f);
                    break;
                case SimplePlaceholderMotionMode.Pace:
                    transform.localPosition = _originPosition + Vector3.LerpUnclamped(-_paceOffset, _paceOffset, (Mathf.Sin(t) + 1f) * 0.5f);
                    break;
                default:
                    transform.localPosition = _originPosition;
                    transform.localRotation = _originRotation;
                    break;
            }
        }
    }
}
