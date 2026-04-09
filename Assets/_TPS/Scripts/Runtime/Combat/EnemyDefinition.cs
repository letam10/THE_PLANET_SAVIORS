using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "ENM_NewEnemy", menuName = "TPS/RPG/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _enemyId = "enemy_new";
        [SerializeField] private string _displayName = "New Enemy";
        [SerializeField] private StatBlock _stats = new StatBlock();
        [SerializeField] private ResistanceProfile _resistanceProfile = new ResistanceProfile();
        [SerializeField] private List<SkillDefinition> _skills = new List<SkillDefinition>();

        public string EnemyId => _enemyId;
        public string DisplayName => _displayName;
        public StatBlock Stats => _stats;
        public ResistanceProfile ResistanceProfile => _resistanceProfile;
        public IReadOnlyList<SkillDefinition> Skills => _skills;
    }
}
