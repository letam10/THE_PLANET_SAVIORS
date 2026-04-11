using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "ZON_NewZone", menuName = "TPS/RPG/Zone")]
    public sealed class ZoneDefinition : ScriptableObject
    {
        [SerializeField] private string _zoneId = "zone_new";
        [SerializeField] private string _displayName = "New Zone";
        [SerializeField] private EncounterTableDefinition _defaultEncounterTable;
        [SerializeField] private List<ZoneEncounterTableOverride> _encounterTableOverrides = new List<ZoneEncounterTableOverride>();

        public string ZoneId => _zoneId;
        public string DisplayName => _displayName;
        public EncounterTableDefinition DefaultEncounterTable => _defaultEncounterTable;
        public IReadOnlyList<ZoneEncounterTableOverride> EncounterTableOverrides => _encounterTableOverrides;
    }
}
