using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "ARC_NewArchetype", menuName = "TPS/RPG/Character Archetype")]
    public sealed class CharacterArchetypeDefinition : ScriptableObject
    {
        [SerializeField] private string _archetypeId = "archetype_new";
        [SerializeField] private string _displayName = "New Archetype";
        [SerializeField] private StatBlock _baseStats = new StatBlock();
        [SerializeField] private StatBlock _growthStats = new StatBlock();
        [SerializeField] private ResistanceProfile _baseResistance = new ResistanceProfile();
        [SerializeField] private List<SkillUnlockDefinition> _skillUnlocks = new List<SkillUnlockDefinition>();

        public string ArchetypeId => _archetypeId;
        public string DisplayName => _displayName;
        public StatBlock BaseStats => _baseStats;
        public StatBlock GrowthStats => _growthStats;
        public ResistanceProfile BaseResistance => _baseResistance;
        public IReadOnlyList<SkillUnlockDefinition> SkillUnlocks => _skillUnlocks;
    }
}
