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

        private void OnGUI()
        {
            bool inBattle = SceneLoader.Instance != null && SceneLoader.Instance.CurrentContentScene == "BTL_Standard";
            bool menuVisible = RuntimeMenuCanvasController.Instance != null && (RuntimeMenuCanvasController.Instance.IsMenuVisible || inBattle);

            if (!menuVisible)
            {
                DrawStatusPanel(inBattle);
                DrawSmokePanel(inBattle);
            }

            if (!string.IsNullOrWhiteSpace(_lastMessage) && UnityEngine.Time.unscaledTime <= _messageExpireAt)
            {
                GUI.Box(new Rect((Screen.width - 420f) * 0.5f, Screen.height - 90f, 420f, 50f), _lastMessage);
            }
        }

        private void DrawStatusPanel(bool inBattle)
        {
            float width = 340f;
            float height = inBattle ? 180f : 260f;
            GUI.Box(new Rect(10f, Screen.height - height - 10f, width, height), "Functional Lock Runtime");

            float y = Screen.height - height + 20f;
            string timeText = WorldClock.Instance != null ? WorldClock.Instance.GetFormattedTime() : "Time: --";
            GUI.Label(new Rect(20f, y, width - 20f, 20f), timeText);
            y += 20f;

            string weatherText = WeatherSystem.Instance != null ? $"Weather: {WeatherSystem.Instance.CurrentWeather}" : "Weather: --";
            GUI.Label(new Rect(20f, y, width - 20f, 20f), weatherText);
            y += 20f;

            string currencyText = EconomyService.Instance != null ? $"Currency: {EconomyService.Instance.Currency}" : "Currency: --";
            GUI.Label(new Rect(20f, y, width - 20f, 20f), currencyText);
            y += 24f;

            if (SceneManager.GetActiveScene().name == "ZN_Town_AsterHarbor")
            {
                GUI.Label(new Rect(20f, y, width - 20f, 18f), "Environment: replace-safe generated scaffolding active");
                y += 18f;
            }

            if (PartyService.Instance != null)
            {
                List<string> activeMembers = PartyService.Instance.GetActiveMemberIds();
                for (int i = 0; i < activeMembers.Count && y < Screen.height - 55f; i++)
                {
                    CharacterStatSnapshot snapshot = PartyService.Instance.GetMemberSnapshot(activeMembers[i]);
                    if (snapshot == null)
                    {
                        continue;
                    }

                    string weaponName = snapshot.EquippedWeapon != null ? snapshot.EquippedWeapon.DisplayName : "No Weapon";
                    GUI.Label(new Rect(20f, y, width - 20f, 18f), $"{snapshot.DisplayName} Lv{snapshot.Level} | {weaponName}");
                    y += 18f;
                    GUI.Label(new Rect(20f, y, width - 20f, 18f), $"HP {PartyService.Instance.GetCurrentHP(snapshot.CharacterId)}/{snapshot.Stats.MaxHP} MP {PartyService.Instance.GetCurrentMP(snapshot.CharacterId)}/{snapshot.Stats.MaxMP} | ATK {snapshot.Stats.Attack} MAG {snapshot.Stats.Magic} DEF {snapshot.Stats.Defense} RES {snapshot.Stats.Resistance} SPD {snapshot.Stats.Speed}");
                    y += 20f;
                }
            }

            if (!inBattle)
            {
                if (GUI.Button(new Rect(20f, y, 60f, 24f), "Save") && SaveLoadManager.Instance != null) SaveLoadManager.Instance.SaveGame();
                if (GUI.Button(new Rect(90f, y, 60f, 24f), "Load") && SaveLoadManager.Instance != null) SaveLoadManager.Instance.LoadGame();
                if (GUI.Button(new Rect(160f, y, 50f, 24f), "Sun") && WeatherSystem.Instance != null) WeatherSystem.Instance.SetWeather(WeatherType.Sunny);
                if (GUI.Button(new Rect(220f, y, 50f, 24f), "Rain") && WeatherSystem.Instance != null) WeatherSystem.Instance.SetWeather(WeatherType.Rain);
                y += 30f;
                string focusLabel = RuntimeUiInputState.IsUiFocused
                    ? $"UI mode ON | Esc closes"
                    : "Gameplay mode ON | I/C/J/K open panels | F5 save | F9 load | P system";
                GUI.Label(new Rect(20f, y, width - 20f, 18f), focusLabel);
            }
        }

        private void DrawSmokePanel(bool inBattle)
        {
            if (Phase1SmokeRunner.Instance == null)
            {
                return;
            }

            float width = 500f;
            float height = inBattle ? 220f : 270f;
            GUI.Box(new Rect(10f, 10f, width, height), "Functional Lock Smoke");

            float rowY = 35f;
            string[] statusLines = Phase1SmokeRunner.Instance.BuildStatusLines();
            for (int i = 0; i < statusLines.Length; i++)
            {
                GUI.Label(new Rect(20f, rowY, width - 30f, 18f), statusLines[i]);
                rowY += 18f;
            }

            if (GUI.Button(new Rect(20f, rowY + 4f, 110f, 24f), "Log Snapshot"))
            {
                Phase1SmokeRunner.Instance.LogManualSnapshot();
            }

            if (GUI.Button(new Rect(140f, rowY + 4f, 110f, 24f), "Reset Smoke"))
            {
                Phase1SmokeRunner.Instance.ResetTelemetry();
            }

            rowY += 36f;
            GUI.Label(new Rect(20f, rowY, width - 30f, 18f), "Recent smoke events");
            rowY += 18f;

            IReadOnlyList<string> events = Phase1SmokeRunner.Instance.Timeline;
            int start = Mathf.Max(0, events.Count - (inBattle ? 5 : 7));
            for (int i = start; i < events.Count; i++)
            {
                GUI.Label(new Rect(20f, rowY, width - 30f, 18f), events[i]);
                rowY += 18f;
            }
        }
    }
}
