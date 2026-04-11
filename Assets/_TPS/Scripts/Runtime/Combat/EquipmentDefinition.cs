using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "EQP_NewEquipment", menuName = "TPS/RPG/Equipment")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [SerializeField] private string _equipmentId = "equipment_new";
        [SerializeField] private string _displayName = "New Equipment";
        [SerializeField] private EquipmentSlotType _slotType = EquipmentSlotType.Weapon;
        [SerializeField] private WeaponFamilyType _weaponFamily = WeaponFamilyType.None;
        [Min(0)] [SerializeField] private int _weaponPower = 0;
        [SerializeField] private StatBlock _statBonus = new StatBlock();
        [SerializeField] private ResistanceProfile _resistanceModifier = new ResistanceProfile();
        [SerializeField] private List<SkillDefinition> _grantedSkills = new List<SkillDefinition>();
        [Min(0)] [SerializeField] private int _buyPrice = 40;
        [Min(0)] [SerializeField] private int _sellPrice = 20;

        public string EquipmentId => _equipmentId;
        public string DisplayName => _displayName;
        public EquipmentSlotType SlotType => _slotType;
        public WeaponFamilyType WeaponFamily => _weaponFamily;
        public int WeaponPower => _weaponPower;
        public StatBlock StatBonus => _statBonus;
        public ResistanceProfile ResistanceModifier => _resistanceModifier;
        public IReadOnlyList<SkillDefinition> GrantedSkills => _grantedSkills;
        public int BuyPrice => _buyPrice;
        public int SellPrice => _sellPrice;
    }
}
