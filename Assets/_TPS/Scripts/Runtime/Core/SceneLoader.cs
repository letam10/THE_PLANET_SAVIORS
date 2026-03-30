using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Runtime.Core
{
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] private string _currentContentScene;

        public string CurrentContentScene => _currentContentScene;

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

        public IEnumerator LoadContentSceneAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneLoader: scene name is null or empty.");
                yield break;
            }

            if (!string.IsNullOrEmpty(_currentContentScene) && _currentContentScene == sceneName)
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(_currentContentScene))
            {
                Scene oldScene = SceneManager.GetSceneByName(_currentContentScene);
                if (oldScene.isLoaded)
                {
                    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(_currentContentScene);
                    if (unloadOp != null)
                    {
                        yield return unloadOp;
                    }
                }
            }

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"SceneLoader: failed to load scene '{sceneName}'.");
                yield break;
            }

            yield return loadOp;

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                SceneManager.SetActiveScene(loadedScene);
                _currentContentScene = sceneName;
            }
            else
            {
                Debug.LogError($"SceneLoader: loaded scene '{sceneName}' is invalid.");
            }
        }
    }
}