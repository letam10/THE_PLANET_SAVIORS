using System;
using UnityEngine;

namespace TPS.Runtime.Weather
{
    public enum WeatherType
    {
        Sunny = 0,
        Rain = 1
    }

    public sealed class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        public event Action<WeatherType> WeatherChanged;

        [SerializeField] private WeatherType _currentWeather = WeatherType.Sunny;

        public WeatherType CurrentWeather => _currentWeather;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(WeatherType weatherType)
        {
            SetWeather(weatherType, true);
        }

        public void SetWeather(WeatherType weatherType, bool force = false)
        {
            if (!force && _currentWeather == weatherType)
            {
                return;
            }

            _currentWeather = weatherType;
            WeatherChanged?.Invoke(_currentWeather);
            TPS.Runtime.Core.GameEventBus.PublishWeatherChanged(_currentWeather);
            Debug.Log($"Weather changed to: {_currentWeather}");
        }
    }
}