using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Core
{
    /// <summary>
    /// Global Game State Manager.
    /// Stores boolean, integer, float, and string values.
    /// Publishes changes to the GameEventBus.
    /// Does not serialize its own files, but provides data shapes to SaveLoadManager.
    /// </summary>
    public sealed class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        private readonly Dictionary<string, bool> _boolStates = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _intStates = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _floatStates = new Dictionary<string, float>();
        private readonly Dictionary<string, string> _stringStates = new Dictionary<string, string>();

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

        // ==========================================
        // BOOL STATES
        // ==========================================
        public void SetBool(string key, bool value)
        {
            if (_boolStates.TryGetValue(key, out bool currentValue) && currentValue == value) return;
            
            _boolStates[key] = value;
            GameEventBus.PublishGameStateChanged(key);
            Debug.Log($"[GameState] Set Bool '{key}' to {value}");
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return _boolStates.TryGetValue(key, out bool value) ? value : defaultValue;
        }

        // ==========================================
        // INT STATES
        // ==========================================
        public void SetInt(string key, int value)
        {
            if (_intStates.TryGetValue(key, out int currentValue) && currentValue == value) return;
            
            _intStates[key] = value;
            GameEventBus.PublishGameStateChanged(key);
            Debug.Log($"[GameState] Set Int '{key}' to {value}");
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return _intStates.TryGetValue(key, out int value) ? value : defaultValue;
        }

        // ==========================================
        // FLOAT STATES
        // ==========================================
        public void SetFloat(string key, float value)
        {
            if (_floatStates.TryGetValue(key, out float currentValue) && Mathf.Approximately(currentValue, value)) return;
            
            _floatStates[key] = value;
            GameEventBus.PublishGameStateChanged(key);
            Debug.Log($"[GameState] Set Float '{key}' to {value}");
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _floatStates.TryGetValue(key, out float value) ? value : defaultValue;
        }

        // ==========================================
        // STRING STATES
        // ==========================================
        public void SetString(string key, string value)
        {
            if (_stringStates.TryGetValue(key, out string currentValue) && currentValue == value) return;
            
            _stringStates[key] = value;
            GameEventBus.PublishGameStateChanged(key);
            Debug.Log($"[GameState] Set String '{key}' to {value}");
        }

        public string GetString(string key, string defaultValue = "")
        {
            return _stringStates.TryGetValue(key, out string value) ? value : defaultValue;
        }

        // ==========================================
        // BATCH DATA TRANSFER (FOR SAVE/LOAD)
        // ==========================================
        public Dictionary<string, bool> GetAllBoolStates() => new Dictionary<string, bool>(_boolStates);
        public Dictionary<string, int> GetAllIntStates() => new Dictionary<string, int>(_intStates);
        public Dictionary<string, float> GetAllFloatStates() => new Dictionary<string, float>(_floatStates);
        public Dictionary<string, string> GetAllStringStates() => new Dictionary<string, string>(_stringStates);

        public void RestoreAllStates(
            Dictionary<string, bool> bools, 
            Dictionary<string, int> ints, 
            Dictionary<string, float> floats, 
            Dictionary<string, string> strings)
        {
            _boolStates.Clear();
            _intStates.Clear();
            _floatStates.Clear();
            _stringStates.Clear();

            if (bools != null) foreach (var kvp in bools) _boolStates[kvp.Key] = kvp.Value;
            if (ints != null) foreach (var kvp in ints) _intStates[kvp.Key] = kvp.Value;
            if (floats != null) foreach (var kvp in floats) _floatStates[kvp.Key] = kvp.Value;
            if (strings != null) foreach (var kvp in strings) _stringStates[kvp.Key] = kvp.Value;

            Debug.Log("[GameState] Fully restored from Save Data.");
        }
    }
}
