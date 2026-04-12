using System.Collections;
using System.IO;
using UnityEngine;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using TPS.Runtime.Time;
using TPS.Runtime.UI;
using TPS.Runtime.Weather;
using TPS.Runtime.Spawn;
using TPS.Runtime.World;

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

            if (DialogueStateService.Instance != null) data.DialogueState = DialogueStateService.Instance.CaptureState();
            if (QuestService.Instance != null) data.QuestState = QuestService.Instance.CaptureState();
            if (PartyService.Instance != null) data.PartyState = PartyService.Instance.CaptureState();
            if (InventoryService.Instance != null) data.InventoryState = InventoryService.Instance.CaptureState();
            if (ProgressionService.Instance != null) data.ProgressionState = ProgressionService.Instance.CaptureState();
            if (EncounterService.Instance != null) data.EncounterState = EncounterService.Instance.CaptureState();
            if (ZoneStateService.Instance != null) data.ZoneState = ZoneStateService.Instance.CaptureState();
            if (EconomyService.Instance != null) data.EconomyState = EconomyService.Instance.CaptureState();

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
            if (data == null)
            {
                Debug.LogError("[SaveLoad] Save file is invalid JSON. Load aborted.");
                yield break;
            }

            if (data.SaveVersion != SaveData.CurrentVersion)
            {
                Debug.LogWarning($"[SaveLoad] Unsupported save version {data.SaveVersion}. Expected {SaveData.CurrentVersion}. Save ignored.");
                yield break;
            }

            // 2. Load Scene
            if (SceneLoader.Instance != null && !string.IsNullOrEmpty(data.CurrentSceneName))
            {
                if (PlayerSpawnSystem.Instance != null)
                {
                    PlayerSpawnSystem.Instance.SetPendingSpawnTransform(data.PlayerPosition, data.PlayerRotation);
                }

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
                WorldClock.Instance.SetDateTime(data.WorldDay, data.WorldHour, data.WorldMinute);
            }

            // 4. Restore WeatherSystem
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.SetWeather(data.CurrentWeather, force: true);
            }

            if (InventoryService.Instance != null) InventoryService.Instance.RestoreState(data.InventoryState);
            if (ProgressionService.Instance != null) ProgressionService.Instance.RestoreState(data.ProgressionState);
            if (PartyService.Instance != null) PartyService.Instance.RestoreState(data.PartyState);
            if (DialogueStateService.Instance != null) DialogueStateService.Instance.RestoreState(data.DialogueState);
            if (EncounterService.Instance != null) EncounterService.Instance.RestoreState(data.EncounterState);
            if (ZoneStateService.Instance != null) ZoneStateService.Instance.RestoreState(data.ZoneState);
            if (EconomyService.Instance != null) EconomyService.Instance.RestoreState(data.EconomyState);
            if (QuestService.Instance != null) QuestService.Instance.RestoreState(data.QuestState);

            Debug.Log("[SaveLoad] Publishing OnGameLoaded...");
            GameEventBus.PublishGameLoaded();

            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.ResolveAll();
            }

            if (PlayerSpawnSystem.Instance != null)
            {
                PlayerSpawnSystem.Instance.EnsurePlayerOnValidGround("Default");
            }

            RuntimeUiInputState.RestoreGameplayFocus();
            Debug.Log("[SaveLoad] Load Sequence Complete.");
        }
    }
}
