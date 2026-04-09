using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using UnityEngine;

namespace TPS.Runtime.World
{
    public sealed class EncounterService : MonoBehaviour
    {
        public sealed class PendingEncounterContext
        {
            public EncounterDefinition EncounterDefinition;
            public string ReturnSceneName;
            public Vector3 ReturnPosition;
            public Quaternion ReturnRotation;
            public string ZoneId;
        }

        public static EncounterService Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private readonly HashSet<string> _clearedEncounterIds = new HashSet<string>();
        private PendingEncounterContext _pendingEncounter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool IsEncounterCleared(string encounterId)
        {
            return !string.IsNullOrWhiteSpace(encounterId) && _clearedEncounterIds.Contains(encounterId);
        }

        public void MarkEncounterCleared(EncounterDefinition encounterDefinition)
        {
            if (encounterDefinition == null || string.IsNullOrWhiteSpace(encounterDefinition.EncounterId))
            {
                return;
            }

            if (_clearedEncounterIds.Add(encounterDefinition.EncounterId))
            {
                GameEventBus.PublishEncounterResolved(encounterDefinition.EncounterId, true);
            }
        }

        public EncounterTableDefinition GetResolvedEncounterTable(string zoneId)
        {
            if (_contentCatalog == null)
            {
                return null;
            }

            ZoneDefinition zoneDefinition = _contentCatalog.GetZone(zoneId);
            if (zoneDefinition == null)
            {
                return null;
            }

            IReadOnlyList<ZoneEncounterTableOverride> overrides = zoneDefinition.EncounterTableOverrides;
            for (int i = 0; i < overrides.Count; i++)
            {
                ZoneEncounterTableOverride tableOverride = overrides[i];
                if (tableOverride != null && tableOverride.EncounterTable != null && (tableOverride.Conditions == null || tableOverride.Conditions.EvaluateAll()))
                {
                    return tableOverride.EncounterTable;
                }
            }

            return zoneDefinition.DefaultEncounterTable;
        }

        public EncounterDefinition RollEncounterForZone(string zoneId)
        {
            EncounterTableDefinition table = GetResolvedEncounterTable(zoneId);
            if (table == null || table.Entries == null || table.Entries.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            IReadOnlyList<WeightedEncounterEntry> entries = table.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].Encounter != null)
                {
                    totalWeight += Mathf.Max(0, entries[i].Weight);
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, totalWeight);
            int cursor = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                WeightedEncounterEntry entry = entries[i];
                if (entry == null || entry.Encounter == null)
                {
                    continue;
                }

                cursor += Mathf.Max(0, entry.Weight);
                if (roll < cursor)
                {
                    return entry.Encounter;
                }
            }

            return null;
        }

        public void BeginEncounter(EncounterDefinition encounterDefinition, string returnSceneName, Vector3 returnPosition, Quaternion returnRotation)
        {
            if (encounterDefinition == null)
            {
                return;
            }

            _pendingEncounter = new PendingEncounterContext
            {
                EncounterDefinition = encounterDefinition,
                ReturnSceneName = returnSceneName,
                ReturnPosition = returnPosition,
                ReturnRotation = returnRotation,
                ZoneId = encounterDefinition.ZoneId
            };
        }

        public bool TryGetPendingEncounter(out PendingEncounterContext context)
        {
            context = _pendingEncounter;
            return context != null && context.EncounterDefinition != null;
        }

        public void ClearPendingEncounter()
        {
            _pendingEncounter = null;
        }

        public void MirrorResolvedStateToGameState()
        {
            if (_contentCatalog == null || GameStateManager.Instance == null)
            {
                return;
            }

            IReadOnlyList<ZoneDefinition> zones = _contentCatalog.Zones;
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneDefinition zone = zones[i];
                if (zone == null)
                {
                    continue;
                }

                EncounterTableDefinition encounterTable = GetResolvedEncounterTable(zone.ZoneId);
                if (encounterTable != null)
                {
                    GameStateManager.Instance.SetString($"zone.{zone.ZoneId}.encounter_table", encounterTable.TableId);
                }
            }
        }

        public EncounterStateData CaptureState()
        {
            var data = new EncounterStateData();
            foreach (string encounterId in _clearedEncounterIds)
            {
                data.ClearedEncounterIds.Add(new StringListEntry { Value = encounterId });
            }

            return data;
        }

        public void RestoreState(EncounterStateData data)
        {
            _clearedEncounterIds.Clear();
            _pendingEncounter = null;

            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.ClearedEncounterIds.Count; i++)
            {
                string encounterId = data.ClearedEncounterIds[i].Value;
                if (!string.IsNullOrWhiteSpace(encounterId))
                {
                    _clearedEncounterIds.Add(encounterId);
                }
            }
        }
    }
}
