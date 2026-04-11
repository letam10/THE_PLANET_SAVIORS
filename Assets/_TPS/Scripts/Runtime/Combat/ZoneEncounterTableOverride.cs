using System;
using TPS.Runtime.Conditions;

namespace TPS.Runtime.Combat
{
    [Serializable]
    public sealed class ZoneEncounterTableOverride
    {
        public ConditionResolver Conditions = new ConditionResolver();
        public EncounterTableDefinition EncounterTable;
    }
}
