using System;
using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;
using TPS.Runtime.World;
using UnityEngine;

namespace TPS.Runtime.Conditions
{
    public enum ConditionGroupMode
    {
        All,
        Any
    }

    public enum ConditionType
    {
        TimeRange,
        StateEquals,
        WeatherEquals,
        QuestState,
        DialogueFlag,
        EncounterCleared,
        InventoryCountAtLeast,
        PartyMemberRecruited,
        ZoneFactBoolEquals,
        CurrencyAtLeast
    }

    [Serializable]
    public class GameCondition
    {
        public ConditionType Type;

        [Header("Time Range")]
        [Range(0, 23)] public int StartHour;
        [Range(0, 23)] public int EndHour;

        [Header("Legacy GameState Bool")]
        public string StateKey;
        public bool ExpectedBool = true;

        [Header("Weather")]
        public WeatherType ExpectedWeather = WeatherType.Sunny;

        [Header("Quest")]
        public string QuestId;
        public QuestStatus ExpectedQuestStatus = QuestStatus.Active;

        [Header("Dialogue")]
        public string DialogueFlagId;

        [Header("Encounter")]
        public string EncounterId;

        [Header("Inventory")]
        public string InventoryDefinitionId;
        [Min(0)] public int MinimumCount = 1;

        [Header("Party")]
        public string PartyMemberId;

        [Header("Zone Fact")]
        public string ZoneId;
        public string ZoneFactId;

        [Header("Economy")]
        [Min(0)] public int MinimumCurrency = 0;

        public bool Evaluate()
        {
            switch (Type)
            {
                case ConditionType.TimeRange:
                    return EvaluateTimeRange();

                case ConditionType.StateEquals:
                    return GameStateManager.Instance != null && GameStateManager.Instance.GetBool(StateKey, false) == ExpectedBool;

                case ConditionType.WeatherEquals:
                    return WeatherSystem.Instance != null && WeatherSystem.Instance.CurrentWeather == ExpectedWeather;

                case ConditionType.QuestState:
                    return QuestService.Instance != null && QuestService.Instance.GetQuestStatus(QuestId) == ExpectedQuestStatus;

                case ConditionType.DialogueFlag:
                    return DialogueStateService.Instance != null && DialogueStateService.Instance.HasFlag(DialogueFlagId) == ExpectedBool;

                case ConditionType.EncounterCleared:
                    return EncounterService.Instance != null && EncounterService.Instance.IsEncounterCleared(EncounterId) == ExpectedBool;

                case ConditionType.InventoryCountAtLeast:
                    return InventoryService.Instance != null &&
                           (InventoryService.Instance.GetItemCount(InventoryDefinitionId) + InventoryService.Instance.GetEquipmentCount(InventoryDefinitionId)) >= MinimumCount;

                case ConditionType.PartyMemberRecruited:
                    return PartyService.Instance != null && PartyService.Instance.IsMemberRecruited(PartyMemberId) == ExpectedBool;

                case ConditionType.ZoneFactBoolEquals:
                    return ZoneStateService.Instance != null && ZoneStateService.Instance.GetBoolFact(ZoneId, ZoneFactId, false) == ExpectedBool;

                case ConditionType.CurrencyAtLeast:
                    return EconomyService.Instance != null && EconomyService.Instance.Currency >= MinimumCurrency;

                default:
                    return false;
            }
        }

        private bool EvaluateTimeRange()
        {
            if (WorldClock.Instance == null)
            {
                return false;
            }

            int currentHour = WorldClock.Instance.CurrentHour;
            if (StartHour <= EndHour)
            {
                return currentHour >= StartHour && currentHour <= EndHour;
            }

            return currentHour >= StartHour || currentHour <= EndHour;
        }
    }

    [Serializable]
    public class ConditionGroup
    {
        public ConditionGroupMode Mode = ConditionGroupMode.All;
        public List<GameCondition> Conditions = new List<GameCondition>();

        public bool Evaluate()
        {
            if (Conditions == null || Conditions.Count == 0)
            {
                return true;
            }

            if (Mode == ConditionGroupMode.All)
            {
                for (int i = 0; i < Conditions.Count; i++)
                {
                    GameCondition condition = Conditions[i];
                    if (condition != null && !condition.Evaluate())
                    {
                        return false;
                    }
                }

                return true;
            }

            for (int i = 0; i < Conditions.Count; i++)
            {
                GameCondition condition = Conditions[i];
                if (condition != null && condition.Evaluate())
                {
                    return true;
                }
            }

            return false;
        }
    }

    [CreateAssetMenu(fileName = "COND_NewCondition", menuName = "TPS/RPG/Condition Definition")]
    public sealed class ConditionDefinition : ScriptableObject
    {
        [SerializeField] private ConditionGroup _conditions = new ConditionGroup();

        public ConditionGroup Conditions => _conditions;
    }

    [Serializable]
    public class ConditionResolver
    {
        public ConditionGroupMode Mode = ConditionGroupMode.All;
        public List<GameCondition> Conditions = new List<GameCondition>();
        [SerializeField] private ConditionDefinition _sharedDefinition;
        public ConditionDefinition SharedDefinition => _sharedDefinition;

        public bool EvaluateAll()
        {
            if (_sharedDefinition != null && _sharedDefinition.Conditions != null)
            {
                return _sharedDefinition.Conditions.Evaluate();
            }

            var inlineGroup = new ConditionGroup
            {
                Mode = Mode,
                Conditions = Conditions
            };

            return inlineGroup.Evaluate();
        }
    }
}
