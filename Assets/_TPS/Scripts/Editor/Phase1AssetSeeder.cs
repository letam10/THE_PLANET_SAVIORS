using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Quest;
using UnityEditor;
using UnityEngine;

namespace TPS.Editor
{
    internal static class Phase1AssetSeeder
    {
        public static void EnsureFolders()
        {
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/Core");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/Combat");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/Characters");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/Enemies");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/Items");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/Dialogue");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/Quests");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Phase1/World");
        }

        public static Phase1Assets CreateOrUpdateAssets()
        {
            var assets = new Phase1Assets();

            assets.ProgressionCurve = Phase1InstallerShared.LoadOrCreateAsset<ProgressionCurveDefinition>("Assets/_TPS/Data/Phase1/Core/CFG_Phase1ProgressionCurve.asset");
            ConfigureProgressionCurve(assets.ProgressionCurve);

            assets.Poison = Phase1InstallerShared.LoadOrCreateAsset<StatusEffectDefinition>("Assets/_TPS/Data/Phase1/Combat/STS_Poison.asset");
            assets.Burn = Phase1InstallerShared.LoadOrCreateAsset<StatusEffectDefinition>("Assets/_TPS/Data/Phase1/Combat/STS_Burn.asset");
            assets.Wet = Phase1InstallerShared.LoadOrCreateAsset<StatusEffectDefinition>("Assets/_TPS/Data/Phase1/Combat/STS_Wet.asset");
            assets.GuardBreak = Phase1InstallerShared.LoadOrCreateAsset<StatusEffectDefinition>("Assets/_TPS/Data/Phase1/Combat/STS_GuardBreak.asset");
            ConfigureStatus(assets.Poison, "status_poison", "Poison", CombatStatusType.Poison, 3);
            ConfigureStatus(assets.Burn, "status_burn", "Burn", CombatStatusType.Burn, 2);
            ConfigureStatus(assets.Wet, "status_wet", "Wet", CombatStatusType.Wet, 2);
            ConfigureStatus(assets.GuardBreak, "status_guard_break", "Guard Break", CombatStatusType.GuardBreak, 2);

            assets.GuardBreakSkill = Phase1InstallerShared.LoadOrCreateAsset<SkillDefinition>("Assets/_TPS/Data/Phase1/Combat/SKL_GuardBreak.asset");
            assets.FireBurstSkill = Phase1InstallerShared.LoadOrCreateAsset<SkillDefinition>("Assets/_TPS/Data/Phase1/Combat/SKL_FireBurst.asset");
            assets.HealingWaveSkill = Phase1InstallerShared.LoadOrCreateAsset<SkillDefinition>("Assets/_TPS/Data/Phase1/Combat/SKL_HealingWave.asset");
            assets.ShockShotSkill = Phase1InstallerShared.LoadOrCreateAsset<SkillDefinition>("Assets/_TPS/Data/Phase1/Combat/SKL_ShockShot.asset");
            assets.RaiderStrikeSkill = Phase1InstallerShared.LoadOrCreateAsset<SkillDefinition>("Assets/_TPS/Data/Phase1/Combat/SKL_RaiderStrike.asset");
            assets.StormSpitSkill = Phase1InstallerShared.LoadOrCreateAsset<SkillDefinition>("Assets/_TPS/Data/Phase1/Combat/SKL_StormSpit.asset");
            ConfigureSkill(assets.GuardBreakSkill, "skill_guard_break", "Guard Break", DamageKind.Physical, ElementType.Physical, 12, 3, 1f, 0f, false, 0, CombatStatusType.GuardBreak, 1f, 2);
            ConfigureSkill(assets.FireBurstSkill, "skill_fire_burst", "Fire Burst", DamageKind.Magical, ElementType.Fire, 13, 4, 0f, 1.1f, false, 0, CombatStatusType.Burn, 0.6f, 2);
            ConfigureSkill(assets.HealingWaveSkill, "skill_healing_wave", "Healing Wave", DamageKind.Magical, ElementType.Physical, 0, 4, 0f, 0f, true, 18, CombatStatusType.None, 0f, 0);
            ConfigureSkill(assets.ShockShotSkill, "skill_shock_shot", "Shock Shot", DamageKind.Physical, ElementType.Lightning, 11, 3, 1f, 0.1f, false, 0, CombatStatusType.None, 0f, 0);
            ConfigureSkill(assets.RaiderStrikeSkill, "skill_raider_strike", "Raider Strike", DamageKind.Physical, ElementType.Physical, 10, 0, 1f, 0f, false, 0, CombatStatusType.None, 0f, 0);
            ConfigureSkill(assets.StormSpitSkill, "skill_storm_spit", "Storm Spit", DamageKind.Magical, ElementType.Lightning, 9, 2, 0f, 1f, false, 0, CombatStatusType.Wet, 0.8f, 2);

            assets.Potion = Phase1InstallerShared.LoadOrCreateAsset<ItemDefinition>("Assets/_TPS/Data/Phase1/Items/ITM_Potion.asset");
            assets.Ether = Phase1InstallerShared.LoadOrCreateAsset<ItemDefinition>("Assets/_TPS/Data/Phase1/Items/ITM_Ether.asset");
            ConfigureItem(assets.Potion, "item_potion", "Potion", 30, 15, 22, 0);
            ConfigureItem(assets.Ether, "item_ether", "Ether", 45, 22, 0, 12);

            assets.BronzeBlade = Phase1InstallerShared.LoadOrCreateAsset<EquipmentDefinition>("Assets/_TPS/Data/Phase1/Items/EQP_BronzeBlade.asset");
            assets.FocusWand = Phase1InstallerShared.LoadOrCreateAsset<EquipmentDefinition>("Assets/_TPS/Data/Phase1/Items/EQP_FocusWand.asset");
            assets.HunterBow = Phase1InstallerShared.LoadOrCreateAsset<EquipmentDefinition>("Assets/_TPS/Data/Phase1/Items/EQP_HunterBow.asset");
            assets.IronPike = Phase1InstallerShared.LoadOrCreateAsset<EquipmentDefinition>("Assets/_TPS/Data/Phase1/Items/EQP_IronPike.asset");
            ConfigureEquipment(assets.BronzeBlade, "equipment_bronze_blade", "Bronze Blade", WeaponFamilyType.Blade, 4, 70, 35, 2, 0, 0, 0, 0, 0, assets.GuardBreakSkill);
            ConfigureEquipment(assets.FocusWand, "equipment_focus_wand", "Focus Wand", WeaponFamilyType.Focus, 3, 75, 38, 0, 3, 0, 0, 1, 0, assets.FireBurstSkill, assets.HealingWaveSkill);
            ConfigureEquipment(assets.HunterBow, "equipment_hunter_bow", "Hunter Bow", WeaponFamilyType.Bow, 3, 70, 35, 1, 0, 0, 0, 0, 2, assets.ShockShotSkill);
            ConfigureEquipment(assets.IronPike, "equipment_iron_pike", "Iron Pike", WeaponFamilyType.Polearm, 5, 110, 55, 3, 0, 1, 0, 0, 0);

            assets.Vanguard = Phase1InstallerShared.LoadOrCreateAsset<CharacterArchetypeDefinition>("Assets/_TPS/Data/Phase1/Characters/ARC_Vanguard.asset");
            assets.Ranger = Phase1InstallerShared.LoadOrCreateAsset<CharacterArchetypeDefinition>("Assets/_TPS/Data/Phase1/Characters/ARC_Ranger.asset");
            assets.Mystic = Phase1InstallerShared.LoadOrCreateAsset<CharacterArchetypeDefinition>("Assets/_TPS/Data/Phase1/Characters/ARC_Mystic.asset");
            ConfigureArchetype(assets.Vanguard, "arch_vanguard", "Vanguard", 40, 10, 10, 4, 7, 5, 6, 6, 2, 2, 1, 2, 1, 1, assets.GuardBreakSkill, 2, WeaponFamilyType.Blade);
            ConfigureArchetype(assets.Ranger, "arch_ranger", "Ranger", 34, 12, 8, 5, 5, 5, 8, 4, 2, 2, 1, 1, 1, 2, assets.ShockShotSkill, 2, WeaponFamilyType.Bow);
            ConfigureArchetype(assets.Mystic, "arch_mystic", "Mystic", 30, 18, 5, 10, 4, 7, 6, 3, 4, 1, 3, 1, 2, 1, assets.HealingWaveSkill, 2, WeaponFamilyType.Focus);

            assets.Ari = Phase1InstallerShared.LoadOrCreateAsset<CharacterDefinition>("Assets/_TPS/Data/Phase1/Characters/CHR_Ari.asset");
            assets.Noa = Phase1InstallerShared.LoadOrCreateAsset<CharacterDefinition>("Assets/_TPS/Data/Phase1/Characters/CHR_Noa.asset");
            assets.Lina = Phase1InstallerShared.LoadOrCreateAsset<CharacterDefinition>("Assets/_TPS/Data/Phase1/Characters/CHR_Lina.asset");
            ConfigureCharacter(assets.Ari, "char_ari", "Ari", assets.Vanguard, assets.BronzeBlade, 1);
            ConfigureCharacter(assets.Noa, "char_noa", "Noa", assets.Mystic, assets.FocusWand, 1, assets.FireBurstSkill);
            ConfigureCharacter(assets.Lina, "char_lina", "Lina", assets.Ranger, assets.HunterBow, 1);

            assets.RaiderScout = Phase1InstallerShared.LoadOrCreateAsset<EnemyDefinition>("Assets/_TPS/Data/Phase1/Enemies/ENM_RaiderScout.asset");
            assets.RaiderCaptain = Phase1InstallerShared.LoadOrCreateAsset<EnemyDefinition>("Assets/_TPS/Data/Phase1/Enemies/ENM_RaiderCaptain.asset");
            assets.RainMite = Phase1InstallerShared.LoadOrCreateAsset<EnemyDefinition>("Assets/_TPS/Data/Phase1/Enemies/ENM_RainMite.asset");
            ConfigureEnemy(assets.RaiderScout, "enemy_raider_scout", "Raider Scout", 34, 8, 8, 3, 5, 4, 6, assets.RaiderStrikeSkill);
            ConfigureEnemy(assets.RaiderCaptain, "enemy_raider_captain", "Raider Captain", 58, 10, 11, 3, 7, 5, 6, assets.RaiderStrikeSkill);
            ConfigureEnemy(assets.RainMite, "enemy_rain_mite", "Rain Mite", 28, 12, 5, 7, 4, 5, 5, assets.StormSpitSkill);

            assets.ScoutReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/Combat/RWD_Scout.asset");
            assets.CaptainReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/Combat/RWD_Captain.asset");
            assets.QuestReward = Phase1InstallerShared.LoadOrCreateAsset<RewardTableDefinition>("Assets/_TPS/Data/Phase1/Combat/RWD_Quest.asset");
            ConfigureReward(assets.ScoutReward, "reward_scout", 28, 22, assets.Potion, 1);
            ConfigureReward(assets.CaptainReward, "reward_captain", 90, 55, assets.Ether, 1, null, null, assets.IronPike, 1);
            ConfigureReward(assets.QuestReward, "reward_harbor_quest", 120, 45, assets.Ether, 1);

            assets.HarborScoutEncounter = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/World/ENC_HarborScout.asset");
            assets.HarborCaptainEncounter = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/World/ENC_HarborCaptain.asset");
            assets.PostBossEncounter = Phase1InstallerShared.LoadOrCreateAsset<EncounterDefinition>("Assets/_TPS/Data/Phase1/World/ENC_PostBossPatrol.asset");
            ConfigureEncounter(assets.HarborScoutEncounter, "enc_harbor_scout", "Harbor Scout Patrol", "aster_harbor", false, assets.ScoutReward, assets.RaiderScout);
            ConfigureEncounter(assets.HarborCaptainEncounter, "enc_harbor_captain", "Raider Captain", "aster_harbor", true, assets.CaptainReward, assets.RaiderCaptain, assets.RainMite);
            ConfigureEncounter(assets.PostBossEncounter, "enc_post_boss_patrol", "Rain Mite Sweep", "aster_harbor", false, assets.ScoutReward, assets.RainMite, assets.RaiderScout);

            assets.PreBossTable = Phase1InstallerShared.LoadOrCreateAsset<EncounterTableDefinition>("Assets/_TPS/Data/Phase1/World/ECT_AsterHarbor_PreBoss.asset");
            assets.PostBossTable = Phase1InstallerShared.LoadOrCreateAsset<EncounterTableDefinition>("Assets/_TPS/Data/Phase1/World/ECT_AsterHarbor_PostBoss.asset");
            ConfigureEncounterTable(assets.PreBossTable, "table_aster_preboss", assets.HarborScoutEncounter);
            ConfigureEncounterTable(assets.PostBossTable, "table_aster_postboss", assets.PostBossEncounter);

            assets.AsterHarborZone = Phase1InstallerShared.LoadOrCreateAsset<ZoneDefinition>("Assets/_TPS/Data/Phase1/World/ZN_AsterHarbor.asset");
            ConfigureZone(assets.AsterHarborZone, "aster_harbor", "Aster Harbor", assets.PreBossTable, assets.PostBossTable, assets.HarborCaptainEncounter.EncounterId);

            assets.HarborQuest = Phase1InstallerShared.LoadOrCreateAsset<QuestDefinition>("Assets/_TPS/Data/Phase1/Quests/QST_ClearHarborThreat.asset");
            ConfigureQuest(assets.HarborQuest, "quest_clear_harbor_threat", "Clear Harbor Threat", "Defeat the raider captain at the gate so the harbor patrol and town support can recover.", assets.QuestReward, assets.Lina, assets.HarborCaptainEncounter.EncounterId);

            assets.HarborCaptainDialogue = Phase1InstallerShared.LoadOrCreateAsset<DialogueDefinition>("Assets/_TPS/Data/Phase1/Dialogue/DLG_HarborCaptain.asset");
            ConfigureDialogue(assets.HarborCaptainDialogue, assets.HarborQuest);

            assets.GeneralShop = Phase1InstallerShared.LoadOrCreateAsset<ShopDefinition>("Assets/_TPS/Data/Phase1/World/SHP_HarborGeneralStore.asset");
            ConfigureShop(assets.GeneralShop, "shop_harbor_general", "Harbor General Store", assets.Potion, assets.Ether, assets.IronPike);

            assets.Catalog = Phase1InstallerShared.LoadOrCreateAsset<Phase1ContentCatalog>("Assets/_TPS/Data/Phase1/Core/CAT_Phase1Content.asset");
            ConfigureCatalog(assets);

            return assets;
        }

        private static void ConfigureProgressionCurve(ProgressionCurveDefinition asset)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_baseExp").intValue = 20;
            so.FindProperty("_linearExp").intValue = 10;
            so.FindProperty("_quadraticExp").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureStatus(StatusEffectDefinition asset, string id, string name, CombatStatusType statusType, int duration)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_statusId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_statusType").enumValueIndex = (int)statusType;
            so.FindProperty("_defaultDurationTurns").intValue = duration;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureSkill(SkillDefinition asset, string id, string name, DamageKind damageKind, ElementType elementType, int power, int mpCost, float attackScale, float magicScale, bool healing, int flatHealing, CombatStatusType statusType, float statusChance, int statusDuration)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_skillId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_damageKind").enumValueIndex = (int)damageKind;
            so.FindProperty("_elementType").enumValueIndex = (int)elementType;
            so.FindProperty("_resourceType").enumValueIndex = (int)ResourceType.MP;
            so.FindProperty("_resourceCost").intValue = mpCost;
            so.FindProperty("_power").intValue = power;
            so.FindProperty("_attackScale").floatValue = attackScale;
            so.FindProperty("_magicScale").floatValue = magicScale;
            so.FindProperty("_isHealingSkill").boolValue = healing;
            so.FindProperty("_flatHealing").intValue = flatHealing;
            SerializedProperty statuses = so.FindProperty("_appliedStatuses");
            statuses.arraySize = statusType == CombatStatusType.None ? 0 : 1;
            if (statuses.arraySize == 1)
            {
                SerializedProperty status = statuses.GetArrayElementAtIndex(0);
                status.FindPropertyRelative("StatusType").enumValueIndex = (int)statusType;
                status.FindPropertyRelative("DurationTurns").intValue = statusDuration;
                status.FindPropertyRelative("Chance").floatValue = statusChance;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureItem(ItemDefinition asset, string id, string name, int buyPrice, int sellPrice, int restoreHP, int restoreMP)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_itemId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_buyPrice").intValue = buyPrice;
            so.FindProperty("_sellPrice").intValue = sellPrice;
            so.FindProperty("_restoreHP").intValue = restoreHP;
            so.FindProperty("_restoreMP").intValue = restoreMP;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEquipment(EquipmentDefinition asset, string id, string name, WeaponFamilyType family, int weaponPower, int buyPrice, int sellPrice, int atk, int mag, int def, int res, int hp, int speed, params SkillDefinition[] grantedSkills)
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
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_grantedSkills"), grantedSkills);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureArchetype(CharacterArchetypeDefinition asset, string id, string name, int hp, int mp, int atk, int mag, int def, int res, int speed, int hpGrowth, int mpGrowth, int atkGrowth, int magGrowth, int defGrowth, int resGrowth, int speedGrowth, SkillDefinition unlockSkill, int unlockLevel, WeaponFamilyType requiredWeaponFamily)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_archetypeId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            Phase1InstallerShared.SetStats(so.FindProperty("_baseStats"), hp, mp, atk, mag, def, res, speed);
            Phase1InstallerShared.SetStats(so.FindProperty("_growthStats"), hpGrowth, mpGrowth, atkGrowth, magGrowth, defGrowth, resGrowth, speedGrowth);
            SerializedProperty unlocks = so.FindProperty("_skillUnlocks");
            unlocks.arraySize = unlockSkill != null ? 1 : 0;
            if (unlocks.arraySize == 1)
            {
                SerializedProperty unlock = unlocks.GetArrayElementAtIndex(0);
                unlock.FindPropertyRelative("Skill").objectReferenceValue = unlockSkill;
                unlock.FindPropertyRelative("RequiredLevel").intValue = unlockLevel;
                unlock.FindPropertyRelative("RequiredWeaponFamily").enumValueIndex = (int)requiredWeaponFamily;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureCharacter(CharacterDefinition asset, string id, string name, CharacterArchetypeDefinition archetype, EquipmentDefinition weapon, int startingLevel, params SkillDefinition[] startingSkills)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_characterId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_archetype").objectReferenceValue = archetype;
            so.FindProperty("_startingLevel").intValue = startingLevel;
            so.FindProperty("_startingWeapon").objectReferenceValue = weapon;
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_startingSkills"), startingSkills);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEnemy(EnemyDefinition asset, string id, string name, int hp, int mp, int atk, int mag, int def, int res, int speed, params SkillDefinition[] skills)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_enemyId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            Phase1InstallerShared.SetStats(so.FindProperty("_stats"), hp, mp, atk, mag, def, res, speed);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_skills"), skills);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureReward(RewardTableDefinition asset, string id, int currency, int exp, ItemDefinition guaranteedItem, int itemAmount, ItemDefinition dropItem = null, EquipmentDefinition dropEquipment = null, EquipmentDefinition guaranteedEquipment = null, int equipmentAmount = 1)
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
                items.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = itemAmount;
            }
            SerializedProperty equipment = so.FindProperty("_guaranteedEquipment");
            equipment.arraySize = guaranteedEquipment != null ? 1 : 0;
            if (equipment.arraySize == 1)
            {
                equipment.GetArrayElementAtIndex(0).FindPropertyRelative("Equipment").objectReferenceValue = guaranteedEquipment;
                equipment.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = equipmentAmount;
            }
            SerializedProperty drops = so.FindProperty("_weightedDrops");
            drops.arraySize = dropItem != null || dropEquipment != null ? 1 : 0;
            if (drops.arraySize == 1)
            {
                SerializedProperty drop = drops.GetArrayElementAtIndex(0);
                drop.FindPropertyRelative("Weight").intValue = 1;
                drop.FindPropertyRelative("Item").objectReferenceValue = dropItem;
                drop.FindPropertyRelative("Equipment").objectReferenceValue = dropEquipment;
                drop.FindPropertyRelative("Amount").intValue = 1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEncounter(EncounterDefinition asset, string id, string name, string zoneId, bool countsAsClear, RewardTableDefinition reward, params EnemyDefinition[] enemies)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_encounterId").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_zoneId").stringValue = zoneId;
            so.FindProperty("_battleSceneName").stringValue = "BTL_Standard";
            so.FindProperty("_countsAsClear").boolValue = countsAsClear;
            so.FindProperty("_rewardTable").objectReferenceValue = reward;
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_enemies"), enemies);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureEncounterTable(EncounterTableDefinition asset, string id, EncounterDefinition encounterDefinition)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_tableId").stringValue = id;
            SerializedProperty entries = so.FindProperty("_entries");
            entries.arraySize = 1;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("Weight").intValue = 1;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("Encounter").objectReferenceValue = encounterDefinition;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureZone(ZoneDefinition asset, string zoneId, string displayName, EncounterTableDefinition defaultTable, EncounterTableDefinition overrideTable, string clearedEncounterId)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_zoneId").stringValue = zoneId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_defaultEncounterTable").objectReferenceValue = defaultTable;
            SerializedProperty overrides = so.FindProperty("_encounterTableOverrides");
            overrides.arraySize = 1;
            SerializedProperty entry = overrides.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("EncounterTable").objectReferenceValue = overrideTable;
            SerializedProperty conditions = entry.FindPropertyRelative("Conditions");
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

        private static void ConfigureQuest(QuestDefinition asset, string questId, string title, string summary, RewardTableDefinition reward, CharacterDefinition recruitReward, string clearedEncounterId)
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
            objective.FindPropertyRelative("ObjectiveId").stringValue = "defeat_raider_captain";
            objective.FindPropertyRelative("Description").stringValue = "Defeat the raider captain near the gate.";
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

        private static void ConfigureDialogue(DialogueDefinition asset, QuestDefinition questDefinition)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_dialogueId").stringValue = "dialogue_harbor_captain";
            SerializedProperty variants = so.FindProperty("_variants");
            variants.arraySize = 4;
            ConfigureDialogueVariant(variants.GetArrayElementAtIndex(0), "completed", "Captain Rhea", "The gate is clear, patrol routes are back online, and Lina is free to join your party. Aster Harbor remembers this.", ConditionType.QuestState, questDefinition.QuestId, QuestStatus.Completed, DialogueActionType.SetFlag, questDefinition, "dialogue.harbor.completed");
            ConfigureDialogueVariant(variants.GetArrayElementAtIndex(1), "turn_in", "Captain Rhea", "You broke the raider line. Take this pay, then bring Lina with you while we reset the harbor watch.", ConditionType.QuestState, questDefinition.QuestId, QuestStatus.ReadyToTurnIn, DialogueActionType.TryCompleteQuest, questDefinition, null);
            ConfigureDialogueVariant(variants.GetArrayElementAtIndex(2), "active", "Captain Rhea", "The raider captain still holds the gate. Once he falls, the harbor encounter routes will shift immediately.", ConditionType.QuestState, questDefinition.QuestId, QuestStatus.Active, DialogueActionType.SetFlag, null, "dialogue.harbor.quest_active");
            ConfigureDialogueVariant(variants.GetArrayElementAtIndex(3), "start", "Captain Rhea", "I stand in the square from 07:00 to 12:00, but when rain hits I move into the tavern. First, help us break the raider captain at the gate.", null, questDefinition.QuestId, QuestStatus.NotStarted, DialogueActionType.AcceptQuest, questDefinition, null);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureShop(ShopDefinition asset, string shopId, string displayName, ItemDefinition potion, ItemDefinition ether, EquipmentDefinition ironPike)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_shopId").stringValue = shopId;
            so.FindProperty("_displayName").stringValue = displayName;
            SerializedProperty entries = so.FindProperty("_entries");
            entries.arraySize = 3;
            ConfigureShopEntry(entries.GetArrayElementAtIndex(0), potion, null, 4, potion.BuyPrice);
            ConfigureShopEntry(entries.GetArrayElementAtIndex(1), ether, null, 3, ether.BuyPrice);
            ConfigureShopEntry(entries.GetArrayElementAtIndex(2), null, ironPike, 1, ironPike.BuyPrice);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureCatalog(Phase1Assets assets)
        {
            SerializedObject so = new SerializedObject(assets.Catalog);
            so.FindProperty("_progressionCurve").objectReferenceValue = assets.ProgressionCurve;
            so.FindProperty("_startingCurrency").intValue = 100;
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_startingPartyMembers"), assets.Ari, assets.Noa);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_characters"), assets.Ari, assets.Noa, assets.Lina);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_enemies"), assets.RaiderScout, assets.RaiderCaptain, assets.RainMite);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_items"), assets.Potion, assets.Ether);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_equipment"), assets.BronzeBlade, assets.FocusWand, assets.HunterBow, assets.IronPike);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_skills"), assets.GuardBreakSkill, assets.FireBurstSkill, assets.HealingWaveSkill, assets.ShockShotSkill, assets.RaiderStrikeSkill, assets.StormSpitSkill);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_statuses"), assets.Poison, assets.Burn, assets.Wet, assets.GuardBreak);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_rewardTables"), assets.ScoutReward, assets.CaptainReward, assets.QuestReward);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_encounters"), assets.HarborScoutEncounter, assets.HarborCaptainEncounter, assets.PostBossEncounter);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_encounterTables"), assets.PreBossTable, assets.PostBossTable);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_zones"), assets.AsterHarborZone);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_shops"), assets.GeneralShop);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_dialogues"), assets.HarborCaptainDialogue);
            Phase1InstallerShared.AssignObjectArray(so.FindProperty("_quests"), assets.HarborQuest);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assets.Catalog);
        }

        private static void ConfigureDialogueVariant(SerializedProperty variant, string variantId, string speaker, string body, ConditionType? conditionType, string questId, QuestStatus expectedStatus, DialogueActionType actionType, QuestDefinition questDefinition, string flagId)
        {
            variant.FindPropertyRelative("VariantId").stringValue = variantId;
            variant.FindPropertyRelative("SpeakerName").stringValue = speaker;
            variant.FindPropertyRelative("Body").stringValue = body;
            variant.FindPropertyRelative("OneShot").boolValue = false;
            SerializedProperty conditions = variant.FindPropertyRelative("Conditions");
            conditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditionList = conditions.FindPropertyRelative("Conditions");
            conditionList.arraySize = conditionType.HasValue ? 1 : 0;
            if (conditionList.arraySize == 1)
            {
                SerializedProperty condition = conditionList.GetArrayElementAtIndex(0);
                condition.FindPropertyRelative("Type").enumValueIndex = (int)conditionType.Value;
                condition.FindPropertyRelative("QuestId").stringValue = questId;
                condition.FindPropertyRelative("ExpectedQuestStatus").enumValueIndex = (int)expectedStatus;
            }

            SerializedProperty actions = variant.FindPropertyRelative("Actions");
            actions.arraySize = 1;
            actions.GetArrayElementAtIndex(0).FindPropertyRelative("ActionType").enumValueIndex = (int)actionType;
            actions.GetArrayElementAtIndex(0).FindPropertyRelative("Quest").objectReferenceValue = questDefinition;
            actions.GetArrayElementAtIndex(0).FindPropertyRelative("FlagId").stringValue = flagId;
        }

        private static void ConfigureShopEntry(SerializedProperty entry, ItemDefinition item, EquipmentDefinition equipment, int stock, int price)
        {
            entry.FindPropertyRelative("Item").objectReferenceValue = item;
            entry.FindPropertyRelative("Equipment").objectReferenceValue = equipment;
            entry.FindPropertyRelative("Stock").intValue = stock;
            entry.FindPropertyRelative("PriceOverride").intValue = price;
        }
    }
}
