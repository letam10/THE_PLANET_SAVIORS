using System;
using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;

namespace TPS.Editor
{
    internal static class PhaseContentValidationRules
    {
        private sealed class CatalogReferenceIndex
        {
            public readonly HashSet<string> CharacterIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> EnemyIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> ItemIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> EquipmentIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> RewardIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> EncounterIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> ZoneIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> QuestIds = new HashSet<string>(StringComparer.Ordinal);
        }

        public static void ValidateAdditionalRules(Phase1ContentCatalog catalog, ContentValidationResult result)
        {
            if (catalog == null)
            {
                return;
            }

            CatalogReferenceIndex refs = BuildReferenceIndex(catalog);
            ValidateCharacters(catalog.Characters, result);
            ValidateRewards(catalog.RewardTables, result);
            ValidateDialogues(catalog.Dialogues, refs, result);
            ValidateQuests(catalog.Quests, refs, result);
            ValidateEncounters(catalog.Encounters, refs, result);
            ValidateZones(catalog.Zones, refs, result);
            ValidateShops(catalog.Shops, refs, result);
            ValidateEnemies(catalog.Enemies, result);
            ValidateSkills(catalog.Skills, result);
        }

        private static CatalogReferenceIndex BuildReferenceIndex(Phase1ContentCatalog catalog)
        {
            var refs = new CatalogReferenceIndex();
            AddIds(catalog.Characters, d => d != null ? d.CharacterId : string.Empty, refs.CharacterIds);
            AddIds(catalog.Enemies, d => d != null ? d.EnemyId : string.Empty, refs.EnemyIds);
            AddIds(catalog.Items, d => d != null ? d.ItemId : string.Empty, refs.ItemIds);
            AddIds(catalog.Equipment, d => d != null ? d.EquipmentId : string.Empty, refs.EquipmentIds);
            AddIds(catalog.RewardTables, d => d != null ? d.RewardId : string.Empty, refs.RewardIds);
            AddIds(catalog.Encounters, d => d != null ? d.EncounterId : string.Empty, refs.EncounterIds);
            AddIds(catalog.Zones, d => d != null ? d.ZoneId : string.Empty, refs.ZoneIds);
            AddIds(catalog.Quests, d => d != null ? d.QuestId : string.Empty, refs.QuestIds);
            return refs;
        }

        private static void ValidateCharacters(IReadOnlyList<CharacterDefinition> characters, ContentValidationResult result)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition character = characters[i];
                if (character == null)
                {
                    continue;
                }

                if (character.Archetype == null)
                {
                    result.Errors.Add($"Character '{character.CharacterId}' is missing an archetype.");
                }

                if (character.StartingWeapon != null && character.StartingWeapon.SlotType != EquipmentSlotType.Weapon)
                {
                    result.Errors.Add($"Character '{character.CharacterId}' has a starting equipment that is not a weapon.");
                }
            }
        }

        private static void ValidateRewards(IReadOnlyList<RewardTableDefinition> rewards, ContentValidationResult result)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                RewardTableDefinition reward = rewards[i];
                if (reward == null)
                {
                    continue;
                }

                if (reward.CurrencyReward <= 0 && reward.ExpReward <= 0 && reward.GuaranteedItems.Count == 0 && reward.GuaranteedEquipment.Count == 0 && reward.WeightedDrops.Count == 0)
                {
                    result.Warnings.Add($"Reward '{reward.RewardId}' grants nothing.");
                }

                ValidateItemGrants(reward.RewardId, reward.GuaranteedItems, result);
                ValidateEquipmentGrants(reward.RewardId, reward.GuaranteedEquipment, result);

                IReadOnlyList<WeightedDropEntry> drops = reward.WeightedDrops;
                for (int dropIndex = 0; dropIndex < drops.Count; dropIndex++)
                {
                    WeightedDropEntry drop = drops[dropIndex];
                    if (drop == null)
                    {
                        result.Errors.Add($"Reward '{reward.RewardId}' has a null weighted drop entry.");
                        continue;
                    }

                    bool hasItem = drop.Item != null;
                    bool hasEquipment = drop.Equipment != null;
                    if (!hasItem && !hasEquipment)
                    {
                        result.Errors.Add($"Reward '{reward.RewardId}' has a weighted drop with no item or equipment assigned.");
                    }
                    else if (hasItem && hasEquipment)
                    {
                        result.Errors.Add($"Reward '{reward.RewardId}' has a weighted drop that assigns both item and equipment.");
                    }

                    if (drop.Weight <= 0)
                    {
                        result.Errors.Add($"Reward '{reward.RewardId}' has a weighted drop with non-positive weight.");
                    }
                }
            }
        }

        private static void ValidateDialogues(IReadOnlyList<DialogueDefinition> dialogues, CatalogReferenceIndex refs, ContentValidationResult result)
        {
            for (int i = 0; i < dialogues.Count; i++)
            {
                DialogueDefinition dialogue = dialogues[i];
                if (dialogue == null)
                {
                    continue;
                }

                IReadOnlyList<DialogueVariant> variants = dialogue.Variants;
                for (int variantIndex = 0; variantIndex < variants.Count; variantIndex++)
                {
                    DialogueVariant variant = variants[variantIndex];
                    if (variant == null)
                    {
                        continue;
                    }

                    ValidateConditions($"Dialogue '{dialogue.DialogueId}' variant '{variant.VariantId}'", variant.Conditions, refs, result);
                    ValidateDialogueActions($"Dialogue '{dialogue.DialogueId}' variant '{variant.VariantId}'", variant.Actions, refs, result);

                    IReadOnlyList<DialogueChoiceDefinition> choices = variant.Choices;
                    for (int choiceIndex = 0; choiceIndex < choices.Count; choiceIndex++)
                    {
                        DialogueChoiceDefinition choice = choices[choiceIndex];
                        if (choice == null)
                        {
                            result.Errors.Add($"Dialogue '{dialogue.DialogueId}' variant '{variant.VariantId}' has a null choice.");
                            continue;
                        }

                        ValidateConditions($"Dialogue '{dialogue.DialogueId}' choice '{choice.ChoiceId}'", choice.Conditions, refs, result);
                        ValidateDialogueActions($"Dialogue '{dialogue.DialogueId}' choice '{choice.ChoiceId}'", choice.Actions, refs, result);
                    }
                }
            }
        }

        private static void ValidateQuests(IReadOnlyList<QuestDefinition> quests, CatalogReferenceIndex refs, ContentValidationResult result)
        {
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null)
                {
                    continue;
                }

                IReadOnlyList<QuestObjectiveDefinition> objectives = quest.Objectives;
                for (int objectiveIndex = 0; objectiveIndex < objectives.Count; objectiveIndex++)
                {
                    QuestObjectiveDefinition objective = objectives[objectiveIndex];
                    if (objective == null)
                    {
                        continue;
                    }

                    ValidateConditions($"Quest '{quest.QuestId}' objective '{objective.ObjectiveId}'", objective.CompletionConditions, refs, result);
                }
            }
        }

        private static void ValidateEncounters(IReadOnlyList<EncounterDefinition> encounters, CatalogReferenceIndex refs, ContentValidationResult result)
        {
            for (int i = 0; i < encounters.Count; i++)
            {
                EncounterDefinition encounter = encounters[i];
                if (encounter == null)
                {
                    continue;
                }

                if (!refs.ZoneIds.Contains(encounter.ZoneId))
                {
                    result.Errors.Add($"Encounter '{encounter.EncounterId}' references missing zone '{encounter.ZoneId}'.");
                }
            }
        }

        private static void ValidateZones(IReadOnlyList<ZoneDefinition> zones, CatalogReferenceIndex refs, ContentValidationResult result)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneDefinition zone = zones[i];
                if (zone == null)
                {
                    continue;
                }

                if (zone.DefaultEncounterTable == null)
                {
                    result.Errors.Add($"Zone '{zone.ZoneId}' is missing a default encounter table.");
                }

                IReadOnlyList<ZoneEncounterTableOverride> overrides = zone.EncounterTableOverrides;
                for (int overrideIndex = 0; overrideIndex < overrides.Count; overrideIndex++)
                {
                    ZoneEncounterTableOverride tableOverride = overrides[overrideIndex];
                    if (tableOverride == null)
                    {
                        result.Errors.Add($"Zone '{zone.ZoneId}' has a null encounter-table override.");
                        continue;
                    }

                    if (tableOverride.EncounterTable == null)
                    {
                        result.Errors.Add($"Zone '{zone.ZoneId}' has an override without an encounter table.");
                    }

                    ValidateConditions($"Zone '{zone.ZoneId}' encounter override", tableOverride.Conditions, refs, result);
                }
            }
        }

        private static void ValidateShops(IReadOnlyList<ShopDefinition> shops, CatalogReferenceIndex refs, ContentValidationResult result)
        {
            for (int i = 0; i < shops.Count; i++)
            {
                ShopDefinition shop = shops[i];
                if (shop == null)
                {
                    continue;
                }

                ValidateConditions($"Shop '{shop.ShopId}' availability", shop.AvailabilityConditions, refs, result);
            }
        }

        private static void ValidateEnemies(IReadOnlyList<EnemyDefinition> enemies, ContentValidationResult result)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyDefinition enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                IReadOnlyList<SkillDefinition> skills = enemy.Skills;
                for (int skillIndex = 0; skillIndex < skills.Count; skillIndex++)
                {
                    if (skills[skillIndex] == null)
                    {
                        result.Errors.Add($"Enemy '{enemy.EnemyId}' has a null skill reference.");
                    }
                }
            }
        }

        private static void ValidateSkills(IReadOnlyList<SkillDefinition> skills, ContentValidationResult result)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null)
                {
                    continue;
                }

                IReadOnlyList<StatusApplicationDefinition> statuses = skill.AppliedStatuses;
                for (int statusIndex = 0; statusIndex < statuses.Count; statusIndex++)
                {
                    StatusApplicationDefinition status = statuses[statusIndex];
                    if (status == null)
                    {
                        result.Errors.Add($"Skill '{skill.SkillId}' has a null status application.");
                    }
                    else if (status.StatusType == CombatStatusType.None)
                    {
                        result.Errors.Add($"Skill '{skill.SkillId}' applies status type None.");
                    }
                }
            }
        }

        private static void ValidateDialogueActions(string context, IReadOnlyList<DialogueActionDefinition> actions, CatalogReferenceIndex refs, ContentValidationResult result)
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
                    result.Errors.Add($"{context} contains a null dialogue action.");
                    continue;
                }

                switch (action.ActionType)
                {
                    case DialogueActionType.AcceptQuest:
                    case DialogueActionType.TryCompleteQuest:
                        if (action.Quest == null || !refs.QuestIds.Contains(action.Quest.QuestId))
                        {
                            result.Errors.Add($"{context} references a missing quest in a dialogue action.");
                        }
                        break;
                    case DialogueActionType.RecruitMember:
                        if (action.Character == null || !refs.CharacterIds.Contains(action.Character.CharacterId))
                        {
                            result.Errors.Add($"{context} references a missing character in a dialogue action.");
                        }
                        break;
                    case DialogueActionType.SetZoneFact:
                        if (!refs.ZoneIds.Contains(action.ZoneId) || string.IsNullOrWhiteSpace(action.ZoneFactId))
                        {
                            result.Errors.Add($"{context} contains an invalid SetZoneFact action.");
                        }
                        break;
                    case DialogueActionType.SetFlag:
                        if (string.IsNullOrWhiteSpace(action.FlagId))
                        {
                            result.Errors.Add($"{context} contains a SetFlag action with empty flag id.");
                        }
                        break;
                    case DialogueActionType.ConsumeOneShot:
                        if (string.IsNullOrWhiteSpace(action.OneShotId))
                        {
                            result.Errors.Add($"{context} contains a ConsumeOneShot action with empty id.");
                        }
                        break;
                }
            }
        }

        private static void ValidateConditions(string context, ConditionResolver resolver, CatalogReferenceIndex refs, ContentValidationResult result)
        {
            if (resolver == null)
            {
                return;
            }

            ValidateConditions(context, resolver.Conditions, refs, result);
            if (resolver.SharedDefinition != null && resolver.SharedDefinition.Conditions != null)
            {
                ValidateConditions($"{context} (shared)", resolver.SharedDefinition.Conditions.Conditions, refs, result);
            }
        }

        private static void ValidateConditions(string context, IReadOnlyList<GameCondition> conditions, CatalogReferenceIndex refs, ContentValidationResult result)
        {
            if (conditions == null)
            {
                return;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                GameCondition condition = conditions[i];
                if (condition == null)
                {
                    result.Errors.Add($"{context} contains a null condition.");
                    continue;
                }

                switch (condition.Type)
                {
                    case ConditionType.StateEquals:
                        if (string.IsNullOrWhiteSpace(condition.StateKey))
                        {
                            result.Errors.Add($"{context} has a StateEquals condition with empty state key.");
                        }
                        break;
                    case ConditionType.QuestState:
                        if (!refs.QuestIds.Contains(condition.QuestId))
                        {
                            result.Errors.Add($"{context} references missing quest '{condition.QuestId}' in a condition.");
                        }
                        break;
                    case ConditionType.EncounterCleared:
                        if (!refs.EncounterIds.Contains(condition.EncounterId))
                        {
                            result.Errors.Add($"{context} references missing encounter '{condition.EncounterId}' in a condition.");
                        }
                        break;
                    case ConditionType.InventoryCountAtLeast:
                        if (!refs.ItemIds.Contains(condition.InventoryDefinitionId) && !refs.EquipmentIds.Contains(condition.InventoryDefinitionId))
                        {
                            result.Errors.Add($"{context} references missing inventory id '{condition.InventoryDefinitionId}' in a condition.");
                        }
                        break;
                    case ConditionType.PartyMemberRecruited:
                        if (!refs.CharacterIds.Contains(condition.PartyMemberId))
                        {
                            result.Errors.Add($"{context} references missing party member '{condition.PartyMemberId}' in a condition.");
                        }
                        break;
                    case ConditionType.ZoneFactBoolEquals:
                        if (!refs.ZoneIds.Contains(condition.ZoneId) || string.IsNullOrWhiteSpace(condition.ZoneFactId))
                        {
                            result.Errors.Add($"{context} contains an invalid zone fact condition.");
                        }
                        break;
                    case ConditionType.DialogueFlag:
                        if (string.IsNullOrWhiteSpace(condition.DialogueFlagId))
                        {
                            result.Errors.Add($"{context} has a dialogue flag condition with empty flag id.");
                        }
                        break;
                }
            }
        }

        private static void ValidateItemGrants(string rewardId, IReadOnlyList<ItemGrantDefinition> grants, ContentValidationResult result)
        {
            for (int i = 0; i < grants.Count; i++)
            {
                ItemGrantDefinition grant = grants[i];
                if (grant == null || grant.Item == null || grant.Amount <= 0)
                {
                    result.Errors.Add($"Reward '{rewardId}' contains an invalid item grant.");
                }
            }
        }

        private static void ValidateEquipmentGrants(string rewardId, IReadOnlyList<EquipmentGrantDefinition> grants, ContentValidationResult result)
        {
            for (int i = 0; i < grants.Count; i++)
            {
                EquipmentGrantDefinition grant = grants[i];
                if (grant == null || grant.Equipment == null || grant.Amount <= 0)
                {
                    result.Errors.Add($"Reward '{rewardId}' contains an invalid equipment grant.");
                }
            }
        }

        private static void AddIds<T>(IReadOnlyList<T> definitions, Func<T, string> selector, HashSet<string> target)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                T definition = definitions[i];
                if (EqualityComparer<T>.Default.Equals(definition, default))
                {
                    continue;
                }

                string id = selector(definition);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    target.Add(id);
                }
            }
        }
    }
}
