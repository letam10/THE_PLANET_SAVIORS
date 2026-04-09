using System.Collections.Generic;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    public sealed class InventoryService : MonoBehaviour
    {
        public static InventoryService Instance { get; private set; }

        [SerializeField] private List<ItemGrantDefinition> _startingItems = new List<ItemGrantDefinition>();
        [SerializeField] private List<EquipmentGrantDefinition> _startingEquipment = new List<EquipmentGrantDefinition>();

        private readonly Dictionary<string, int> _itemCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _equipmentCounts = new Dictionary<string, int>();
        private bool _hasInitializedDefaults;

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

        public int GetItemCount(string itemId)
        {
            return _itemCounts.TryGetValue(itemId, out int count) ? count : 0;
        }

        public int GetEquipmentCount(string equipmentId)
        {
            return _equipmentCounts.TryGetValue(equipmentId, out int count) ? count : 0;
        }

        public void AddItem(ItemDefinition itemDefinition, int amount)
        {
            if (itemDefinition == null || amount <= 0)
            {
                return;
            }

            AddToStack(_itemCounts, itemDefinition.ItemId, amount);
            GameEventBus.PublishInventoryChanged(itemDefinition.ItemId);
        }

        public bool RemoveItem(ItemDefinition itemDefinition, int amount)
        {
            if (itemDefinition == null || amount <= 0)
            {
                return false;
            }

            bool removed = RemoveFromStack(_itemCounts, itemDefinition.ItemId, amount);
            if (removed)
            {
                GameEventBus.PublishInventoryChanged(itemDefinition.ItemId);
            }

            return removed;
        }

        public void AddEquipment(EquipmentDefinition equipmentDefinition, int amount)
        {
            if (equipmentDefinition == null || amount <= 0)
            {
                return;
            }

            AddToStack(_equipmentCounts, equipmentDefinition.EquipmentId, amount);
            GameEventBus.PublishInventoryChanged(equipmentDefinition.EquipmentId);
        }

        public bool RemoveEquipment(EquipmentDefinition equipmentDefinition, int amount)
        {
            if (equipmentDefinition == null || amount <= 0)
            {
                return false;
            }

            bool removed = RemoveFromStack(_equipmentCounts, equipmentDefinition.EquipmentId, amount);
            if (removed)
            {
                GameEventBus.PublishInventoryChanged(equipmentDefinition.EquipmentId);
            }

            return removed;
        }

        public InventoryStateData CaptureState()
        {
            var data = new InventoryStateData();
            foreach (var pair in _itemCounts)
            {
                data.ItemStacks.Add(new InventoryStackStateEntry
                {
                    DefinitionId = pair.Key,
                    Count = pair.Value
                });
            }

            foreach (var pair in _equipmentCounts)
            {
                data.EquipmentStacks.Add(new InventoryStackStateEntry
                {
                    DefinitionId = pair.Key,
                    Count = pair.Value
                });
            }

            return data;
        }

        public void RestoreState(InventoryStateData data)
        {
            _itemCounts.Clear();
            _equipmentCounts.Clear();
            _hasInitializedDefaults = true;

            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.ItemStacks.Count; i++)
            {
                InventoryStackStateEntry entry = data.ItemStacks[i];
                if (!string.IsNullOrWhiteSpace(entry.DefinitionId) && entry.Count > 0)
                {
                    _itemCounts[entry.DefinitionId] = entry.Count;
                }
            }

            for (int i = 0; i < data.EquipmentStacks.Count; i++)
            {
                InventoryStackStateEntry entry = data.EquipmentStacks[i];
                if (!string.IsNullOrWhiteSpace(entry.DefinitionId) && entry.Count > 0)
                {
                    _equipmentCounts[entry.DefinitionId] = entry.Count;
                }
            }
        }

        private void EnsureDefaults()
        {
            if (_hasInitializedDefaults)
            {
                return;
            }

            _hasInitializedDefaults = true;

            for (int i = 0; i < _startingItems.Count; i++)
            {
                ItemGrantDefinition entry = _startingItems[i];
                if (entry != null && entry.Item != null)
                {
                    AddToStack(_itemCounts, entry.Item.ItemId, entry.Amount);
                }
            }

            for (int i = 0; i < _startingEquipment.Count; i++)
            {
                EquipmentGrantDefinition entry = _startingEquipment[i];
                if (entry != null && entry.Equipment != null)
                {
                    AddToStack(_equipmentCounts, entry.Equipment.EquipmentId, entry.Amount);
                }
            }
        }

        private static void AddToStack(Dictionary<string, int> dictionary, string key, int amount)
        {
            if (dictionary.TryGetValue(key, out int currentCount))
            {
                dictionary[key] = currentCount + amount;
            }
            else
            {
                dictionary[key] = amount;
            }
        }

        private static bool RemoveFromStack(Dictionary<string, int> dictionary, string key, int amount)
        {
            if (!dictionary.TryGetValue(key, out int currentCount) || currentCount < amount)
            {
                return false;
            }

            int newCount = currentCount - amount;
            if (newCount <= 0)
            {
                dictionary.Remove(key);
            }
            else
            {
                dictionary[key] = newCount;
            }

            return true;
        }
    }
}
