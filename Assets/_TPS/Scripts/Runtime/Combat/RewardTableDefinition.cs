using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "RWD_NewReward", menuName = "TPS/RPG/Reward Table")]
    public sealed class RewardTableDefinition : ScriptableObject
    {
        [SerializeField] private string _rewardId = "reward_new";
        [Min(0)] [SerializeField] private int _currencyReward = 0;
        [Min(0)] [SerializeField] private int _expReward = 0;
        [SerializeField] private List<ItemGrantDefinition> _guaranteedItems = new List<ItemGrantDefinition>();
        [SerializeField] private List<EquipmentGrantDefinition> _guaranteedEquipment = new List<EquipmentGrantDefinition>();
        [SerializeField] private List<WeightedDropEntry> _weightedDrops = new List<WeightedDropEntry>();

        public string RewardId => _rewardId;
        public int CurrencyReward => _currencyReward;
        public int ExpReward => _expReward;
        public IReadOnlyList<ItemGrantDefinition> GuaranteedItems => _guaranteedItems;
        public IReadOnlyList<EquipmentGrantDefinition> GuaranteedEquipment => _guaranteedEquipment;
        public IReadOnlyList<WeightedDropEntry> WeightedDrops => _weightedDrops;
    }
}
