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
        private string _pendingSpawnId;
        private bool _hasPendingExactTransform;
        private Vector3 _pendingExactPosition;
        private Quaternion _pendingExactRotation = Quaternion.identity;
        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation = Quaternion.identity;
        private bool _hasLastSafeTransform;

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

            EnsurePlayerInstance();

            if (_hasPendingExactTransform &&
                TryResolveSafeTransform(scene, _pendingExactPosition, _pendingExactRotation, out Vector3 exactPosition, out Quaternion exactRotation))
            {
                Debug.Log($"PlayerSpawnSystem: Restoring exact transform in scene '{scene.name}'.");
                TeleportPlayerInternal(exactPosition, exactRotation);
                CacheSafeTransform(exactPosition, exactRotation);
                _hasPendingExactTransform = false;
                _pendingSpawnId = null;
                return;
            }

            string spawnId = !string.IsNullOrWhiteSpace(_pendingSpawnId) ? _pendingSpawnId : _config.DefaultSpawnId;
            if (TryTeleportToSpawn(scene, spawnId, $"scene '{scene.name}'"))
            {
                _hasPendingExactTransform = false;
                _pendingSpawnId = null;
                return;
            }

            if (_hasLastSafeTransform &&
                TryResolveSafeTransform(scene, _lastSafePosition, _lastSafeRotation, out Vector3 fallbackPosition, out Quaternion fallbackRotation))
            {
                Debug.LogWarning($"PlayerSpawnSystem: Falling back to last safe transform in scene '{scene.name}'.");
                TeleportPlayerInternal(fallbackPosition, fallbackRotation);
                CacheSafeTransform(fallbackPosition, fallbackRotation);
            }
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

        private void EnsurePlayerInstance()
        {
            if (_playerInstance != null)
            {
                return;
            }

            _playerInstance = Instantiate(_config.PlayerPrefab);
            _playerInstance.name = "Player";
            DontDestroyOnLoad(_playerInstance);
        }

        private bool TryTeleportToSpawn(Scene scene, string spawnId, string contextLabel)
        {
            SpawnPoint point = FindSpawnPointInScene(scene, spawnId);
            if (point == null && spawnId != _config.DefaultSpawnId)
            {
                point = FindSpawnPointInScene(scene, _config.DefaultSpawnId);
            }

            if (point == null)
            {
                return false;
            }

            Vector3 desiredPosition = point.transform.position;
            Quaternion desiredRotation = point.transform.rotation;
            if (!TryResolveSafeTransform(scene, desiredPosition, desiredRotation, out Vector3 safePosition, out Quaternion safeRotation))
            {
                safePosition = desiredPosition;
                safeRotation = desiredRotation;
            }

            Debug.Log($"PlayerSpawnSystem: Teleporting player to '{point.SpawnId}' in {contextLabel}.");
            TeleportPlayerInternal(safePosition, safeRotation);
            CacheSafeTransform(safePosition, safeRotation);
            return true;
        }

        private bool TryResolveSafeTransform(Scene scene, Vector3 desiredPosition, Quaternion desiredRotation, out Vector3 safePosition, out Quaternion safeRotation)
        {
            safePosition = desiredPosition;
            safeRotation = desiredRotation;

            float halfHeight = GetCharacterHalfHeight();
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(0.75f, 0f, 0f),
                new Vector3(-0.75f, 0f, 0f),
                new Vector3(0f, 0f, 0.75f),
                new Vector3(0f, 0f, -0.75f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 sample = desiredPosition + offsets[i];
                Vector3 origin = sample + Vector3.up * 25f;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore))
                {
                    safePosition = hit.point + Vector3.up * (halfHeight + 0.05f);
                    safeRotation = desiredRotation;
                    return true;
                }
            }

            SpawnPoint defaultPoint = FindSpawnPointInScene(scene, _config != null ? _config.DefaultSpawnId : "Default");
            if (defaultPoint != null)
            {
                safePosition = defaultPoint.transform.position;
                safeRotation = defaultPoint.transform.rotation;
                return true;
            }

            return false;
        }

        private float GetCharacterHalfHeight()
        {
            if (_playerInstance == null)
            {
                return 1f;
            }

            CharacterController controller = _playerInstance.GetComponent<CharacterController>();
            if (controller != null)
            {
                return Mathf.Max(0.5f, controller.height * 0.5f);
            }

            return 1f;
        }

        private void TeleportPlayer(Transform target)
        {
            if (target == null)
            {
                return;
            }

            EnsurePlayerInstance();
            TeleportPlayerInternal(target.position, target.rotation);
            CacheSafeTransform(target.position, target.rotation);
        }

        public void TeleportPlayerExact(Vector3 position, Quaternion rotation)
        {
            EnsurePlayerInstance();
            TeleportPlayerInternal(position, rotation);
            CacheSafeTransform(position, rotation);
        }

        public void TeleportPlayerSafe(Vector3 desiredPosition, Quaternion desiredRotation, string fallbackSpawnId = null)
        {
            EnsurePlayerInstance();
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() &&
                activeScene.isLoaded &&
                TryResolveSafeTransform(activeScene, desiredPosition, desiredRotation, out Vector3 safePosition, out Quaternion safeRotation))
            {
                TeleportPlayerInternal(safePosition, safeRotation);
                CacheSafeTransform(safePosition, safeRotation);
                return;
            }

            if (!string.IsNullOrWhiteSpace(fallbackSpawnId))
            {
                SpawnPoint fallback = FindSpawnPointInScene(activeScene, fallbackSpawnId);
                if (fallback != null)
                {
                    TeleportPlayerInternal(fallback.transform.position, fallback.transform.rotation);
                    CacheSafeTransform(fallback.transform.position, fallback.transform.rotation);
                    return;
                }
            }

            TeleportPlayerInternal(desiredPosition, desiredRotation);
            CacheSafeTransform(desiredPosition, desiredRotation);
        }

        public bool EnsurePlayerOnValidGround(string fallbackSpawnId = null)
        {
            if (_playerInstance == null)
            {
                return false;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            Vector3 currentPosition = _playerInstance.transform.position;
            Quaternion currentRotation = _playerInstance.transform.rotation;
            if (activeScene.IsValid() &&
                activeScene.isLoaded &&
                TryResolveSafeTransform(activeScene, currentPosition, currentRotation, out Vector3 safePosition, out Quaternion safeRotation))
            {
                if (Vector3.Distance(currentPosition, safePosition) > 0.05f || Quaternion.Angle(currentRotation, safeRotation) > 1f)
                {
                    TeleportPlayerInternal(safePosition, safeRotation);
                }

                CacheSafeTransform(safePosition, safeRotation);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(fallbackSpawnId))
            {
                SpawnPoint fallback = FindSpawnPointInScene(activeScene, fallbackSpawnId);
                if (fallback != null)
                {
                    TeleportPlayerInternal(fallback.transform.position, fallback.transform.rotation);
                    CacheSafeTransform(fallback.transform.position, fallback.transform.rotation);
                    return true;
                }
            }

            return false;
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

        public void SetPendingSpawnId(string spawnId)
        {
            _pendingSpawnId = string.IsNullOrWhiteSpace(spawnId)
                ? _config != null ? _config.DefaultSpawnId : "Default"
                : spawnId;
            _hasPendingExactTransform = false;
        }

        public void SetPendingSpawnTransform(Vector3 position, Quaternion rotation, string fallbackSpawnId = null)
        {
            _pendingExactPosition = position;
            _pendingExactRotation = rotation;
            _hasPendingExactTransform = true;
            _pendingSpawnId = string.IsNullOrWhiteSpace(fallbackSpawnId)
                ? _config != null ? _config.DefaultSpawnId : "Default"
                : fallbackSpawnId;
        }

        private void TeleportPlayerInternal(Vector3 position, Quaternion rotation)
        {
            if (_playerInstance == null) return;

            CharacterController controller = _playerInstance.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            _playerInstance.transform.position = position;
            _playerInstance.transform.rotation = rotation;

            if (controller != null) controller.enabled = true;
        }

        private void CacheSafeTransform(Vector3 position, Quaternion rotation)
        {
            _lastSafePosition = position;
            _lastSafeRotation = rotation;
            _hasLastSafeTransform = true;
        }
    }
}
