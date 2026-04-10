using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [System.Serializable]
    public sealed class ComputedStats
    {
        [Min(1)] public int MaxHP = 1;
        [Min(0)] public int MaxMP = 0;
        public int Attack = 0;
        public int Magic = 0;
        public int Defense = 0;
        public int Resistance = 0;
        public int Speed = 0;

        public static ComputedStats FromBase(StatBlock baseStats)
        {
            if (baseStats == null)
            {
                return new ComputedStats();
            }

            return new ComputedStats
            {
                MaxHP = Mathf.Max(1, baseStats.MaxHP),
                MaxMP = Mathf.Max(0, baseStats.MaxMP),
                Attack = baseStats.Attack,
                Magic = baseStats.Magic,
                Defense = baseStats.Defense,
                Resistance = baseStats.Resistance,
                Speed = baseStats.Speed
            };
        }

        public StatBlock ToStatBlock()
        {
            return new StatBlock
            {
                MaxHP = MaxHP,
                MaxMP = MaxMP,
                Attack = Attack,
                Magic = Magic,
                Defense = Defense,
                Resistance = Resistance,
                Speed = Speed
            };
        }

        public void ApplyStatBlock(StatBlock statBlock)
        {
            if (statBlock == null)
            {
                return;
            }

            MaxHP += statBlock.MaxHP;
            MaxMP += statBlock.MaxMP;
            Attack += statBlock.Attack;
            Magic += statBlock.Magic;
            Defense += statBlock.Defense;
            Resistance += statBlock.Resistance;
            Speed += statBlock.Speed;
        }

        public void ApplyModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                return;
            }

            MaxHP += modifier.MaxHP;
            MaxMP += modifier.MaxMP;
            Attack += modifier.Attack;
            Magic += modifier.Magic;
            Defense += modifier.Defense;
            Resistance += modifier.Resistance;
            Speed += modifier.Speed;
        }

        public void Clamp()
        {
            MaxHP = Mathf.Max(1, MaxHP);
            MaxMP = Mathf.Max(0, MaxMP);
        }
    }

    public sealed class CharacterStatSnapshot
    {
        public string CharacterId;
        public string DisplayName;
        public int Level;
        public EquipmentDefinition EquippedWeapon;
        public ComputedStats Stats = new ComputedStats();
        public ResistanceProfile ResistanceProfile = new ResistanceProfile();
        public readonly List<SkillDefinition> Skills = new List<SkillDefinition>();
        public readonly List<string> ActiveUnlockIds = new List<string>();
    }
}
