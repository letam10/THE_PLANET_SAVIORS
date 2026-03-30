using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TPS.Data.Config;
using TPS.Runtime.Core;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;

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

            yield return EnsureCoreSceneLoaded(_gameConfig.CoreSceneName);

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

            WorldClock.Instance.Initialize(
                _gameConfig.StartDay,
                _gameConfig.StartHour,
                _gameConfig.StartMinute,
                _gameConfig.WorldMinutesPerRealSecond
            );

            WeatherSystem.Instance.Initialize(_gameConfig.StartingWeather);

            string firstScene = _gameConfig.BootToMainMenu
                ? _gameConfig.MainMenuSceneName
                : _gameConfig.StartingWorldSceneName;

            yield return SceneLoader.Instance.LoadContentSceneAsync(firstScene);
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