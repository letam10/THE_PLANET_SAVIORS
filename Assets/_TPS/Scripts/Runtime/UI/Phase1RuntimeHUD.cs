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
        private enum RuntimePanelType
        {
            None = 0,
            Inventory = 1,
            Character = 2,
            Quest = 3,
            Equipment = 4,
            System = 5
        }

        public static Phase1RuntimeHUD Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private MerchantAnchor _activeMerchant;
        private string _lastMessage = "";
        private float _messageExpireAt = -1f;
        private RuntimePanelType _activePanel;
        private string _selectedMemberId;

        public MerchantAnchor ActiveMerchant => _activeMerchant;
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
            _activePanel = RuntimePanelType.None;
            EnsureSelectedMember();
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                RuntimeUiInputState.RestoreGameplayFocus();
            }
        }

        private void Update()
        {
            if (RuntimeMenuCanvasController.Instance != null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                ToggleUiFocus();
            }
            else if (keyboard.iKey.wasPressedThisFrame)
            {
                OpenPanel(RuntimePanelType.Inventory);
            }
            else if (keyboard.cKey.wasPressedThisFrame)
            {
                OpenPanel(RuntimePanelType.Character);
            }
            else if (keyboard.jKey.wasPressedThisFrame)
            {
                OpenPanel(RuntimePanelType.Quest);
            }
            else if (keyboard.kKey.wasPressedThisFrame)
            {
                OpenPanel(RuntimePanelType.Equipment);
            }
            else if (keyboard.f5Key.wasPressedThisFrame && SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame();
                ShowMessage("Manual save requested.");
            }
            else if (keyboard.f9Key.wasPressedThisFrame && SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.LoadGame();
                ShowMessage("Manual load requested.");
            }
            else if (keyboard.pKey.wasPressedThisFrame)
            {
                OpenPanel(RuntimePanelType.System);
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_activeMerchant != null)
                {
                    CloseShop();
                }
                else if (_activePanel != RuntimePanelType.None)
                {
                    CloseAllPanels(restoreGameplayFocus: true);
                }
                else if (RuntimeUiInputState.IsUiFocused)
                {
                    OpenPanel(RuntimePanelType.System);
                }
                else
                {
                    OpenPanel(RuntimePanelType.System);
                }
            }

            if (keyboard.digit1Key.wasPressedThisFrame) SelectMemberByIndex(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) SelectMemberByIndex(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) SelectMemberByIndex(2);
        }

        public void ShowMessage(string message, float duration = 4f)
        {
            _lastMessage = message;
            _messageExpireAt = UnityEngine.Time.unscaledTime + duration;
        }

        public void ToggleShop(MerchantAnchor merchantAnchor)
        {
            _activeMerchant = _activeMerchant == merchantAnchor ? null : merchantAnchor;
            if (_activeMerchant != null)
            {
                _activePanel = RuntimePanelType.None;
            }

            RuntimeUiInputState.SetUiFocused(_activeMerchant != null);
        }

        public void CloseShop()
        {
            _activeMerchant = null;
            if (_activePanel == RuntimePanelType.None)
            {
                RuntimeUiInputState.RestoreGameplayFocus();
            }
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

            if (!inBattle)
            {
                bool useCanvasMenus = RuntimeMenuCanvasController.Instance != null;
                if (!useCanvasMenus)
                {
                    DrawActiveWorldPanel();
                    if (_activeMerchant != null)
                    {
                        DrawMerchantPanel();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(_lastMessage) && UnityEngine.Time.unscaledTime <= _messageExpireAt)
            {
                GUI.Box(new Rect((Screen.width - 420f) * 0.5f, Screen.height - 90f, 420f, 50f), _lastMessage);
            }
        }

        private void ToggleUiFocus()
        {
            if (_activePanel == RuntimePanelType.None && _activeMerchant == null)
            {
                RuntimeUiInputState.ToggleUiFocused();
            }
            else
            {
                CloseAllPanels(restoreGameplayFocus: true);
                return;
            }

            if (!RuntimeUiInputState.IsUiFocused)
            {
                _activeMerchant = null;
            }

            ShowMessage(RuntimeUiInputState.IsUiFocused
                ? "UI focus enabled. Cursor unlocked for HUD buttons."
                : "Gameplay focus restored. Cursor locked for camera.");
        }

        private void OpenPanel(RuntimePanelType panel)
        {
            _activeMerchant = null;
            _activePanel = panel;
            EnsureSelectedMember();
            RuntimeUiInputState.SetUiFocused(panel != RuntimePanelType.None);
            ShowMessage(panel == RuntimePanelType.None
                ? "Gameplay focus restored."
                : $"Opened {GetPanelDisplayName(panel)}.");
        }

        private void CloseAllPanels(bool restoreGameplayFocus)
        {
            _activeMerchant = null;
            _activePanel = RuntimePanelType.None;
            if (restoreGameplayFocus)
            {
                RuntimeUiInputState.RestoreGameplayFocus();
            }
        }

        private void DrawActiveWorldPanel()
        {
            switch (_activePanel)
            {
                case RuntimePanelType.Inventory:
                    DrawInventoryPanel(showEquipment: false);
                    break;
                case RuntimePanelType.Character:
                    DrawCharacterPanel();
                    break;
                case RuntimePanelType.Quest:
                    DrawQuestPanel();
                    break;
                case RuntimePanelType.Equipment:
                    DrawInventoryPanel(showEquipment: true);
                    break;
                case RuntimePanelType.System:
                    DrawSystemPanel();
                    break;
            }
        }

        private void EnsureSelectedMember()
        {
            if (PartyService.Instance == null)
            {
                _selectedMemberId = null;
                return;
            }

            List<string> activeMembers = PartyService.Instance.GetActiveMemberIds();
            if (activeMembers.Count == 0)
            {
                _selectedMemberId = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectedMemberId) || !activeMembers.Contains(_selectedMemberId))
            {
                _selectedMemberId = activeMembers[0];
            }
        }

        private void SelectMemberByIndex(int index)
        {
            if (PartyService.Instance == null)
            {
                return;
            }

            List<string> activeMembers = PartyService.Instance.GetActiveMemberIds();
            if (index < 0 || index >= activeMembers.Count)
            {
                return;
            }

            _selectedMemberId = activeMembers[index];
            ShowMessage($"Selected {_selectedMemberId}.");
        }

        private string GetPanelDisplayName(RuntimePanelType panel)
        {
            switch (panel)
            {
                case RuntimePanelType.Inventory:
                    return "Inventory";
                case RuntimePanelType.Character:
                    return "Character";
                case RuntimePanelType.Quest:
                    return "Quest Log";
                case RuntimePanelType.Equipment:
                    return "Equipment";
                case RuntimePanelType.System:
                    return "System";
                default:
                    return "Gameplay";
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
                    ? $"UI mode ON | Active: {GetPanelDisplayName(_activePanel)} | Esc closes | 1-3 select member"
                    : "Gameplay mode ON | Tab unlocks cursor | I/C/J/K open panels | F5 save | F9 load | P system";
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

        private void DrawInventoryPanel(bool showEquipment)
        {
            if (_contentCatalog == null || InventoryService.Instance == null || PartyService.Instance == null)
            {
                return;
            }

            float width = 360f;
            float height = 260f;
            float x = Screen.width - width - 10f;
            float y = Screen.height - height - 10f;
            GUI.Box(new Rect(x, y, width, height), showEquipment ? "Equipment" : "Inventory");

            float rowY = y + 30f;
            DrawMemberSelector(x + 10f, ref rowY, width - 20f);

            if (!showEquipment)
            {
                IReadOnlyList<ItemDefinition> items = _contentCatalog.Items;
                for (int i = 0; i < items.Count && rowY < y + height - 30f; i++)
                {
                    ItemDefinition item = items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    int count = InventoryService.Instance.GetItemCount(item.ItemId);
                    if (count <= 0)
                    {
                        continue;
                    }

                    GUI.Label(new Rect(x + 10f, rowY, 150f, 20f), $"{item.DisplayName} x{count}");
                    if (GUI.Button(new Rect(x + 170f, rowY - 2f, 60f, 24f), "Use"))
                    {
                        UseConsumable(item);
                    }
                    if (GUI.Button(new Rect(x + 240f, rowY - 2f, 50f, 24f), "Sell"))
                    {
                        if (EconomyService.Instance != null && EconomyService.Instance.SellItem(item))
                        {
                            ShowMessage($"Sold {item.DisplayName}.");
                        }
                    }
                    rowY += 24f;
                }
            }
            else
            {
                IReadOnlyList<EquipmentDefinition> equipment = _contentCatalog.Equipment;
                for (int i = 0; i < equipment.Count && rowY < y + height - 50f; i++)
                {
                    EquipmentDefinition item = equipment[i];
                    if (item == null)
                    {
                        continue;
                    }

                    int count = InventoryService.Instance.GetEquipmentCount(item.EquipmentId);
                    if (count <= 0)
                    {
                        continue;
                    }

                    GUI.Label(new Rect(x + 10f, rowY, 150f, 20f), $"{item.DisplayName} x{count}");
                    if (!string.IsNullOrWhiteSpace(_selectedMemberId) &&
                        GUI.Button(new Rect(x + 170f, rowY - 2f, 80f, 24f), $"Equip {_selectedMemberId}") &&
                        PartyService.Instance.EquipWeapon(_selectedMemberId, item))
                    {
                        ShowMessage($"{_selectedMemberId} equipped {item.DisplayName}.");
                    }

                    EquipmentDefinition equippedWeapon = !string.IsNullOrWhiteSpace(_selectedMemberId)
                        ? PartyService.Instance.GetEquippedWeapon(_selectedMemberId)
                        : null;
                    if (equippedWeapon == item && GUI.Button(new Rect(x + 170f, rowY + 22f, 80f, 24f), "Unequip"))
                    {
                        if (PartyService.Instance.UnequipWeapon(_selectedMemberId))
                        {
                            ShowMessage($"{_selectedMemberId} unequipped {item.DisplayName}.");
                        }
                    }

                    if (GUI.Button(new Rect(x + 260f, rowY - 2f, 50f, 24f), "Sell"))
                    {
                        if (EconomyService.Instance != null && EconomyService.Instance.SellEquipment(item))
                        {
                            ShowMessage($"Sold {item.DisplayName}.");
                        }
                        else
                        {
                            ShowMessage($"Could not sell {item.DisplayName}.");
                        }
                    }
                    rowY += 48f;
                }
            }
        }

        private void DrawCharacterPanel()
        {
            if (PartyService.Instance == null)
            {
                return;
            }

            EnsureSelectedMember();
            float width = 360f;
            float height = 250f;
            float x = Screen.width - width - 10f;
            float y = Screen.height - height - 10f;
            GUI.Box(new Rect(x, y, width, height), "Character");

            float rowY = y + 30f;
            DrawMemberSelector(x + 10f, ref rowY, width - 20f);

            if (string.IsNullOrWhiteSpace(_selectedMemberId))
            {
                GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), "No active party member selected.");
                return;
            }

            CharacterStatSnapshot snapshot = PartyService.Instance.GetMemberSnapshot(_selectedMemberId);
            if (snapshot == null)
            {
                GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), "Character snapshot unavailable.");
                return;
            }

            GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), $"{snapshot.DisplayName} Lv{snapshot.Level}");
            rowY += 22f;
            GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), $"HP {PartyService.Instance.GetCurrentHP(snapshot.CharacterId)}/{snapshot.Stats.MaxHP}  MP {PartyService.Instance.GetCurrentMP(snapshot.CharacterId)}/{snapshot.Stats.MaxMP}");
            rowY += 22f;
            GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), $"ATK {snapshot.Stats.Attack}  MAG {snapshot.Stats.Magic}  DEF {snapshot.Stats.Defense}");
            rowY += 20f;
            GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), $"RES {snapshot.Stats.Resistance}  SPD {snapshot.Stats.Speed}");
            rowY += 20f;
            string weaponName = snapshot.EquippedWeapon != null ? snapshot.EquippedWeapon.DisplayName : "No Weapon";
            GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), $"Weapon: {weaponName}");
            rowY += 24f;
            GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), "Skills");
            rowY += 18f;
            for (int i = 0; i < snapshot.Skills.Count && rowY < y + height - 20f; i++)
            {
                SkillDefinition skill = snapshot.Skills[i];
                if (skill == null)
                {
                    continue;
                }

                GUI.Label(new Rect(x + 20f, rowY, width - 30f, 18f), $"- {skill.DisplayName} ({skill.ResourceCost} MP)");
                rowY += 18f;
            }
        }

        private void DrawQuestPanel()
        {
            if (_contentCatalog == null || QuestService.Instance == null)
            {
                return;
            }

            float width = 320f;
            float height = 190f;
            float x = Screen.width - width - 10f;
            float panelY = 10f;
            GUI.Box(new Rect(x, panelY, width, height), "Quests");
            float rowY = panelY + 30f;
            IReadOnlyList<QuestDefinition> quests = _contentCatalog.Quests;
            for (int i = 0; i < quests.Count && rowY < panelY + height - 24f; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null)
                {
                    continue;
                }

                int completed = QuestService.Instance.GetCompletedObjectiveCount(quest.QuestId);
                int total = QuestService.Instance.GetObjectiveCount(quest.QuestId);
                string objectiveText = QuestService.Instance.GetNextIncompleteObjectiveDescription(quest.QuestId);
                GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), $"{quest.Title}: {QuestService.Instance.GetQuestStatus(quest.QuestId)} ({completed}/{Mathf.Max(total, 0)})");
                rowY += 20f;
                if (!string.IsNullOrWhiteSpace(objectiveText))
                {
                    GUI.Label(new Rect(x + 10f, rowY, width - 20f, 34f), objectiveText);
                    rowY += 36f;
                }
            }
        }

        private void DrawSystemPanel()
        {
            float width = 280f;
            float height = 210f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;
            GUI.Box(new Rect(x, y, width, height), "System");
            GUI.Label(new Rect(x + 15f, y + 30f, width - 30f, 20f), $"Mode: {RuntimeUiInputState.CurrentMode}");
            GUI.Label(new Rect(x + 15f, y + 50f, width - 30f, 20f), $"Scene: {SceneManager.GetActiveScene().name}");

            if (GUI.Button(new Rect(x + 15f, y + 82f, width - 30f, 28f), "Save Game") && SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.SaveGame();
                ShowMessage("Game saved.");
            }

            if (GUI.Button(new Rect(x + 15f, y + 116f, width - 30f, 28f), "Load Game") && SaveLoadManager.Instance != null)
            {
                SaveLoadManager.Instance.LoadGame();
                ShowMessage("Game loaded.");
            }

            if (GUI.Button(new Rect(x + 15f, y + 150f, width - 30f, 28f), "Return To Gameplay"))
            {
                CloseAllPanels(restoreGameplayFocus: true);
            }
        }

        private void DrawMemberSelector(float x, ref float rowY, float width)
        {
            if (PartyService.Instance == null)
            {
                return;
            }

            EnsureSelectedMember();
            List<string> activeMembers = PartyService.Instance.GetActiveMemberIds();
            if (activeMembers.Count == 0)
            {
                return;
            }

            GUI.Label(new Rect(x, rowY, width, 18f), $"Selected: {_selectedMemberId}");
            rowY += 20f;

            float buttonWidth = Mathf.Min(95f, width / Mathf.Max(1, activeMembers.Count));
            for (int i = 0; i < activeMembers.Count; i++)
            {
                string memberId = activeMembers[i];
                if (GUI.Button(new Rect(x + i * (buttonWidth + 6f), rowY, buttonWidth, 22f), $"{i + 1}:{memberId}"))
                {
                    _selectedMemberId = memberId;
                    ShowMessage($"Selected {memberId}.");
                }
            }

            rowY += 28f;
        }

        private void DrawMerchantPanel()
        {
            ShopDefinition shop = _activeMerchant != null ? _activeMerchant.ShopDefinition : null;
            if (shop == null || EconomyService.Instance == null)
            {
                return;
            }

            float width = 360f;
            float height = 220f;
            float x = (Screen.width - width) * 0.5f;
            float y = 20f;
            GUI.Box(new Rect(x, y, width, height), shop.DisplayName);

            float rowY = y + 35f;
            IReadOnlyList<ShopEntryDefinition> entries = shop.Entries;
            for (int i = 0; i < entries.Count && rowY < y + height - 35f; i++)
            {
                ShopEntryDefinition entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                string label = entry.Item != null ? entry.Item.DisplayName : entry.Equipment != null ? entry.Equipment.DisplayName : "Entry";
                int stock = EconomyService.Instance.GetEntryStock(shop, entry);
                int price = entry.PriceOverride >= 0 ? entry.PriceOverride : entry.Item != null ? entry.Item.BuyPrice : entry.Equipment != null ? entry.Equipment.BuyPrice : 0;
                GUI.Label(new Rect(x + 10f, rowY, 180f, 20f), $"{label} ({price}) x{(stock == int.MaxValue ? 999 : stock)}");
                if (GUI.Button(new Rect(x + width - 80f, rowY - 2f, 60f, 24f), "Buy"))
                {
                    if (EconomyService.Instance.BuyItem(shop, entry))
                    {
                        ShowMessage($"Bought {label}.");
                    }
                    else
                    {
                        ShowMessage($"Could not buy {label}.");
                    }
                }
                rowY += 26f;
            }

            if (GUI.Button(new Rect(x + 10f, y + height - 35f, width - 20f, 24f), "Close Shop"))
            {
                CloseShop();
            }
        }

        private void UseConsumable(ItemDefinition item)
        {
            if (item == null || InventoryService.Instance == null || PartyService.Instance == null)
            {
                return;
            }

            EnsureSelectedMember();
            string targetMember = _selectedMemberId;
            if (targetMember == null)
            {
                return;
            }

            if (PartyService.Instance.TryUseConsumable(targetMember, item))
            {
                ShowMessage($"Used {item.DisplayName} on {targetMember}.");
            }
        }
    }
}
