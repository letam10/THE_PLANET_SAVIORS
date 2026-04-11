using System;
using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Spawn;
using TPS.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Editor
{
    internal static class PhaseWorldExpansionTools
    {
        private const string BuildProfilePath = "Assets/Settings/Build Profiles/Windows.asset";
        private const string AsterHarborScenePath = "Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity";
        private const string GullwatchScenePath = "Assets/_TPS/Scenes/World/ZN_Settlement_Gullwatch.unity";
        private const string RedCedarScenePath = "Assets/_TPS/Scenes/World/ZN_Settlement_RedCedar.unity";
        private const string TideCavernsScenePath = "Assets/_TPS/Scenes/Dungeons/DG_TideCaverns.unity";
        private const string QuarryRuinsScenePath = "Assets/_TPS/Scenes/Dungeons/DG_QuarryRuins.unity";
        private const string BattleScenePath = "Assets/_TPS/Scenes/Battle/BTL_Standard.unity";

        [MenuItem("Tools/TPS/World/Install Expanded Playable World")]
        private static void InstallExpandedPlayableWorldMenu()
        {
            InstallExpandedPlayableWorld();
        }

        [MenuItem("Tools/TPS/World/Rebuild Expanded AsterHarbor")]
        private static void RebuildExpandedAsterHarborMenu()
        {
            WorldExpansionAssets assets = PhaseWorldExpansionAssetSeeder.CreateOrUpdateAssets();
            Phase1SceneInstaller.InstallVerticalSlice();
            RebuildExpandedAsterHarbor(assets);
        }

        [MenuItem("Tools/TPS/World/Rebuild Settlements")]
        private static void RebuildSettlementsMenu()
        {
            WorldExpansionAssets assets = PhaseWorldExpansionAssetSeeder.CreateOrUpdateAssets();
            RebuildSettlements(assets);
        }

        [MenuItem("Tools/TPS/Dungeon/Rebuild Dungeon Scaffolds")]
        private static void RebuildDungeonScaffoldsMenu()
        {
            WorldExpansionAssets assets = PhaseWorldExpansionAssetSeeder.CreateOrUpdateAssets();
            RebuildDungeons(assets);
        }

        [MenuItem("Tools/TPS/Battle/Rebuild Standard Arena")]
        private static void RebuildStandardArenaMenu()
        {
            RebuildStandardArena();
        }

        [MenuItem("Tools/TPS/World/Validate Expanded Layout")]
        private static void ValidateExpandedLayoutMenu()
        {
            ValidateExpandedLayout(logResult: true);
        }

        public static void InstallExpandedPlayableWorld()
        {
            WorldExpansionAssets assets = PhaseWorldExpansionAssetSeeder.CreateOrUpdateAssets();
            Phase1SceneInstaller.InstallVerticalSlice();
            RebuildExpandedAsterHarbor(assets);
            RebuildSettlements(assets);
            RebuildDungeons(assets);
            RebuildStandardArena();
            EnsureScenesInBuildProfile();
            ValidateExpandedLayout(logResult: true);
        }

        public static bool ValidateExpandedLayout(bool logResult)
        {
            var errors = new List<string>();
            ValidateSceneBasics(GullwatchScenePath, "WorldRoot", "TRV_ToAsterHarbor", "ENC_Gullwatch_Shoreline", errors);
            ValidateSceneBasics(RedCedarScenePath, "WorldRoot", "TRV_ToAsterHarbor", "ENC_RedCedar_Roadside", errors);
            ValidateSceneBasics(TideCavernsScenePath, "WorldRoot", "TRV_ExitToAsterHarbor", "ENC_TideCaverns_Boss", errors);
            ValidateSceneBasics(QuarryRuinsScenePath, "WorldRoot", "TRV_ExitToAsterHarbor", "ENC_QuarryRuins_Boss", errors);

            if (logResult)
            {
                if (errors.Count == 0)
                {
                    Debug.Log("[TPSWorld] Expanded world validation passed.");
                }
                else
                {
                    for (int i = 0; i < errors.Count; i++)
                    {
                        Debug.LogError($"[TPSWorld] {errors[i]}");
                    }
                }
            }

            return errors.Count == 0;
        }

        private static void ValidateSceneBasics(string scenePath, string rootName, string travelName, string encounterName, List<string> errors)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                errors.Add($"Missing scene '{scenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                if (FindDeep(scene, rootName) == null) errors.Add($"{scenePath} is missing {rootName}.");
                if (FindDeep(scene, "MK_PlayerSpawn_Default") == null) errors.Add($"{scenePath} is missing MK_PlayerSpawn_Default.");
                if (FindDeep(scene, travelName) == null) errors.Add($"{scenePath} is missing {travelName}.");
                if (FindDeep(scene, encounterName) == null) errors.Add($"{scenePath} is missing {encounterName}.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void RebuildExpandedAsterHarbor(WorldExpansionAssets assets)
        {
            Scene scene = EditorSceneManager.OpenScene(AsterHarborScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject worldRoot = FindDeep(scene, "WorldRoot");
                if (worldRoot == null)
                {
                    worldRoot = new GameObject("WorldRoot");
                    SceneManager.MoveGameObjectToScene(worldRoot, scene);
                }

                PhaseEnvironmentTools.EnsureAsterHarborEnvironment(scene, worldRoot, includeAmbient: true, includeVegetation: true);
                EnsureSpawnPoint(worldRoot.transform, "MK_Spawn_Gullwatch_Return", new Vector3(-26f, 0.1f, 14f), Quaternion.Euler(0f, 90f, 0f), "GullwatchDock");
                EnsureSpawnPoint(worldRoot.transform, "MK_Spawn_RedCedar_Return", new Vector3(-27f, 0.1f, -12f), Quaternion.Euler(0f, 85f, 0f), "RedCedarRoad");
                EnsureSpawnPoint(worldRoot.transform, "MK_Spawn_TideCaverns_Return", new Vector3(26f, 0.1f, 13f), Quaternion.Euler(0f, -90f, 0f), "TideCavernsGate");
                EnsureSpawnPoint(worldRoot.transform, "MK_Spawn_QuarryRuins_Return", new Vector3(27f, 0.1f, -13f), Quaternion.Euler(0f, -95f, 0f), "QuarryRuinsGate");

                EnsureTravelAnchor(worldRoot.transform, "TRV_ToGullwatch", new Vector3(-24.5f, 0.5f, 14f), new Vector3(2f, 2f, 2f), "ZN_Settlement_Gullwatch", "Default", "sail to Gullwatch");
                EnsureTravelAnchor(worldRoot.transform, "TRV_ToRedCedar", new Vector3(-24.5f, 0.5f, -12f), new Vector3(2f, 2f, 2f), "ZN_Settlement_RedCedar", "Default", "ride to Red Cedar");
                EnsureTravelAnchor(worldRoot.transform, "TRV_ToTideCaverns", new Vector3(24.5f, 0.5f, 13f), new Vector3(2f, 2f, 2f), "DG_TideCaverns", "Default", "enter Tide Caverns");
                EnsureTravelAnchor(worldRoot.transform, "TRV_ToQuarryRuins", new Vector3(24.5f, 0.5f, -13f), new Vector3(2f, 2f, 2f), "DG_QuarryRuins", "Default", "enter Quarry Ruins");

                EnsureRumorNpc(worldRoot.transform, "NPC_HarborWayfinder", new Vector3(-4f, 0f, 10.5f), assets.GullwatchDialogue, "consult");
                BuildAsterHarborExpansionRoot(worldRoot.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void RebuildSettlements(WorldExpansionAssets assets)
        {
            BuildSettlementScene(
                GullwatchScenePath,
                "ZN_Settlement_Gullwatch",
                "Gullwatch",
                "A small harbor hamlet stretched along the rocks.",
                new Vector3(0f, 0.5f, -9f),
                new Vector3(10f, 0.5f, 3.5f),
                assets.GullwatchDialogue,
                assets.GullwatchEncounter,
                "TRV_ToAsterHarbor",
                "ZN_Town_AsterHarbor",
                "GullwatchDock",
                "ENC_Gullwatch_Shoreline");

            BuildSettlementScene(
                RedCedarScenePath,
                "ZN_Settlement_RedCedar",
                "Red Cedar",
                "An inland roadside hamlet with field edges and timber posts.",
                new Vector3(0f, 0.5f, -10f),
                new Vector3(11f, 0.5f, 2f),
                assets.RedCedarDialogue,
                assets.RedCedarEncounter,
                "TRV_ToAsterHarbor",
                "ZN_Town_AsterHarbor",
                "RedCedarRoad",
                "ENC_RedCedar_Roadside");
        }

        private static void RebuildDungeons(WorldExpansionAssets assets)
        {
            BuildDungeonScene(
                TideCavernsScenePath,
                "DG_TideCaverns",
                "Tide Caverns",
                assets.TideCavernsPatrol,
                assets.TideCavernsBoss,
                "TRV_ExitToAsterHarbor",
                "ZN_Town_AsterHarbor",
                "TideCavernsGate",
                "ENC_TideCaverns_Patrol",
                "ENC_TideCaverns_Boss");

            BuildDungeonScene(
                QuarryRuinsScenePath,
                "DG_QuarryRuins",
                "Quarry Ruins",
                assets.QuarryRuinsPatrol,
                assets.QuarryRuinsBoss,
                "TRV_ExitToAsterHarbor",
                "ZN_Town_AsterHarbor",
                "QuarryRuinsGate",
                "ENC_QuarryRuins_Patrol",
                "ENC_QuarryRuins_Boss");
        }

        private static void RebuildStandardArena()
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject battleRoot = FindDeep(scene, "BattleRoot");
                if (battleRoot == null)
                {
                    battleRoot = new GameObject("BattleRoot");
                    SceneManager.MoveGameObjectToScene(battleRoot, scene);
                }

                GameObject generatedRoot = EnsureManagedNode(battleRoot.transform, "ENV_Battle_Generated", "battle_standard");
                GameObject blockout = EnsureManagedNode(generatedRoot.transform, "ENV_Blockout", "battle_standard");
                GameObject props = EnsureManagedNode(generatedRoot.transform, "ENV_Props", "battle_standard");
                GameObject debug = EnsureManagedNode(generatedRoot.transform, "ENV_Debug", "battle_standard");

                BuildBattleBlockout(blockout.transform);
                BuildBattleProps(props.transform);
                BuildBattleDebug(debug.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BuildSettlementScene(string scenePath, string sceneName, string districtName, string districtDescription, Vector3 travelPosition, Vector3 encounterPosition, DialogueDefinition dialogue, EncounterDefinition encounter, string travelAnchorName, string returnSceneName, string returnSpawnTarget, string encounterAnchorName)
        {
            Scene scene = OpenOrCreateScene(scenePath, sceneName);
            try
            {
                GameObject worldRoot = EnsureSceneRoot(scene, "WorldRoot");
                EnsureSpawnPoint(worldRoot.transform, "MK_PlayerSpawn_Default", Vector3.zero, Quaternion.identity, "Default");
                EnsureTravelAnchor(worldRoot.transform, travelAnchorName, travelPosition, new Vector3(2.4f, 2f, 2.4f), returnSceneName, returnSpawnTarget, $"return to {districtName}");
                EnsureDialogueNpc(worldRoot.transform, $"NPC_{sceneName}_Guide", new Vector3(-3f, 0f, 2.5f), dialogue, "talk");
                EnsureEncounterAnchor(worldRoot.transform, encounterAnchorName, encounterPosition, encounter, encounter.ZoneId, hideWhenCleared: true);
                BuildSettlementGenerated(worldRoot.transform, districtName, districtDescription);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BuildDungeonScene(string scenePath, string sceneName, string districtName, EncounterDefinition patrolEncounter, EncounterDefinition bossEncounter, string travelAnchorName, string returnSceneName, string returnSpawnTarget, string patrolAnchorName, string bossAnchorName)
        {
            Scene scene = OpenOrCreateScene(scenePath, sceneName);
            try
            {
                GameObject worldRoot = EnsureSceneRoot(scene, "WorldRoot");
                EnsureSpawnPoint(worldRoot.transform, "MK_PlayerSpawn_Default", new Vector3(0f, 0f, -10f), Quaternion.identity, "Default");
                EnsureTravelAnchor(worldRoot.transform, travelAnchorName, new Vector3(0f, 0.5f, -12f), new Vector3(2.2f, 2f, 2.2f), returnSceneName, returnSpawnTarget, "leave dungeon");
                EnsureEncounterAnchor(worldRoot.transform, patrolAnchorName, new Vector3(0f, 0.5f, -1.5f), patrolEncounter, patrolEncounter.ZoneId, hideWhenCleared: false);
                EnsureEncounterAnchor(worldRoot.transform, bossAnchorName, new Vector3(0f, 0.5f, 9.5f), bossEncounter, bossEncounter.ZoneId, hideWhenCleared: true);
                BuildDungeonGenerated(worldRoot.transform, districtName);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Scene OpenOrCreateScene(string scenePath, string sceneName)
        {
            if (System.IO.File.Exists(scenePath))
            {
                return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EnsureSceneRoot(scene, sceneName);
            EditorSceneManager.SaveScene(scene, scenePath);
            return scene;
        }

        private static GameObject EnsureSceneRoot(Scene scene, string rootName)
        {
            GameObject root = FindDeep(scene, rootName);
            if (root == null)
            {
                root = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return root;
        }

        private static void EnsureSpawnPoint(Transform parent, string name, Vector3 position, Quaternion rotation, string spawnId)
        {
            GameObject target = FindImmediateChild(parent, name);
            if (target == null)
            {
                target = new GameObject(name);
                target.transform.SetParent(parent, false);
            }

            target.transform.localPosition = position;
            target.transform.localRotation = rotation;
            target.transform.localScale = Vector3.one;
            SpawnPoint spawn = target.GetComponent<SpawnPoint>();
            if (spawn == null)
            {
                spawn = target.AddComponent<SpawnPoint>();
            }

            SerializedObject so = new SerializedObject(spawn);
            so.FindProperty("_spawnId").stringValue = spawnId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawn);
        }

        private static void EnsureTravelAnchor(Transform parent, string name, Vector3 position, Vector3 scale, string targetSceneName, string targetSpawnId, string interactionLabel)
        {
            GameObject target = FindImmediateChild(parent, name);
            if (target == null)
            {
                target = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                target.name = name;
                target.transform.SetParent(parent, false);
            }

            target.transform.localPosition = position;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = scale;

            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider>();
            }

            collider.isTrigger = true;
            SceneTravelAnchor anchor = target.GetComponent<SceneTravelAnchor>();
            if (anchor == null)
            {
                anchor = target.AddComponent<SceneTravelAnchor>();
            }

            SerializedObject so = new SerializedObject(anchor);
            so.FindProperty("_travelId").stringValue = name.ToLowerInvariant();
            so.FindProperty("_targetSceneName").stringValue = targetSceneName;
            so.FindProperty("_targetSpawnId").stringValue = targetSpawnId;
            so.FindProperty("_interactionLabel").stringValue = interactionLabel;
            so.FindProperty("_hideWhenUnavailable").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anchor);
        }

        private static void EnsureEncounterAnchor(Transform parent, string name, Vector3 position, EncounterDefinition encounterDefinition, string zoneId, bool hideWhenCleared)
        {
            GameObject target = FindImmediateChild(parent, name);
            if (target == null)
            {
                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = name;
                target.transform.SetParent(parent, false);
            }

            target.transform.localPosition = position;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = new Vector3(2.2f, 2f, 2.2f);

            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider>();
            }

            collider.isTrigger = true;
            EncounterAnchor anchor = target.GetComponent<EncounterAnchor>();
            if (anchor == null)
            {
                anchor = target.AddComponent<EncounterAnchor>();
            }

            SerializedObject so = new SerializedObject(anchor);
            so.FindProperty("_anchorId").stringValue = name.ToLowerInvariant();
            so.FindProperty("_zoneId").stringValue = zoneId;
            so.FindProperty("_directEncounter").objectReferenceValue = encounterDefinition;
            so.FindProperty("_useZoneEncounterTable").boolValue = false;
            so.FindProperty("_triggerOnEnter").boolValue = false;
            so.FindProperty("_triggerOnce").boolValue = hideWhenCleared;
            so.FindProperty("_hideWhenCleared").boolValue = hideWhenCleared;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anchor);
        }

        private static void EnsureDialogueNpc(Transform parent, string name, Vector3 position, DialogueDefinition dialogue, string interactionLabel)
        {
            GameObject target = FindImmediateChild(parent, name);
            if (target == null)
            {
                target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                target.name = name;
                target.transform.SetParent(parent, false);
            }

            target.transform.localPosition = position;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            DialogueAnchor anchor = target.GetComponent<DialogueAnchor>();
            if (anchor == null)
            {
                anchor = target.AddComponent<DialogueAnchor>();
            }

            SerializedObject so = new SerializedObject(anchor);
            so.FindProperty("_anchorId").stringValue = name.ToLowerInvariant();
            so.FindProperty("_dialogueDefinition").objectReferenceValue = dialogue;
            so.FindProperty("_interactionLabel").stringValue = interactionLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(anchor);
        }

        private static void EnsureRumorNpc(Transform parent, string name, Vector3 position, DialogueDefinition dialogue, string interactionLabel)
        {
            EnsureDialogueNpc(parent, name, position, dialogue, interactionLabel);
        }

        private static void BuildAsterHarborExpansionRoot(Transform worldRoot)
        {
            GameObject root = EnsureManagedNode(worldRoot, "ENV_AsterHarborExpansion", "aster_harbor_expansion");
            GameObject districts = EnsureManagedNode(root.transform, "ENV_Districts", "aster_harbor_expansion");
            GameObject frontier = EnsureManagedNode(root.transform, "ENV_Outskirts", "aster_harbor_expansion");
            GameObject travel = EnsureManagedNode(root.transform, "ENV_TravelLandmarks", "aster_harbor_expansion");

            BuildPrimitiveSlot(districts.transform, "DIST_MarketArcade", PrimitiveType.Cube, new Vector3(0f, 0.3f, 12f), new Vector3(20f, 0.4f, 6f), "Expanded market frontage.");
            BuildPrimitiveSlot(districts.transform, "DIST_EastHarbor", PrimitiveType.Cube, new Vector3(21f, 0.2f, 4f), new Vector3(10f, 0.35f, 12f), "Expanded dock district.");
            BuildPrimitiveSlot(districts.transform, "DIST_WestResidential", PrimitiveType.Cube, new Vector3(-20f, 0.2f, 3f), new Vector3(14f, 0.35f, 15f), "Expanded residential district.");
            BuildPrimitiveSlot(frontier.transform, "DIST_NorthRoad", PrimitiveType.Cube, new Vector3(0f, 0.2f, 18f), new Vector3(7f, 0.3f, 8f), "Northern road and lookout.");
            BuildPrimitiveSlot(frontier.transform, "DIST_SouthRoad", PrimitiveType.Cube, new Vector3(0f, 0.2f, -16f), new Vector3(8f, 0.3f, 9f), "Southern encounter fringe.");

            BuildHouseRow(districts.transform, "ROW_NorthMarket", new Vector3(-8f, 0f, 13.5f), 5, 3.6f, new Vector3(3f, 3f, 2.8f));
            BuildHouseRow(districts.transform, "ROW_WestResidential", new Vector3(-21f, 0f, 9.5f), 5, 4f, new Vector3(3.2f, 3f, 3.4f));
            BuildHouseRow(districts.transform, "ROW_WestResidential_South", new Vector3(-21f, 0f, -0.5f), 4, 4f, new Vector3(3f, 2.8f, 3f));
            BuildHouseRow(districts.transform, "ROW_EastHarbor_Sheds", new Vector3(19f, 0f, 10.5f), 4, 3.8f, new Vector3(3.6f, 2.6f, 2.8f));

            BuildLandmarkCluster(travel.transform, "LMK_GullwatchRoad", new Vector3(-24f, 0f, 14f));
            BuildLandmarkCluster(travel.transform, "LMK_RedCedarRoad", new Vector3(-24f, 0f, -12f));
            BuildLandmarkCluster(travel.transform, "LMK_TideCavernsRoad", new Vector3(24f, 0f, 13f));
            BuildLandmarkCluster(travel.transform, "LMK_QuarryRuinsRoad", new Vector3(24f, 0f, -13f));
        }

        private static void BuildSettlementGenerated(Transform worldRoot, string districtName, string description)
        {
            GameObject root = EnsureManagedNode(worldRoot, "ENV_Generated", districtName.ToLowerInvariant());
            GameObject blockout = EnsureManagedNode(root.transform, "ENV_Blockout", districtName.ToLowerInvariant());
            GameObject buildings = EnsureManagedNode(root.transform, "ENV_Buildings", districtName.ToLowerInvariant());
            GameObject props = EnsureManagedNode(root.transform, "ENV_Props", districtName.ToLowerInvariant());
            GameObject ambient = EnsureManagedNode(root.transform, "ENV_Ambient", districtName.ToLowerInvariant());

            BuildPrimitiveSlot(blockout.transform, "PAD_Main", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(20f, 0.35f, 22f), description);
            BuildPrimitiveSlot(blockout.transform, "PATH_Center", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(4f, 0.12f, 19f), "Main route through settlement.");
            BuildHouseRow(buildings.transform, "ROW_A", new Vector3(-7f, 0f, 6f), 3, 5f, new Vector3(3.2f, 3f, 3f));
            BuildHouseRow(buildings.transform, "ROW_B", new Vector3(6f, 0f, 3f), 3, 5f, new Vector3(3f, 2.8f, 3f));
            BuildCrateLine(props.transform, "PRP_Crates", new Vector3(6f, 0f, -4f), 4, 1f);
            BuildFenceLine(props.transform, "PRP_Fence", new Vector3(-9f, 0f, -5f), 8, 1.3f);
            BuildAmbientPair(ambient.transform, "AMB_TownPair", new Vector3(1f, 0f, 2f));
            BuildAmbientSolo(ambient.transform, "AMB_Lookout", new Vector3(7f, 0f, 7f));
            BuildAmbientCreature(ambient.transform, "AMB_Creature", new Vector3(-5f, 0f, -3f));
        }

        private static void BuildDungeonGenerated(Transform worldRoot, string districtName)
        {
            GameObject root = EnsureManagedNode(worldRoot, "ENV_Generated", districtName.ToLowerInvariant());
            GameObject blockout = EnsureManagedNode(root.transform, "ENV_Blockout", districtName.ToLowerInvariant());
            GameObject props = EnsureManagedNode(root.transform, "ENV_Props", districtName.ToLowerInvariant());
            GameObject debug = EnsureManagedNode(root.transform, "ENV_Debug", districtName.ToLowerInvariant());

            BuildPrimitiveSlot(blockout.transform, "ROOM_Entrance", PrimitiveType.Cube, new Vector3(0f, 0.2f, -8f), new Vector3(10f, 0.3f, 7f), "Dungeon entrance room.");
            BuildPrimitiveSlot(blockout.transform, "ROOM_Mid", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(8f, 0.3f, 8f), "Dungeon mid room.");
            BuildPrimitiveSlot(blockout.transform, "ROOM_Boss", PrimitiveType.Cube, new Vector3(0f, 0.2f, 9f), new Vector3(12f, 0.3f, 9f), "Dungeon endpoint room.");
            BuildPrimitiveSlot(blockout.transform, "PATH_Connector_A", PrimitiveType.Cube, new Vector3(0f, 0.25f, -4f), new Vector3(3f, 0.12f, 3.5f), "Connector path.");
            BuildPrimitiveSlot(blockout.transform, "PATH_Connector_B", PrimitiveType.Cube, new Vector3(0f, 0.25f, 5f), new Vector3(3f, 0.12f, 3.5f), "Connector path.");

            BuildLandmarkCluster(props.transform, "PRP_EntranceLandmark", new Vector3(3.5f, 0f, -9f));
            BuildLandmarkCluster(props.transform, "PRP_MidLandmark", new Vector3(-3.5f, 0f, 0f));
            BuildLandmarkCluster(props.transform, "PRP_BossLandmark", new Vector3(0f, 0f, 10.5f));
            BuildPrimitiveSlot(debug.transform, "DBG_RoomMarker", PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 9f), new Vector3(0.4f, 2.8f, 0.4f), "Boss room readability marker.");
        }

        private static void BuildBattleBlockout(Transform container)
        {
            BuildPrimitiveSlot(container, "ARENA_Floor", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(22f, 0.2f, 18f), "Battle arena floor.");
            BuildPrimitiveSlot(container, "ARENA_TeamLane", PrimitiveType.Cube, new Vector3(-6f, 0.12f, -1.5f), new Vector3(5f, 0.12f, 9f), "Party lane.");
            BuildPrimitiveSlot(container, "ARENA_EnemyLane", PrimitiveType.Cube, new Vector3(6f, 0.12f, 1.5f), new Vector3(5f, 0.12f, 9f), "Enemy lane.");
            BuildPrimitiveSlot(container, "ARENA_Backdrop_West", PrimitiveType.Cube, new Vector3(-12f, 2.5f, 0f), new Vector3(0.6f, 5f, 20f), "West arena wall.");
            BuildPrimitiveSlot(container, "ARENA_Backdrop_East", PrimitiveType.Cube, new Vector3(12f, 2.5f, 0f), new Vector3(0.6f, 5f, 20f), "East arena wall.");
        }

        private static void BuildBattleProps(Transform container)
        {
            BuildCrateLine(container, "PRP_BattleHarborCrates", new Vector3(8f, 0f, -5f), 4, 1f);
            BuildFenceLine(container, "PRP_BattleBarrier", new Vector3(-9f, 0f, 6f), 6, 1.5f);
            BuildLandmarkCluster(container, "PRP_BattleBeacon", new Vector3(10f, 0f, 6f));
        }

        private static void BuildBattleDebug(Transform container)
        {
            BuildPrimitiveSlot(container, "DBG_PartyFocus", PrimitiveType.Cylinder, new Vector3(-6f, 1.6f, -1.5f), new Vector3(0.3f, 3.2f, 0.3f), "Party lane marker.");
            BuildPrimitiveSlot(container, "DBG_EnemyFocus", PrimitiveType.Cylinder, new Vector3(6f, 1.6f, 1.5f), new Vector3(0.3f, 3.2f, 0.3f), "Enemy lane marker.");
        }

        private static void BuildHouseRow(Transform parent, string groupName, Vector3 startPosition, int count, float spacing, Vector3 scale)
        {
            GameObject group = EnsureManagedNode(parent, groupName, GetGenerationId(parent));
            for (int i = 0; i < count; i++)
            {
                BuildHouseSlot(group.transform, $"BLD_{i + 1:00}", startPosition + new Vector3(i * spacing, 0f, 0f), scale);
            }
        }

        private static void BuildHouseSlot(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = position;
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;
            if (HasManualChildren(slot, GetGenerationId(parent))) return;

            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            CreatePrimitiveVisual(slot.transform, "GEN_Body", PrimitiveType.Cube, new Vector3(0f, scale.y * 0.5f, 0f), scale, GetGenerationId(parent), "Building shell");
            CreatePrimitiveVisual(slot.transform, "GEN_Roof", PrimitiveType.Cube, new Vector3(0f, scale.y + 0.3f, 0f), new Vector3(scale.x + 0.4f, 0.35f, scale.z + 0.5f), GetGenerationId(parent), "Roof shell");
        }

        private static void BuildCrateLine(Transform parent, string name, Vector3 startPosition, int count, float spacing)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = startPosition;
            if (HasManualChildren(slot, GetGenerationId(parent))) return;

            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            for (int i = 0; i < count; i++)
            {
                CreatePrimitiveVisual(slot.transform, $"GEN_Crate_{i + 1}", PrimitiveType.Cube, new Vector3(i * spacing, 0.35f, 0f), new Vector3(0.7f, 0.7f, 0.7f), GetGenerationId(parent), "Cargo crate");
            }
        }

        private static void BuildFenceLine(Transform parent, string name, Vector3 startPosition, int count, float spacing)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = startPosition;
            if (HasManualChildren(slot, GetGenerationId(parent))) return;

            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            for (int i = 0; i < count; i++)
            {
                CreatePrimitiveVisual(slot.transform, $"GEN_Post_{i + 1}", PrimitiveType.Cylinder, new Vector3(i * spacing, 0.55f, 0f), new Vector3(0.16f, 0.55f, 0.16f), GetGenerationId(parent), "Fence post");
            }
        }

        private static void BuildLandmarkCluster(Transform parent, string name, Vector3 position)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = position;
            if (HasManualChildren(slot, GetGenerationId(parent))) return;

            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            CreatePrimitiveVisual(slot.transform, "GEN_Base", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 1.2f), GetGenerationId(parent), "Landmark base");
            CreatePrimitiveVisual(slot.transform, "GEN_Spire", PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.25f, 2f, 0.25f), GetGenerationId(parent), "Landmark spire");
        }

        private static void BuildAmbientPair(Transform parent, string name, Vector3 position)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = position;
            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            CreatePrimitiveVisual(slot.transform, "GEN_A", PrimitiveType.Capsule, new Vector3(-0.45f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), GetGenerationId(parent), "Ambient actor");
            CreatePrimitiveVisual(slot.transform, "GEN_B", PrimitiveType.Capsule, new Vector3(0.45f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), GetGenerationId(parent), "Ambient actor");
        }

        private static void BuildAmbientSolo(Transform parent, string name, Vector3 position)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = position;
            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            CreatePrimitiveVisual(slot.transform, "GEN_A", PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), GetGenerationId(parent), "Ambient actor");
        }

        private static void BuildAmbientCreature(Transform parent, string name, Vector3 position)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = position;
            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            CreatePrimitiveVisual(slot.transform, "GEN_Creature", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.45f, 0.3f, 0.45f), GetGenerationId(parent), "Ambient creature");
        }

        private static void BuildPrimitiveSlot(Transform parent, string name, PrimitiveType primitiveType, Vector3 position, Vector3 scale, string notes)
        {
            GameObject slot = EnsureManagedNode(parent, name, GetGenerationId(parent));
            slot.transform.localPosition = position;
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;
            if (HasManualChildren(slot, GetGenerationId(parent))) return;

            ClearManagedChildren(slot.transform, GetGenerationId(parent));
            CreatePrimitiveVisual(slot.transform, "GEN_Visual", primitiveType, Vector3.zero, scale, GetGenerationId(parent), notes);
        }

        private static GameObject EnsureManagedNode(Transform parent, string name, string generationId)
        {
            GameObject target = FindImmediateChild(parent, name);
            if (target == null)
            {
                target = new GameObject(name);
                target.transform.SetParent(parent, false);
            }

            EnvironmentGeneratedMarker marker = target.GetComponent<EnvironmentGeneratedMarker>();
            if (marker == null) marker = target.AddComponent<EnvironmentGeneratedMarker>();

            SerializedObject so = new SerializedObject(marker);
            so.FindProperty("_generationId").stringValue = generationId;
            so.FindProperty("_category").enumValueIndex = (int)EnvironmentGeneratedCategory.Root;
            so.FindProperty("_replaceSafe").boolValue = true;
            so.FindProperty("_preserveManualChildren").boolValue = true;
            so.FindProperty("_notes").stringValue = $"Generated scaffold for {name}. Add art as child objects and preserve the slot root.";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(marker);
            return target;
        }

        private static GameObject CreatePrimitiveVisual(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, string generationId, string notes)
        {
            GameObject target = GameObject.CreatePrimitive(type);
            target.name = name;
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = localScale;
            EnvironmentGeneratedMarker marker = target.AddComponent<EnvironmentGeneratedMarker>();
            SerializedObject so = new SerializedObject(marker);
            so.FindProperty("_generationId").stringValue = generationId;
            so.FindProperty("_category").enumValueIndex = (int)EnvironmentGeneratedCategory.Prop;
            so.FindProperty("_replaceSafe").boolValue = true;
            so.FindProperty("_preserveManualChildren").boolValue = false;
            so.FindProperty("_notes").stringValue = notes;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(marker);
            return target;
        }

        private static bool HasManualChildren(GameObject slot, string generationId)
        {
            for (int i = 0; i < slot.transform.childCount; i++)
            {
                Transform child = slot.transform.GetChild(i);
                EnvironmentGeneratedMarker marker = child.GetComponent<EnvironmentGeneratedMarker>();
                if (marker == null || !string.Equals(marker.GenerationId, generationId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ClearManagedChildren(Transform parent, string generationId)
        {
            var toDestroy = new List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;
                EnvironmentGeneratedMarker marker = child.GetComponent<EnvironmentGeneratedMarker>();
                if (marker != null && string.Equals(marker.GenerationId, generationId, StringComparison.Ordinal))
                {
                    toDestroy.Add(child);
                }
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(toDestroy[i]);
            }
        }

        private static string GetGenerationId(Transform parent)
        {
            EnvironmentGeneratedMarker marker = parent.GetComponent<EnvironmentGeneratedMarker>();
            return marker != null ? marker.GenerationId : "world_expansion_generated";
        }

        private static GameObject FindDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject found = FindChild(root, name);
                if (found != null) return found;
            }

            return null;
        }

        private static GameObject FindChild(GameObject parent, string name)
        {
            if (parent.name == name) return parent;

            foreach (Transform child in parent.transform)
            {
                GameObject found = FindChild(child.gameObject, name);
                if (found != null) return found;
            }

            return null;
        }

        private static GameObject FindImmediateChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child.gameObject;
            }

            return null;
        }

        private static void EnsureScenesInBuildProfile()
        {
            EnsureBuildProfileScenesOrdered(new[]
            {
                AsterHarborScenePath,
                BattleScenePath,
                GullwatchScenePath,
                RedCedarScenePath,
                TideCavernsScenePath,
                QuarryRuinsScenePath
            });

            var editorScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AppendEditorBuildScene(editorScenes, GullwatchScenePath);
            AppendEditorBuildScene(editorScenes, RedCedarScenePath);
            AppendEditorBuildScene(editorScenes, TideCavernsScenePath);
            AppendEditorBuildScene(editorScenes, QuarryRuinsScenePath);
            EditorBuildSettings.scenes = editorScenes.ToArray();
        }

        private static void EnsureBuildProfileScenesOrdered(IReadOnlyList<string> orderedScenePaths)
        {
            UnityEngine.Object profile = AssetDatabase.LoadMainAssetAtPath(BuildProfilePath);
            if (profile == null) return;

            SerializedObject so = new SerializedObject(profile);
            SerializedProperty scenes = so.FindProperty("m_Scenes");
            if (scenes == null) return;

            var existingEntries = new List<(string path, string guid, bool enabled)>();
            for (int i = 0; i < scenes.arraySize; i++)
            {
                SerializedProperty entry = scenes.GetArrayElementAtIndex(i);
                existingEntries.Add((
                    entry.FindPropertyRelative("m_path").stringValue,
                    entry.FindPropertyRelative("m_guid").stringValue,
                    entry.FindPropertyRelative("m_enabled").boolValue));
            }

            int battleIndex = existingEntries.FindIndex(entry => entry.path == BattleScenePath);
            if (battleIndex < 0)
            {
                battleIndex = existingEntries.Count;
            }

            foreach (string scenePath in orderedScenePaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(scenePath);
                if (string.IsNullOrWhiteSpace(guid))
                {
                    continue;
                }

                int existingIndex = existingEntries.FindIndex(entry => entry.path == scenePath);
                if (existingIndex >= 0)
                {
                    existingEntries[existingIndex] = (scenePath, guid, true);
                    continue;
                }

                int insertIndex = scenePath == AsterHarborScenePath || scenePath == BattleScenePath
                    ? battleIndex
                    : battleIndex + 1;

                existingEntries.Insert(insertIndex, (scenePath, guid, true));
                if (scenePath != AsterHarborScenePath && scenePath != BattleScenePath)
                {
                    battleIndex++;
                }
            }

            EnsureEntryOrder(existingEntries, BattleScenePath, GullwatchScenePath);
            EnsureEntryOrder(existingEntries, GullwatchScenePath, RedCedarScenePath);
            EnsureEntryOrder(existingEntries, RedCedarScenePath, TideCavernsScenePath);
            EnsureEntryOrder(existingEntries, TideCavernsScenePath, QuarryRuinsScenePath);

            scenes.arraySize = existingEntries.Count;
            for (int i = 0; i < existingEntries.Count; i++)
            {
                SerializedProperty entry = scenes.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("m_enabled").boolValue = existingEntries[i].enabled;
                entry.FindPropertyRelative("m_path").stringValue = existingEntries[i].path;
                entry.FindPropertyRelative("m_guid").stringValue = existingEntries[i].guid;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureEntryOrder(List<(string path, string guid, bool enabled)> entries, string anchorPath, string movingPath)
        {
            int anchorIndex = entries.FindIndex(entry => entry.path == anchorPath);
            int movingIndex = entries.FindIndex(entry => entry.path == movingPath);
            if (anchorIndex < 0 || movingIndex < 0 || movingIndex > anchorIndex)
            {
                return;
            }

            var movingEntry = entries[movingIndex];
            entries.RemoveAt(movingIndex);
            anchorIndex = entries.FindIndex(entry => entry.path == anchorPath);
            entries.Insert(anchorIndex + 1, movingEntry);
        }

        private static void AppendEditorBuildScene(List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }
    }
}
