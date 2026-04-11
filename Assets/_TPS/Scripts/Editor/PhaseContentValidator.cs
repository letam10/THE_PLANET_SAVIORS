using System;
using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using UnityEditor;

namespace TPS.Editor
{
    public sealed class ContentValidationResult
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
    }

    public static class PhaseContentValidator
    {
        public const string SharedCatalogPath = "Assets/_TPS/Data/Phase1/Core/CAT_Phase1Content.asset";

        public static Phase1ContentCatalog LoadSharedCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<Phase1ContentCatalog>(SharedCatalogPath);
        }

        public static ContentValidationResult ValidateSharedCatalogAsset()
        {
            return ValidateCatalog(LoadSharedCatalog());
        }

        public static ContentValidationResult ValidateCatalog(Phase1ContentCatalog catalog)
        {
            var result = new ContentValidationResult();
            if (catalog == null)
            {
                result.Errors.Add("Content catalog asset is missing.");
                return result;
            }

            ValidateUniqueIds(catalog.Characters, definition => definition != null ? definition.CharacterId : string.Empty, "character", result.Errors);
            ValidateUniqueIds(catalog.Enemies, definition => definition != null ? definition.EnemyId : string.Empty, "enemy", result.Errors);
            ValidateUniqueIds(catalog.Items, definition => definition != null ? definition.ItemId : string.Empty, "item", result.Errors);
            ValidateUniqueIds(catalog.Equipment, definition => definition != null ? definition.EquipmentId : string.Empty, "equipment", result.Errors);
            ValidateUniqueIds(catalog.Skills, definition => definition != null ? definition.SkillId : string.Empty, "skill", result.Errors);
            ValidateUniqueIds(catalog.RewardTables, definition => definition != null ? definition.RewardId : string.Empty, "reward", result.Errors);
            ValidateUniqueIds(catalog.Encounters, definition => definition != null ? definition.EncounterId : string.Empty, "encounter", result.Errors);
            ValidateUniqueIds(catalog.Zones, definition => definition != null ? definition.ZoneId : string.Empty, "zone", result.Errors);
            ValidateUniqueIds(catalog.Shops, definition => definition != null ? definition.ShopId : string.Empty, "shop", result.Errors);
            ValidateUniqueIds(catalog.Dialogues, definition => definition != null ? definition.DialogueId : string.Empty, "dialogue", result.Errors);
            ValidateUniqueIds(catalog.Quests, definition => definition != null ? definition.QuestId : string.Empty, "quest", result.Errors);

            ValidateDialogues(catalog.Dialogues, result.Errors);
            ValidateQuests(catalog.Quests, result.Errors);
            ValidateEncounters(catalog.Encounters, result.Errors);
            ValidateShops(catalog.Shops, result.Errors);
            PhaseContentValidationRules.ValidateAdditionalRules(catalog, result);
            return result;
        }

        private static void ValidateDialogues(IReadOnlyList<DialogueDefinition> dialogues, List<string> errors)
        {
            for (int i = 0; i < dialogues.Count; i++)
            {
                DialogueDefinition dialogue = dialogues[i];
                if (dialogue == null)
                {
                    continue;
                }

                var seenIds = new HashSet<string>(StringComparer.Ordinal);
                IReadOnlyList<DialogueVariant> variants = dialogue.Variants;
                for (int variantIndex = 0; variantIndex < variants.Count; variantIndex++)
                {
                    DialogueVariant variant = variants[variantIndex];
                    if (variant == null)
                    {
                        errors.Add($"Dialogue '{dialogue.DialogueId}' has a null variant entry.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(variant.VariantId))
                    {
                        errors.Add($"Dialogue '{dialogue.DialogueId}' contains a variant with empty id.");
                        continue;
                    }

                    if (!seenIds.Add(variant.VariantId))
                    {
                        errors.Add($"Dialogue '{dialogue.DialogueId}' contains duplicate variant id '{variant.VariantId}'.");
                    }
                }
            }
        }

        private static void ValidateQuests(IReadOnlyList<QuestDefinition> quests, List<string> errors)
        {
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null)
                {
                    continue;
                }

                var seenObjectiveIds = new HashSet<string>(StringComparer.Ordinal);
                IReadOnlyList<QuestObjectiveDefinition> objectives = quest.Objectives;
                for (int objectiveIndex = 0; objectiveIndex < objectives.Count; objectiveIndex++)
                {
                    QuestObjectiveDefinition objective = objectives[objectiveIndex];
                    if (objective == null)
                    {
                        errors.Add($"Quest '{quest.QuestId}' has a null objective entry.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(objective.ObjectiveId))
                    {
                        errors.Add($"Quest '{quest.QuestId}' contains an objective with empty id.");
                        continue;
                    }

                    if (!seenObjectiveIds.Add(objective.ObjectiveId))
                    {
                        errors.Add($"Quest '{quest.QuestId}' contains duplicate objective id '{objective.ObjectiveId}'.");
                    }
                }
            }
        }

        private static void ValidateEncounters(IReadOnlyList<EncounterDefinition> encounters, List<string> errors)
        {
            for (int i = 0; i < encounters.Count; i++)
            {
                EncounterDefinition encounter = encounters[i];
                if (encounter == null)
                {
                    continue;
                }

                if (encounter.RewardTable == null)
                {
                    errors.Add($"Encounter '{encounter.EncounterId}' is missing a reward table.");
                }

                if (encounter.Enemies == null || encounter.Enemies.Count == 0)
                {
                    errors.Add($"Encounter '{encounter.EncounterId}' has no enemy lineup.");
                }
            }
        }

        private static void ValidateShops(IReadOnlyList<ShopDefinition> shops, List<string> errors)
        {
            for (int i = 0; i < shops.Count; i++)
            {
                ShopDefinition shop = shops[i];
                if (shop == null)
                {
                    continue;
                }

                IReadOnlyList<ShopEntryDefinition> entries = shop.Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    ShopEntryDefinition entry = entries[entryIndex];
                    if (entry == null)
                    {
                        errors.Add($"Shop '{shop.ShopId}' has a null entry.");
                        continue;
                    }

                    if (entry.Item == null && entry.Equipment == null)
                    {
                        errors.Add($"Shop '{shop.ShopId}' contains an entry with neither item nor equipment assigned.");
                    }
                }
            }
        }

        private static void ValidateUniqueIds<T>(IReadOnlyList<T> definitions, Func<T, string> selector, string label, List<string> errors)
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Count; i++)
            {
                T definition = definitions[i];
                if (EqualityComparer<T>.Default.Equals(definition, default))
                {
                    continue;
                }

                string id = selector(definition);
                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add($"A {label} definition has an empty id.");
                    continue;
                }

                if (!seenIds.Add(id))
                {
                    errors.Add($"Duplicate {label} id '{id}' found in content catalog.");
                }
            }
        }
    }
}
