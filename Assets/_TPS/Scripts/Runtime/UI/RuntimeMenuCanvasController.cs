using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Quest;
using TPS.Runtime.SaveLoad;
using TPS.Runtime.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TPS.Runtime.UI
{
    public sealed class RuntimeMenuCanvasController : MonoBehaviour
    {
        private enum PanelType
        {
            None = 0,
            Inventory = 1,
            Character = 2,
            Quest = 3,
            Equipment = 4,
            System = 5,
            Shop = 6,
            Help = 7,
            Battle = 8,
            BattleEnd = 9,
            Sleep = 10
        }

        public static RuntimeMenuCanvasController Instance { get; private set; }

        private const string LanguageKey = "TPS.Runtime.Language";
        private const string MasterVolumeKey = "TPS.Runtime.Audio.Master";
        private const string MusicVolumeKey = "TPS.Runtime.Audio.Music";
        private const string SfxVolumeKey = "TPS.Runtime.Audio.Sfx";
        private const string FullscreenKey = "TPS.Runtime.Display.Fullscreen";
        private const string ResolutionWidthKey = "TPS.Runtime.Display.Width";
        private const string ResolutionHeightKey = "TPS.Runtime.Display.Height";
        private const string TideRouteQuestId = "quest_secure_tide_route";
        private const string DockSuppliesZoneId = "aster_harbor";
        private const string DockSuppliesFactId = "dock_supplies_secured";
        private const string TideRouteZoneId = "gullwatch";
        private const string TideRouteSecuredFactId = "tide_route_secured";

        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private CanvasScaler _scaler;
        private EventSystem _eventSystem;
        private InputSystemUIInputModule _inputModule;
        private RectTransform _root;
        private RectTransform _menuPanel;
        private RectTransform _titleBar;
        private Text _titleText;
        private RectTransform _buttonBar;
        private RectTransform _contentRoot;
        private RectTransform _contentViewport;
        private RectTransform _contentColumn;
        private ScrollRect _contentScroll;
        private RectTransform _footerBar;
        private Text _hintText;
        private Font _font;
        private PanelType _activePanel;
        private string _selectedMemberId;
        private MerchantAnchor _activeMerchant;
        private InnAnchor _activeInn;
        private float _lastRefreshAt = -1f;
        private bool _battleWasActive;

        public bool IsMenuVisible => _activePanel != PanelType.None;
        public MerchantAnchor ActiveMerchant => _activeMerchant;

        public static void EnsureExists()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject go = new GameObject("RuntimeMenuCanvasController");
            go.AddComponent<RuntimeMenuCanvasController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameEventBus.OnGameLoaded += HandleRuntimeReset;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            BuildUiShell();
            ApplyPersistedSettings();
        }

        private void OnDestroy()
        {
            GameEventBus.OnGameLoaded -= HandleRuntimeReset;
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            EnsureSelectedMember();
            EnsureEventSystem();
            SyncBattlePanelState();
            SyncMerchantPanelState();
            HandleHotkeys();
        }

        public void ToggleMerchantShop(MerchantAnchor merchant)
        {
            if (merchant == null)
            {
                CloseMerchantShop();
                return;
            }

            if (_activeMerchant == merchant && _activePanel == PanelType.Shop)
            {
                CloseMerchantShop();
                return;
            }

            _activeMerchant = merchant;
            _activeInn = null;
            OpenPanel(PanelType.Shop);
        }

        public void CloseMerchantShop()
        {
            _activeMerchant = null;
            if (_activePanel == PanelType.Shop)
            {
                _activePanel = PanelType.None;
                RuntimeUiInputState.RestoreGameplayFocus();
                RefreshVisibility();
            }
        }

        public void OpenSleepPanel(InnAnchor inn)
        {
            if (inn == null)
            {
                return;
            }

            _activeInn = inn;
            _activeMerchant = null;
            OpenPanel(PanelType.Sleep);
        }

        public void PrepareForSceneTransition(string message = null)
        {
            _activeMerchant = null;
            _activeInn = null;
            _battleWasActive = false;

            if (!IsBattlePanelActive())
            {
                _activePanel = PanelType.None;
                RuntimeUiInputState.RestoreGameplayFocus();
                RefreshVisibility();
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                Notify(message);
            }
        }

        private void BuildUiShell()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1600f, 900f);
            _scaler.matchWidthOrHeight = 0.5f;
            _raycaster = gameObject.AddComponent<GraphicRaycaster>();

            _root = gameObject.GetComponent<RectTransform>();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            Image dimmer = CreateImage("RuntimeMenuDimmer", _root, new Color(0f, 0f, 0f, 0.18f));
            RectTransform dimmerRect = dimmer.rectTransform;
            dimmerRect.anchorMin = Vector2.zero;
            dimmerRect.anchorMax = Vector2.one;
            dimmerRect.offsetMin = Vector2.zero;
            dimmerRect.offsetMax = Vector2.zero;
            dimmer.gameObject.SetActive(false);

            _menuPanel = CreateStretchPanel("RuntimeMenuPanel", _root, new Color(0.08f, 0.11f, 0.15f, 0.94f), new Vector2(68f, 58f), new Vector2(-68f, -58f));
            _menuPanel.gameObject.SetActive(false);

            VerticalLayoutGroup menuLayout = _menuPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            menuLayout.padding = new RectOffset(18, 18, 18, 18);
            menuLayout.spacing = 10f;
            menuLayout.childControlHeight = false;
            menuLayout.childControlWidth = true;
            menuLayout.childForceExpandHeight = false;
            menuLayout.childForceExpandWidth = true;

            _titleBar = CreateContainer("TitleBar", _menuPanel, 42f);
            HorizontalLayoutGroup titleLayout = _titleBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlHeight = true;
            titleLayout.childControlWidth = true;
            titleLayout.childForceExpandHeight = true;
            titleLayout.childForceExpandWidth = true;
            _titleText = CreateText("Title", _titleBar, "Runtime Menu", 24, TextAnchor.MiddleLeft, FontStyle.Bold);

            _buttonBar = CreateContainer("ButtonBar", _menuPanel, 40f);
            GridLayoutGroup buttonGrid = _buttonBar.gameObject.AddComponent<GridLayoutGroup>();
            buttonGrid.cellSize = new Vector2(150f, 34f);
            buttonGrid.spacing = new Vector2(8f, 8f);
            buttonGrid.constraint = GridLayoutGroup.Constraint.Flexible;

            AddTabButton("Inventory", PanelType.Inventory);
            AddTabButton("Character", PanelType.Character);
            AddTabButton("Quest Log", PanelType.Quest);
            AddTabButton("Equipment", PanelType.Equipment);
            AddTabButton("System", PanelType.System);
            AddTabButton("Help", PanelType.Help);

            _contentRoot = CreatePanel("ContentRoot", _menuPanel, new Color(0.12f, 0.16f, 0.22f, 0.82f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f));
            LayoutElement contentLayout = _contentRoot.gameObject.AddComponent<LayoutElement>();
            contentLayout.flexibleHeight = 1f;
            contentLayout.minHeight = 340f;
            _contentViewport = CreateStretchPanel("Viewport", _contentRoot, new Color(0.07f, 0.09f, 0.12f, 0.16f), new Vector2(10f, 10f), new Vector2(-10f, -10f));
            Mask mask = _contentViewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            _contentScroll = _contentRoot.gameObject.AddComponent<ScrollRect>();
            _contentScroll.horizontal = false;
            _contentScroll.movementType = ScrollRect.MovementType.Clamped;
            _contentScroll.viewport = _contentViewport;

            GameObject contentColumnGo = new GameObject("ContentColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentColumnGo.transform.SetParent(_contentViewport, false);
            _contentColumn = contentColumnGo.GetComponent<RectTransform>();
            _contentColumn.anchorMin = new Vector2(0f, 1f);
            _contentColumn.anchorMax = new Vector2(1f, 1f);
            _contentColumn.pivot = new Vector2(0.5f, 1f);
            _contentColumn.offsetMin = new Vector2(0f, 0f);
            _contentColumn.offsetMax = new Vector2(0f, 0f);
            VerticalLayoutGroup contentGroup = _contentColumn.GetComponent<VerticalLayoutGroup>();
            contentGroup.padding = new RectOffset(14, 14, 14, 14);
            contentGroup.spacing = 8f;
            contentGroup.childControlHeight = false;
            contentGroup.childControlWidth = true;
            contentGroup.childForceExpandHeight = false;
            contentGroup.childForceExpandWidth = true;
            ContentSizeFitter fitter = _contentColumn.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _contentScroll.content = _contentColumn;

            _footerBar = CreateContainer("FooterBar", _menuPanel, 34f);
            HorizontalLayoutGroup footerLayout = _footerBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerLayout.childAlignment = TextAnchor.MiddleLeft;
            footerLayout.spacing = 12f;
            footerLayout.childControlHeight = true;
            footerLayout.childControlWidth = true;
            footerLayout.childForceExpandHeight = true;
            footerLayout.childForceExpandWidth = true;
            _hintText = CreateText("Hint", _footerBar, string.Empty, 16, TextAnchor.MiddleLeft, FontStyle.Normal);

            EnsureEventSystem();
            RefreshHintText();
            RefreshVisibility();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                _eventSystem = EventSystem.current;
                _inputModule = _eventSystem.GetComponent<InputSystemUIInputModule>();
                if (_inputModule == null)
                {
                    _inputModule = _eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                return;
            }

            if (_eventSystem != null)
            {
                return;
            }

            GameObject eventSystemGo = new GameObject("RuntimeEventSystem");
            _eventSystem = eventSystemGo.AddComponent<EventSystem>();
            _inputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(eventSystemGo);
        }

        private void HandleHotkeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

             BattleWorldBridge battleBridge = BattleWorldBridge.Instance;
             bool battleActive = battleBridge != null && battleBridge.HasActiveEncounter;

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                if (battleActive)
                {
                    if (_activePanel == PanelType.System)
                    {
                        ClosePanels();
                    }
                    else
                    {
                        OpenPanel(PanelType.System);
                    }
                }
                else if (_activePanel == PanelType.None)
                {
                    OpenPanel(PanelType.System);
                }
                else
                {
                    ClosePanels();
                }
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_activePanel != PanelType.None || GetActiveMerchant() != null || battleActive)
                {
                    ClosePanels();
                }
                else
                {
                    OpenPanel(PanelType.System);
                }
                return;
            }

            if (battleActive)
            {
                if (keyboard.hKey.wasPressedThisFrame)
                {
                    OpenPanel(PanelType.Help);
                }

                return;
            }

            if (keyboard.iKey.wasPressedThisFrame) OpenPanel(PanelType.Inventory);
            else if (keyboard.cKey.wasPressedThisFrame) OpenPanel(PanelType.Character);
            else if (keyboard.jKey.wasPressedThisFrame) OpenPanel(PanelType.Quest);
            else if (keyboard.kKey.wasPressedThisFrame) OpenPanel(PanelType.Equipment);
            else if (keyboard.pKey.wasPressedThisFrame) OpenPanel(PanelType.System);
            else if (keyboard.hKey.wasPressedThisFrame) OpenPanel(PanelType.Help);
            else if (keyboard.f5Key.wasPressedThisFrame && SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame();
                Notify("Manual save requested.");
            }
            else if (keyboard.f9Key.wasPressedThisFrame && SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.LoadGame();
                Notify("Manual load requested.");
            }
        }

        private void SyncMerchantPanelState()
        {
            if (IsBattlePanelActive() || _activeInn != null)
            {
                return;
            }

            if (_activeMerchant != null && _activePanel == PanelType.None)
            {
                OpenPanel(PanelType.Shop);
                return;
            }

            if (_activeMerchant == null && _activePanel == PanelType.Shop)
            {
                ClosePanels();
            }
        }

        private void SyncBattlePanelState()
        {
            BattleWorldBridge bridge = BattleWorldBridge.Instance;
            bool battleActive = bridge != null && bridge.HasActiveEncounter;
            if (battleActive)
            {
                RuntimeUiInputState.SetUiFocused(true);
                PanelType desired = bridge.IsBattleEnded ? PanelType.BattleEnd : PanelType.Battle;
                if (!_battleWasActive)
                {
                    _activeMerchant = null;
                    _activeInn = null;
                    _activePanel = desired;
                    RebuildContent();
                    RefreshVisibility();
                }
                else if (_activePanel != desired && _activePanel != PanelType.System && _activePanel != PanelType.Help)
                {
                    _activePanel = desired;
                    RebuildContent();
                    RefreshVisibility();
                }

                _battleWasActive = true;
                return;
            }

            _battleWasActive = false;
            if (_activePanel == PanelType.Battle || _activePanel == PanelType.BattleEnd)
            {
                _activePanel = PanelType.None;
                RuntimeUiInputState.RestoreGameplayFocus();
                RefreshVisibility();
            }
        }

        public void ForceRebuild()
        {
            if (_activePanel == PanelType.None)
            {
                RefreshVisibility();
                return;
            }

            GameObject currentSelected = null;
            if (_eventSystem != null)
            {
                currentSelected = _eventSystem.currentSelectedGameObject;
            }
            string selectedName = currentSelected != null ? currentSelected.name : null;

            _lastRefreshAt = UnityEngine.Time.unscaledTime;
            RebuildContent();

            if (!string.IsNullOrEmpty(selectedName) && _eventSystem != null && _contentColumn != null)
            {
                Transform newSelected = _contentColumn.Find(selectedName);
                if (newSelected != null)
                {
                    _eventSystem.SetSelectedGameObject(newSelected.gameObject);
                }
                else
                {
                    // If the exact object is gone, try to focus the first child
                    if (_contentColumn.childCount > 0)
                    {
                        _eventSystem.SetSelectedGameObject(_contentColumn.GetChild(0).gameObject);
                    }
                }
            }
        }

        private void OpenPanel(PanelType panel)
        {
            if (panel == PanelType.Inventory ||
                panel == PanelType.Character ||
                panel == PanelType.Quest ||
                panel == PanelType.Equipment ||
                panel == PanelType.Shop ||
                panel == PanelType.Sleep)
            {
                BattleWorldBridge bridge = BattleWorldBridge.Instance;
                if (bridge != null && bridge.HasActiveEncounter)
                {
                    return;
                }
            }

            if (panel == PanelType.Shop)
            {
                _activeInn = null;
            }
            else if (panel == PanelType.Sleep)
            {
                _activeMerchant = null;
            }
            else if (panel != PanelType.None)
            {
                _activeInn = null;
            }

            _activePanel = panel;
            RuntimeUiInputState.SetUiFocused(true);
            RebuildContent();
            RefreshVisibility();
        }

        private void ClosePanels()
        {
            BattleWorldBridge bridge = BattleWorldBridge.Instance;
            if (bridge != null && bridge.HasActiveEncounter)
            {
                _activePanel = bridge.IsBattleEnded ? PanelType.BattleEnd : PanelType.Battle;
                RuntimeUiInputState.SetUiFocused(true);
                RefreshVisibility();
                return;
            }

            if (_activePanel != PanelType.Shop && _activeMerchant != null)
            {
                _activeInn = null;
                _activePanel = PanelType.Shop;
                RuntimeUiInputState.SetUiFocused(true);
                RebuildContent();
                RefreshVisibility();
                return;
            }

            _activeInn = null;
            _activeMerchant = null;
            _activePanel = PanelType.None;
            RuntimeUiInputState.RestoreGameplayFocus();
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (_menuPanel == null)
            {
                return;
            }

            bool visible = _activePanel != PanelType.None;
            _menuPanel.gameObject.SetActive(visible);
            Transform dimmer = _root.Find("RuntimeMenuDimmer");
            if (dimmer != null)
            {
                dimmer.gameObject.SetActive(visible);
            }

            if (_buttonBar != null)
            {
                _buttonBar.gameObject.SetActive(!IsBattlePanelActive());
            }

            RefreshHintText();
        }

        private void RebuildContent()
        {
            if (_contentRoot == null)
            {
                return;
            }

            ClearChildren(_contentColumn);
            _titleText.text = GetPanelTitle(_activePanel);
            _contentScroll.normalizedPosition = Vector2.one;
            switch (_activePanel)
            {
                case PanelType.Inventory:
                    BuildInventoryPanel();
                    break;
                case PanelType.Character:
                    BuildCharacterPanel();
                    break;
                case PanelType.Quest:
                    BuildQuestPanel();
                    break;
                case PanelType.Equipment:
                    BuildEquipmentPanel();
                    break;
                case PanelType.System:
                    BuildSystemPanel();
                    break;
                case PanelType.Shop:
                    BuildShopPanel();
                    break;
                case PanelType.Help:
                    BuildHelpPanel();
                    break;
                case PanelType.Sleep:
                    BuildSleepPanel();
                    break;
                case PanelType.Battle:
                    BuildBattlePanel();
                    break;
                case PanelType.BattleEnd:
                    BuildBattleEndPanel();
                    break;
            }
        }

        private void BuildInventoryPanel()
        {
            Phase1ContentCatalog catalog = GetCatalog();
            if (catalog == null || InventoryService.Instance == null)
            {
                AddInfoLine("Inventory unavailable.");
                return;
            }

            if (PartyService.Instance != null)
            {
                AddMemberSelector();
                CharacterStatSnapshot snapshot = GetSelectedSnapshot();
                if (snapshot != null)
                {
                    AddInfoLine($"{snapshot.DisplayName} | HP {PartyService.Instance.GetCurrentHP(snapshot.CharacterId)}/{snapshot.Stats.MaxHP} | MP {PartyService.Instance.GetCurrentMP(snapshot.CharacterId)}/{snapshot.Stats.MaxMP}");
                }
            }

            AddHeader("Consumables");
            bool hasAny = false;
            for (int i = 0; i < catalog.Items.Count; i++)
            {
                ItemDefinition item = catalog.Items[i];
                if (item == null)
                {
                    continue;
                }

                int count = InventoryService.Instance.GetItemCount(item.ItemId);
                if (count <= 0)
                {
                    continue;
                }

                hasAny = true;
                RectTransform row = CreateRow(78f);
                CreateText("ItemLabel", row, $"{item.DisplayName} x{count}\n{item.Description}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 56f);
                CreateActionButton(row, T("Use", "Dùng"), () => UseItem(item), 94f);
                CreateActionButton(row, T("Sell", "Bán"), () => SellItem(item), 94f);
            }

            if (!hasAny)
            {
                AddInfoLine(T("No consumables in the bag yet.", "Túi đồ chưa có vật phẩm dùng được."));
            }
        }

        private void BuildEquipmentPanel()
        {
            Phase1ContentCatalog catalog = GetCatalog();
            if (catalog == null || InventoryService.Instance == null || PartyService.Instance == null)
            {
                AddInfoLine("Equipment unavailable.");
                return;
            }

            AddMemberSelector();
            CharacterStatSnapshot selectedSnapshot = GetSelectedSnapshot();
            if (selectedSnapshot != null)
            {
                string weaponName = selectedSnapshot.EquippedWeapon != null ? selectedSnapshot.EquippedWeapon.DisplayName : "No Weapon";
            AddInfoLine($"{selectedSnapshot.DisplayName} | {T("Current weapon", "Vũ khí hiện tại")}: {weaponName}");
            }

            bool hasAny = false;
            for (int i = 0; i < catalog.Equipment.Count; i++)
            {
                EquipmentDefinition equipment = catalog.Equipment[i];
                if (equipment == null)
                {
                    continue;
                }

                int count = InventoryService.Instance.GetEquipmentCount(equipment.EquipmentId);
                if (count <= 0)
                {
                    continue;
                }

                hasAny = true;
                RectTransform row = CreateRow(78f);
                CreateText("EquipmentLabel", row, $"{equipment.DisplayName} x{count}\nATK+{equipment.StatBonus.Attack} MAG+{equipment.StatBonus.Magic} DEF+{equipment.StatBonus.Defense}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 56f);
                CreateActionButton(row, T("Equip", "Trang bị"), () => EquipSelected(equipment), 94f);
                CreateActionButton(row, T("Sell", "Bán"), () => SellEquipment(equipment), 94f);
            }

            if (selectedSnapshot != null && selectedSnapshot.EquippedWeapon != null)
            {
                RectTransform row = CreateRow(62f);
                CreateText("UnequipLabel", row, $"{T("Unequip", "Gỡ")} {selectedSnapshot.EquippedWeapon.DisplayName}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, false);
                CreateActionButton(row, T("Unequip", "Gỡ"), UnequipSelected, 110f);
            }

            if (!hasAny)
            {
                AddInfoLine(T("No spare equipment in inventory.", "Không có trang bị dự phòng trong túi."));
            }
        }

        private void BuildCharacterPanel()
        {
            AddMemberSelector();
            CharacterStatSnapshot snapshot = GetSelectedSnapshot();
            if (snapshot == null || PartyService.Instance == null)
            {
                AddInfoLine(T("No active party member selected.", "Chưa chọn nhân vật hoạt động."));
                return;
            }

            AddInfoLine($"{snapshot.DisplayName} Lv{snapshot.Level}");
            if (ProgressionService.Instance != null)
            {
                AddInfoLine($"EXP {ProgressionService.Instance.GetCurrentExp(snapshot.CharacterId)} | {T("Unlocked skills", "Ky nang da mo")}: {snapshot.Skills.Count}");
            }
            AddInfoLine($"HP {PartyService.Instance.GetCurrentHP(snapshot.CharacterId)}/{snapshot.Stats.MaxHP} | MP {PartyService.Instance.GetCurrentMP(snapshot.CharacterId)}/{snapshot.Stats.MaxMP}");
            AddInfoLine($"ATK {snapshot.Stats.Attack} | MAG {snapshot.Stats.Magic} | DEF {snapshot.Stats.Defense} | RES {snapshot.Stats.Resistance} | SPD {snapshot.Stats.Speed}");
            AddInfoLine($"{T("Weapon", "Vũ khí")}: {(snapshot.EquippedWeapon != null ? snapshot.EquippedWeapon.DisplayName : T("No Weapon", "Chưa có"))}");
            AddHeader(T("Known Skills", "Kỹ năng"));
            if (snapshot.Skills.Count == 0)
            {
                AddInfoLine(T("No learned skills yet.", "Chưa học kỹ năng nào."));
            }
            else
            {
                for (int i = 0; i < snapshot.Skills.Count; i++)
                {
                    SkillDefinition skill = snapshot.Skills[i];
                    if (skill != null)
                    {
                        AddInfoLine($"- {skill.DisplayName} ({skill.ResourceCost} MP)");
                    }
                }
            }

            AddHeader(T("Party Formation", "Party formation"));
            List<string> recruited = PartyService.Instance.GetRecruitedMemberIds();
            if (recruited.Count == 0)
            {
                AddInfoLine(T("No recruited members.", "No recruited members."));
                return;
            }

            for (int i = 0; i < recruited.Count; i++)
            {
                string memberId = recruited[i];
                CharacterStatSnapshot memberSnapshot = PartyService.Instance.GetMemberSnapshot(memberId);
                string displayName = memberSnapshot != null ? memberSnapshot.DisplayName : memberId;
                bool isActive = PartyService.Instance.GetActiveMemberIds().Contains(memberId);
                RectTransform row = CreateRow(62f);
                CreateText("PartyMemberLabel", row, $"{displayName} ({memberId}) {(isActive ? "[Active]" : "[Bench]")}", 15, TextAnchor.MiddleLeft, FontStyle.Normal, false);
                CreateActionButton(row, "Slot 1", () => SetMemberSlot(memberId, 0), 80f);
                CreateActionButton(row, "Slot 2", () => SetMemberSlot(memberId, 1), 80f);
                CreateActionButton(row, "Slot 3", () => SetMemberSlot(memberId, 2), 80f);
                CreateActionButton(row, T("Bench", "Bench"), () => BenchMember(memberId), 86f);
            }
        }

        private void BuildQuestPanel()
        {
            Phase1ContentCatalog catalog = GetCatalog();
            if (catalog == null || QuestService.Instance == null)
            {
                AddInfoLine(T("Quest log unavailable.", "Nhật ký nhiệm vụ chưa sẵn sàng."));
                return;
            }

            AddRouteGuidanceSection();
            for (int i = 0; i < catalog.Quests.Count; i++)
            {
                QuestDefinition quest = catalog.Quests[i];
                if (quest == null)
                {
                    continue;
                }

                AddHeader(quest.Title);
                int completed = QuestService.Instance.GetCompletedObjectiveCount(quest.QuestId);
                int total = QuestService.Instance.GetObjectiveCount(quest.QuestId);
                AddInfoLine($"{QuestService.Instance.GetQuestStatus(quest.QuestId)} ({completed}/{Mathf.Max(total, 0)})");
                string objective = QuestService.Instance.GetNextIncompleteObjectiveDescription(quest.QuestId);
                if (!string.IsNullOrWhiteSpace(objective))
                {
                    AddInfoLine(objective);
                }
            }
        }

        private void AddRouteGuidanceSection()
        {
            AddHeader(T("Next Steps", "Buoc tiep theo"));
            List<string> lines = GetRouteGuidanceLines();
            for (int i = 0; i < lines.Count; i++)
            {
                AddInfoLine(lines[i]);
            }
        }

        private List<string> GetRouteGuidanceLines()
        {
            var lines = new List<string>();
            QuestStatus routeStatus = QuestService.Instance != null ? QuestService.Instance.GetQuestStatus(TideRouteQuestId) : QuestStatus.NotStarted;
            bool dockSuppliesSecured = ZoneStateService.Instance != null && ZoneStateService.Instance.GetBoolFact(DockSuppliesZoneId, DockSuppliesFactId);
            bool tideRouteSecured = ZoneStateService.Instance != null && ZoneStateService.Instance.GetBoolFact(TideRouteZoneId, TideRouteSecuredFactId);

            if (!dockSuppliesSecured)
            {
                lines.Add(T("Start in Aster Harbor: check the dock quarter, use the general store, and read the board near the harbor lane.", "Bat dau o Aster Harbor: ghe khu cau cang, vao cua hang tong hop, va doc bang huong dan gan lane ben cang."));
            }
            else if (routeStatus == QuestStatus.NotStarted)
            {
                lines.Add(T("Travel west to Gullwatch and speak with Mira at the beacon frame to open the coastal route task.", "Di ve phia tay den Gullwatch va noi chuyen voi Mira o khung hai dang de mo nhiem vu tuyen duong ven bien."));
            }
            else if (routeStatus == QuestStatus.Active)
            {
                lines.Add(T("Follow the spray markers from Gullwatch into Tide Caverns, clear the patrol, then push to the matriarch.", "Di theo cac coc moc phun song tu Gullwatch vao Tide Caverns, don toan to tuan tra, sau do tien den matriarch."));
            }
            else if (routeStatus == QuestStatus.ReadyToTurnIn)
            {
                lines.Add(T("Return to Mira in Gullwatch to turn in Secure the Tide Route and light the beacon.", "Quay lai Mira o Gullwatch de nop nhiem vu Secure the Tide Route va thap sang hai dang."));
            }
            else if (routeStatus == QuestStatus.Completed || tideRouteSecured)
            {
                lines.Add(T("The Tide Route is secure. Use the inn, shop, and travel lanes to prepare for the next outing.", "Tuyen Tide Route da an toan. Dung nha tro, cua hang, va cac duong di de chuan bi cho chuyen di tiep theo."));
            }
            else
            {
                lines.Add(T("Open the quest log and follow the highest unfinished objective.", "Mo quest log va lam objective chua hoan thanh gan nhat."));
            }

            lines.Add(T("Use the inn to restore the party and advance to morning before a dungeon push.", "Dung nha tro de hoi day party va sang buoi sang truoc khi vao dungeon."));
            lines.Add(T("Inventory handles consumables, Equipment handles gear, and Character shows stats plus unlocked skill summary.", "Inventory dung cho do tieu hao, Equipment dung cho trang bi, va Character hien stat cung tong ket ky nang da mo."));
            return lines;
        }

        private void BuildSystemPanel()
        {
            bool battleActive = BattleWorldBridge.Instance != null && BattleWorldBridge.Instance.HasActiveEncounter;
            AddHeader(T("Session", "Phiên chơi"));
            AddInfoLine($"{T("Mode", "Chế độ")}: {RuntimeUiInputState.CurrentMode}");
            AddInfoLine($"{T("Scene", "Màn")}: {SceneManager.GetActiveScene().name}");

            RectTransform languageRow = CreateRow(62f);
            CreateText("LanguageLabel", languageRow, T("Language", "Ngôn ngữ"), 16, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            CreateActionButton(languageRow, "English", SetEnglish, 110f);
            CreateActionButton(languageRow, "Tieng Viet", SetVietnamese, 110f);

            RectTransform graphicsRow = CreateRow(62f);
            CreateText("GraphicsLabel", graphicsRow, T("Graphics Preset", "Mức đồ họa"), 16, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            CreateActionButton(graphicsRow, "Low", () => SetQualityPreset(0), 86f);
            CreateActionButton(graphicsRow, "Medium", () => SetQualityPreset(Mathf.Min(1, QualitySettings.names.Length - 1)), 96f);
            CreateActionButton(graphicsRow, "High", () => SetQualityPreset(Mathf.Min(2, QualitySettings.names.Length - 1)), 86f);
            CreateActionButton(graphicsRow, T("Fullscreen", "Toàn màn hình"), ToggleFullscreen, 118f);

            RectTransform resolutionRow = CreateRow(62f);
            CreateText("ResolutionLabel", resolutionRow, T("Resolution", "Độ phân giải"), 16, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            CreateActionButton(resolutionRow, "1280x720", () => ApplyResolution(1280, 720), 102f);
            CreateActionButton(resolutionRow, "1600x900", () => ApplyResolution(1600, 900), 102f);
            CreateActionButton(resolutionRow, "1920x1080", () => ApplyResolution(1920, 1080), 110f);

            AddHeader(T("Audio", "Âm thanh"));
            RectTransform audioMasterRow = CreateRow(40f);
            CreateText("AudioMasterLabel", audioMasterRow, T("Master Volume", "Âm lượng tổng"), 16, TextAnchor.MiddleLeft, FontStyle.Normal, false, 32f);
            CreateSlider(audioMasterRow, 400f, 0f, 1f, GetVolume(MasterVolumeKey), v => SetVolumeDirect(MasterVolumeKey, v, "Master"));

            RectTransform audioMusicRow = CreateRow(40f);
            CreateText("AudioMusicLabel", audioMusicRow, T("Music Volume", "Nhạc nền"), 16, TextAnchor.MiddleLeft, FontStyle.Normal, false, 32f);
            CreateSlider(audioMusicRow, 400f, 0f, 1f, GetVolume(MusicVolumeKey), v => SetVolumeDirect(MusicVolumeKey, v, "Music"));

            RectTransform audioSfxRow = CreateRow(40f);
            CreateText("AudioSfxLabel", audioSfxRow, T("SFX Volume", "Hiệu ứng"), 16, TextAnchor.MiddleLeft, FontStyle.Normal, false, 32f);
            CreateSlider(audioSfxRow, 400f, 0f, 1f, GetVolume(SfxVolumeKey), v => SetVolumeDirect(SfxVolumeKey, v, "SFX"));

            AddHeader(T("Controls", "Điều khiển"));
            AddInfoLine(T("Tab: Toggle UI mode / cursor", "Tab: Bật/tắt chế độ UI / chuột") + "  |  E: " + T("Interact", "Tương tác"));
            AddInfoLine("I: " + T("Inventory", "Túi đồ") + "  |  K: " + T("Equipment", "Trang bị") + "  |  C: " + T("Character", "Nhân vật"));
            AddInfoLine("J: " + T("Quest Log", "Nhật ký nhiệm vụ") + "  |  P or Esc: " + T("System / Close", "Hệ thống / Đóng"));
            AddInfoLine(T("F5: Save", "F5: Lưu") + "  |  " + T("F9: Load", "F9: Tải"));

            RectTransform actionRow = CreateRow(62f);
            if (battleActive)
            {
                AddInfoLine(T("Save/load is disabled while a battle is active.", "Khong cho phep save/load khi dang trong tran dau."));
            }
            if (!battleActive)
            {
                CreateActionButton(actionRow, T("Save Game", "Lưu game"), SaveGame, 140f);
                CreateActionButton(actionRow, T("Load Game", "Tải game"), LoadGame, 140f);
            }
            CreateActionButton(actionRow, T("Return", "Quay lại"), ClosePanels, 140f);
        }

        private void SetVolumeDirect(string key, float volume, string labelName)
        {
            PlayerPrefs.SetFloat(key, volume);
            PlayerPrefs.Save();
            if (key == MasterVolumeKey)
            {
                AudioListener.volume = volume;
            }
        }

        private void BuildShopPanel()
        {
            ShopDefinition shop = _activeMerchant != null ? _activeMerchant.ShopDefinition : null;
            if (shop == null || EconomyService.Instance == null)
            {
                AddInfoLine(T("Shop is unavailable.", "Cửa hàng chưa khả dụng."));
                return;
            }

            AddInfoLine($"{T("Currency", "Tiền")}: {EconomyService.Instance.Currency}");
            AddHeader(T("Buy Stock", "Hang dang ban"));
            for (int i = 0; i < shop.Entries.Count; i++)
            {
                ShopEntryDefinition entry = shop.Entries[i];
                if (entry == null)
                {
                    continue;
                }

                string label = entry.Item != null ? entry.Item.DisplayName : entry.Equipment != null ? entry.Equipment.DisplayName : "Entry";
                int price = entry.PriceOverride >= 0 ? entry.PriceOverride : entry.Item != null ? entry.Item.BuyPrice : entry.Equipment != null ? entry.Equipment.BuyPrice : 0;
                int stock = EconomyService.Instance.GetEntryStock(shop, entry);
                RectTransform row = CreateRow(78f);
                string owned = entry.Item != null && InventoryService.Instance != null
                    ? InventoryService.Instance.GetItemCount(entry.Item.ItemId).ToString()
                    : entry.Equipment != null && InventoryService.Instance != null
                        ? InventoryService.Instance.GetEquipmentCount(entry.Equipment.EquipmentId).ToString()
                        : "0";
                CreateText("ShopLabel", row, $"{label}\n{T("Price", "Giá")} {price} | {T("Stock", "Tồn")} {(stock == int.MaxValue ? "Inf" : stock.ToString())} | {T("Owned", "Đang có")} {owned}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 56f);
                CreateActionButton(row, T("Buy", "Mua"), () => BuyEntry(shop, entry, label), 94f);
            }

            Phase1ContentCatalog catalog = GetCatalog();
            if (catalog != null && InventoryService.Instance != null)
            {
                bool showedSellHeader = false;
                for (int i = 0; i < catalog.Items.Count; i++)
                {
                    ItemDefinition item = catalog.Items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    int count = InventoryService.Instance.GetItemCount(item.ItemId);
                    if (count <= 0)
                    {
                        continue;
                    }

                    if (!showedSellHeader)
                    {
                        AddHeader(T("Sell From Bag", "Ban tu tui do"));
                        showedSellHeader = true;
                    }

                    RectTransform sellRow = CreateRow(70f);
                    CreateText("SellItemLabel", sellRow, $"{item.DisplayName} x{count}\n{T("Sell", "Ban")} {item.SellPrice}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 52f);
                    CreateActionButton(sellRow, T("Sell", "Ban"), () => SellItem(item), 94f);
                }

                for (int i = 0; i < catalog.Equipment.Count; i++)
                {
                    EquipmentDefinition equipment = catalog.Equipment[i];
                    if (equipment == null)
                    {
                        continue;
                    }

                    int count = InventoryService.Instance.GetEquipmentCount(equipment.EquipmentId);
                    if (count <= 0)
                    {
                        continue;
                    }

                    if (!showedSellHeader)
                    {
                        AddHeader(T("Sell From Bag", "Ban tu tui do"));
                        showedSellHeader = true;
                    }

                    RectTransform sellRow = CreateRow(70f);
                    CreateText("SellEquipmentLabel", sellRow, $"{equipment.DisplayName} x{count}\n{T("Sell", "Ban")} {equipment.SellPrice}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 52f);
                    CreateActionButton(sellRow, T("Sell", "Ban"), () => SellEquipment(equipment), 94f);
                }

                if (!showedSellHeader)
                {
                    AddInfoLine(T("Nothing in the bag can be sold yet.", "Chua co vat pham nao de ban."));
                }
            }

            RectTransform closeRow = CreateRow(62f);
            CreateActionButton(closeRow, T("Close Shop", "Đóng cửa hàng"), ClosePanels, 180f);
        }

/*
            Phase1ContentCatalog catalog = GetCatalog();
            if (catalog != null && InventoryService.Instance != null)
            {
                bool hasSellableItems = false;
                for (int i = 0; i < catalog.Items.Count; i++)
                {
                    ItemDefinition item = catalog.Items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    int count = InventoryService.Instance.GetItemCount(item.ItemId);
                    if (count <= 0)
                    {
                        continue;
                    }

                    if (!hasSellableItems)
                    {
                        AddHeader(T("Sell Consumables", "Ban vat pham"));
                        hasSellableItems = true;
                    }

                    RectTransform sellRow = CreateRow(70f);
                    CreateText("SellItemLabel", sellRow, $"{item.DisplayName} x{count}\n{T("Sell", "Ban")} {item.SellPrice}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 52f);
                    CreateActionButton(sellRow, T("Sell", "Ban"), () => SellItem(item), 94f);
                }

                bool hasSellableEquipment = false;
                for (int i = 0; i < catalog.Equipment.Count; i++)
                {
                    EquipmentDefinition equipment = catalog.Equipment[i];
                    if (equipment == null)
                    {
                        continue;
                    }

                    int count = InventoryService.Instance.GetEquipmentCount(equipment.EquipmentId);
                    if (count <= 0)
                    {
                        continue;
                    }

                    if (!hasSellableEquipment)
                    {
                        AddHeader(T("Sell Equipment", "Ban trang bi"));
                        hasSellableEquipment = true;
                    }

                    RectTransform sellRow = CreateRow(70f);
                    CreateText("SellEquipmentLabel", sellRow, $"{equipment.DisplayName} x{count}\n{T("Sell", "Ban")} {equipment.SellPrice}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 52f);
                    CreateActionButton(sellRow, T("Sell", "Ban"), () => SellEquipment(equipment), 94f);
                }

                if (!hasSellableItems && !hasSellableEquipment)
                {
                    AddInfoLine(T("Nothing in the bag can be sold yet.", "Chua co vat pham nao de ban."));
                }
            }

            RectTransform closeRow = CreateRow(62f);
            CreateActionButton(closeRow, T("Close Shop", "ÄÃ³ng cá»­a hÃ ng"), ClosePanels, 180f);
        }

*/
        private void BuildSleepPanel()
        {
            if (_activeInn == null)
            {
                AddInfoLine(T("Rest is unavailable here.", "Khong the nghi tai day."));
                return;
            }

            AddHeader(T("Rest Until Morning", "Nghi den sang"));
            AddInfoLine(string.Format(
                T("Wake time: {0:00}:{1:00}", "Gio thuc day: {0:00}:{1:00}"),
                _activeInn.WakeHour,
                _activeInn.WakeMinute));
            AddInfoLine(T(
                "Sleeping restores the party, advances the day, and refreshes shop stock and schedules.",
                "Ngu hoi phuc doi hinh, sang ngay moi, va lam moi cua hang cung lich sinh hoat."));

            RectTransform row = CreateRow(62f);
            CreateActionButton(row, T("Sleep", "Ngu"), ConfirmSleep, 140f);
            CreateActionButton(row, T("Cancel", "Huy"), ClosePanels, 140f);
        }

        private void BuildHelpPanel()
        {
            AddRouteGuidanceSection();
            AddHeader(T("Tips", "Gợi ý"));
            AddInfoLine(T("Use UI mode when trading, equipping, saving, or checking quests.", "Hãy dùng UI mode khi mua bán, trang bị, lưu hoặc xem nhiệm vụ."));
            AddInfoLine(T("If the weather changes, watch NPC shelter spots and the district readout.", "Khi thời tiết đổi, hãy nhìn NPC trú mưa và biến đổi ở từng khu."));
            AddInfoLine(T("Travel anchors should drop you on safe ground; if not, the spawn fallback will correct it.", "Travel anchor sẽ cố đặt bạn xuống mặt đất an toàn; nếu không, spawn fallback sẽ tự sửa."));
        }

        private void BuildBattlePanel()
        {
            BattleWorldBridge bridge = BattleWorldBridge.Instance;
            if (bridge == null || !bridge.HasActiveEncounter)
            {
                AddInfoLine(T("Battle state unavailable.", "KhÃ´ng cÃ³ tráº¡ng thÃ¡i chiáº¿n Ä‘áº¥u."));
                return;
            }

            AddInfoLine(bridge.EncounterTitle);
            AddInfoLine(bridge.CurrentActorLabel);

            AddHeader(T("Turn Order", "Thá»© tá»± lÆ°á»£t"));
            foreach (string line in bridge.GetTurnOrderLabels())
            {
                AddInfoLine(line);
            }

            AddHeader(T("Party", "Äá»™i hÃ¬nh"));
            foreach (string line in bridge.GetPartyStatusLines())
            {
                AddInfoLine(line);
            }

            AddHeader(T("Enemies", "Káº» Ä‘á»‹ch"));
            foreach (string line in bridge.GetEnemyStatusLines())
            {
                AddInfoLine(line);
            }

            AddHeader(T("Targets", "Má»¥c tiÃªu"));
            RectTransform enemyTargetRow = CreateRow(54f);
            CreateText("EnemyTargetLabel", enemyTargetRow, $"{T("Enemy", "Äá»‹ch")}: {bridge.GetEnemyTargetLabel()}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, false);
            CreateActionButton(enemyTargetRow, "<", () => { bridge.CycleEnemyTarget(-1); RebuildContent(); }, 44f);
            CreateActionButton(enemyTargetRow, ">", () => { bridge.CycleEnemyTarget(1); RebuildContent(); }, 44f);

            RectTransform allyTargetRow = CreateRow(54f);
            CreateText("AllyTargetLabel", allyTargetRow, $"{T("Ally", "Äá»“ng minh")}: {bridge.GetAllyTargetLabel()}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, false);
            CreateActionButton(allyTargetRow, "<", () => { bridge.CycleAllyTarget(-1); RebuildContent(); }, 44f);
            CreateActionButton(allyTargetRow, ">", () => { bridge.CycleAllyTarget(1); RebuildContent(); }, 44f);

            AddHeader(T("Actions", "HÃ nh Ä‘á»™ng"));
            IReadOnlyList<BattleWorldBridge.BattleActionView> actions = bridge.GetAvailableActions();
            if (actions.Count == 0)
            {
                AddInfoLine(T("Waiting for the next battle update...", "Dang cho cap nhat tran dau..."));
            }
            for (int i = 0; i < actions.Count; i++)
            {
                BattleWorldBridge.BattleActionView action = actions[i];
                RectTransform row = CreateRow(72f);
                CreateText("BattleActionLabel", row, $"{action.Label}\n{action.Detail}", 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 56f);
                Button button = CreateActionButton(row, action.CanUse ? T("Confirm", "XÃ¡c nháº­n") : T("Unavailable", "ChÆ°a dÃ¹ng Ä‘Æ°á»£c"), () =>
                {
                    bridge.ExecuteActionFromUi(action);
                    RebuildContent();
                }, 130f);
                button.interactable = action.CanUse;
            }

            AddHeader(T("Battle Feed", "Nháº­t kÃ½ giao tranh"));
            foreach (string line in bridge.GetCombatLogLines())
            {
                AddInfoLine(line);
            }
        }

        private void BuildBattleEndPanel()
        {
            BattleWorldBridge bridge = BattleWorldBridge.Instance;
            if (bridge == null)
            {
                AddInfoLine(T("Battle already closed.", "Tráº­n Ä‘áº¥u Ä‘Ã£ Ä‘Ã³ng."));
                return;
            }

            AddHeader(bridge.ResultTitle);
            AddInfoLine(bridge.RewardSummary);
            AddInfoLine($"{T("Returning to", "Quay vá»")}: {bridge.ReturnSceneName}");
            RectTransform row = CreateRow(62f);
            CreateActionButton(row, T("Return To World", "Quay vá» tháº¿ giá»›i"), bridge.ReturnToWorldFromUi, 220f);

            AddHeader(T("Battle Feed", "Nháº­t kÃ½ giao tranh"));
            foreach (string line in bridge.GetCombatLogLines())
            {
                AddInfoLine(line);
            }
        }

        private RectTransform CreateRow(float preferredHeight = 62f)
        {
            RectTransform row = CreateContainer("Row", _contentColumn, preferredHeight);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            return row;
        }

        private void AddHeader(string text)
        {
            RectTransform row = CreateContainer("Header", _contentColumn, 28f);
            CreateText("HeaderText", row, text, 20, TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        private void AddInfoLine(string text)
        {
            RectTransform row = CreateContainer("Info", _contentColumn, 36f);
            CreateText("InfoText", row, text, 16, TextAnchor.MiddleLeft, FontStyle.Normal, true, 34f);
        }

        private void AddMemberSelector()
        {
            if (PartyService.Instance == null)
            {
                return;
            }

            RectTransform row = CreateContainer("MemberSelector", _contentColumn, 42f);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;
            CreateText("SelectedLabel", row, $"{T("Selected", "Selected")}: {_selectedMemberId}", 16, TextAnchor.MiddleLeft, FontStyle.Bold);

            List<string> members = PartyService.Instance.GetActiveMemberIds();
            for (int i = 0; i < members.Count; i++)
            {
                string memberId = members[i];
                CreateActionButton(row, $"{i + 1}:{memberId}", () =>
                {
                    _selectedMemberId = memberId;
                    Notify($"{T("Selected", "Selected")} {memberId}.");
                    RebuildContent();
                }, 104f);
            }
        }

        private CharacterStatSnapshot GetSelectedSnapshot()
        {
            EnsureSelectedMember();
            return string.IsNullOrWhiteSpace(_selectedMemberId) || PartyService.Instance == null
                ? null
                : PartyService.Instance.GetMemberSnapshot(_selectedMemberId);
        }

        private void EnsureSelectedMember()
        {
            if (PartyService.Instance == null)
            {
                _selectedMemberId = null;
                return;
            }

            List<string> members = PartyService.Instance.GetActiveMemberIds();
            if (members.Count == 0)
            {
                _selectedMemberId = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedMemberId) || !members.Contains(_selectedMemberId))
            {
                _selectedMemberId = members[0];
            }
        }

        private void UseItem(ItemDefinition item)
        {
            if (item == null || PartyService.Instance == null)
            {
                return;
            }

            EnsureSelectedMember();
            if (PartyService.Instance.TryUseConsumable(_selectedMemberId, item))
            {
                Notify($"{T("Used", "Đã dùng")} {item.DisplayName} {T("for", "cho")} {_selectedMemberId}.");
                RebuildContent();
            }
            else
            {
                Notify($"{T("Could not use", "Không thể dùng")} {item.DisplayName}.");
            }
        }

        private void SellItem(ItemDefinition item)
        {
            if (item == null || EconomyService.Instance == null)
            {
                return;
            }

            if (EconomyService.Instance.SellItem(item))
            {
                Notify($"{T("Sold", "Đã bán")} {item.DisplayName}.");
                RebuildContent();
            }
            else
            {
                Notify($"{T("Could not sell", "Không thể bán")} {item.DisplayName}.");
            }
        }

        private void EquipSelected(EquipmentDefinition equipment)
        {
            if (equipment == null || PartyService.Instance == null)
            {
                return;
            }

            EnsureSelectedMember();
            if (PartyService.Instance.EquipWeapon(_selectedMemberId, equipment))
            {
                Notify($"{_selectedMemberId} {T("equipped", "đã trang bị")} {equipment.DisplayName}.");
                RebuildContent();
            }
            else
            {
                Notify($"{T("Could not equip", "Không thể trang bị")} {equipment.DisplayName}.");
            }
        }

        private void SetMemberSlot(string memberId, int slot)
        {
            if (PartyService.Instance == null || string.IsNullOrWhiteSpace(memberId))
            {
                return;
            }

            if (PartyService.Instance.SetMemberActiveSlot(memberId, slot))
            {
                _selectedMemberId = memberId;
                Notify($"{memberId} assigned to slot {slot + 1}.");
                RebuildContent();
            }
            else
            {
                Notify($"{memberId} could not be assigned to slot {slot + 1}.");
            }
        }

        private void BenchMember(string memberId)
        {
            if (PartyService.Instance == null || string.IsNullOrWhiteSpace(memberId))
            {
                return;
            }

            if (PartyService.Instance.BenchMember(memberId))
            {
                Notify($"{memberId} moved to reserve.");
                RebuildContent();
            }
            else
            {
                Notify($"{memberId} cannot be benched (keep at least one active member).");
            }
        }

        private void UnequipSelected()
        {
            if (PartyService.Instance == null)
            {
                return;
            }

            EnsureSelectedMember();
            if (PartyService.Instance.UnequipWeapon(_selectedMemberId))
            {
                Notify($"{_selectedMemberId} {T("unequipped their weapon", "đã gỡ vũ khí")}.");
                RebuildContent();
            }
            else
            {
                Notify(T("Nothing to unequip.", "Không có gì để gỡ."));
            }
        }

        private void SellEquipment(EquipmentDefinition equipment)
        {
            if (equipment == null || EconomyService.Instance == null)
            {
                return;
            }

            if (EconomyService.Instance.SellEquipment(equipment))
            {
                Notify($"{T("Sold", "Đã bán")} {equipment.DisplayName}.");
                RebuildContent();
            }
            else
            {
                Notify($"{T("Could not sell", "Không thể bán")} {equipment.DisplayName}.");
            }
        }

        private void BuyEntry(ShopDefinition shop, ShopEntryDefinition entry, string label)
        {
            if (EconomyService.Instance == null)
            {
                return;
            }

            if (EconomyService.Instance.BuyItem(shop, entry))
            {
                Notify($"{T("Bought", "Đã mua")} {label}.");
                RebuildContent();
            }
            else
            {
                Notify($"{T("Could not buy", "Không thể mua")} {label}.");
            }
        }

        private void SaveGame()
        {
            if (SaveLoadManager.Instance == null)
            {
                return;
            }

            _activeMerchant = null;
            _activeInn = null;
            SaveLoadManager.Instance.SaveGame();
            Notify(T("Game saved.", "Đã lưu game."));
        }

        private void LoadGame()
        {
            if (SaveLoadManager.Instance == null)
            {
                return;
            }

            PrepareForSceneTransition(T("Loading game...", "Đang tải game..."));
            SaveLoadManager.Instance.LoadGame();
            Notify(T("Game loaded.", "Đã tải game."));
        }

        private void Notify(string message)
        {
            if (Phase1RuntimeHUD.Instance != null)
            {
                Phase1RuntimeHUD.Instance.ShowMessage(message);
            }
        }

        private void ConfirmSleep()
        {
            InnAnchor inn = _activeInn;
            _activeInn = null;
            _activeMerchant = null;
            _activePanel = PanelType.None;
            RuntimeUiInputState.RestoreGameplayFocus();
            RefreshVisibility();

            if (inn != null)
            {
                inn.SleepNow();
            }
        }

        private void HandleRuntimeReset()
        {
            _activeMerchant = null;
            _activeInn = null;
            _battleWasActive = false;
            BattleWorldBridge bridge = BattleWorldBridge.Instance;
            if (bridge != null && bridge.HasActiveEncounter)
            {
                _activePanel = bridge.IsBattleEnded ? PanelType.BattleEnd : PanelType.Battle;
                RuntimeUiInputState.SetUiFocused(true);
            }
            else
            {
                _activePanel = PanelType.None;
                RuntimeUiInputState.RestoreGameplayFocus();
            }

            if (_activePanel != PanelType.None)
            {
                RebuildContent();
            }

            RefreshVisibility();
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            HandleRuntimeReset();
        }

        private string GetPanelTitle(PanelType panel)
        {
            if (panel == PanelType.Sleep)
            {
                return T("Rest", "Nghi ngo");
            }

            switch (panel)
            {
                case PanelType.Inventory:
                    return T("Inventory", "Túi đồ");
                case PanelType.Character:
                    return T("Character", "Nhân vật");
                case PanelType.Quest:
                    return T("Quest Log", "Nhật ký nhiệm vụ");
                case PanelType.Equipment:
                    return T("Equipment", "Trang bị");
                case PanelType.System:
                    return T("System", "Hệ thống");
                case PanelType.Shop:
                    return _activeMerchant != null && _activeMerchant.ShopDefinition != null ? _activeMerchant.ShopDefinition.DisplayName : "Shop";
                case PanelType.Help:
                    return T("Help", "Trợ giúp");
                case PanelType.Battle:
                    return T("Battle", "Chiến đấu");
                case PanelType.BattleEnd:
                    return T("Battle Summary", "Tổng kết trận đấu");
                default:
                    return T("Runtime Menu", "Menu Runtime");
            }
        }

        private MerchantAnchor GetActiveMerchant()
        {
            return _activeMerchant;
        }

        private Phase1ContentCatalog GetCatalog()
        {
            return Phase1RuntimeHUD.Instance != null ? Phase1RuntimeHUD.Instance.ContentCatalog : null;
        }

        private void AddTabButton(string label, PanelType panel)
        {
            CreateActionButton(_buttonBar, label, () => OpenPanel(panel), 150f);
        }

        private Button CreateActionButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action, float width)
        {
            GameObject buttonGo = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.transform.SetParent(parent, false);
            LayoutElement layout = buttonGo.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 30f;
            Image image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.23f, 0.33f, 0.44f, 0.96f);
            Button button = buttonGo.GetComponent<Button>();
            button.onClick.AddListener(action);
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.33f, 0.46f, 0.61f, 1f);
            colors.pressedColor = new Color(0.17f, 0.26f, 0.35f, 1f);
            button.colors = colors;

            Text text = CreateText("Label", buttonGo.GetComponent<RectTransform>(), label, 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.color = Color.white;
            return button;
        }

        private RectTransform CreateContainer(string name, RectTransform parent, float preferredHeight)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = 1f;
            return rect;
        }

        private RectTransform CreateStretchPanel(string name, RectTransform parent, Color color, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Color color, Vector2 anchor, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = sizeDelta;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private Image CreateImage(string name, RectTransform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, RectTransform parent, string value, int fontSize, TextAnchor alignment, FontStyle fontStyle, bool wrap = false, float preferredHeight = 24f)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            text.fontStyle = fontStyle;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = wrap ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.preferredHeight = wrap ? preferredHeight : 24f;
            return text;
        }

        private void SetEnglish()
        {
            PlayerPrefs.SetInt(LanguageKey, 0);
            PlayerPrefs.Save();
            RefreshHintText();
            RebuildContent();
        }

        private void SetVietnamese()
        {
            PlayerPrefs.SetInt(LanguageKey, 1);
            PlayerPrefs.Save();
            RefreshHintText();
            RebuildContent();
        }

        private bool IsVietnamese => PlayerPrefs.GetInt(LanguageKey, 0) == 1;

        private string T(string english, string vietnamese)
        {
            return IsVietnamese ? vietnamese : english;
        }

        private void RefreshHintText()
        {
            if (_hintText == null)
            {
                return;
            }

            if (IsBattlePanelActive())
            {
                _hintText.text = T(
                    "Battle mode: choose targets and actions in the panel. Esc opens system. Tab returns to the battle view.",
                    "Battle mode: chọn mục tiêu và hành động trong panel. Esc mở system. Tab quay về battle view.");
                return;
            }

            _hintText.text = IsMenuVisible
                ? T("Esc closes the current panel. Tab returns to gameplay. F5/F9 save and load.", "Esc đóng panel hiện tại. Tab quay lại gameplay. F5/F9 để lưu và tải.")
                : T("Tab opens playtest UI. I/C/J/K open panels directly. E is for world interaction.", "Tab mở UI test. I/C/J/K mở panel trực tiếp. E dùng để tương tác ngoài world.");
        }

        private void ApplyPersistedSettings()
        {
            AudioListener.volume = GetVolume(MasterVolumeKey, 0.9f);
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            int storedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
            int storedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
            if (storedWidth > 0 && storedHeight > 0)
            {
                Screen.SetResolution(storedWidth, storedHeight, Screen.fullScreen);
            }
            int qualityLevel = Mathf.Clamp(PlayerPrefs.GetInt("TPS.Runtime.Quality", QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(qualityLevel, true);
            }
        }

        private float GetVolume(string key, float fallback = 0.8f)
        {
            return PlayerPrefs.GetFloat(key, fallback);
        }

        private void AdjustVolume(string key, float delta, string label)
        {
            float value = Mathf.Clamp01(GetVolume(key) + delta);
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            if (key == MasterVolumeKey)
            {
                AudioListener.volume = value;
            }

            Notify($"{label} {T("volume", "âm lượng")} {Mathf.RoundToInt(value * 100f)}");
            RebuildContent();
        }

        private void SetQualityPreset(int index)
        {
            if (QualitySettings.names.Length == 0)
            {
                return;
            }

            int level = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt("TPS.Runtime.Quality", level);
            PlayerPrefs.Save();
            Notify($"{T("Graphics preset", "Mức đồ họa")} {QualitySettings.names[level]}");
            RebuildContent();
        }

        private string GetQualityName()
        {
            if (QualitySettings.names.Length == 0)
            {
                return "N/A";
            }

            int level = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1);
            return QualitySettings.names[level];
        }

        private void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            PlayerPrefs.SetInt(FullscreenKey, Screen.fullScreen ? 1 : 0);
            PlayerPrefs.Save();
            Notify(Screen.fullScreen ? T("Fullscreen enabled.", "Đã bật toàn màn hình.") : T("Fullscreen disabled.", "Đã tắt toàn màn hình."));
            RebuildContent();
        }

        private void ApplyResolution(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            Screen.SetResolution(width, height, Screen.fullScreen);
            PlayerPrefs.SetInt(ResolutionWidthKey, width);
            PlayerPrefs.SetInt(ResolutionHeightKey, height);
            PlayerPrefs.Save();
            Notify($"{T("Resolution", "Độ phân giải")} {width}x{height}");
            RebuildContent();
        }

        private Slider CreateSlider(RectTransform parent, float width, float min, float max, float value, UnityEngine.Events.UnityAction<float> onValueChanged)
        {
            GameObject sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(LayoutElement), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            LayoutElement layout = sliderGo.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 24f;
            
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.2f, 1f);

            GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = new Vector2(-10, 0);

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            fillGo.GetComponent<Image>().color = new Color(0.33f, 0.46f, 0.61f, 1f);

            GameObject handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform handleAreaRect = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0, 0);
            handleAreaRect.anchorMax = new Vector2(1, 1);
            handleAreaRect.sizeDelta = new Vector2(-20, 0);

            GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            RectTransform handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            handleGo.GetComponent<Image>().color = new Color(0.95f, 0.96f, 0.98f, 1f);

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(onValueChanged);

            return slider;
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private bool IsBattlePanelActive()
        {
            return _activePanel == PanelType.Battle || _activePanel == PanelType.BattleEnd;
        }
    }
}
