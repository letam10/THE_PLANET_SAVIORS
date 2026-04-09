using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "STS_NewStatusEffect", menuName = "TPS/RPG/Status Effect")]
    public sealed class StatusEffectDefinition : ScriptableObject
    {
        [SerializeField] private string _statusId = "status_new";
        [SerializeField] private string _displayName = "New Status";
        [SerializeField] private CombatStatusType _statusType = CombatStatusType.Poison;
        [Min(1)] [SerializeField] private int _defaultDurationTurns = 2;

        public string StatusId => _statusId;
        public string DisplayName => _displayName;
        public CombatStatusType StatusType => _statusType;
        public int DefaultDurationTurns => _defaultDurationTurns;
    }
}
