using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "ENC_NewEncounter", menuName = "TPS/RPG/Encounter")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        [SerializeField] private string _encounterId = "encounter_new";
        [SerializeField] private string _displayName = "New Encounter";
        [SerializeField] private string _zoneId = "zone_unknown";
        [SerializeField] private string _battleSceneName = "BTL_Standard";
        [SerializeField] private bool _countsAsClear = true;
        [SerializeField] private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();
        [SerializeField] private RewardTableDefinition _rewardTable;

        public string EncounterId => _encounterId;
        public string DisplayName => _displayName;
        public string ZoneId => _zoneId;
        public string BattleSceneName => _battleSceneName;
        public bool CountsAsClear => _countsAsClear;
        public IReadOnlyList<EnemyDefinition> Enemies => _enemies;
        public RewardTableDefinition RewardTable => _rewardTable;
    }
}
