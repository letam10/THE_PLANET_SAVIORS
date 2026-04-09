using UnityEngine;
using UnityEngine.SceneManagement;
using TPS.Data.Config;

namespace TPS.Runtime.Spawn
{
    /// <summary>
    /// Manages player instantiation and teleportation across scenes.
    /// Subscribes to SceneManager.sceneLoaded to auto-find SpawnPoints when new scenes load.
    /// </summary>
    public sealed class PlayerSpawnSystem : MonoBehaviour
    {
        public static PlayerSpawnSystem Instance { get; private set; }

        private GameConfig _config;
        private GameObject _playerInstance;

        public GameObject PlayerInstance => _playerInstance;

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

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Called by GameBootstrap after Core scene services are ready.
        /// Stores config for later use when content scenes load.
        /// </summary>
        public void Initialize(GameConfig config)
        {
            _config = config;
            Debug.Log("PlayerSpawnSystem: Initialized.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_config == null) return;
            if (_config.PlayerPrefab == null)
            {
                Debug.LogWarning("PlayerSpawnSystem: PlayerPrefab is null in GameConfig.");
                return;
            }

            // Find a spawn point in the newly loaded scene
            string spawnId = _config.DefaultSpawnId;
            SpawnPoint point = FindSpawnPointInScene(scene, spawnId);

            if (point == null)
            {
                // No spawn point in this scene (e.g. Core, Bootstrap) — skip
                return;
            }

            if (_playerInstance == null)
            {
                _playerInstance = Instantiate(_config.PlayerPrefab);
                _playerInstance.name = "Player";
                DontDestroyOnLoad(_playerInstance);
                Debug.Log($"PlayerSpawnSystem: Spawned player at '{point.SpawnId}' in scene '{scene.name}'");
            }
            else
            {
                Debug.Log($"PlayerSpawnSystem: Teleporting player to '{point.SpawnId}' in scene '{scene.name}'");
            }

            TeleportPlayer(point.transform);
        }

        private SpawnPoint FindSpawnPointInScene(Scene scene, string spawnId)
        {
            if (!scene.isLoaded) return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SpawnPoint[] points = root.GetComponentsInChildren<SpawnPoint>(true);
                foreach (SpawnPoint p in points)
                {
                    if (p.SpawnId == spawnId)
                    {
                        return p;
                    }
                }
            }

            return null;
        }

        private void TeleportPlayer(Transform target)
        {
            if (_playerInstance == null) return;

            CharacterController cc = _playerInstance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            _playerInstance.transform.position = target.position;
            _playerInstance.transform.rotation = target.rotation;

            if (cc != null) cc.enabled = true;
        }

        public void TeleportPlayerExact(Vector3 position, Quaternion rotation)
        {
            if (_playerInstance == null) return;

            CharacterController cc = _playerInstance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            _playerInstance.transform.position = position;
            _playerInstance.transform.rotation = rotation;

            if (cc != null) cc.enabled = true;
        }

        public bool TryGetPlayerTransform(out Vector3 position, out Quaternion rotation)
        {
            if (_playerInstance != null)
            {
                position = _playerInstance.transform.position;
                rotation = _playerInstance.transform.rotation;
                return true;
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }
    }
}
