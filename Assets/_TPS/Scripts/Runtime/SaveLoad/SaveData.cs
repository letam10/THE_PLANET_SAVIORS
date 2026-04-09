using System;
using System.Collections.Generic;
using UnityEngine;
using TPS.Runtime.Weather;

namespace TPS.Runtime.SaveLoad
{
    [Serializable]
    public class BoolStateEntry
    {
        public string Key;
        public bool Value;
    }

    [Serializable]
    public class IntStateEntry
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public class FloatStateEntry
    {
        public string Key;
        public float Value;
    }

    [Serializable]
    public class StringStateEntry
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public class GameStateData
    {
        public List<BoolStateEntry> BoolStates = new List<BoolStateEntry>();
        public List<IntStateEntry> IntStates = new List<IntStateEntry>();
        public List<FloatStateEntry> FloatStates = new List<FloatStateEntry>();
        public List<StringStateEntry> StringStates = new List<StringStateEntry>();
    }

    /// <summary>
    /// Explicit data schema for cross-system save information.
    /// Compatible with Unity JsonUtility.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int SaveVersion = 1;
        public string CurrentSceneName = "";
        
        // Player Transform
        public Vector3 PlayerPosition;
        public Quaternion PlayerRotation;

        // Time System
        public int WorldDay = 1;
        public int WorldHour = 8;
        public int WorldMinute = 0;

        // Weather System
        public WeatherType CurrentWeather = WeatherType.Sunny;

        // Game State entries serialized to Lists because JsonUtility doesn't support generic Dictionaries.
        public GameStateData GameState = new GameStateData();
    }
}
