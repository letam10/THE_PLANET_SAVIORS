using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "ITM_NewItem", menuName = "TPS/RPG/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string _itemId = "item_new";
        [SerializeField] private string _displayName = "New Item";
        [TextArea] [SerializeField] private string _description = "";
        [Min(0)] [SerializeField] private int _buyPrice = 10;
        [Min(0)] [SerializeField] private int _sellPrice = 5;
        [Min(0)] [SerializeField] private int _restoreHP = 0;
        [Min(0)] [SerializeField] private int _restoreMP = 0;
        [SerializeField] private List<CombatStatusType> _curedStatuses = new List<CombatStatusType>();

        public string ItemId => _itemId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int BuyPrice => _buyPrice;
        public int SellPrice => _sellPrice;
        public int RestoreHP => _restoreHP;
        public int RestoreMP => _restoreMP;
        public IReadOnlyList<CombatStatusType> CuredStatuses => _curedStatuses;
    }
}
