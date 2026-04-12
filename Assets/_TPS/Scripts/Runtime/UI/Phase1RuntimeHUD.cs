using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using TPS.Runtime.SaveLoad;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;
using TPS.Runtime.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TPS.Runtime.UI
{
    public sealed class Phase1RuntimeHUD : MonoBehaviour
    {
        public static Phase1RuntimeHUD Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private string _lastMessage = "";
        private float _messageExpireAt = -1f;

        public MerchantAnchor ActiveMerchant => RuntimeMenuCanvasController.Instance != null
            ? RuntimeMenuCanvasController.Instance.ActiveMerchant
            : null;
        public Phase1ContentCatalog ContentCatalog => _contentCatalog;

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
            RuntimeMenuCanvasController.EnsureExists();
            WeatherPresentationController.EnsureExists();
            RuntimeUiInputState.RestoreGameplayFocus();
            GameEventBus.OnGameLoaded += HandleRuntimeReset;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDisable()
        {
            GameEventBus.OnGameLoaded -= HandleRuntimeReset;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            if (Instance == this)
            {
                RuntimeUiInputState.RestoreGameplayFocus();
            }
        }

        private void Update()
        {
            // Legacy input logic removed; Canvas Action Map in RuntimeMenuCanvasController handles UI input.
        }

        public void ShowMessage(string message, float duration = 4f)
        {
            _lastMessage = message;
            _messageExpireAt = UnityEngine.Time.unscaledTime + duration;
        }

        public void ToggleShop(MerchantAnchor merchantAnchor)
        {
            if (RuntimeMenuCanvasController.Instance != null)
            {
                RuntimeMenuCanvasController.Instance.ToggleMerchantShop(merchantAnchor);
            }
        }

        public void CloseShop()
        {
            if (RuntimeMenuCanvasController.Instance != null)
            {
                RuntimeMenuCanvasController.Instance.CloseMerchantShop();
            }
        }

        private void HandleRuntimeReset()
        {
            if (RuntimeMenuCanvasController.Instance == null)
            {
                RuntimeUiInputState.RestoreGameplayFocus();
            }
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            HandleRuntimeReset();
        }

        public ItemDefinition FindFirstUsableConsumable()
        {
            if (_contentCatalog == null || InventoryService.Instance == null)
            {
                return null;
            }

            IReadOnlyList<ItemDefinition> items = _contentCatalog.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition item = items[i];
                if (item != null && InventoryService.Instance.GetItemCount(item.ItemId) > 0 && (item.RestoreHP > 0 || item.RestoreMP > 0))
                {
                    return item;
                }
            }

            return null;
        }


    }
}
