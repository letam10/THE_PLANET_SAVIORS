using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.NPC;
using TPS.Runtime.Quest;
using TPS.Runtime.Triggers;
using TPS.Runtime.UI;
using TPS.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Editor
{
    public static class Phase1SceneInstaller
    {
        [MenuItem("Tools/TPS/Phase 1/Install Vertical Slice")]
        public static void InstallVerticalSlice()
        {
            Phase1AssetSeeder.EnsureFolders();
            Phase1Assets assets = Phase1AssetSeeder.CreateOrUpdateAssets();
            SetupCoreScene(assets);
            SetupWorldScene(assets);
            SetupBattleScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 1 vertical slice installed.");
        }

        [MenuItem("Tools/TPS/Install Phase1 Vertical Slice")]
        private static void InstallVerticalSliceAlias()
        {
            InstallVerticalSlice();
        }

        private static void SetupCoreScene(Phase1Assets assets)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/_TPS/Scenes/Core/Core.unity", OpenSceneMode.Additive);
            try
            {
                GameObject root = FindDeep(scene, "CoreServices");
                AddAndConfigureCatalogComponent<QuestService>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<DialogueStateService>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<PartyService>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<ProgressionService>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<EconomyService>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<EncounterService>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<Phase1RuntimeHUD>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<Phase1SmokeRunner>(root, assets.Catalog);
                AddAndConfigureCatalogComponent<Phase1AutomationDriver>(root, assets.Catalog);
                AddComponentIfMissing<InventoryService>(root);
                AddComponentIfMissing<ZoneStateService>(root);
                AddComponentIfMissing<RewardService>(root);
                AddComponentIfMissing<StateResolver>(root);
                ConfigureInventoryService(root.GetComponent<InventoryService>(), assets);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void SetupWorldScene(Phase1Assets assets)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity", OpenSceneMode.Additive);
            try
            {
                GameObject npc = FindDeep(scene, "PF_NPC_Test_Citizen");
                GameObject shopBlock = FindDeep(scene, "BLD_Shop_Block");
                GameObject tavernBlock = FindDeep(scene, "BLD_Tavern_Block");
                GameObject worldRoot = FindDeep(scene, "WorldRoot");
                GameObject squareMarker = FindDeep(scene, "MK_Square_01");
                GameObject tavernMarker = FindDeep(scene, "MK_Tavern_01");
                GameObject triggerPrototype = FindDeep(scene, "PF_EncounterTrigger_Test");
                GameObject gateBlock = FindDeep(scene, "BLD_Gate_Block");
                GameObject cube = FindDeep(scene, "Cube");

                ConfigureNpc(npc, assets, squareMarker.transform, tavernMarker.transform);
                ConfigureMerchant(shopBlock, assets.GeneralShop);
                AddComponentIfMissing<InnAnchor>(tavernBlock);
                ConfigureZoneEncounterAnchor(triggerPrototype, "aster_patrol_anchor", "aster_harbor");
                ConfigureBossAnchor(worldRoot, gateBlock.transform.position + new Vector3(0f, 0.5f, 4f), assets.HarborCaptainEncounter);
                ConfigureAmbientCube(cube, assets.HarborCaptainEncounter.EncounterId);
                ConfigureSideQuestNpc(worldRoot, shopBlock.transform.position + new Vector3(4f, 0f, -2f), assets.DockworkerDialogue);
                ConfigureSideQuestEncounterAnchor(worldRoot, shopBlock.transform.position + new Vector3(8f, 0.5f, -5f), assets.SideQuestEncounter);
                ConfigureSideQuestBanner(worldRoot, shopBlock.transform.position + new Vector3(6f, 1.25f, -1f));
                PhaseEnvironmentTools.EnsureAsterHarborEnvironment(scene, worldRoot);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void SetupBattleScene()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/_TPS/Scenes/Battle/BTL_Standard.unity", OpenSceneMode.Additive);
            try
            {
                GameObject battleRoot = FindDeep(scene, "BattleRoot");
                AddComponentIfMissing<BattleWorldBridge>(battleRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ConfigureInventoryService(InventoryService inventoryService, Phase1Assets assets)
        {
            SerializedObject so = new SerializedObject(inventoryService);
            SerializedProperty items = so.FindProperty("_startingItems");
            items.arraySize = 2;
            items.GetArrayElementAtIndex(0).FindPropertyRelative("Item").objectReferenceValue = assets.Potion;
            items.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = 2;
            items.GetArrayElementAtIndex(1).FindPropertyRelative("Item").objectReferenceValue = assets.Ether;
            items.GetArrayElementAtIndex(1).FindPropertyRelative("Amount").intValue = 1;

            SerializedProperty equipment = so.FindProperty("_startingEquipment");
            equipment.arraySize = 3;
            equipment.GetArrayElementAtIndex(0).FindPropertyRelative("Equipment").objectReferenceValue = assets.BronzeBlade;
            equipment.GetArrayElementAtIndex(0).FindPropertyRelative("Amount").intValue = 1;
            equipment.GetArrayElementAtIndex(1).FindPropertyRelative("Equipment").objectReferenceValue = assets.FocusWand;
            equipment.GetArrayElementAtIndex(1).FindPropertyRelative("Amount").intValue = 1;
            equipment.GetArrayElementAtIndex(2).FindPropertyRelative("Equipment").objectReferenceValue = assets.HunterBow;
            equipment.GetArrayElementAtIndex(2).FindPropertyRelative("Amount").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inventoryService);
        }

        private static void ConfigureNpc(GameObject npc, Phase1Assets assets, Transform squareMarker, Transform tavernMarker)
        {
            NPCSchedule schedule = AddComponentIfMissing<NPCSchedule>(npc);
            SerializedObject scheduleSo = new SerializedObject(schedule);
            scheduleSo.FindProperty("_npcId").stringValue = "harbor_captain";
            scheduleSo.FindProperty("_hideIfNoSlotMatched").boolValue = true;
            SerializedProperty slots = scheduleSo.FindProperty("_slots");
            slots.arraySize = 3;
            ConfigureSlot(slots.GetArrayElementAtIndex(0), "square_morning", 7, 12, squareMarker, true, ConditionType.WeatherEquals, TPS.Runtime.Weather.WeatherType.Sunny);
            ConfigureSlot(slots.GetArrayElementAtIndex(1), "tavern_rain", 7, 12, tavernMarker, true, ConditionType.WeatherEquals, TPS.Runtime.Weather.WeatherType.Rain);
            ConfigureSlot(slots.GetArrayElementAtIndex(2), "tavern_evening", 13, 23, tavernMarker, true, null, TPS.Runtime.Weather.WeatherType.Sunny);
            scheduleSo.ApplyModifiedPropertiesWithoutUndo();

            DialogueAnchor dialogueAnchor = AddComponentIfMissing<DialogueAnchor>(npc);
            SerializedObject dialogueSo = new SerializedObject(dialogueAnchor);
            dialogueSo.FindProperty("_anchorId").stringValue = "harbor_captain";
            dialogueSo.FindProperty("_dialogueDefinition").objectReferenceValue = assets.HarborCaptainDialogue;
            dialogueSo.FindProperty("_interactionLabel").stringValue = "talk";
            dialogueSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npc);
        }

        private static void ConfigureMerchant(GameObject shopBlock, ShopDefinition shopDefinition)
        {
            MerchantAnchor merchantAnchor = AddComponentIfMissing<MerchantAnchor>(shopBlock);
            SerializedObject so = new SerializedObject(merchantAnchor);
            so.FindProperty("_merchantId").stringValue = "harbor_general_store";
            so.FindProperty("_shopDefinition").objectReferenceValue = shopDefinition;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shopBlock);
        }

        private static void ConfigureZoneEncounterAnchor(GameObject target, string anchorId, string zoneId)
        {
            EncounterTrigger oldTrigger = target.GetComponent<EncounterTrigger>();
            if (oldTrigger != null)
            {
                Object.DestroyImmediate(oldTrigger);
            }

            EncounterAnchor anchor = AddComponentIfMissing<EncounterAnchor>(target);
            SerializedObject so = new SerializedObject(anchor);
            so.FindProperty("_anchorId").stringValue = anchorId;
            so.FindProperty("_zoneId").stringValue = zoneId;
            so.FindProperty("_useZoneEncounterTable").boolValue = true;
            so.FindProperty("_triggerOnEnter").boolValue = true;
            so.FindProperty("_triggerOnce").boolValue = false;
            so.FindProperty("_hideWhenCleared").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureBossAnchor(GameObject worldRoot, Vector3 position, EncounterDefinition encounterDefinition)
        {
            GameObject anchorObject = FindChild(worldRoot, "ENC_SubBoss_Anchor");
            if (anchorObject == null)
            {
                anchorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                anchorObject.name = "ENC_SubBoss_Anchor";
                anchorObject.transform.SetParent(worldRoot.transform);
            }

            anchorObject.transform.position = position;
            anchorObject.transform.localScale = new Vector3(2f, 2f, 2f);
            BoxCollider collider = anchorObject.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            EncounterAnchor anchor = AddComponentIfMissing<EncounterAnchor>(anchorObject);
            SerializedObject so = new SerializedObject(anchor);
            so.FindProperty("_anchorId").stringValue = "harbor_captain_anchor";
            so.FindProperty("_zoneId").stringValue = "aster_harbor";
            so.FindProperty("_directEncounter").objectReferenceValue = encounterDefinition;
            so.FindProperty("_useZoneEncounterTable").boolValue = false;
            so.FindProperty("_triggerOnEnter").boolValue = true;
            so.FindProperty("_triggerOnce").boolValue = true;
            so.FindProperty("_hideWhenCleared").boolValue = true;
            SerializedProperty conditions = so.FindProperty("_availabilityConditions");
            conditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditionList = conditions.FindPropertyRelative("Conditions");
            conditionList.arraySize = 1;
            SerializedProperty condition = conditionList.GetArrayElementAtIndex(0);
            condition.FindPropertyRelative("Type").enumValueIndex = (int)ConditionType.QuestState;
            condition.FindPropertyRelative("QuestId").stringValue = "quest_clear_harbor_threat";
            condition.FindPropertyRelative("ExpectedQuestStatus").enumValueIndex = (int)QuestStatus.Active;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anchorObject);
        }

        private static void ConfigureAmbientCube(GameObject cube, string encounterId)
        {
            ConditionalActivator activator = AddComponentIfMissing<ConditionalActivator>(cube);
            SerializedObject so = new SerializedObject(activator);
            SerializedProperty resolver = so.FindProperty("_resolver");
            resolver.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditions = resolver.FindPropertyRelative("Conditions");
            conditions.arraySize = 1;
            SerializedProperty condition = conditions.GetArrayElementAtIndex(0);
            condition.FindPropertyRelative("Type").enumValueIndex = (int)ConditionType.EncounterCleared;
            condition.FindPropertyRelative("EncounterId").stringValue = encounterId;
            condition.FindPropertyRelative("ExpectedBool").boolValue = true;
            so.FindProperty("_targetMode").enumValueIndex = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cube);
        }

        private static void ConfigureSideQuestNpc(GameObject worldRoot, Vector3 position, DialogueDefinition dialogueDefinition)
        {
            GameObject npcObject = FindChild(worldRoot, "NPC_DockQuartermaster");
            if (npcObject == null)
            {
                npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npcObject.name = "NPC_DockQuartermaster";
                npcObject.transform.SetParent(worldRoot.transform);
            }

            npcObject.transform.position = position;
            npcObject.transform.localScale = new Vector3(0.9f, 1.3f, 0.9f);
            NPCSchedule schedule = AddComponentIfMissing<NPCSchedule>(npcObject);
            SerializedObject scheduleSo = new SerializedObject(schedule);
            scheduleSo.FindProperty("_npcId").stringValue = "dock_quartermaster";
            scheduleSo.FindProperty("_hideIfNoSlotMatched").boolValue = true;
            SerializedProperty slots = scheduleSo.FindProperty("_slots");
            slots.arraySize = 1;
            ConfigureSlot(slots.GetArrayElementAtIndex(0), "dock_shift", 7, 18, null, true, null, TPS.Runtime.Weather.WeatherType.Sunny);
            scheduleSo.ApplyModifiedPropertiesWithoutUndo();

            DialogueAnchor dialogueAnchor = AddComponentIfMissing<DialogueAnchor>(npcObject);
            SerializedObject dialogueSo = new SerializedObject(dialogueAnchor);
            dialogueSo.FindProperty("_anchorId").stringValue = "dock_quartermaster";
            dialogueSo.FindProperty("_dialogueDefinition").objectReferenceValue = dialogueDefinition;
            dialogueSo.FindProperty("_interactionLabel").stringValue = "check supplies";
            dialogueSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npcObject);
        }

        private static void ConfigureSideQuestEncounterAnchor(GameObject worldRoot, Vector3 position, EncounterDefinition encounterDefinition)
        {
            GameObject anchorObject = FindChild(worldRoot, "ENC_DockRainMites_Anchor");
            if (anchorObject == null)
            {
                anchorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                anchorObject.name = "ENC_DockRainMites_Anchor";
                anchorObject.transform.SetParent(worldRoot.transform);
            }

            anchorObject.transform.position = position;
            anchorObject.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            BoxCollider collider = anchorObject.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            EncounterAnchor anchor = AddComponentIfMissing<EncounterAnchor>(anchorObject);
            SerializedObject so = new SerializedObject(anchor);
            so.FindProperty("_anchorId").stringValue = "dock_rain_mites_anchor";
            so.FindProperty("_zoneId").stringValue = "aster_harbor";
            so.FindProperty("_directEncounter").objectReferenceValue = encounterDefinition;
            so.FindProperty("_useZoneEncounterTable").boolValue = false;
            so.FindProperty("_triggerOnEnter").boolValue = true;
            so.FindProperty("_triggerOnce").boolValue = true;
            so.FindProperty("_hideWhenCleared").boolValue = true;
            SerializedProperty conditions = so.FindProperty("_availabilityConditions");
            conditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditionList = conditions.FindPropertyRelative("Conditions");
            conditionList.arraySize = 1;
            SerializedProperty condition = conditionList.GetArrayElementAtIndex(0);
            condition.FindPropertyRelative("Type").enumValueIndex = (int)ConditionType.QuestState;
            condition.FindPropertyRelative("QuestId").stringValue = "quest_secure_dock_supplies";
            condition.FindPropertyRelative("ExpectedQuestStatus").enumValueIndex = (int)QuestStatus.Active;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anchorObject);
        }

        private static void ConfigureSideQuestBanner(GameObject worldRoot, Vector3 position)
        {
            GameObject controller = FindChild(worldRoot, "PRP_DockSupplyBanner_Controller");
            if (controller == null)
            {
                controller = new GameObject("PRP_DockSupplyBanner_Controller");
                controller.transform.SetParent(worldRoot.transform);
            }

            controller.transform.position = position;
            GameObject visual = FindChild(controller, "PRP_DockSupplyBanner_Visual");
            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "PRP_DockSupplyBanner_Visual";
                visual.transform.SetParent(controller.transform);
            }

            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.5f, 2.5f, 0.2f);
            ConditionalActivator activator = AddComponentIfMissing<ConditionalActivator>(controller);
            SerializedObject so = new SerializedObject(activator);
            SerializedProperty resolver = so.FindProperty("_resolver");
            resolver.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditions = resolver.FindPropertyRelative("Conditions");
            conditions.arraySize = 1;
            SerializedProperty condition = conditions.GetArrayElementAtIndex(0);
            condition.FindPropertyRelative("Type").enumValueIndex = (int)ConditionType.ZoneFactBoolEquals;
            condition.FindPropertyRelative("ZoneId").stringValue = "aster_harbor";
            condition.FindPropertyRelative("ZoneFactId").stringValue = "dock_supplies_secured";
            condition.FindPropertyRelative("ExpectedBool").boolValue = true;
            so.FindProperty("_targetMode").enumValueIndex = (int)ActivatorTargetMode.GameObjectSetActive;
            so.FindProperty("_targetGameObject").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();
            visual.SetActive(false);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(visual);
        }

        private static void ConfigureSlot(SerializedProperty slot, string slotName, int startHour, int endHour, Transform targetMarker, bool visible, ConditionType? conditionType, TPS.Runtime.Weather.WeatherType weatherType)
        {
            slot.FindPropertyRelative("SlotName").stringValue = slotName;
            slot.FindPropertyRelative("StartHour").intValue = startHour;
            slot.FindPropertyRelative("EndHour").intValue = endHour;
            slot.FindPropertyRelative("TargetMarker").objectReferenceValue = targetMarker;
            slot.FindPropertyRelative("Visible").boolValue = visible;
            SerializedProperty extraConditions = slot.FindPropertyRelative("ExtraConditions");
            extraConditions.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditionList = extraConditions.FindPropertyRelative("Conditions");
            conditionList.arraySize = conditionType.HasValue ? 1 : 0;
            if (conditionList.arraySize == 1)
            {
                SerializedProperty condition = conditionList.GetArrayElementAtIndex(0);
                condition.FindPropertyRelative("Type").enumValueIndex = (int)conditionType.Value;
                condition.FindPropertyRelative("ExpectedWeather").enumValueIndex = (int)weatherType;
            }
        }

        private static void AddAndConfigureCatalogComponent<T>(GameObject target, Phase1ContentCatalog catalog) where T : Component
        {
            T component = AddComponentIfMissing<T>(target);
            SerializedObject so = new SerializedObject(component);
            SerializedProperty contentCatalog = so.FindProperty("_contentCatalog");
            if (contentCatalog != null)
            {
                contentCatalog.objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(component);
        }

        private static T AddComponentIfMissing<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }
            return component;
        }

        private static GameObject FindDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }

                GameObject child = FindChild(root, name);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject FindChild(GameObject parent, string name)
        {
            foreach (Transform child in parent.transform)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }

                GameObject nested = FindChild(child.gameObject, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
