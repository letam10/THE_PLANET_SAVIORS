using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    public enum StatType
    {
        MaxHP = 0,
        MaxMP = 1,
        Attack = 2,
        Magic = 3,
        Defense = 4,
        Resistance = 5,
        Speed = 6
    }

    public enum DamageKind
    {
        Physical = 0,
        Magical = 1,
        Pure = 2
    }

    public enum ElementType
    {
        Physical = 0,
        Fire = 1,
        Ice = 2,
        Lightning = 3
    }

    public enum ResourceType
    {
        None = 0,
        MP = 1
    }

    public enum WeaponFamilyType
    {
        None = 0,
        Blade = 1,
        Polearm = 2,
        Bow = 3,
        Focus = 4
    }

    public enum EquipmentSlotType
    {
        Weapon = 0,
        Armor = 1,
        Accessory = 2
    }

    public enum CombatStatusType
    {
        None = 0,
        Poison = 1,
        Burn = 2,
        Wet = 3,
        GuardBreak = 4
    }

    public enum CombatTargetType
    {
        SingleEnemy = 0,
        AllEnemies = 1,
        Self = 2,
        SingleAlly = 3,
        AllAllies = 4
    }

    [Serializable]
    public sealed class StatBlock
    {
        [Min(1)] public int MaxHP = 30;
        [Min(0)] public int MaxMP = 10;
        public int Attack = 8;
        public int Magic = 6;
        public int Defense = 5;
        public int Resistance = 5;
        public int Speed = 6;

        public int Get(StatType statType)
        {
            switch (statType)
            {
                case StatType.MaxHP: return MaxHP;
                case StatType.MaxMP: return MaxMP;
                case StatType.Attack: return Attack;
                case StatType.Magic: return Magic;
                case StatType.Defense: return Defense;
                case StatType.Resistance: return Resistance;
                case StatType.Speed: return Speed;
                default: return 0;
            }
        }

        public void Set(StatType statType, int value)
        {
            switch (statType)
            {
                case StatType.MaxHP: MaxHP = Mathf.Max(1, value); break;
                case StatType.MaxMP: MaxMP = Mathf.Max(0, value); break;
                case StatType.Attack: Attack = value; break;
                case StatType.Magic: Magic = value; break;
                case StatType.Defense: Defense = value; break;
                case StatType.Resistance: Resistance = value; break;
                case StatType.Speed: Speed = value; break;
            }
        }

        public StatBlock Clone()
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

        public void Add(StatBlock other)
        {
            if (other == null)
            {
                return;
            }

            MaxHP += other.MaxHP;
            MaxMP += other.MaxMP;
            Attack += other.Attack;
            Magic += other.Magic;
            Defense += other.Defense;
            Resistance += other.Resistance;
            Speed += other.Speed;
        }
    }

    [Serializable]
    public sealed class StatModifier
    {
        public int MaxHP = 0;
        public int MaxMP = 0;
        public int Attack = 0;
        public int Magic = 0;
        public int Defense = 0;
        public int Resistance = 0;
        public int Speed = 0;
    }

    [Serializable]
    public sealed class ResistanceProfile
    {
        [Min(0f)] public float Physical = 1f;
        [Min(0f)] public float Fire = 1f;
        [Min(0f)] public float Ice = 1f;
        [Min(0f)] public float Lightning = 1f;

        public float GetMultiplier(ElementType elementType)
        {
            switch (elementType)
            {
                case ElementType.Fire: return Fire;
                case ElementType.Ice: return Ice;
                case ElementType.Lightning: return Lightning;
                case ElementType.Physical:
                default:
                    return Physical;
            }
        }

        public ResistanceProfile Clone()
        {
            return new ResistanceProfile
            {
                Physical = Physical,
                Fire = Fire,
                Ice = Ice,
                Lightning = Lightning
            };
        }

        public void Add(ResistanceProfile other)
        {
            if (other == null)
            {
                return;
            }

            Physical *= other.Physical;
            Fire *= other.Fire;
            Ice *= other.Ice;
            Lightning *= other.Lightning;
        }
    }

    [Serializable]
    public sealed class StatusApplicationDefinition
    {
        public CombatStatusType StatusType = CombatStatusType.None;
        [Min(1)] public int DurationTurns = 2;
        [Range(0f, 1f)] public float Chance = 1f;
    }

    [Serializable]
    public sealed class WeightedDropEntry
    {
        [Min(1)] public int Weight = 1;
        public ItemDefinition Item;
        public EquipmentDefinition Equipment;
        [Min(1)] public int Amount = 1;
    }

    [Serializable]
    public sealed class ItemGrantDefinition
    {
        public ItemDefinition Item;
        [Min(1)] public int Amount = 1;
    }

    [Serializable]
    public sealed class EquipmentGrantDefinition
    {
        public EquipmentDefinition Equipment;
        [Min(1)] public int Amount = 1;
    }

    [Serializable]
    public sealed class CombatStatusRuntimeData
    {
        public CombatStatusType StatusType = CombatStatusType.None;
        public int RemainingTurns;
    }

    [Serializable]
    public sealed class SkillUnlockDefinition
    {
        public string UnlockId = "unlock_new";
        public SkillDefinition Skill;
        [Min(1)] public int RequiredLevel = 1;
        public WeaponFamilyType RequiredWeaponFamily = WeaponFamilyType.None;
        public StatModifier PassiveStatModifier = new StatModifier();
        public ResistanceProfile PassiveResistanceModifier = new ResistanceProfile();
    }

    [Serializable]
    public sealed class ShopEntryDefinition
    {
        public ItemDefinition Item;
        public EquipmentDefinition Equipment;
        [Min(0)] public int Stock = 0;
        [Min(-1)] public int PriceOverride = -1;
    }

    [Serializable]
    public sealed class WeightedEncounterEntry
    {
        [Min(1)] public int Weight = 1;
        public EncounterDefinition Encounter;
    }
}
