using System.Collections.Generic;
using TPS.Runtime.Conditions;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "SHP_NewShop", menuName = "TPS/RPG/Shop")]
    public sealed class ShopDefinition : ScriptableObject
    {
        [SerializeField] private string _shopId = "shop_new";
        [SerializeField] private string _displayName = "New Shop";
        [SerializeField] private ConditionResolver _availabilityConditions = new ConditionResolver();
        [SerializeField] private List<ShopEntryDefinition> _entries = new List<ShopEntryDefinition>();

        public string ShopId => _shopId;
        public string DisplayName => _displayName;
        public ConditionResolver AvailabilityConditions => _availabilityConditions;
        public IReadOnlyList<ShopEntryDefinition> Entries => _entries;
    }
}
