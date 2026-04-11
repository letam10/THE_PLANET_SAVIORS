using System;
using System.Collections.Generic;
using TPS.Runtime.Combat;
using TPS.Runtime.Conditions;
using TPS.Runtime.Dialogue;
using TPS.Runtime.Spawn;
using TPS.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
                ValidateSpawnGraph(scene, scenePath, errors);
                ValidateTravelTargets(scene, scenePath, errors);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateSpawnGraph(Scene scene, string scenePath, List<string> errors)
        {
            SpawnPoint[] spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            bool foundAny = false;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SpawnPoint point = spawnPoints[i];
                if (point == null || point.gameObject.scene != scene)
                {
                    continue;
                }

                foundAny = true;
                if (!seen.Add(point.SpawnId))
                {
                    errors.Add($"{scenePath} has duplicate spawn id '{point.SpawnId}'.");
                }

                if (!HasGroundBelow(point.transform.position))
                {
                    errors.Add($"{scenePath} spawn '{point.SpawnId}' is not snapped to valid ground.");
                }
            }

            if (!foundAny)
            {
                errors.Add($"{scenePath} has no SpawnPoint components.");
            }
        }

        private static void ValidateTravelTargets(Scene scene, string scenePath, List<string> errors)
        {
            SceneTravelAnchor[] travelAnchors = Object.FindObjectsByType<SceneTravelAnchor>(FindObjectsInactive.Include);
            for (int i = 0; i < travelAnchors.Length; i++)
            {
                SceneTravelAnchor anchor = travelAnchors[i];
                if (anchor == null || anchor.gameObject.scene != scene)
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(anchor);
                string targetSceneName = so.FindProperty("_targetSceneName").stringValue;
                string targetSpawnId = so.FindProperty("_targetSpawnId").stringValue;
                string targetScenePath = ResolveScenePath(targetSceneName);
                if (string.IsNullOrWhiteSpace(targetScenePath) || !System.IO.File.Exists(targetScenePath))
                {
                    errors.Add($"{scenePath} travel anchor '{anchor.name}' points to missing scene '{targetSceneName}'.");
                    continue;
                }

                if (!SceneContainsSpawn(targetScenePath, targetSpawnId))
                {
                    errors.Add($"{scenePath} travel anchor '{anchor.name}' points to missing spawn '{targetSpawnId}' in '{targetSceneName}'.");
                }
            }
        }

        private static string ResolveScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            if (guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        private static bool SceneContainsSpawn(string scenePath, string spawnId)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                SpawnPoint[] spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include);
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    SpawnPoint point = spawnPoints[i];
                    if (point != null && point.gameObject.scene == scene && string.Equals(point.SpawnId, spawnId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool HasGroundBelow(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * 8f;
            return Physics.Raycast(origin, Vector3.down, out _, 24f, ~0, QueryTriggerInteraction.Ignore);
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
                BuildAsterHarborGoldenPathFocus(worldRoot.transform);

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
                if (sceneName == "ZN_Settlement_Gullwatch")
                {
                    BuildGullwatchFocusedPass(worldRoot.transform);
                }
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
                if (sceneName == "DG_TideCaverns")
                {
                    BuildTideCavernsFocusedPass(worldRoot.transform);
                }
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
            GameObject props = EnsureManagedNode(root.transform, "ENV_ExpansionProps", "aster_harbor_expansion");
            GameObject ambient = EnsureManagedNode(root.transform, "ENV_ExpansionAmbient", "aster_harbor_expansion");

            BuildPrimitiveSlot(districts.transform, "DIST_MarketArcade", PrimitiveType.Cube, new Vector3(0f, 0.3f, 12f), new Vector3(20f, 0.4f, 6f), "Expanded market frontage.");
            BuildPrimitiveSlot(districts.transform, "DIST_EastHarbor", PrimitiveType.Cube, new Vector3(21f, 0.2f, 4f), new Vector3(10f, 0.35f, 12f), "Expanded dock district.");
            BuildPrimitiveSlot(districts.transform, "DIST_WestResidential", PrimitiveType.Cube, new Vector3(-20f, 0.2f, 3f), new Vector3(14f, 0.35f, 15f), "Expanded residential district.");
            BuildPrimitiveSlot(frontier.transform, "DIST_NorthRoad", PrimitiveType.Cube, new Vector3(0f, 0.2f, 18f), new Vector3(7f, 0.3f, 8f), "Northern road and lookout.");
            BuildPrimitiveSlot(frontier.transform, "DIST_SouthRoad", PrimitiveType.Cube, new Vector3(0f, 0.2f, -16f), new Vector3(8f, 0.3f, 9f), "Southern encounter fringe.");

            BuildHouseRow(districts.transform, "ROW_NorthMarket", new Vector3(-8f, 0f, 13.5f), 5, 3.6f, new Vector3(3f, 3f, 2.8f));
            BuildHouseRow(districts.transform, "ROW_WestResidential", new Vector3(-21f, 0f, 9.5f), 5, 4f, new Vector3(3.2f, 3f, 3.4f));
            BuildHouseRow(districts.transform, "ROW_WestResidential_South", new Vector3(-21f, 0f, -0.5f), 4, 4f, new Vector3(3f, 2.8f, 3f));
            BuildHouseRow(districts.transform, "ROW_EastHarbor_Sheds", new Vector3(19f, 0f, 10.5f), 4, 3.8f, new Vector3(3.6f, 2.6f, 2.8f));
            BuildHouseRow(districts.transform, "ROW_MarketSouth", new Vector3(-6f, 0f, 8.5f), 4, 4f, new Vector3(2.8f, 2.8f, 2.4f));
            BuildHouseRow(districts.transform, "ROW_DockStorefronts", new Vector3(16.5f, 0f, 1f), 3, 4.1f, new Vector3(3.4f, 3f, 3f));
            BuildHouseRow(frontier.transform, "ROW_NorthLookout", new Vector3(-4.5f, 0f, 18.8f), 3, 4.2f, new Vector3(2.8f, 2.8f, 2.6f));
            BuildHouseRow(frontier.transform, "ROW_SouthGate", new Vector3(-4f, 0f, -17.8f), 3, 4f, new Vector3(2.6f, 2.6f, 2.8f));

            BuildCrateLine(props.transform, "PRP_MarketCarts", new Vector3(3f, 0f, 10.5f), 6, 1.1f);
            BuildCrateLine(props.transform, "PRP_DockCargoLong", new Vector3(18f, 0f, 6f), 6, 1f);
            BuildFenceLine(props.transform, "PRP_WestBlocks", new Vector3(-23f, 0f, -4.2f), 9, 1.6f);
            BuildFenceLine(props.transform, "PRP_EastDocksRail", new Vector3(23f, 0f, -3f), 9, 1.4f);
            BuildLandmarkCluster(props.transform, "LMK_MarketTower", new Vector3(-1f, 0f, 16.5f));
            BuildLandmarkCluster(props.transform, "LMK_WestBell", new Vector3(-24f, 0f, 7f));
            BuildLandmarkCluster(props.transform, "LMK_DockMast", new Vector3(24f, 0f, 8f));

            BuildAmbientPair(ambient.transform, "AMB_NorthMarketPair", new Vector3(0.5f, 0f, 14.2f));
            BuildAmbientPair(ambient.transform, "AMB_WestFamily", new Vector3(-18.5f, 0f, 6.8f));
            BuildAmbientSolo(ambient.transform, "AMB_DockGuard", new Vector3(21.6f, 0f, 3.8f));
            BuildAmbientSolo(ambient.transform, "AMB_GateWatcher", new Vector3(-0.8f, 0f, -14.2f));
            BuildAmbientCreature(ambient.transform, "AMB_BirdNorth", new Vector3(1.5f, 1.8f, 16.2f));
            BuildAmbientCreature(ambient.transform, "AMB_DogSouth", new Vector3(-6.5f, 0f, -12.6f));

            BuildLandmarkCluster(travel.transform, "LMK_GullwatchRoad", new Vector3(-24f, 0f, 14f));
            BuildLandmarkCluster(travel.transform, "LMK_RedCedarRoad", new Vector3(-24f, 0f, -12f));
            BuildLandmarkCluster(travel.transform, "LMK_TideCavernsRoad", new Vector3(24f, 0f, 13f));
            BuildLandmarkCluster(travel.transform, "LMK_QuarryRuinsRoad", new Vector3(24f, 0f, -13f));
        }

        private static void BuildAsterHarborGoldenPathFocus(Transform worldRoot)
        {
            GameObject root = EnsureManagedNode(worldRoot, "ENV_GoldenPathFocus", "aster_harbor_focus");
            GameObject pathProps = EnsureManagedNode(root.transform, "ENV_PathProps", "aster_harbor_focus");
            GameObject landmarks = EnsureManagedNode(root.transform, "ENV_Landmarks", "aster_harbor_focus");
            GameObject ambient = EnsureManagedNode(root.transform, "ENV_Ambient", "aster_harbor_focus");

            BuildCrateLine(pathProps.transform, "PRP_DockLaneMarkers", new Vector3(5f, 0f, 2.5f), 5, 0.95f);
            BuildFenceLine(pathProps.transform, "PRP_GullwatchRoutePosts", new Vector3(-20f, 0f, 13f), 5, 1.35f);
            BuildLandmarkCluster(landmarks.transform, "LMK_DockObjectiveBoard", new Vector3(6.5f, 0f, 3f));
            BuildLandmarkCluster(landmarks.transform, "LMK_GullwatchGuide", new Vector3(-22f, 0f, 14f));
            BuildAmbientSolo(ambient.transform, "AMB_PathRunner", new Vector3(4.2f, 0f, 3.8f));
            BuildAmbientCreature(ambient.transform, "AMB_GullwatchBirds", new Vector3(-18.2f, 1.9f, 13.4f));
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
            BuildPrimitiveSlot(blockout.transform, "PAD_SideYard_West", PrimitiveType.Cube, new Vector3(-8.5f, 0.22f, 7f), new Vector3(7f, 0.22f, 7f), "West hamlet yard.");
            BuildPrimitiveSlot(blockout.transform, "PAD_SideYard_East", PrimitiveType.Cube, new Vector3(8.5f, 0.22f, 2f), new Vector3(7f, 0.22f, 8f), "East hamlet yard.");
            BuildHouseRow(buildings.transform, "ROW_A", new Vector3(-7f, 0f, 6f), 3, 5f, new Vector3(3.2f, 3f, 3f));
            BuildHouseRow(buildings.transform, "ROW_B", new Vector3(6f, 0f, 3f), 3, 5f, new Vector3(3f, 2.8f, 3f));
            BuildHouseRow(buildings.transform, "ROW_C_Back", new Vector3(-5.5f, 0f, -5f), 3, 5.2f, new Vector3(2.8f, 2.6f, 2.8f));
            BuildCrateLine(props.transform, "PRP_Crates", new Vector3(6f, 0f, -4f), 4, 1f);
            BuildCrateLine(props.transform, "PRP_Crates_Back", new Vector3(-3f, 0f, -6f), 3, 0.95f);
            BuildFenceLine(props.transform, "PRP_Fence", new Vector3(-9f, 0f, -5f), 8, 1.3f);
            BuildFenceLine(props.transform, "PRP_Fence_Edge", new Vector3(4.5f, 0f, 9f), 6, 1.3f);
            BuildLandmarkCluster(props.transform, "LMK_SettlementBeacon", new Vector3(0f, 0f, 10.5f));
            BuildLandmarkCluster(props.transform, "LMK_RoadMarker", new Vector3(-10.5f, 0f, 0.5f));
            BuildAmbientPair(ambient.transform, "AMB_TownPair", new Vector3(1f, 0f, 2f));
            BuildAmbientSolo(ambient.transform, "AMB_Lookout", new Vector3(7f, 0f, 7f));
            BuildAmbientCreature(ambient.transform, "AMB_Creature", new Vector3(-5f, 0f, -3f));
            BuildAmbientSolo(ambient.transform, "AMB_Worker", new Vector3(-7f, 0f, 7.5f));
            BuildAmbientPair(ambient.transform, "AMB_ShelterPair", new Vector3(5.5f, 0f, -4.5f));
            BuildAmbientCreature(ambient.transform, "AMB_Bird", new Vector3(2f, 1.6f, 8.4f));
        }

        private static void BuildGullwatchFocusedPass(Transform worldRoot)
        {
            GameObject root = EnsureManagedNode(worldRoot, "ENV_GullwatchFocus", "gullwatch_focus");
            GameObject props = EnsureManagedNode(root.transform, "ENV_Props", "gullwatch_focus");
            GameObject ambient = EnsureManagedNode(root.transform, "ENV_Ambient", "gullwatch_focus");
            GameObject interiors = EnsureManagedNode(root.transform, "ENV_Interiors", "gullwatch_focus");
            GameObject consequence = EnsureManagedNode(root.transform, "ENV_Consequence", "gullwatch_focus");

            BuildLandmarkCluster(props.transform, "LMK_ShoreBeaconFrame", new Vector3(9.2f, 0f, 9.8f));
            BuildFenceLine(props.transform, "PRP_ShoreWalkPosts", new Vector3(6.2f, 0f, 6.8f), 6, 1.2f);
            BuildCrateLine(props.transform, "PRP_FishingGear", new Vector3(-4.2f, 0f, 5.5f), 4, 0.95f);
            BuildAmbientPair(ambient.transform, "AMB_FisherFamily", new Vector3(1.6f, 0f, 5.2f));
            BuildAmbientSolo(ambient.transform, "AMB_WatchMira", new Vector3(8.1f, 0f, 7.6f));
            BuildAmbientCreature(ambient.transform, "AMB_ShoreBirds", new Vector3(5.2f, 1.8f, 10.2f));

            BuildFocusedInteriorRoom(interiors.transform, "INT_GullwatchBeaconHouse", new Vector3(28f, 0f, 14f), new Vector3(8f, 3.2f, 8f), new Vector3(5.8f, 0f, 7f));

            GameObject activeBeacon = EnsureManagedNode(consequence.transform, "PRP_BeaconLit", "gullwatch_focus");
            activeBeacon.transform.localPosition = new Vector3(9.2f, 0f, 9.8f);
            ClearManagedChildren(activeBeacon.transform, GetGenerationId(consequence.transform));
            CreatePrimitiveVisual(activeBeacon.transform, "GEN_Base", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 1.2f), GetGenerationId(consequence.transform), "Lit beacon base.");
            CreatePrimitiveVisual(activeBeacon.transform, "GEN_Spire", PrimitiveType.Cylinder, new Vector3(0f, 2.5f, 0f), new Vector3(0.28f, 2.1f, 0.28f), GetGenerationId(consequence.transform), "Lit beacon spire.");
            CreatePrimitiveVisual(activeBeacon.transform, "GEN_Flame", PrimitiveType.Sphere, new Vector3(0f, 4.9f, 0f), new Vector3(0.7f, 0.7f, 0.7f), GetGenerationId(consequence.transform), "Beacon flame placeholder.");
            EnsureConditionalActivator(activeBeacon, "gullwatch", "tide_route_secured", true, false);

            GameObject dormantBeacon = EnsureManagedNode(consequence.transform, "PRP_BeaconDormant", "gullwatch_focus");
            dormantBeacon.transform.localPosition = new Vector3(9.2f, 0f, 9.8f);
            ClearManagedChildren(dormantBeacon.transform, GetGenerationId(consequence.transform));
            CreatePrimitiveVisual(dormantBeacon.transform, "GEN_Base", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 1f, 1.1f), GetGenerationId(consequence.transform), "Dormant beacon base.");
            CreatePrimitiveVisual(dormantBeacon.transform, "GEN_Spire", PrimitiveType.Cylinder, new Vector3(0f, 2.3f, 0f), new Vector3(0.24f, 2f, 0.24f), GetGenerationId(consequence.transform), "Dormant beacon spire.");
            EnsureConditionalActivator(dormantBeacon, "gullwatch", "tide_route_secured", true, true);
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
            BuildPrimitiveSlot(blockout.transform, "WALL_EntranceWest", PrimitiveType.Cube, new Vector3(-5.2f, 2.4f, -8f), new Vector3(0.8f, 4.8f, 7.8f), "Entrance west wall.");
            BuildPrimitiveSlot(blockout.transform, "WALL_EntranceEast", PrimitiveType.Cube, new Vector3(5.2f, 2.4f, -8f), new Vector3(0.8f, 4.8f, 7.8f), "Entrance east wall.");
            BuildPrimitiveSlot(blockout.transform, "WALL_MidWest", PrimitiveType.Cube, new Vector3(-4.2f, 2.4f, 0f), new Vector3(0.8f, 4.8f, 8.6f), "Mid west wall.");
            BuildPrimitiveSlot(blockout.transform, "WALL_MidEast", PrimitiveType.Cube, new Vector3(4.2f, 2.4f, 0f), new Vector3(0.8f, 4.8f, 8.6f), "Mid east wall.");
            BuildPrimitiveSlot(blockout.transform, "WALL_BossWest", PrimitiveType.Cube, new Vector3(-6.1f, 2.6f, 9f), new Vector3(0.8f, 5.2f, 9.4f), "Boss west wall.");
            BuildPrimitiveSlot(blockout.transform, "WALL_BossEast", PrimitiveType.Cube, new Vector3(6.1f, 2.6f, 9f), new Vector3(0.8f, 5.2f, 9.4f), "Boss east wall.");

            BuildLandmarkCluster(props.transform, "PRP_EntranceLandmark", new Vector3(3.5f, 0f, -9f));
            BuildLandmarkCluster(props.transform, "PRP_MidLandmark", new Vector3(-3.5f, 0f, 0f));
            BuildLandmarkCluster(props.transform, "PRP_BossLandmark", new Vector3(0f, 0f, 10.5f));
            BuildCrateLine(props.transform, "PRP_EntranceDebris", new Vector3(-3.5f, 0f, -10.5f), 4, 1f);
            BuildCrateLine(props.transform, "PRP_MidDebris", new Vector3(1.8f, 0f, -1.6f), 3, 1f);
            BuildFenceLine(props.transform, "PRP_BossSpikes_Left", new Vector3(-4f, 0f, 11.8f), 4, 1.1f);
            BuildFenceLine(props.transform, "PRP_BossSpikes_Right", new Vector3(0.5f, 0f, 11.8f), 4, 1.1f);
            BuildPrimitiveSlot(debug.transform, "DBG_RoomMarker", PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 9f), new Vector3(0.4f, 2.8f, 0.4f), "Boss room readability marker.");
        }

        private static void BuildTideCavernsFocusedPass(Transform worldRoot)
        {
            GameObject root = EnsureManagedNode(worldRoot, "ENV_TideFocus", "tide_caverns_focus");
            GameObject leadIn = EnsureManagedNode(root.transform, "ENV_LeadIn", "tide_caverns_focus");
            GameObject props = EnsureManagedNode(root.transform, "ENV_Props", "tide_caverns_focus");
            GameObject ambient = EnsureManagedNode(root.transform, "ENV_Ambient", "tide_caverns_focus");

            BuildLandmarkCluster(leadIn.transform, "LMK_TideGate", new Vector3(0f, 0f, -10.8f));
            BuildFenceLine(leadIn.transform, "PRP_TideMarkers", new Vector3(-3.4f, 0f, -7.4f), 6, 1.25f);
            BuildCrateLine(props.transform, "PRP_WetDebris", new Vector3(-4.5f, 0f, -2.4f), 5, 0.95f);
            BuildLandmarkCluster(props.transform, "LMK_MatriarchNest", new Vector3(0f, 0f, 12.2f));
            BuildAmbientCreature(ambient.transform, "AMB_TideDriftA", new Vector3(-2.5f, 0.2f, -5.2f));
            BuildAmbientCreature(ambient.transform, "AMB_TideDriftB", new Vector3(2.8f, 0.2f, 4.4f));
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
            BuildLandmarkCluster(container, "PRP_BattleBeacon_West", new Vector3(-10f, 0f, -6f));
            BuildCrateLine(container, "PRP_BattleCrates_West", new Vector3(-8f, 0f, -5f), 4, 1f);
            BuildFenceLine(container, "PRP_BattleBarrier_East", new Vector3(5f, 0f, 7.5f), 5, 1.4f);
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

        private static void BuildFocusedInteriorRoom(Transform container, string name, Vector3 interiorPosition, Vector3 roomSize, Vector3 exteriorDoorPosition)
        {
            GameObject slot = EnsureManagedNode(container, name, GetGenerationId(container));
            slot.transform.localPosition = interiorPosition;
            slot.transform.localRotation = Quaternion.identity;
            slot.transform.localScale = Vector3.one;

            if (HasManualChildren(slot, GetGenerationId(container)))
            {
                return;
            }

            ClearManagedChildren(slot.transform, GetGenerationId(container));
            CreatePrimitiveVisual(slot.transform, "GEN_Floor", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(roomSize.x, 0.1f, roomSize.z), GetGenerationId(container), "Settlement interior floor.");
            CreatePrimitiveVisual(slot.transform, "GEN_Ceiling", PrimitiveType.Cube, new Vector3(0f, roomSize.y, 0f), new Vector3(roomSize.x, 0.1f, roomSize.z), GetGenerationId(container), "Settlement interior ceiling.");
            CreatePrimitiveVisual(slot.transform, "GEN_Wall_North", PrimitiveType.Cube, new Vector3(0f, roomSize.y * 0.5f, roomSize.z * 0.5f), new Vector3(roomSize.x, roomSize.y, 0.2f), GetGenerationId(container), "Settlement interior wall.");
            CreatePrimitiveVisual(slot.transform, "GEN_Wall_South_Left", PrimitiveType.Cube, new Vector3(-roomSize.x * 0.28f, roomSize.y * 0.5f, -roomSize.z * 0.5f), new Vector3(roomSize.x * 0.44f, roomSize.y, 0.2f), GetGenerationId(container), "Settlement interior wall.");
            CreatePrimitiveVisual(slot.transform, "GEN_Wall_South_Right", PrimitiveType.Cube, new Vector3(roomSize.x * 0.28f, roomSize.y * 0.5f, -roomSize.z * 0.5f), new Vector3(roomSize.x * 0.44f, roomSize.y, 0.2f), GetGenerationId(container), "Settlement interior wall.");
            CreatePrimitiveVisual(slot.transform, "GEN_Wall_West", PrimitiveType.Cube, new Vector3(-roomSize.x * 0.5f, roomSize.y * 0.5f, 0f), new Vector3(0.2f, roomSize.y, roomSize.z), GetGenerationId(container), "Settlement interior wall.");
            CreatePrimitiveVisual(slot.transform, "GEN_Wall_East", PrimitiveType.Cube, new Vector3(roomSize.x * 0.5f, roomSize.y * 0.5f, 0f), new Vector3(0.2f, roomSize.y, roomSize.z), GetGenerationId(container), "Settlement interior wall.");
            CreatePrimitiveVisual(slot.transform, "GEN_Table", PrimitiveType.Cube, new Vector3(0f, 0.62f, 1.2f), new Vector3(2.1f, 0.2f, 1.1f), GetGenerationId(container), "Settlement table.");
            CreatePrimitiveVisual(slot.transform, "GEN_Bunk", PrimitiveType.Cube, new Vector3(-2f, 0.4f, -1.1f), new Vector3(1.8f, 0.45f, 1f), GetGenerationId(container), "Settlement bunk.");
            CreatePrimitiveVisual(slot.transform, "GEN_Crates", PrimitiveType.Cube, new Vector3(2.1f, 0.4f, -1.2f), new Vector3(0.9f, 0.8f, 0.9f), GetGenerationId(container), "Settlement cargo.");

            Transform interiorEntry = EnsureMarker(slot.transform, "MK_InteriorEntry", new Vector3(0f, 0.15f, -roomSize.z * 0.33f), Quaternion.identity);
            Transform interiorExit = EnsureMarker(slot.transform, "MK_InteriorExit", new Vector3(0f, 0.15f, -roomSize.z * 0.45f), Quaternion.identity);
            Transform exteriorMarker = EnsureMarker(container.parent, $"{name}_ExteriorReturn", exteriorDoorPosition, Quaternion.Euler(0f, 180f, 0f));

            EnsureDoor(container.parent, $"{name}_ExteriorDoor", exteriorDoorPosition, interiorEntry, true);
            EnsureDoor(slot.transform, "Door_Exit", interiorExit.localPosition, exteriorMarker, false);
        }

        private static Transform EnsureMarker(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
        {
            GameObject marker = FindImmediateChild(parent, name);
            if (marker == null)
            {
                marker = new GameObject(name);
                marker.transform.SetParent(parent, false);
            }

            marker.transform.localPosition = localPosition;
            marker.transform.localRotation = localRotation;
            marker.transform.localScale = Vector3.one;
            return marker.transform;
        }

        private static void EnsureDoor(Transform parent, string name, Vector3 localPosition, Transform targetMarker, bool outwardFacing)
        {
            GameObject doorRoot = FindImmediateChild(parent, name);
            if (doorRoot == null)
            {
                doorRoot = new GameObject(name);
                doorRoot.transform.SetParent(parent, false);
            }

            doorRoot.transform.localPosition = localPosition;
            doorRoot.transform.localRotation = outwardFacing ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            doorRoot.transform.localScale = Vector3.one;

            BoxCollider collider = doorRoot.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = doorRoot.AddComponent<BoxCollider>();
            }

            collider.isTrigger = true;
            collider.size = new Vector3(1.6f, 2.2f, 0.8f);
            collider.center = new Vector3(0f, 1.1f, 0f);

            InteriorTravelDoor travelDoor = doorRoot.GetComponent<InteriorTravelDoor>();
            if (travelDoor == null)
            {
                travelDoor = doorRoot.AddComponent<InteriorTravelDoor>();
            }

            GameObject leaf = FindImmediateChild(doorRoot.transform, "GEN_DoorLeaf");
            if (leaf == null)
            {
                leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = "GEN_DoorLeaf";
                leaf.transform.SetParent(doorRoot.transform, false);
            }

            leaf.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            leaf.transform.localScale = new Vector3(1f, 1.9f, 0.16f);
            PlaceholderScaffoldStyleUtility.ApplyStyle(leaf, EnvironmentGeneratedCategory.Building, "GEN_Door", "Simple settlement door scaffold.");

            SerializedObject so = new SerializedObject(travelDoor);
            so.FindProperty("_doorId").stringValue = name.ToLowerInvariant();
            so.FindProperty("_interactionLabel").stringValue = outwardFacing ? "enter" : "leave";
            so.FindProperty("_targetMarker").objectReferenceValue = targetMarker;
            so.FindProperty("_doorLeaf").objectReferenceValue = leaf.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(travelDoor);
        }

        private static void EnsureConditionalActivator(GameObject target, string zoneId, string zoneFactId, bool expectedValue, bool invertResult)
        {
            ConditionalActivator activator = target.GetComponent<ConditionalActivator>();
            if (activator == null)
            {
                activator = target.AddComponent<ConditionalActivator>();
            }

            SerializedObject so = new SerializedObject(activator);
            SerializedProperty resolver = so.FindProperty("_resolver");
            resolver.FindPropertyRelative("Mode").enumValueIndex = (int)ConditionGroupMode.All;
            SerializedProperty conditions = resolver.FindPropertyRelative("Conditions");
            conditions.arraySize = 1;
            SerializedProperty condition = conditions.GetArrayElementAtIndex(0);
            condition.FindPropertyRelative("Type").enumValueIndex = (int)ConditionType.ZoneFactBoolEquals;
            condition.FindPropertyRelative("ZoneId").stringValue = zoneId;
            condition.FindPropertyRelative("ZoneFactId").stringValue = zoneFactId;
            condition.FindPropertyRelative("ExpectedBool").boolValue = expectedValue;
            so.FindProperty("_targetMode").enumValueIndex = (int)ActivatorTargetMode.GameObjectSetActive;
            so.FindProperty("_targetGameObject").objectReferenceValue = target;
            so.FindProperty("_invertResult").boolValue = invertResult;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activator);
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
            PlaceholderScaffoldStyleUtility.ApplyStyle(target, EnvironmentGeneratedCategory.Prop, name, notes);
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
