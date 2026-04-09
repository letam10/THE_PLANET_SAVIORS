using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Quest;
using TPS.Runtime.SaveLoad;
using TPS.Runtime.Weather;
using TPS.Runtime.World;
using UnityEngine;

namespace TPS.Runtime.Dialogue
{
    public sealed class DialogueStateService : MonoBehaviour
    {
        public static DialogueStateService Instance { get; private set; }

        [SerializeField] private Phase1ContentCatalog _contentCatalog;

        private readonly HashSet<string> _openFlags = new HashSet<string>();
        private readonly Dictionary<string, string> _chosenChoices = new Dictionary<string, string>();
        private readonly HashSet<string> _consumedOneShots = new HashSet<string>();
        private readonly Dictionary<string, int> _relationshipValues = new Dictionary<string, int>();

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

        public bool HasFlag(string flagId)
        {
            return !string.IsNullOrWhiteSpace(flagId) && _openFlags.Contains(flagId);
        }

        public void SetFlag(string flagId, bool value)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return;
            }

            bool changed = value ? _openFlags.Add(flagId) : _openFlags.Remove(flagId);
            if (changed)
            {
                GameEventBus.PublishDialogueStateChanged(flagId);
            }
        }

        public void RecordChoice(string dialogueId, string choiceId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId) || string.IsNullOrWhiteSpace(choiceId))
            {
                return;
            }

            if (_chosenChoices.TryGetValue(dialogueId, out string currentChoice) && currentChoice == choiceId)
            {
                return;
            }

            _chosenChoices[dialogueId] = choiceId;
            GameEventBus.PublishDialogueStateChanged(dialogueId);
        }

        public string GetRecordedChoice(string dialogueId)
        {
            return _chosenChoices.TryGetValue(dialogueId, out string choiceId) ? choiceId : string.Empty;
        }

        public bool HasConsumedOneShot(string oneShotId)
        {
            return !string.IsNullOrWhiteSpace(oneShotId) && _consumedOneShots.Contains(oneShotId);
        }

        public void ConsumeOneShot(string oneShotId)
        {
            if (string.IsNullOrWhiteSpace(oneShotId))
            {
                return;
            }

            if (_consumedOneShots.Add(oneShotId))
            {
                GameEventBus.PublishDialogueStateChanged(oneShotId);
            }
        }

        public DialogueVariant ResolveCurrentVariant(DialogueDefinition dialogueDefinition)
        {
            if (dialogueDefinition == null)
            {
                return null;
            }

            IReadOnlyList<DialogueVariant> variants = dialogueDefinition.Variants;
            for (int i = 0; i < variants.Count; i++)
            {
                DialogueVariant variant = variants[i];
                if (variant == null)
                {
                    continue;
                }

                string oneShotKey = BuildOneShotKey(dialogueDefinition.DialogueId, variant);
                if (variant.OneShot && HasConsumedOneShot(oneShotKey))
                {
                    continue;
                }

                if (variant.Conditions == null || variant.Conditions.EvaluateAll())
                {
                    return variant;
                }
            }

            return null;
        }

        public string Interact(DialogueDefinition dialogueDefinition)
        {
            DialogueVariant variant = ResolveCurrentVariant(dialogueDefinition);
            if (variant == null)
            {
                return string.Empty;
            }

            ApplyActions(variant.Actions);

            DialogueChoiceDefinition chosenChoice = SelectChoice(variant);
            if (chosenChoice != null)
            {
                RecordChoice(dialogueDefinition.DialogueId, chosenChoice.ChoiceId);
                ApplyActions(chosenChoice.Actions);
            }

            if (variant.OneShot)
            {
                ConsumeOneShot(BuildOneShotKey(dialogueDefinition.DialogueId, variant));
            }

            return variant.Body;
        }

        public DialogueStateData CaptureState()
        {
            var data = new DialogueStateData();

            foreach (string flag in _openFlags)
            {
                data.OpenFlags.Add(new StringListEntry { Value = flag });
            }

            foreach (var choice in _chosenChoices)
            {
                data.ChosenChoices.Add(new DialogueChoiceStateEntry
                {
                    DialogueId = choice.Key,
                    ChoiceId = choice.Value
                });
            }

            foreach (string oneShot in _consumedOneShots)
            {
                data.ConsumedOneShots.Add(new StringListEntry { Value = oneShot });
            }

            foreach (var relationship in _relationshipValues)
            {
                data.RelationshipValues.Add(new DialogueAffectionStateEntry
                {
                    EntityId = relationship.Key,
                    Value = relationship.Value
                });
            }

            return data;
        }

        public void RestoreState(DialogueStateData data)
        {
            _openFlags.Clear();
            _chosenChoices.Clear();
            _consumedOneShots.Clear();
            _relationshipValues.Clear();

            if (data == null)
            {
                return;
            }

            for (int i = 0; i < data.OpenFlags.Count; i++)
            {
                string value = data.OpenFlags[i].Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _openFlags.Add(value);
                }
            }

            for (int i = 0; i < data.ChosenChoices.Count; i++)
            {
                DialogueChoiceStateEntry entry = data.ChosenChoices[i];
                if (!string.IsNullOrWhiteSpace(entry.DialogueId) && !string.IsNullOrWhiteSpace(entry.ChoiceId))
                {
                    _chosenChoices[entry.DialogueId] = entry.ChoiceId;
                }
            }

            for (int i = 0; i < data.ConsumedOneShots.Count; i++)
            {
                string value = data.ConsumedOneShots[i].Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _consumedOneShots.Add(value);
                }
            }

            for (int i = 0; i < data.RelationshipValues.Count; i++)
            {
                DialogueAffectionStateEntry entry = data.RelationshipValues[i];
                if (!string.IsNullOrWhiteSpace(entry.EntityId))
                {
                    _relationshipValues[entry.EntityId] = entry.Value;
                }
            }
        }

        private DialogueChoiceDefinition SelectChoice(DialogueVariant variant)
        {
            if (variant == null || variant.Choices == null)
            {
                return null;
            }

            for (int i = 0; i < variant.Choices.Count; i++)
            {
                DialogueChoiceDefinition choice = variant.Choices[i];
                if (choice != null && (choice.Conditions == null || choice.Conditions.EvaluateAll()))
                {
                    return choice;
                }
            }

            return null;
        }

        private void ApplyActions(IReadOnlyList<DialogueActionDefinition> actions)
        {
            if (actions == null)
            {
                return;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                DialogueActionDefinition action = actions[i];
                if (action == null)
                {
                    continue;
                }

                switch (action.ActionType)
                {
                    case DialogueActionType.SetFlag:
                        SetFlag(action.FlagId, action.BoolValue);
                        break;

                    case DialogueActionType.ConsumeOneShot:
                        ConsumeOneShot(action.OneShotId);
                        break;

                    case DialogueActionType.AcceptQuest:
                        if (QuestService.Instance != null && action.Quest != null)
                        {
                            QuestService.Instance.AcceptQuest(action.Quest);
                        }
                        break;

                    case DialogueActionType.TryCompleteQuest:
                        if (QuestService.Instance != null && action.Quest != null)
                        {
                            QuestService.Instance.TryCompleteQuest(action.Quest);
                        }
                        break;

                    case DialogueActionType.RecruitMember:
                        if (PartyService.Instance != null && action.Character != null)
                        {
                            PartyService.Instance.RecruitMember(action.Character, true);
                        }
                        break;

                    case DialogueActionType.SetZoneFact:
                        if (ZoneStateService.Instance != null)
                        {
                            ZoneStateService.Instance.SetBoolFact(action.ZoneId, action.ZoneFactId, action.BoolValue);
                        }
                        break;

                    case DialogueActionType.AddCurrency:
                        if (EconomyService.Instance != null)
                        {
                            EconomyService.Instance.AddCurrency(action.CurrencyAmount);
                        }
                        break;

                    case DialogueActionType.SetWeather:
                        if (WeatherSystem.Instance != null)
                        {
                            WeatherSystem.Instance.SetWeather(action.WeatherType);
                        }
                        break;
                }
            }
        }

        private static string BuildOneShotKey(string dialogueId, DialogueVariant variant)
        {
            if (variant == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(variant.OneShotConsumptionId)
                ? variant.OneShotConsumptionId
                : $"{dialogueId}:{variant.VariantId}";
        }
    }
}
