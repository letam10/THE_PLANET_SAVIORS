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

namespace TPS.Runtime.UI
{
    public sealed class Phase1RuntimeHUD : MonoBehaviour
    {
        public static Phase1RuntimeHUD Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private MerchantAnchor _activeMerchant;
        private string _lastMessage = "";
        private float _messageExpireAt = -1f;

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

        public void ShowMessage(string message, float duration = 4f)
        {
            _lastMessage = message;
            _messageExpireAt = UnityEngine.Time.unscaledTime + duration;
        }

        public void ToggleShop(MerchantAnchor merchantAnchor)
        {
            _activeMerchant = _activeMerchant == merchantAnchor ? null : merchantAnchor;
        }

        public void CloseShop()
        {
            _activeMerchant = null;
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

            DrawStatusPanel(inBattle);
            DrawSmokePanel(inBattle);
            if (!inBattle)
            {
                DrawInventoryPanel();
                DrawQuestPanel();
                if (_activeMerchant != null)
                {
                    DrawMerchantPanel();
                }
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
            GUI.Box(new Rect(10f, Screen.height - height - 10f, width, height), "Phase 1 Runtime");

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
            GUI.Box(new Rect(10f, 10f, width, height), "Phase 1 Smoke");

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

        private void DrawInventoryPanel()
        {
            if (_contentCatalog == null || InventoryService.Instance == null)
            {
                return;
            }

            float width = 360f;
            float height = 260f;
            float x = Screen.width - width - 10f;
            float y = Screen.height - height - 10f;
            GUI.Box(new Rect(x, y, width, height), "Inventory / Equipment");

            float rowY = y + 30f;
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
                if (GUI.Button(new Rect(x + 170f, rowY - 2f, 50f, 24f), "Use"))
                {
                    UseConsumable(item);
                }
                if (GUI.Button(new Rect(x + 230f, rowY - 2f, 50f, 24f), "Sell"))
                {
                    if (EconomyService.Instance != null && EconomyService.Instance.SellItem(item))
                    {
                        ShowMessage($"Sold {item.DisplayName}.");
                    }
                }
                rowY += 24f;
            }

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
                List<string> activeMembers = PartyService.Instance != null ? PartyService.Instance.GetActiveMemberIds() : null;
                if (activeMembers != null && activeMembers.Count > 0)
                {
                    string targetMember = activeMembers[0];
                    if (GUI.Button(new Rect(x + 170f, rowY - 2f, 70f, 24f), $"Equip {targetMember}") && PartyService.Instance.EquipWeapon(targetMember, item))
                    {
                        ShowMessage($"{targetMember} equipped {item.DisplayName}.");
                    }

                    EquipmentDefinition equippedWeapon = PartyService.Instance.GetEquippedWeapon(targetMember);
                    if (equippedWeapon == item && GUI.Button(new Rect(x + 170f, rowY + 22f, 70f, 24f), $"Unequip"))
                    {
                        if (PartyService.Instance.UnequipWeapon(targetMember))
                        {
                            ShowMessage($"{targetMember} unequipped {item.DisplayName}.");
                        }
                    }
                }
                if (GUI.Button(new Rect(x + 250f, rowY - 2f, 50f, 24f), "Sell"))
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

        private void DrawQuestPanel()
        {
            if (_contentCatalog == null || QuestService.Instance == null)
            {
                return;
            }

            float width = 320f;
            float height = 150f;
            float x = Screen.width - width - 10f;
            GUI.Box(new Rect(x, 10f, width, height), "Quests");
            float rowY = 40f;
            IReadOnlyList<QuestDefinition> quests = _contentCatalog.Quests;
            for (int i = 0; i < quests.Count && rowY < 140f; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null)
                {
                    continue;
                }

                GUI.Label(new Rect(x + 10f, rowY, width - 20f, 20f), $"{quest.Title}: {QuestService.Instance.GetQuestStatus(quest.QuestId)}");
                rowY += 22f;
            }
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

            List<string> activeMembers = PartyService.Instance.GetActiveMemberIds();
            string targetMember = activeMembers.Count > 0 ? activeMembers[0] : null;
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
