using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "CHR_NewCharacter", menuName = "TPS/RPG/Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string _characterId = "character_new";
        [SerializeField] private string _displayName = "New Character";
        [SerializeField] private CharacterArchetypeDefinition _archetype;
        [Min(1)] [SerializeField] private int _startingLevel = 1;
        [SerializeField] private EquipmentDefinition _startingWeapon;
        [SerializeField] private List<SkillDefinition> _startingSkills = new List<SkillDefinition>();
        [SerializeField] private ResistanceProfile _resistanceModifier = new ResistanceProfile();

        public string CharacterId => _characterId;
        public string DisplayName => _displayName;
        public CharacterArchetypeDefinition Archetype => _archetype;
        public int StartingLevel => _startingLevel;
        public EquipmentDefinition StartingWeapon => _startingWeapon;
        public IReadOnlyList<SkillDefinition> StartingSkills => _startingSkills;
        public ResistanceProfile ResistanceModifier => _resistanceModifier;
    }
}
