using UnityEngine;

namespace TPS.Runtime.World
{
    public sealed class PlaceholderAmbientMotion : MonoBehaviour
    {
        [SerializeField] private float _bobAmplitude = 0.06f;
        [SerializeField] private float _bobFrequency = 1.6f;
        [SerializeField] private float _yawSpeed = 24f;
        [SerializeField] private bool _enableBob = true;
        [SerializeField] private bool _enableYaw = true;

        private Vector3 _baseLocalPosition;
        private float _phaseOffset;

        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
            _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_enableBob)
            {
                float offset = Mathf.Sin((UnityEngine.Time.time * _bobFrequency) + _phaseOffset) * _bobAmplitude;
                transform.localPosition = _baseLocalPosition + new Vector3(0f, offset, 0f);
            }

            if (_enableYaw)
            {
                transform.Rotate(0f, _yawSpeed * UnityEngine.Time.deltaTime, 0f, Space.Self);
            }
        }
    }
}
