using System;
using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Quest;
using TPS.Runtime.Weather;
using UnityEngine;

namespace TPS.Runtime.Dialogue
{
    public enum DialogueActionType
    {
        None = 0,
        SetFlag = 1,
        ConsumeOneShot = 2,
        AcceptQuest = 3,
        TryCompleteQuest = 4,
        RecruitMember = 5,
        SetZoneFact = 6,
        AddCurrency = 7,
        SetWeather = 8
    }

    [Serializable]
    public sealed class DialogueActionDefinition
    {
        public DialogueActionType ActionType = DialogueActionType.None;
        public string FlagId;
        public bool BoolValue = true;
        public string OneShotId;
        public QuestDefinition Quest;
        public CharacterDefinition Character;
        public string ZoneId;
        public string ZoneFactId;
        public int CurrencyAmount;
        public WeatherType WeatherType = WeatherType.Sunny;
    }

    [Serializable]
    public sealed class DialogueChoiceDefinition
    {
        public string ChoiceId = "choice_new";
        public string Label = "Continue";
        public ConditionResolver Conditions = new ConditionResolver();
        public List<DialogueActionDefinition> Actions = new List<DialogueActionDefinition>();
    }

    [Serializable]
    public sealed class DialogueVariant
    {
        public string VariantId = "variant_new";
        public string SpeakerName = "NPC";
        [TextArea(2, 5)] public string Body = "";
        public ConditionResolver Conditions = new ConditionResolver();
        public bool OneShot = false;
        public string OneShotConsumptionId = "";
        public List<DialogueActionDefinition> Actions = new List<DialogueActionDefinition>();
        public List<DialogueChoiceDefinition> Choices = new List<DialogueChoiceDefinition>();
    }

    [CreateAssetMenu(fileName = "DLG_NewDialogue", menuName = "TPS/RPG/Dialogue")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField] private string _dialogueId = "dialogue_new";
        [SerializeField] private List<DialogueVariant> _variants = new List<DialogueVariant>();

        public string DialogueId => _dialogueId;
        public IReadOnlyList<DialogueVariant> Variants => _variants;
    }
}
