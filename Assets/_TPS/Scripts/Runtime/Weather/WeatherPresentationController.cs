using TPS.Runtime.Spawn;
using UnityEngine;

namespace TPS.Runtime.Weather
{
    public sealed class WeatherPresentationController : MonoBehaviour
    {
        public static WeatherPresentationController Instance { get; private set; }

        private ParticleSystem _rainParticles;
        private bool _subscribed;
        private Color _baseAmbientLight;
        private Color _baseFogColor;
        private float _baseFogDensity;
        private bool _baseFogEnabled;

        public static void EnsureExists()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject go = new GameObject("WeatherPresentationController");
            go.AddComponent<WeatherPresentationController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheBaseRenderSettings();
            BuildRainEmitter();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Unsubscribe();
        }

        private void Update()
        {
            TrySubscribe();
        }

        private void LateUpdate()
        {
            if (_rainParticles == null)
            {
                return;
            }

            Vector3 anchor = Vector3.zero;
            if (PlayerSpawnSystem.Instance != null && PlayerSpawnSystem.Instance.TryGetPlayerTransform(out Vector3 playerPosition, out _))
            {
                anchor = playerPosition;
            }
            else if (Camera.main != null)
            {
                anchor = Camera.main.transform.position;
            }

            _rainParticles.transform.position = anchor + new Vector3(0f, 10f, 0f);
        }

        private void TrySubscribe()
        {
            if (_subscribed || WeatherSystem.Instance == null)
            {
                return;
            }

            WeatherSystem.Instance.WeatherChanged += OnWeatherChanged;
            _subscribed = true;
            ApplyPresentation(WeatherSystem.Instance.CurrentWeather);
        }

        private void Unsubscribe()
        {
            if (!_subscribed || WeatherSystem.Instance == null)
            {
                return;
            }

            WeatherSystem.Instance.WeatherChanged -= OnWeatherChanged;
            _subscribed = false;
        }

        private void CacheBaseRenderSettings()
        {
            _baseAmbientLight = RenderSettings.ambientLight;
            _baseFogColor = RenderSettings.fogColor;
            _baseFogDensity = RenderSettings.fogDensity;
            _baseFogEnabled = RenderSettings.fog;
        }

        private void BuildRainEmitter()
        {
            GameObject rainGo = new GameObject("RainEmitter");
            rainGo.transform.SetParent(transform, false);
            _rainParticles = rainGo.AddComponent<ParticleSystem>();
            var main = _rainParticles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.2f;
            main.startSpeed = 16f;
            main.startSize = 0.08f;
            main.startColor = new Color(0.72f, 0.84f, 0.95f, 0.75f);
            main.maxParticles = 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = _rainParticles.emission;
            emission.rateOverTime = 420f;

            var shape = _rainParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 1f, 18f);

            var velocityOverLifetime = _rainParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-22f);

            var noise = _rainParticles.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.5f;

            var renderer = _rainParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.6f;
            renderer.velocityScale = 0.16f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(0.7f, 0.82f, 0.95f, 0.72f);
        }

        private void OnWeatherChanged(WeatherType weatherType)
        {
            ApplyPresentation(weatherType);
        }

        private void ApplyPresentation(WeatherType weatherType)
        {
            bool isRain = weatherType == WeatherType.Rain;
            if (_rainParticles != null)
            {
                if (isRain && !_rainParticles.isPlaying)
                {
                    _rainParticles.Play();
                }
                else if (!isRain && _rainParticles.isPlaying)
                {
                    _rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            RenderSettings.fog = isRain || _baseFogEnabled;
            RenderSettings.fogColor = isRain ? new Color(0.54f, 0.61f, 0.68f, 1f) : _baseFogColor;
            RenderSettings.fogDensity = isRain ? Mathf.Max(_baseFogDensity, 0.013f) : _baseFogDensity;
            RenderSettings.ambientLight = isRain ? new Color(0.62f, 0.68f, 0.74f, 1f) : _baseAmbientLight;
        }
    }
}
