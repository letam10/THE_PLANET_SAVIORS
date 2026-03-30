using System;
using UnityEngine;

namespace TPS.Runtime.Time
{
    public sealed class WorldClock : MonoBehaviour
    {
        public static WorldClock Instance { get; private set; }

        public event Action<int, int, int> TimeChanged;
        public event Action<int> HourChanged;

        [SerializeField] private bool _isRunning = true;
        [SerializeField] private int _currentDay = 1;
        [SerializeField] private int _currentHour = 8;
        [SerializeField] private int _currentMinute = 0;
        [Min(0.01f)][SerializeField] private float _worldMinutesPerRealSecond = 1f;

        private float _minuteAccumulator;

        public int CurrentDay => _currentDay;
        public int CurrentHour => _currentHour;
        public int CurrentMinute => _currentMinute;

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

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            _minuteAccumulator += Time.deltaTime * _worldMinutesPerRealSecond;

            while (_minuteAccumulator >= 1f)
            {
                _minuteAccumulator -= 1f;
                AdvanceMinutes(1);
            }
        }

        public void Initialize(int day, int hour, int minute, float worldMinutesPerRealSecond)
        {
            _currentDay = Mathf.Max(1, day);
            _currentHour = Mathf.Clamp(hour, 0, 23);
            _currentMinute = Mathf.Clamp(minute, 0, 59);
            _worldMinutesPerRealSecond = Mathf.Max(0.01f, worldMinutesPerRealSecond);
            _minuteAccumulator = 0f;

            TimeChanged?.Invoke(_currentDay, _currentHour, _currentMinute);
        }

        public void SetRunning(bool isRunning)
        {
            _isRunning = isRunning;
        }

        public void AdvanceMinutes(int minutes)
        {
            for (int i = 0; i < minutes; i++)
            {
                AdvanceOneMinute();
            }
        }

        public string GetFormattedTime()
        {
            return $"Day {_currentDay} {_currentHour:00}:{_currentMinute:00}";
        }

        private void AdvanceOneMinute()
        {
            int previousHour = _currentHour;

            _currentMinute++;

            if (_currentMinute >= 60)
            {
                _currentMinute = 0;
                _currentHour++;

                if (_currentHour >= 24)
                {
                    _currentHour = 0;
                    _currentDay++;
                }
            }

            TimeChanged?.Invoke(_currentDay, _currentHour, _currentMinute);

            if (_currentHour != previousHour)
            {
                HourChanged?.Invoke(_currentHour);
            }
        }
    }
}