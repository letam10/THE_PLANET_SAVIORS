using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TPS.Data.Config;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;
using TPS.Runtime.Spawn;
using TPS.Runtime.World;

namespace TPS.Runtime.Bootstrap
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig _gameConfig;

        private IEnumerator Start()
        {
            if (_gameConfig == null)
            {
                Debug.LogError("GameBootstrap: GameConfig is not assigned.");
                yield break;
            }

            // 1. Load Core scene (contains all singletons)
            yield return EnsureCoreSceneLoaded(_gameConfig.CoreSceneName);

            // 2. Validate all core services
            if (SceneLoader.Instance == null)
            {
                Debug.LogError("GameBootstrap: SceneLoader not found in Core scene.");
                yield break;
            }

            if (WorldClock.Instance == null)
            {
                Debug.LogError("GameBootstrap: WorldClock not found in Core scene.");
                yield break;
            }

            if (WeatherSystem.Instance == null)
            {
                Debug.LogError("GameBootstrap: WeatherSystem not found in Core scene.");
                yield break;
            }

            if (PlayerSpawnSystem.Instance == null)
            {
                Debug.LogError("GameBootstrap: PlayerSpawnSystem not found in Core scene.");
                yield break;
            }

            if (QuestService.Instance == null ||
                DialogueStateService.Instance == null ||
                PartyService.Instance == null ||
                InventoryService.Instance == null ||
                ProgressionService.Instance == null ||
                EncounterService.Instance == null ||
                ZoneStateService.Instance == null ||
                EconomyService.Instance == null ||
                RewardService.Instance == null ||
                StateResolver.Instance == null)
            {
                Debug.LogError("GameBootstrap: Phase 1 services are not fully present in Core scene.");
                yield break;
            }

            // 3. Initialize services
            WorldClock.Instance.Initialize(
                _gameConfig.StartDay,
                _gameConfig.StartHour,
                _gameConfig.StartMinute,
                _gameConfig.WorldMinutesPerRealSecond
            );

            WeatherSystem.Instance.Initialize((WeatherType)_gameConfig.StartingWeather);

            PlayerSpawnSystem.Instance.Initialize(_gameConfig);

            // 4. Load first content scene
            // PlayerSpawnSystem will react to sceneLoaded and spawn/teleport the player
            string firstScene = _gameConfig.BootToMainMenu
                ? _gameConfig.MainMenuSceneName
                : _gameConfig.StartingWorldSceneName;

            yield return SceneLoader.Instance.LoadContentSceneAsync(firstScene);

            if (StateResolver.Instance != null)
            {
                StateResolver.Instance.ResolveAll();
            }
        }

        private static IEnumerator EnsureCoreSceneLoaded(string coreSceneName)
        {
            Scene coreScene = SceneManager.GetSceneByName(coreSceneName);
            if (coreScene.IsValid() && coreScene.isLoaded)
            {
                yield break;
            }

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(coreSceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"GameBootstrap: failed to load core scene '{coreSceneName}'.");
                yield break;
            }

            yield return loadOp;
        }
    }
}
