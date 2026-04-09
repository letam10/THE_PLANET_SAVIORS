using System.Collections.Generic;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using TPS.Runtime.Time;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    public sealed class EconomyService : MonoBehaviour
    {
        public static EconomyService Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private readonly Dictionary<string, bool> _shopUnlocks = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> _shopStock = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _lastRestockDay = new Dictionary<string, int>();
        private int _currency;
        private bool _defaultsInitialized;

        public int Currency => _currency;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDefaults();
        }

        public void EnsureDefaults()
        {
            if (_defaultsInitialized)
            {
                return;
            }

            _defaultsInitialized = true;
            _currency = _contentCatalog != null ? _contentCatalog.StartingCurrency : 0;

            if (_contentCatalog == null)
            {
                return;
            }

            IReadOnlyList<ShopDefinition> shops = _contentCatalog.Shops;
            for (int i = 0; i < shops.Count; i++)
            {
                ShopDefinition shop = shops[i];
                if (shop != null)
                {
                    _shopUnlocks[shop.ShopId] = true;
                    RestockShop(shop, WorldClock.Instance != null ? WorldClock.Instance.CurrentDay : 1);
                }
            }
        }

        public void AddCurrency(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            _currency = Mathf.Max(0, _currency + amount);
            GameEventBus.PublishEconomyChanged("currency");
        }

        public bool IsShopUnlocked(string shopId)
        {
            return !_shopUnlocks.TryGetValue(shopId, out bool unlocked) || unlocked;
        }

        public bool CanAccessShop(ShopDefinition shopDefinition)
        {
            if (shopDefinition == null || !IsShopUnlocked(shopDefinition.ShopId))
            {
                return false;
            }

            return shopDefinition.AvailabilityConditions == null || shopDefinition.AvailabilityConditions.EvaluateAll();
        }

        public int GetEntryStock(ShopDefinition shopDefinition, ShopEntryDefinition entry)
        {
            if (shopDefinition == null || entry == null)
            {
                return 0;
            }

            if (entry.Stock <= 0)
            {
                return int.MaxValue;
            }

            string key = BuildStockKey(shopDefinition.ShopId, entry);
            return _shopStock.TryGetValue(key, out int value) ? value : entry.Stock;
        }

        public bool BuyItem(ShopDefinition shopDefinition, ShopEntryDefinition entry)
        {
            if (shopDefinition == null || entry == null || !CanAccessShop(shopDefinition))
            {
                return false;
            }

            int price = ResolvePrice(entry);
            if (_currency < price || GetEntryStock(shopDefinition, entry) <= 0 || InventoryService.Instance == null)
            {
                return false;
            }

            AddCurrency(-price);
            if (entry.Item != null)
            {
                InventoryService.Instance.AddItem(entry.Item, 1);
            }
            else if (entry.Equipment != null)
            {
                InventoryService.Instance.AddEquipment(entry.Equipment, 1);
            }

            ReduceStock(shopDefinition, entry, 1);
            GameEventBus.PublishEconomyChanged(shopDefinition.ShopId);
            return true;
        }

        public bool SellItem(ItemDefinition itemDefinition)
        {
            if (itemDefinition == null || InventoryService.Instance == null || !InventoryService.Instance.RemoveItem(itemDefinition, 1))
            {
                return false;
            }

            AddCurrency(itemDefinition.SellPrice);
            return true;
        }

        public bool SellEquipment(EquipmentDefinition equipmentDefinition)
        {
            if (equipmentDefinition == null || InventoryService.Instance == null || !InventoryService.Instance.RemoveEquipment(equipmentDefinition, 1))
            {
                return false;
            }

            AddCurrency(equipmentDefinition.SellPrice);
            return true;
        }

        public void RestockDaily(int currentDay)
        {
            if (_contentCatalog == null)
            {
                return;
            }

            IReadOnlyList<ShopDefinition> shops = _contentCatalog.Shops;
            for (int i = 0; i < shops.Count; i++)
            {
                ShopDefinition shop = shops[i];
                if (shop == null)
                {
                    continue;
                }

                if (_lastRestockDay.TryGetValue(shop.ShopId, out int lastRestockDay) && lastRestockDay >= currentDay)
                {
                    continue;
                }

                RestockShop(shop, currentDay);
                GameEventBus.PublishEconomyChanged(shop.ShopId);
            }
        }

        public EconomyStateData CaptureState()
        {
            var data = new EconomyStateData
            {
                Currency = _currency
            };

            foreach (var pair in _shopUnlocks)
            {
                data.ShopUnlocks.Add(new BoolMapEntry { Key = pair.Key, Value = pair.Value });
            }

            foreach (var pair in _shopStock)
            {
                data.ShopStock.Add(new IntMapEntry { Key = pair.Key, Value = pair.Value });
            }

            foreach (var pair in _lastRestockDay)
            {
                data.LastRestockDays.Add(new IntMapEntry { Key = pair.Key, Value = pair.Value });
            }

            return data;
        }

        public void RestoreState(EconomyStateData data)
        {
            _shopUnlocks.Clear();
            _shopStock.Clear();
            _lastRestockDay.Clear();
            _defaultsInitialized = true;

            if (data == null)
            {
                return;
            }

            _currency = Mathf.Max(0, data.Currency);

            for (int i = 0; i < data.ShopUnlocks.Count; i++)
            {
                _shopUnlocks[data.ShopUnlocks[i].Key] = data.ShopUnlocks[i].Value;
            }

            for (int i = 0; i < data.ShopStock.Count; i++)
            {
                _shopStock[data.ShopStock[i].Key] = data.ShopStock[i].Value;
            }

            for (int i = 0; i < data.LastRestockDays.Count; i++)
            {
                _lastRestockDay[data.LastRestockDays[i].Key] = data.LastRestockDays[i].Value;
            }
        }

        private void ReduceStock(ShopDefinition shopDefinition, ShopEntryDefinition entry, int amount)
        {
            if (entry.Stock <= 0)
            {
                return;
            }

            string key = BuildStockKey(shopDefinition.ShopId, entry);
            _shopStock[key] = Mathf.Max(0, GetEntryStock(shopDefinition, entry) - amount);
        }

        private void RestockShop(ShopDefinition shopDefinition, int day)
        {
            IReadOnlyList<ShopEntryDefinition> entries = shopDefinition.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                ShopEntryDefinition entry = entries[i];
                if (entry != null && entry.Stock > 0)
                {
                    _shopStock[BuildStockKey(shopDefinition.ShopId, entry)] = entry.Stock;
                }
            }

            _lastRestockDay[shopDefinition.ShopId] = day;
        }

        private static int ResolvePrice(ShopEntryDefinition entry)
        {
            if (entry.PriceOverride >= 0)
            {
                return entry.PriceOverride;
            }

            if (entry.Item != null)
            {
                return entry.Item.BuyPrice;
            }

            if (entry.Equipment != null)
            {
                return entry.Equipment.BuyPrice;
            }

            return 0;
        }

        private static string BuildStockKey(string shopId, ShopEntryDefinition entry)
        {
            string entryId = entry.Item != null ? entry.Item.ItemId : entry.Equipment != null ? entry.Equipment.EquipmentId : "unknown";
            return $"{shopId}|{entryId}";
        }
    }
}
