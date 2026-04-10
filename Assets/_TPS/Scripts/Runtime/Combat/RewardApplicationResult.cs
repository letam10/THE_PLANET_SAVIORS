using System.Collections.Generic;

namespace TPS.Runtime.Combat
{
    public sealed class RewardApplicationResult
    {
        public int CurrencyGranted;
        public int ExpGrantedPerMember;
        public readonly List<string> ItemGrants = new List<string>();
        public readonly List<string> EquipmentGrants = new List<string>();
        public string Summary = "Reward applied.";
    }
}
