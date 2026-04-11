using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "SKL_NewSkill", menuName = "TPS/RPG/Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string _skillId = "skill_new";
        [SerializeField] private string _displayName = "New Skill";
        [TextArea] [SerializeField] private string _description = "";
        [SerializeField] private DamageKind _damageKind = DamageKind.Physical;
        [SerializeField] private ElementType _elementType = ElementType.Physical;
        [SerializeField] private CombatTargetType _targetType = CombatTargetType.SingleEnemy;
        [SerializeField] private ResourceType _resourceType = ResourceType.MP;
        [Min(0)] [SerializeField] private int _resourceCost = 0;
        [Min(0)] [SerializeField] private int _power = 8;
        [Min(0f)] [SerializeField] private float _attackScale = 1f;
        [Min(0f)] [SerializeField] private float _magicScale = 0f;
        [Range(0f, 1f)] [SerializeField] private float _critChanceBonus = 0f;
        [SerializeField] private bool _isHealingSkill;
        [Min(0)] [SerializeField] private int _flatHealing = 0;
        [SerializeField] private List<StatusApplicationDefinition> _appliedStatuses = new List<StatusApplicationDefinition>();

        public string SkillId => _skillId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public DamageKind DamageKind => _damageKind;
        public ElementType ElementType => _elementType;
        public CombatTargetType TargetType => _targetType;
        public ResourceType ResourceType => _resourceType;
        public int ResourceCost => _resourceCost;
        public int Power => _power;
        public float AttackScale => _attackScale;
        public float MagicScale => _magicScale;
        public float CritChanceBonus => _critChanceBonus;
        public bool IsHealingSkill => _isHealingSkill;
        public int FlatHealing => _flatHealing;
        public IReadOnlyList<StatusApplicationDefinition> AppliedStatuses => _appliedStatuses;
    }
}
