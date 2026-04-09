using System;
using System.Collections.Generic;
using UnityEngine;
using TPS.Runtime.Core;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;

namespace TPS.Runtime.Conditions
{
    public enum ConditionGroupMode { All, Any }
    
    public enum ConditionType 
    { 
        TimeRange, 
        StateEquals, 
        WeatherEquals 
    }

    [Serializable]
    public class GameCondition
    {
        public ConditionType Type;

        [Header("Time Range (Same day 24h, overnight not supported yet)")]
        [Range(0, 24)] public int StartHour;
        [Range(0, 24)] public int EndHour;

        [Header("State Check (Bool)")]
        public string StateKey;
        public bool ExpectedBool = true;

        [Header("Weather Check")]
        public WeatherType ExpectedWeather = WeatherType.Sunny;

        /// <summary>
        /// Evaluates if this specific condition is met based on the global state.
        /// </summary>
        public bool Evaluate()
        {
            switch (Type)
            {
                case ConditionType.TimeRange:
                    if (WorldClock.Instance == null) return false;
                    int h = WorldClock.Instance.CurrentHour;
                    // Strict same-day range check. E.g. Start 8, End 12 -> returns true if 8 <= h < 12 (or 8 <= h <= 12, depending on design. Let's do inclusive).
                    return h >= StartHour && h <= EndHour;

                case ConditionType.StateEquals:
                    if (GameStateManager.Instance == null) return false;
                    bool currentState = GameStateManager.Instance.GetBool(StateKey, false);
                    return currentState == ExpectedBool;

                case ConditionType.WeatherEquals:
                    if (WeatherSystem.Instance == null) return false;
                    return WeatherSystem.Instance.CurrentWeather == ExpectedWeather;

                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Utility class/static helper to evaluate lists of Conditions.
    /// </summary>
    [Serializable]
    public class ConditionResolver
    {
        public ConditionGroupMode Mode = ConditionGroupMode.All;
        public List<GameCondition> Conditions = new List<GameCondition>();

        public bool EvaluateAll()
        {
            if (Conditions == null || Conditions.Count == 0) return true; // Empty means always true

            if (Mode == ConditionGroupMode.All)
            {
                foreach (var c in Conditions)
                {
                    if (!c.Evaluate()) return false;
                }
                return true;
            }
            else // Any
            {
                foreach (var c in Conditions)
                {
                    if (c.Evaluate()) return true;
                }
                return false;
            }
        }
    }
}
