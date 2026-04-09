using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using TPS.Runtime.Core;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;
using TPS.Runtime.Spawn;

namespace TPS.Runtime.SaveLoad
{
    /// <summary>
    /// Explicit, strictly-ordered Save/Load implementation.
    /// Ensures that state, time, weather are fully restored before gameplay components react.
    /// </summary>
    public sealed class SaveLoadManager : MonoBehaviour
    {
        public static SaveLoadManager Instance { get; private set; }

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "debug_save.json");

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

        [ContextMenu("Save Game")]
        public void SaveGame()
        {
            SaveData data = new SaveData();

            // 1. Scene Name (Assume SceneLoader is tracking it)
            if (SceneLoader.Instance != null && !string.IsNullOrEmpty(SceneLoader.Instance.CurrentContentScene))
            {
                data.CurrentSceneName = SceneLoader.Instance.CurrentContentScene;
            }
            else
            {
                Debug.LogWarning("[SaveLoad] CurrentContentScene is empty. Save might fail to load.");
            }

            // 2. Player Transform
            if (PlayerSpawnSystem.Instance != null && PlayerSpawnSystem.Instance.TryGetPlayerTransform(out Vector3 pos, out Quaternion rot))
            {
                data.PlayerPosition = pos;
                data.PlayerRotation = rot;
            }

            // 3. Time
            if (WorldClock.Instance != null)
            {
                data.WorldDay = WorldClock.Instance.CurrentDay;
                data.WorldHour = WorldClock.Instance.CurrentHour;
                data.WorldMinute = WorldClock.Instance.CurrentMinute;
            }

            // 4. Weather
            if (WeatherSystem.Instance != null)
            {
                data.CurrentWeather = WeatherSystem.Instance.CurrentWeather;
            }

            // 5. Game State
            if (GameStateManager.Instance != null)
            {
                var bools = GameStateManager.Instance.GetAllBoolStates();
                foreach (var kvp in bools) data.GameState.BoolStates.Add(new BoolStateEntry { Key = kvp.Key, Value = kvp.Value });

                var ints = GameStateManager.Instance.GetAllIntStates();
                foreach (var kvp in ints) data.GameState.IntStates.Add(new IntStateEntry { Key = kvp.Key, Value = kvp.Value });

                var floats = GameStateManager.Instance.GetAllFloatStates();
                foreach (var kvp in floats) data.GameState.FloatStates.Add(new FloatStateEntry { Key = kvp.Key, Value = kvp.Value });

                var strings = GameStateManager.Instance.GetAllStringStates();
                foreach (var kvp in strings) data.GameState.StringStates.Add(new StringStateEntry { Key = kvp.Key, Value = kvp.Value });
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveLoad] Game saved successfully to: {SaveFilePath}");
                GameEventBus.PublishGameSaved();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveLoad] Failed to save game: {e.Message}");
            }
        }

        [ContextMenu("Load Game")]
        public void LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning("[SaveLoad] No save file found!");
                return;
            }

            StartCoroutine(LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            Debug.Log("[SaveLoad] Beginning Load Sequence...");

            // 1. Read JSON
            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // 2. Load Scene
            if (SceneLoader.Instance != null && !string.IsNullOrEmpty(data.CurrentSceneName))
            {
                yield return SceneLoader.Instance.LoadContentSceneAsync(data.CurrentSceneName);
            }
            else
            {
                Debug.LogWarning("[SaveLoad] Skipping scene load because SceneLoader is missing or Data has no scene.");
            }

            // Let Unity initialize fresh scene objects
            yield return null;

            // 3. Restore WorldClock
            if (WorldClock.Instance != null)
            {
                WorldClock.Instance.Initialize(data.WorldDay, data.WorldHour, data.WorldMinute, 1f);
            }

            // 4. Restore WeatherSystem
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.SetWeather(data.CurrentWeather, force: true);
            }

            // 5. Restore GameStateManager
            if (GameStateManager.Instance != null)
            {
                var boolDict = data.GameState.BoolStates.ToDictionary(e => e.Key, e => e.Value);
                var intDict = data.GameState.IntStates.ToDictionary(e => e.Key, e => e.Value);
                var floatDict = data.GameState.FloatStates.ToDictionary(e => e.Key, e => e.Value);
                var stringDict = data.GameState.StringStates.ToDictionary(e => e.Key, e => e.Value);

                GameStateManager.Instance.RestoreAllStates(boolDict, intDict, floatDict, stringDict);
            }

            // 6. Publish OnGameLoaded (So ConditionalActivator and NPCSchedule evaluated immediately)
            Debug.Log("[SaveLoad] Publishing OnGameLoaded...");
            GameEventBus.PublishGameLoaded();

            // 7. Spawn/Teleport Player
            if (PlayerSpawnSystem.Instance != null)
            {
                PlayerSpawnSystem.Instance.TeleportPlayerExact(data.PlayerPosition, data.PlayerRotation);
            }

            Debug.Log("[SaveLoad] Load Sequence Complete.");
        }
    }
}
