using System;
using System.Collections.Generic;

namespace TPS.Runtime.Combat
{
    [Serializable]
    public sealed class BattleParticipantResult
    {
        public string UnitId;
        public int CurrentHP;
        public int CurrentMP;
        public bool IsKnockedOut;
    }

    [Serializable]
    public sealed class BattleResult
    {
        public string EncounterId;
        public bool Victory;
        public int TurnsTaken;
        public string RewardSummary;
        public List<BattleParticipantResult> PartyResults = new List<BattleParticipantResult>();
    }
}
