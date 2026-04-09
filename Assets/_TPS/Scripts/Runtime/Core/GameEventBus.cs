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

        public static event Action<string> OnDialogueStateChanged;
        public static void PublishDialogueStateChanged(string key) => OnDialogueStateChanged?.Invoke(key);

        public static event Action<string> OnQuestChanged;
        public static void PublishQuestChanged(string questId) => OnQuestChanged?.Invoke(questId);

        public static event Action<string> OnPartyChanged;
        public static void PublishPartyChanged(string memberId) => OnPartyChanged?.Invoke(memberId);

        public static event Action<string> OnInventoryChanged;
        public static void PublishInventoryChanged(string key) => OnInventoryChanged?.Invoke(key);

        public static event Action<string> OnProgressionChanged;
        public static void PublishProgressionChanged(string memberId) => OnProgressionChanged?.Invoke(memberId);

        public static event Action<string, int> OnLevelUp;
        public static void PublishLevelUp(string memberId, int newLevel) => OnLevelUp?.Invoke(memberId, newLevel);

        public static event Action<string> OnZoneFactsChanged;
        public static void PublishZoneFactsChanged(string key) => OnZoneFactsChanged?.Invoke(key);

        public static event Action<string> OnEconomyChanged;
        public static void PublishEconomyChanged(string key) => OnEconomyChanged?.Invoke(key);

        public static event Action<string> OnRewardGranted;
        public static void PublishRewardGranted(string summary) => OnRewardGranted?.Invoke(summary);

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

        public static event Action<string, bool> OnEncounterResolved;
        public static void PublishEncounterResolved(string encounterId, bool victory) => OnEncounterResolved?.Invoke(encounterId, victory);

        public static event Action OnStateResolverCompleted;
        public static void PublishStateResolverCompleted() => OnStateResolverCompleted?.Invoke();

        public static event Action<int> OnSleepAdvanced;
        public static void PublishSleepAdvanced(int day) => OnSleepAdvanced?.Invoke(day);

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
            OnDialogueStateChanged = null;
            OnQuestChanged = null;
            OnPartyChanged = null;
            OnInventoryChanged = null;
            OnProgressionChanged = null;
            OnLevelUp = null;
            OnZoneFactsChanged = null;
            OnEconomyChanged = null;
            OnRewardGranted = null;
            OnGameLoaded = null;
            OnGameSaved = null;
            OnEncounterCompleted = null;
            OnEncounterResolved = null;
            OnStateResolverCompleted = null;
            OnSleepAdvanced = null;
        }
    }
}
