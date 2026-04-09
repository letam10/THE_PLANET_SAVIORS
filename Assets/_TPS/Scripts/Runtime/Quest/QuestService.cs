using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.SaveLoad;
using UnityEngine;

namespace TPS.Runtime.Quest
{
    public sealed class QuestService : MonoBehaviour
    {
        private sealed class QuestRuntimeState
        {
            public QuestDefinition Definition;
            public QuestStatus Status = QuestStatus.NotStarted;
            public readonly HashSet<string> CompletedObjectives = new HashSet<string>();
        }

        public static QuestService Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private readonly Dictionary<string, QuestRuntimeState> _questStates = new Dictionary<string, QuestRuntimeState>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RegisterCatalogDefinitions();
        }

        public QuestStatus GetQuestStatus(string questId)
        {
            return _questStates.TryGetValue(questId, out QuestRuntimeState state) ? state.Status : QuestStatus.NotStarted;
        }

        public bool IsObjectiveComplete(string questId, string objectiveId)
        {
            return _questStates.TryGetValue(questId, out QuestRuntimeState state) && state.CompletedObjectives.Contains(objectiveId);
        }

        public void AcceptQuest(QuestDefinition questDefinition)
        {
            if (questDefinition == null)
            {
                return;
            }

            QuestRuntimeState state = GetOrCreateState(questDefinition);
            if (state.Status != QuestStatus.NotStarted)
            {
                return;
            }

            state.Status = QuestStatus.Active;
            GameEventBus.PublishQuestChanged(questDefinition.QuestId);
            RefreshQuestProgress();
        }

        public bool TryCompleteQuest(QuestDefinition questDefinition)
        {
            if (questDefinition == null)
            {
                return false;
            }

            QuestRuntimeState state = GetOrCreateState(questDefinition);
            if (state.Status != QuestStatus.ReadyToTurnIn)
            {
                RefreshQuestProgress();
            }

            if (state.Status != QuestStatus.ReadyToTurnIn)
            {
                return false;
            }

            if (RewardService.Instance != null && questDefinition.CompletionReward != null)
            {
                RewardService.Instance.ApplyRewardTable(questDefinition.CompletionReward, PartyService.Instance != null ? PartyService.Instance.GetActiveMemberIds() : null);
            }

            if (PartyService.Instance != null && questDefinition.RecruitedMemberReward != null)
            {
                PartyService.Instance.RecruitMember(questDefinition.RecruitedMemberReward, true);
            }

            state.Status = QuestStatus.Completed;
            GameEventBus.PublishQuestChanged(questDefinition.QuestId);
            return true;
        }

        public void RefreshQuestProgress()
        {
            foreach (var pair in _questStates)
            {
                QuestRuntimeState state = pair.Value;
                if (state == null || state.Definition == null || state.Status != QuestStatus.Active)
                {
                    continue;
                }

                bool changed = false;
                IReadOnlyList<QuestObjectiveDefinition> objectives = state.Definition.Objectives;
                for (int i = 0; i < objectives.Count; i++)
                {
                    QuestObjectiveDefinition objective = objectives[i];
                    if (objective == null || string.IsNullOrWhiteSpace(objective.ObjectiveId) || state.CompletedObjectives.Contains(objective.ObjectiveId))
                    {
                        continue;
                    }

                    if (objective.CompletionConditions == null || objective.CompletionConditions.EvaluateAll())
                    {
                        state.CompletedObjectives.Add(objective.ObjectiveId);
                        changed = true;
                    }
                }

                if (objectives.Count > 0 && state.CompletedObjectives.Count >= objectives.Count && state.Status != QuestStatus.ReadyToTurnIn)
                {
                    state.Status = QuestStatus.ReadyToTurnIn;
                    changed = true;
                }

                if (changed)
                {
                    GameEventBus.PublishQuestChanged(state.Definition.QuestId);
                }
            }
        }

        public QuestStateData CaptureState()
        {
            var data = new QuestStateData();
            foreach (var pair in _questStates)
            {
                QuestRuntimeState state = pair.Value;
                if (state == null || state.Definition == null || state.Status == QuestStatus.NotStarted)
                {
                    continue;
                }

                var entry = new QuestProgressStateEntry
                {
                    QuestId = state.Definition.QuestId,
                    Status = state.Status
                };

                foreach (string objectiveId in state.CompletedObjectives)
                {
                    entry.CompletedObjectiveIds.Add(objectiveId);
                }

                data.Quests.Add(entry);
            }

            return data;
        }

        public void RestoreState(QuestStateData data)
        {
            _questStates.Clear();
            RegisterCatalogDefinitions();

            if (data == null || _contentCatalog == null)
            {
                return;
            }

            for (int i = 0; i < data.Quests.Count; i++)
            {
                QuestProgressStateEntry entry = data.Quests[i];
                QuestDefinition definition = _contentCatalog.GetQuest(entry.QuestId);
                if (definition == null)
                {
                    continue;
                }

                QuestRuntimeState state = GetOrCreateState(definition);
                state.Status = entry.Status;
                state.CompletedObjectives.Clear();
                for (int objectiveIndex = 0; objectiveIndex < entry.CompletedObjectiveIds.Count; objectiveIndex++)
                {
                    string objectiveId = entry.CompletedObjectiveIds[objectiveIndex];
                    if (!string.IsNullOrWhiteSpace(objectiveId))
                    {
                        state.CompletedObjectives.Add(objectiveId);
                    }
                }
            }
        }

        private void RegisterCatalogDefinitions()
        {
            if (_contentCatalog == null)
            {
                return;
            }

            IReadOnlyList<QuestDefinition> quests = _contentCatalog.Quests;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest != null)
                {
                    GetOrCreateState(quest);
                }
            }
        }

        private QuestRuntimeState GetOrCreateState(QuestDefinition definition)
        {
            if (!_questStates.TryGetValue(definition.QuestId, out QuestRuntimeState state))
            {
                state = new QuestRuntimeState
                {
                    Definition = definition
                };
                _questStates[definition.QuestId] = state;
            }

            return state;
        }
    }
}
