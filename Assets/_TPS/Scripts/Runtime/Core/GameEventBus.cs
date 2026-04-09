using System;
using TPS.Runtime.Weather;

namespace TPS.Runtime.Core
{
    /// <summary>
    /// Global event bus for cross-domain communication without hard coupling.
    /// Used ONLY for macro-level game events. Local mechanics should still use direct references.
    /// </summary>
    public static class GameEventBus
    {
        // ==========================================
        // TIME DOMAIN
        // ==========================================
        public static event Action<int, int> OnHourChanged;
        public static void PublishHourChanged(int day, int hour) => OnHourChanged?.Invoke(day, hour);

        public static event Action<int> OnDayChanged;
        public static void PublishDayChanged(int day) => OnDayChanged?.Invoke(day);

        // ==========================================
        // WEATHER DOMAIN
        // ==========================================
        public static event Action<WeatherType> OnWeatherChanged;
        public static void PublishWeatherChanged(WeatherType weather) => OnWeatherChanged?.Invoke(weather);

        // ==========================================
        // STATE DOMAIN
        // ==========================================
        public static event Action<string> OnGameStateChanged;
        public static void PublishGameStateChanged(string key) => OnGameStateChanged?.Invoke(key);

        // ==========================================
        // SAVE/LOAD DOMAIN
        // ==========================================
        public static event Action OnGameLoaded;
        public static void PublishGameLoaded() => OnGameLoaded?.Invoke();

        public static event Action OnGameSaved;
        public static void PublishGameSaved() => OnGameSaved?.Invoke();

        // ==========================================
        // ENCOUNTER DOMAIN (DEBUG / MOCKUP)
        // ==========================================
        public static event Action<string> OnEncounterCompleted;
        public static void PublishEncounterCompleted(string encounterId) => OnEncounterCompleted?.Invoke(encounterId);

        /// <summary>
        /// Call this to clear all event subscriptions.
        /// Useful when completely destroying the core system or transitioning main states.
        /// </summary>
        public static void ClearAllEvents()
        {
            OnHourChanged = null;
            OnDayChanged = null;
            OnWeatherChanged = null;
            OnGameStateChanged = null;
            OnGameLoaded = null;
            OnGameSaved = null;
            OnEncounterCompleted = null;
        }
    }
}
