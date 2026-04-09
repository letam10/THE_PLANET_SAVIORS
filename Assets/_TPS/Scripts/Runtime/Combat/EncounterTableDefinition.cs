using System.Collections.Generic;
using UnityEngine;

namespace TPS.Runtime.Combat
{
    [CreateAssetMenu(fileName = "ECT_NewEncounterTable", menuName = "TPS/RPG/Encounter Table")]
    public sealed class EncounterTableDefinition : ScriptableObject
    {
        [SerializeField] private string _tableId = "table_new";
        [SerializeField] private List<WeightedEncounterEntry> _entries = new List<WeightedEncounterEntry>();

        public string TableId => _tableId;
        public IReadOnlyList<WeightedEncounterEntry> Entries => _entries;
    }
}
