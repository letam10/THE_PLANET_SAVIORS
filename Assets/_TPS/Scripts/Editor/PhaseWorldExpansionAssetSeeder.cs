using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using UnityEditor;
using UnityEngine;

namespace TPS.Editor
{
    internal sealed class WorldExpansionAssets
    {
        public Phase1Assets Base;
        public ItemDefinition SaltedRation;
        public ItemDefinition StormTonic;
        public EquipmentDefinition QuarryHalberd;
        public EnemyDefinition TideCrab;
        public EnemyDefinition QuarryHusk;
        public RewardTableDefinition GullwatchReward;
        public RewardTableDefinition RedCedarReward;
        public RewardTableDefinition TideCavernsReward;
        public RewardTableDefinition QuarryRuinsReward;
        public RewardTableDefinition GullwatchRouteReward;
        public EncounterDefinition GullwatchEncounter;
        public EncounterDefinition RedCedarEncounter;
        public EncounterDefinition TideCavernsPatrol;
        public EncounterDefinition TideCavernsBoss;
        public EncounterDefinition QuarryRuinsPatrol;
        public EncounterDefinition QuarryRuinsBoss;
        public EncounterTableDefinition GullwatchTable;
        public EncounterTableDefinition RedCedarTable;
        public EncounterTableDefinition TideCavernsTable;
        public EncounterTableDefinition QuarryRuinsTable;
        public ZoneDefinition GullwatchZone;
        public ZoneDefinition RedCedarZone;
        public ZoneDefinition TideCavernsZone;
        public ZoneDefinition QuarryRuinsZone;
        public DialogueDefinition GullwatchDialogue;
        public DialogueDefinition RedCedarDialogue;
        public QuestDefinition GullwatchRouteQuest;
    }

    internal static class PhaseWorldExpansionAssetSeeder
    {
        public static void EnsureFolders()
        {
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/WorldExpansion");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/WorldExpansion/Combat");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/WorldExpansion/Enemies");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/WorldExpansion/Items");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/WorldExpansion/Dialogue");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/WorldExpansion/World");
        }

        public static WorldExpansionAssets CreateOrUpdateAssets()
        {
            EnsureFolders();

            var assets = new WorldExpansionAssets
            {
                Base = Phase1AssetSeeder.CreateOrUpdateAssets()
            };

            assets.SaltedRation = Phase1InstallerShared.LoadOrCreateAsset<ItemDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Items/ITM_SaltedRation.asset");
            assets.StormTonic = Phase1InstallerShared.LoadOrCreateAsset<ItemDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Items/ITM_StormTonic.asset");
            ConfigureItem(assets.SaltedRation, "item_salted_ration", "Salted Ration", "Compact harbor food for long patrols.", 22, 11, 18, 6);
            ConfigureItem(assets.StormTonic, "item_storm_tonic", "Storm Tonic", "Sharp herbal mix used on rough road marches.", 38, 19, 0, 18, CombatStatusType.Wet);

            assets.QuarryHalberd = Phase1InstallerShared.LoadOrCreateAsset<EquipmentDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Items/EQP_QuarryHalberd.asset");
            ConfigureEquipment(assets.QuarryHalberd, "equipment_quarry_halberd", "Quarry Halberd", WeaponFamilyType.Polearm, 6, 145, 72, 4, 0, 1, 1, 0, 1);

            assets.TideCrab = Phase1InstallerShared.LoadOrCreateAsset<EnemyDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Enemies/ENM_TideCrab.asset");
            assets.QuarryHusk = Phase1InstallerShared.LoadOrCreateAsset<EnemyDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Enemies/ENM_QuarryHusk.asset");
            ConfigureEnemy(assets.TideCrab, "enemy_tide_crab", "Tide Crab", 36, 8, 7, 2, 7, 4, 4, assets.Base.RaiderStrikeSkill);
            ConfigureEnemy(assets.QuarryHusk, "enemy_quarry_husk", "Quarry Husk", 52, 10, 9, 4, 8, 6, 4, assets.Base.GuardBreakSkill);

            assets.GullwatchReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Combat/RWD_Gullwatch.asset");
            assets.RedCedarReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Combat/RWD_RedCedar.asset");
            assets.TideCavernsReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Combat/RWD_TideCaverns.asset");
            assets.QuarryRuinsReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Combat/RWD_QuarryRuins.asset");
            assets.GullwatchRouteReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Combat/RWD_GullwatchRoute.asset");
            ConfigureReward(assets.GullwatchReward, "reward_gullwatch_patrol", 34, 28, assets.SaltedRation, 1, null, 0);
            ConfigureReward(assets.RedCedarReward, "reward_redcedar_patrol", 38, 30, assets.StormTonic, 1, null, 0);
            ConfigureReward(assets.TideCavernsReward, "reward_tide_caverns", 65, 48, assets.SaltedRation, 2, assets.Base.Ether, 1);
            ConfigureReward(assets.QuarryRuinsReward, "reward_quarry_ruins", 92, 62, assets.StormTonic, 1, null, 0, assets.QuarryHalberd, 1);
            ConfigureReward(assets.GullwatchRouteReward, "reward_gullwatch_route", 58, 36, assets.StormTonic, 1, assets.SaltedRation, 2);

            assets.GullwatchEncounter = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ENC_GullwatchShoreline.asset");
            assets.RedCedarEncounter = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ENC_RedCedarRoadside.asset");
            assets.TideCavernsPatrol = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ENC_TideCavernsPatrol.asset");
            assets.TideCavernsBoss = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ENC_TideCavernsBoss.asset");
            assets.QuarryRuinsPatrol = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ENC_QuarryRuinsPatrol.asset");
            assets.QuarryRuinsBoss = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ENC_QuarryRuinsBoss.asset");
            ConfigureEncounter(assets.GullwatchEncounter, "enc_gullwatch_shoreline", "Shoreline Foragers", "gullwatch", false, assets.GullwatchReward, assets.TideCrab, assets.Base.RainMite);
            ConfigureEncounter(assets.RedCedarEncounter, "enc_redcedar_roadside", "Roadside Raiders", "redcedar", false, assets.RedCedarReward, assets.Base.RaiderScout, assets.Base.RainMite);
            ConfigureEncounter(assets.TideCavernsPatrol, "enc_tide_caverns_patrol", "Tide Caverns Patrol", "tide_caverns", false, assets.TideCavernsReward, assets.TideCrab, assets.Base.RainMite);
            ConfigureEncounter(assets.TideCavernsBoss, "enc_tide_caverns_boss", "Tide Caverns Matriarch", "tide_caverns", true, assets.TideCavernsReward, assets.TideCrab, assets.TideCrab, assets.Base.RaiderCaptain);
            ConfigureEncounter(assets.QuarryRuinsPatrol, "enc_quarry_ruins_patrol", "Quarry Ruins Patrol", "quarry_ruins", false, assets.QuarryRuinsReward, assets.QuarryHusk, assets.Base.RaiderScout);
            ConfigureEncounter(assets.QuarryRuinsBoss, "enc_quarry_ruins_boss", "Quarry Overseer", "quarry_ruins", true, assets.QuarryRuinsReward, assets.QuarryHusk, assets.QuarryHusk, assets.Base.RaiderCaptain);

            assets.GullwatchTable = Phase1InstallerShared.LoadOrCreateAsset<EncounterTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ECT_Gullwatch.asset");
            assets.RedCedarTable = Phase1InstallerShared.LoadOrCreateAsset<EncounterTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ECT_RedCedar.asset");
            assets.TideCavernsTable = Phase1InstallerShared.LoadOrCreateAsset<EncounterTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ECT_TideCaverns.asset");
            assets.QuarryRuinsTable = Phase1InstallerShared.LoadOrCreateAsset<EncounterTableDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ECT_QuarryRuins.asset");
            ConfigureEncounterTable(assets.GullwatchTable, "table_gullwatch", assets.GullwatchEncounter);
            ConfigureEncounterTable(assets.RedCedarTable, "table_redcedar", assets.RedCedarEncounter);
            ConfigureEncounterTable(assets.TideCavernsTable, "table_tide_caverns", assets.TideCavernsPatrol);
            ConfigureEncounterTable(assets.QuarryRuinsTable, "table_quarry_ruins", assets.QuarryRuinsPatrol);

            assets.GullwatchZone = Phase1InstallerShared.LoadOrCreateAsset<ZoneDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ZN_Gullwatch.asset");
            assets.RedCedarZone = Phase1InstallerShared.LoadOrCreateAsset<ZoneDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ZN_RedCedar.asset");
            assets.TideCavernsZone = Phase1InstallerShared.LoadOrCreateAsset<ZoneDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ZN_TideCaverns.asset");
            assets.QuarryRuinsZone = Phase1InstallerShared.LoadOrCreateAsset<ZoneDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/ZN_QuarryRuins.asset");
            ConfigureZone(assets.GullwatchZone, "gullwatch", "Gullwatch", assets.GullwatchTable);
            ConfigureZone(assets.RedCedarZone, "redcedar", "Red Cedar", assets.RedCedarTable);
            ConfigureZone(assets.TideCavernsZone, "tide_caverns", "Tide Caverns", assets.TideCavernsTable);
            ConfigureZone(assets.QuarryRuinsZone, "quarry_ruins", "Quarry Ruins", assets.QuarryRuinsTable);

            assets.GullwatchDialogue = Phase1InstallerShared.LoadOrCreateAsset<DialogueDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Dialogue/DLG_GullwatchKeeper.asset");
            assets.RedCedarDialogue = Phase1InstallerShared.LoadOrCreateAsset<DialogueDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/Dialogue/DLG_RedCedarWatcher.asset");
            assets.GullwatchRouteQuest = Phase1InstallerShared.LoadOrCreateAsset<QuestDefinition>("Assets/_TPS/Data/Phase1/WorldExpansion/World/QST_SecureTideRoute.asset");
            ConfigureQuest(assets.GullwatchRouteQuest,
                "quest_secure_tide_route",
                "Secure the Tide Route",
                "Leave Aster Harbor for Gullwatch, speak with Mira, then push through Tide Caverns and break the matriarch holding the coastal route shut.",
                assets.GullwatchRouteReward,
                null,
                assets.TideCavernsBoss.EncounterId,
                "defeat_tide_matriarch",
                "Go through Gullwatch, follow the spray markers into Tide Caverns, and defeat the Tide Caverns Matriarch beyond the flooded chambers.");
            ConfigureGullwatchRouteDialogue(assets.GullwatchDialogue, assets.GullwatchRouteQuest, assets.Base.SideQuest, assets.TideCavernsBoss.EncounterId);
            ConfigureSettlementDialogue(assets.RedCedarDialogue, "dialogue_redcedar_watcher", "Renn of Red Cedar",
                "The old road is calm again. Traders are willing to risk the climb.",
                "The ridge still spits raiders onto the road. We need a stronger hand out there.",
                assets.RedCedarEncounter.EncounterId);

            ConfigureExpandedShop(assets.Base.GeneralShop, assets.SaltedRation, assets.StormTonic, assets.Base.IronPike);
            ExtendCatalog(assets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return assets;
        }

        private static void ConfigureItem(ItemDefinition asset, string id, string name, string description, int buyPrice, int sellPrice, int restoreHP, int restoreMP, params CombatStatusType[] curedStatuses)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_itemId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_buyPrice").intValue = buyPrice;
            so.FindProperty("_sellPrice").intValue = sellPrice;
            so.FindProperty("_restoreHP").intValue = restoreHP;
            so.FindProperty("_restoreMP").intValue = restoreMP;
            SerializedProperty statuses = so.FindProperty("_curedStatuses");
            statuses.arraySize = curedStatuses.Length;
            for (int i = 0; i < curedStatuses.Length; i++)
            {
                statuses.GetArrayElementAtIndex(i).enumValueIndex = (int)curedStatuses[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEquipment(EquipmentDefinition asset, string id, string name, WeaponFamilyType family, int weaponPower, int buyPrice, int sellPrice, int atk, int mag, int def, int res, int hp, int speed)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_equipmentId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_slotType").enumValueIndex = (int)EquipmentSlotType.Weapon;
            so.FindProperty("_weaponFamily").enumValueIndex = (int)family;
            so.FindProperty("_weaponPower").intValue = weaponPower;
            so.FindProperty("_buyPrice").intValue = buyPrice;
            so.FindProperty("_sellPrice").intValue = sellPrice;
            Phase1InstallerShared.SetStats(so.FindProperty("_statBonus"), hp, 0, atk, mag, def, res, speed);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEnemy(EnemyDefinition asset, string id, string name, int hp, int mp, int atk, int mag, int def, int res, int speed, params SkillDefinition[] skills)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_enemyId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            Phase1InstallerShared.SetStats(so.FindProperty("_stats"), hp, mp, atk, mag, def, res, speed);
            Phase1InstallerShared.SetResistance(so.FindProperty("_resistanceProfile"), 1f, 1f, 1f, 1f);
            AssignUniqueObjects(so.FindProperty("_skills"), skills);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureReward(RewardTableDefinition asset, string id, int currency, int exp, ItemDefinition guaranteedItem, int guaranteedItemAmount, ItemDefinition weightedItem, int weightedItemAmount, EquipmentDefinition guaranteedEquipment = null, int guaranteedEquipmentAmount = 1)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_rewardId").stringValue = id;
            so.FindProperty("_currencyReward").intValue = currency;
            so.FindProperty("_expReward").intValue = exp;

            SerializedProperty items = so.FindProperty("_guaranteedItems");
            items.arraySize = guaranteedItem != null ? 1 : 0;
            if (items.arraySize == 1)
            {
                items.GetArrayElementAtIndex(0).FindPropertyRelative("Item").objectReferenceValue = guaranteedItem;
                items.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = guaranteedItemAmount;
            }

            SerializedProperty equipment = so.FindProperty("_guaranteedEquipment");
            equipment.arraySize = guaranteedEquipment != null ? 1 : 0;
            if (equipment.arraySize == 1)
            {
                equipment.GetArrayElementAtIndex(0).FindPropertyRelative("Equipment").objectReferenceValue = guaranteedEquipment;
                equipment.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = guaranteedEquipmentAmount;
            }

            SerializedProperty drops = so.FindProperty("_weightedDrops");
            drops.arraySize = weightedItem != null ? 1 : 0;
            if (drops.arraySize == 1)
            {
                drops.GetArrayElementAtIndex(0).FindPropertyRelative("Weight").intValue = 1;
                drops.GetArrayElementAtIndex(0).FindPropertyRelative("Item").objectReferenceValue = weightedItem;
                drops.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = weightedItemAmount;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEncounter(EncounterDefinition asset, string id, string displayName, string zoneId, bool countsAsClear, RewardTableDefinition reward, params EnemyDefinition[] enemies)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_encounterId").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_zoneId").stringValue = zoneId;
            so.FindProperty("_battleSceneName").stringValue = "BTL_Standard";
            so.FindProperty("_countsAsClear").boolValue = countsAsClear;
            so.FindProperty("_rewardTable").objectReferenceValue = reward;
            AssignUniqueObjects(so.FindProperty("_enemies"), enemies);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEncounterTable(EncounterTableDefinition asset, string id, params EncounterDefinition[] encounters)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_tableId").stringValue = id;
            SerializedProperty entries = so.FindProperty("_entries");
            entries.arraySize = encounters.Length;
            for (int i = 0; i < encounters.Length; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").intValue = 1;
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("Encounter").objectReferenceValue = encounters[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureZone(ZoneDefinition asset, string zoneId, string displayName, EncounterTableDefinition table)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_zoneId").stringValue = zoneId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_defaultEncounterTable").objectReferenceValue = table;
            so.FindProperty("_encounterTableOverrides").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureSettlementDialogue(DialogueDefinition asset, string dialogueId, string speakerName, string clearedBody, string unclearedBody, string encounterId)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_dialogueId").stringValue = dialogueId;
            SerializedProperty variants = so.FindProperty("_variants");
            variants.arraySize = 2;
            ConfigureDialogueVariant(variants.GetArrayElementAtIndex(0), "cleared", speakerName, clearedBody, ConditionType.EncounterCleared, encounterId, true);
            ConfigureDialogueVariant(variants.GetArrayElementAtIndex(1), "uncleared", speakerName, unclearedBody, null, encounterId, false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureDialogueVariant(SerializedProperty variant, string variantId, string speakerName, string body, ConditionType? conditionType, string encounterId, bool expectedBool)
        {
            variant.FindPropertyRelative("VariantId").stringValue = variantId;
            variant.FindPropertyRelative("SpeakerName").stringValue = speakerName;
            variant.FindPropertyRelative("Body").stringValue = body;
            variant.FindPropertyRelative("OneShot").boolValue = false;
            variant.FindPropertyRelative("Actions").arraySize = 0;
            variant.FindPropertyRelative("Choices").arraySize = 0;

            SerializedProperty conditions = variant.FindPropertyRelative("Conditions");
            conditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditionList = conditions.FindPropertyRelative("Conditions");
            conditionList.arraySize = conditionType.HasValue ? 1 : 0;
            if (conditionList.arraySize == 1)
            {
                SerializedProperty condition = conditionList.GetArrayElementAtIndex(0);
                condition.FindPropertyRelative("Type").enumValueIndex = (int)conditionType.Value;
                condition.FindPropertyRelative("EncounterId").stringValue = encounterId;
                condition.FindPropertyRelative("ExpectedBool").boolValue = expectedBool;
            }
        }

        private static void ConfigureExpandedShop(ShopDefinition asset, ItemDefinition saltedRation, ItemDefinition stormTonic, EquipmentDefinition ironPike)
        {
            SerializedObject so = new SerializedObject(asset);
            SerializedProperty entries = so.FindProperty("_entries");
            entries.arraySize = 5;
            ConfigureShopEntry(entries.GetArrayElementAtIndex(0), FindCatalogItem(asset, "item_potion"), null, 4, 30);
            ConfigureShopEntry(entries.GetArrayElementAtIndex(1), FindCatalogItem(asset, "item_ether"), null, 3, 45);
            ConfigureShopEntry(entries.GetArrayElementAtIndex(2), saltedRation, null, 4, saltedRation.BuyPrice);
            ConfigureShopEntry(entries.GetArrayElementAtIndex(3), stormTonic, null, 2, stormTonic.BuyPrice);
            ConfigureShopEntry(entries.GetArrayElementAtIndex(4), null, ironPike, 1, ironPike.BuyPrice);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static ItemDefinition FindCatalogItem(ShopDefinition asset, string itemId)
        {
            Phase1ContentCatalog catalog = AssetDatabase.LoadAssetAtPath<Phase1ContentCatalog>("Assets/_TPS/Data/Phase1/Core/CAT_Phase1Content.asset");
            return catalog != null ? catalog.GetItem(itemId) : null;
        }

        private static void ConfigureShopEntry(SerializedProperty entry, ItemDefinition item, EquipmentDefinition equipment, int stock, int price)
        {
            entry.FindPropertyRelative("Item").objectReferenceValue = item;
            entry.FindPropertyRelative("Equipment").objectReferenceValue = equipment;
            entry.FindPropertyRelative("Stock").intValue = stock;
            entry.FindPropertyRelative("PriceOverride").intValue = price;
        }

        private static void ExtendCatalog(WorldExpansionAssets assets)
        {
            SerializedObject so = new SerializedObject(assets.Base.Catalog);
            AppendUniqueObjects(so.FindProperty("_items"), assets.SaltedRation, assets.StormTonic);
            AppendUniqueObjects(so.FindProperty("_equipment"), assets.QuarryHalberd);
            AppendUniqueObjects(so.FindProperty("_enemies"), assets.TideCrab, assets.QuarryHusk);
            AppendUniqueObjects(so.FindProperty("_rewardTables"), assets.GullwatchReward, assets.RedCedarReward, assets.TideCavernsReward, assets.QuarryRuinsReward);
            AppendUniqueObjects(so.FindProperty("_encounters"), assets.GullwatchEncounter, assets.RedCedarEncounter, assets.TideCavernsPatrol, assets.TideCavernsBoss, assets.QuarryRuinsPatrol, assets.QuarryRuinsBoss);
            AppendUniqueObjects(so.FindProperty("_encounterTables"), assets.GullwatchTable, assets.RedCedarTable, assets.TideCavernsTable, assets.QuarryRuinsTable);
            AppendUniqueObjects(so.FindProperty("_zones"), assets.GullwatchZone, assets.RedCedarZone, assets.TideCavernsZone, assets.QuarryRuinsZone);
            AppendUniqueObjects(so.FindProperty("_dialogues"), assets.GullwatchDialogue, assets.RedCedarDialogue);
            AppendUniqueObjects(so.FindProperty("_quests"), assets.GullwatchRouteQuest);
            AppendUniqueObjects(so.FindProperty("_rewardTables"), assets.GullwatchRouteReward);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assets.Base.Catalog);
        }

        private static void ConfigureQuest(QuestDefinition asset, string questId, string title, string summary, RewardTableDefinition reward, CharacterDefinition recruitReward, string clearedEncounterId, string objectiveId, string objectiveDescription)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_questId").stringValue = questId;
            so.FindProperty("_title").stringValue = title;
            so.FindProperty("_summary").stringValue = summary;
            so.FindProperty("_completionReward").objectReferenceValue = reward;
            so.FindProperty("_recruitedMemberReward").objectReferenceValue = recruitReward;
            SerializedProperty objectives = so.FindProperty("_objectives");
            objectives.arraySize = 1;
            SerializedProperty objective = objectives.GetArrayElementAtIndex(0);
            objective.FindPropertyRelative("ObjectiveId").stringValue = objectiveId;
            objective.FindPropertyRelative("Description").stringValue = objectiveDescription;
            SerializedProperty conditions = objective.FindPropertyRelative("CompletionConditions");
            conditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditionList = conditions.FindPropertyRelative("Conditions");
            conditionList.arraySize = 1;
            SerializedProperty condition = conditionList.GetArrayElementAtIndex(0);
            condition.FindPropertyRelative("Type").enumValueIndex = (int)ConditionType.EncounterCleared;
            condition.FindPropertyRelative("EncounterId").stringValue = clearedEncounterId;
            condition.FindPropertyRelative("ExpectedBool").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureGullwatchRouteDialogue(DialogueDefinition asset, QuestDefinition routeQuest, QuestDefinition prerequisiteQuest, string clearedEncounterId)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_dialogueId").stringValue = "dialogue_gullwatch_keeper";
            SerializedProperty variants = so.FindProperty("_variants");
            variants.arraySize = 5;

            ConfigureGullwatchVariant(variants.GetArrayElementAtIndex(0), "completed", "Mira of Gullwatch",
                "The beacon is lit, the surf path is safe, and the ferry hands are back on schedule.",
                ConditionType.QuestState, routeQuest.QuestId, QuestStatus.Completed, null, null, DialogueActionType.SetFlag, null, "dialogue.gullwatch.completed");
            ConfigureGullwatchVariant(variants.GetArrayElementAtIndex(1), "turn_in", "Mira of Gullwatch",
                "You broke the matriarch's hold on the caverns. Take this, and watch the shoreline flare back to life.",
                ConditionType.QuestState, routeQuest.QuestId, QuestStatus.ReadyToTurnIn, null, null, DialogueActionType.TryCompleteQuest, routeQuest, null,
                setZoneFact: true, setZoneId: "gullwatch", setZoneFactId: "tide_route_secured");
            ConfigureGullwatchVariant(variants.GetArrayElementAtIndex(2), "active", "Mira of Gullwatch",
                "Follow the spray markers into Tide Caverns. Clear the patrol at the mouth if it blocks you, then push past the second flooded chamber to reach the matriarch.",
                ConditionType.QuestState, routeQuest.QuestId, QuestStatus.Active, null, null, DialogueActionType.SetFlag, null, "dialogue.gullwatch.route_active");
            ConfigureGullwatchVariant(variants.GetArrayElementAtIndex(3), "start", "Mira of Gullwatch",
                "Quartermaster Ivo said you were coming. Start at the Gullwatch lane, push through Tide Caverns, and we will reopen the full coastal route by dusk.",
                ConditionType.ZoneFactBoolEquals, null, QuestStatus.NotStarted, "aster_harbor", "dock_supplies_secured", DialogueActionType.AcceptQuest, routeQuest, null);
            ConfigureFallbackVariant(variants.GetArrayElementAtIndex(4), "fallback", "Mira of Gullwatch",
                "The gulls are quieter, but we still watch the tide line. Handle the harbor first, then we'll talk routes.");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureGullwatchVariant(
            SerializedProperty variant,
            string variantId,
            string speakerName,
            string body,
            ConditionType primaryConditionType,
            string questId,
            QuestStatus expectedQuestStatus,
            string zoneId,
            string zoneFactId,
            DialogueActionType actionType,
            QuestDefinition quest,
            string flagId,
            bool setZoneFact = false,
            string setZoneId = "gullwatch",
            string setZoneFactId = "tide_route_secured")
        {
            variant.FindPropertyRelative("VariantId").stringValue = variantId;
            variant.FindPropertyRelative("SpeakerName").stringValue = speakerName;
            variant.FindPropertyRelative("Body").stringValue = body;
            variant.FindPropertyRelative("OneShot").boolValue = false;
            variant.FindPropertyRelative("OneShotConsumptionId").stringValue = string.Empty;
            variant.FindPropertyRelative("Choices").arraySize = 0;

            SerializedProperty conditions = variant.FindPropertyRelative("Conditions");
            conditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditionList = conditions.FindPropertyRelative("Conditions");
            conditionList.arraySize = 1;
            if (conditionList.arraySize == 1)
            {
                SerializedProperty condition = conditionList.GetArrayElementAtIndex(0);
                condition.FindPropertyRelative("Type").enumValueIndex = (int)primaryConditionType;
                condition.FindPropertyRelative("QuestId").stringValue = questId ?? string.Empty;
                condition.FindPropertyRelative("ExpectedQuestStatus").enumValueIndex = (int)expectedQuestStatus;
                condition.FindPropertyRelative("ExpectedBool").boolValue = true;
                condition.FindPropertyRelative("ZoneId").stringValue = zoneId ?? string.Empty;
                condition.FindPropertyRelative("ZoneFactId").stringValue = zoneFactId ?? string.Empty;
            }

            SerializedProperty actions = variant.FindPropertyRelative("Actions");
            actions.arraySize = actionType == DialogueActionType.None ? 0 : (setZoneFact ? 2 : 1);
            if (actions.arraySize >= 1)
            {
                SerializedProperty action = actions.GetArrayElementAtIndex(0);
                action.FindPropertyRelative("ActionType").enumValueIndex = (int)actionType;
                action.FindPropertyRelative("Quest").objectReferenceValue = quest;
                action.FindPropertyRelative("FlagId").stringValue = flagId ?? string.Empty;
                action.FindPropertyRelative("BoolValue").boolValue = true;
                action.FindPropertyRelative("ZoneId").stringValue = string.Empty;
                action.FindPropertyRelative("ZoneFactId").stringValue = string.Empty;
            }

            if (setZoneFact && actions.arraySize > 1)
            {
                SerializedProperty action = actions.GetArrayElementAtIndex(1);
                action.FindPropertyRelative("ActionType").enumValueIndex = (int)DialogueActionType.SetZoneFact;
                action.FindPropertyRelative("Quest").objectReferenceValue = null;
                action.FindPropertyRelative("FlagId").stringValue = string.Empty;
                action.FindPropertyRelative("BoolValue").boolValue = true;
                action.FindPropertyRelative("ZoneId").stringValue = setZoneId;
                action.FindPropertyRelative("ZoneFactId").stringValue = setZoneFactId;
            }
        }

        private static void ConfigureFallbackVariant(SerializedProperty variant, string variantId, string speakerName, string body)
        {
            variant.FindPropertyRelative("VariantId").stringValue = variantId;
            variant.FindPropertyRelative("SpeakerName").stringValue = speakerName;
            variant.FindPropertyRelative("Body").stringValue = body;
            variant.FindPropertyRelative("OneShot").boolValue = false;
            variant.FindPropertyRelative("OneShotConsumptionId").stringValue = string.Empty;
            variant.FindPropertyRelative("Choices").arraySize = 0;
            SerializedProperty conditions = variant.FindPropertyRelative("Conditions");
            conditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            conditions.FindPropertyRelative("Conditions").arraySize = 0;
            variant.FindPropertyRelative("Actions").arraySize = 0;
        }

        private static void AppendUniqueObjects(SerializedProperty property, params Object[] additions)
        {
            var values = new List<Object>();
            for (int i = 0; i < property.arraySize; i++)
            {
                Object value = property.GetArrayElementAtIndex(i).objectReferenceValue;
                if (value != null && !values.Contains(value))
                {
                    values.Add(value);
                }
            }

            for (int i = 0; i < additions.Length; i++)
            {
                Object value = additions[i];
                if (value != null && !values.Contains(value))
                {
                    values.Add(value);
                }
            }

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void AssignUniqueObjects(SerializedProperty property, params Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
