using System.Collections.Generic;
using TPS.Runtime.Core;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    public sealed class RewardService : MonoBehaviour
    {
        public static RewardService Instance { get; private set; }

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

        public RewardApplicationResult ApplyRewardTable(RewardTableDefinition rewardTable, IReadOnlyList<string> partyMemberIds)
        {
            if (rewardTable == null)
            {
                return new RewardApplicationResult
                {
                    Summary = "No reward."
                };
            }

            var result = new RewardApplicationResult();
            var summaryParts = new List<string>();

            if (rewardTable.CurrencyReward > 0 && EconomyService.Instance != null)
            {
                EconomyService.Instance.AddCurrency(rewardTable.CurrencyReward);
                result.CurrencyGranted = rewardTable.CurrencyReward;
                summaryParts.Add($"+{rewardTable.CurrencyReward} currency");
            }

            if (ProgressionService.Instance != null && partyMemberIds != null && rewardTable.ExpReward > 0)
            {
                ProgressionService.Instance.AddExpToParty(rewardTable.ExpReward, partyMemberIds);
                result.ExpGrantedPerMember = rewardTable.ExpReward;
                summaryParts.Add($"+{rewardTable.ExpReward} EXP");
            }

            if (InventoryService.Instance != null)
            {
                IReadOnlyList<ItemGrantDefinition> guaranteedItems = rewardTable.GuaranteedItems;
                for (int i = 0; i < guaranteedItems.Count; i++)
                {
                    ItemGrantDefinition itemGrant = guaranteedItems[i];
                    if (itemGrant != null && itemGrant.Item != null)
                    {
                        InventoryService.Instance.AddItem(itemGrant.Item, itemGrant.Amount);
                        result.ItemGrants.Add($"{itemGrant.Amount}x {itemGrant.Item.DisplayName}");
                        summaryParts.Add($"+{itemGrant.Amount} {itemGrant.Item.DisplayName}");
                    }
                }

                IReadOnlyList<EquipmentGrantDefinition> guaranteedEquipment = rewardTable.GuaranteedEquipment;
                for (int i = 0; i < guaranteedEquipment.Count; i++)
                {
                    EquipmentGrantDefinition equipmentGrant = guaranteedEquipment[i];
                    if (equipmentGrant != null && equipmentGrant.Equipment != null)
                    {
                        InventoryService.Instance.AddEquipment(equipmentGrant.Equipment, equipmentGrant.Amount);
                        result.EquipmentGrants.Add($"{equipmentGrant.Amount}x {equipmentGrant.Equipment.DisplayName}");
                        summaryParts.Add($"+{equipmentGrant.Amount} {equipmentGrant.Equipment.DisplayName}");
                    }
                }

                WeightedDropEntry rolledDrop = RollWeightedDrop(rewardTable);
                if (rolledDrop != null)
                {
                    if (rolledDrop.Item != null)
                    {
                        InventoryService.Instance.AddItem(rolledDrop.Item, rolledDrop.Amount);
                        result.ItemGrants.Add($"{rolledDrop.Amount}x {rolledDrop.Item.DisplayName}");
                        summaryParts.Add($"+{rolledDrop.Amount} {rolledDrop.Item.DisplayName}");
                    }
                    else if (rolledDrop.Equipment != null)
                    {
                        InventoryService.Instance.AddEquipment(rolledDrop.Equipment, rolledDrop.Amount);
                        result.EquipmentGrants.Add($"{rolledDrop.Amount}x {rolledDrop.Equipment.DisplayName}");
                        summaryParts.Add($"+{rolledDrop.Amount} {rolledDrop.Equipment.DisplayName}");
                    }
                }
            }

            result.Summary = summaryParts.Count > 0 ? string.Join(", ", summaryParts) : "Reward applied.";
            GameEventBus.PublishRewardGranted(result.Summary);
            return result;
        }

        private static WeightedDropEntry RollWeightedDrop(RewardTableDefinition rewardTable)
        {
            IReadOnlyList<WeightedDropEntry> drops = rewardTable.WeightedDrops;
            if (drops == null || drops.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i] != null)
                {
                    totalWeight += Mathf.Max(0, drops[i].Weight);
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int cursor = 0;
            for (int i = 0; i < drops.Count; i++)
            {
                WeightedDropEntry drop = drops[i];
                if (drop == null)
                {
                    continue;
                }

                cursor += Mathf.Max(0, drop.Weight);
                if (roll < cursor)
                {
                    return drop;
                }
            }

            return null;
        }
    }
}
