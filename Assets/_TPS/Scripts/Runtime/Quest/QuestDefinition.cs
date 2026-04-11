using System;
using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using UnityEngine;

namespace TPS.Runtime.Quest
{
    public enum QuestStatus
    {
        NotStarted = 0,
        Active = 1,
        ReadyToTurnIn = 2,
        Completed = 3,
        Failed = 4
    }

    [Serializable]
    public sealed class QuestObjectiveDefinition
    {
        public string ObjectiveId = "objective_new";
        [TextArea] public string Description = "";
        public ConditionResolver CompletionConditions = new ConditionResolver();
    }

    [CreateAssetMenu(fileName = "QST_NewQuest", menuName = "TPS/RPG/Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string _questId = "quest_new";
        [SerializeField] private string _title = "New Quest";
        [TextArea] [SerializeField] private string _summary = "";
        [SerializeField] private List<QuestObjectiveDefinition> _objectives = new List<QuestObjectiveDefinition>();
        [SerializeField] private RewardTableDefinition _completionReward;
        [SerializeField] private CharacterDefinition _recruitedMemberReward;

        public string QuestId => _questId;
        public string Title => _title;
        public string Summary => _summary;
        public IReadOnlyList<QuestObjectiveDefinition> Objectives => _objectives;
        public RewardTableDefinition CompletionReward => _completionReward;
        public CharacterDefinition RecruitedMemberReward => _recruitedMemberReward;
    }
}
