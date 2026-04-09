using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    public struct DamageFormulaInput
    {
        public DamageKind DamageKind;
        public ElementType ElementType;
        public int Power;
        public float AttackScale;
        public float MagicScale;
        public int Attack;
        public int Magic;
        public int Defense;
        public int Resistance;
        public int WeaponPower;
        public ResistanceProfile TargetResistance;
        public IReadOnlyList<CombatStatusRuntimeData> TargetStatuses;
        public float CritChanceBonus;
    }

    public static class CombatFormula
    {
        public static int CalculateDamage(DamageFormulaInput input, out bool wasCritical, out float elementMultiplier)
        {
            float offensiveStat = input.DamageKind == DamageKind.Magical
                ? input.Magic * Mathf.Max(0f, input.MagicScale)
                : input.Attack * Mathf.Max(0f, input.AttackScale);

            float baseValue = input.Power + offensiveStat + input.WeaponPower;
            float mitigation = input.DamageKind == DamageKind.Magical ? input.Resistance * 0.8f : input.Defense * 0.8f;

            if (HasStatus(input.TargetStatuses, CombatStatusType.GuardBreak))
            {
                mitigation *= 0.75f;
            }

            elementMultiplier = input.TargetResistance != null ? input.TargetResistance.GetMultiplier(input.ElementType) : 1f;
            if (HasStatus(input.TargetStatuses, CombatStatusType.Wet))
            {
                if (input.ElementType == ElementType.Lightning) elementMultiplier *= 1.5f;
                if (input.ElementType == ElementType.Fire) elementMultiplier *= 0.75f;
            }

            float critChance = Mathf.Clamp01(0.1f + input.CritChanceBonus);
            wasCritical = Random.value <= critChance;
            float critMultiplier = wasCritical ? 1.5f : 1f;

            float finalValue = Mathf.Max(1f, (baseValue - mitigation) * elementMultiplier * critMultiplier);
            return Mathf.Max(1, Mathf.RoundToInt(finalValue));
        }

        public static int CalculateHealing(int flatHealing, int magicStat)
        {
            return Mathf.Max(1, flatHealing + Mathf.RoundToInt(magicStat * 0.75f));
        }

        public static int CalculateStatusTickDamage(CombatStatusType statusType, int maxHP)
        {
            switch (statusType)
            {
                case CombatStatusType.Poison:
                    return Mathf.Max(1, Mathf.RoundToInt(maxHP * 0.08f));
                case CombatStatusType.Burn:
                    return Mathf.Max(1, Mathf.RoundToInt(maxHP * 0.10f));
                default:
                    return 0;
            }
        }

        public static bool HasStatus(IReadOnlyList<CombatStatusRuntimeData> statuses, CombatStatusType statusType)
        {
            if (statuses == null)
            {
                return false;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                CombatStatusRuntimeData status = statuses[i];
                if (status != null && status.StatusType == statusType && status.RemainingTurns > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
