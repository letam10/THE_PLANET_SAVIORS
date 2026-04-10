using System.Collections.Generic;
using System.IO;
using TPS.Runtime.Combat;
using TPS.Runtime.Core;
using TPS.Runtime.Dialogue;
using TPS.Runtime.NPC;
using TPS.Runtime.Quest;
using TPS.Runtime.UI;
using TPS.Runtime.World;
using TPS.Runtime.Conditions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Editor
{
    public static class Phase1ProjectAudit
    {
        private sealed class AuditReport
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();

            public void Error(string message) => Errors.Add(message);
            public void Warning(string message) => Warnings.Add(message);

            public void Expect(bool condition, string successContext, string failureMessage)
            {
                if (!condition)
                {
                    Error(failureMessage);
                }
                else
                {
                    Debug.Log($"[Phase1Audit] OK: {successContext}");
                }
            }
        }

        [MenuItem("Tools/TPS/Phase 1/Run Project Audit")]
        public static void RunProjectAuditMenu()
        {
            RunAudit(false);
        }

        [MenuItem("Tools/TPS/Phase 1/Reinstall And Audit")]
        public static void ReinstallAndAuditMenu()
        {
            RunAudit(true);
        }

        [MenuItem("Tools/TPS/Phase 1/Prepare Manual Smoke")]
        public static void PrepareManualSmoke()
        {
            Phase1SceneInstaller.InstallVerticalSlice();
            string savePath = Path.Combine(Application.persistentDataPath, "debug_save.json");
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log($"[Phase1Audit] Deleted previous smoke save: {savePath}");
            }

            EditorSceneManager.OpenScene("Assets/_TPS/Scenes/Bootstrap/Bootstrap.unity", OpenSceneMode.Single);
            Debug.Log("[Phase1Audit] Manual smoke prepared. Press Play from Bootstrap and follow Assets/_TPS/Docs/PHASE1_MANUAL_SMOKE.md");
        }

        private static bool RunAudit(bool reinstallFirst)
        {
            if (reinstallFirst)
            {
                Phase1SceneInstaller.InstallVerticalSlice();
            }

            var report = new AuditReport();
            ValidateBuildProfile(report);
            ValidateContentCatalog(report);
            ValidateScene("Assets/_TPS/Scenes/Core/Core.unity", ValidateCoreScene, report);
            ValidateScene("Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity", ValidateWorldScene, report);
            ValidateScene("Assets/_TPS/Scenes/Battle/BTL_Standard.unity", ValidateBattleScene, report);
            ValidatePrefabs(report);

            foreach (string warning in report.Warnings)
            {
                Debug.LogWarning($"[Phase1Audit] {warning}");
            }

            if (report.Errors.Count > 0)
            {
                foreach (string error in report.Errors)
                {
                    Debug.LogError($"[Phase1Audit] {error}");
                }

                Debug.LogError($"[Phase1Audit] FAILED with {report.Errors.Count} error(s) and {report.Warnings.Count} warning(s).");
                return false;
            }

            Debug.Log($"[Phase1Audit] PASSED with {report.Warnings.Count} warning(s).");
            return true;
        }

        private static void ValidateBuildProfile(AuditReport report)
        {
            const string buildProfilePath = "Assets/Settings/Build Profiles/Windows.asset";
            report.Expect(File.Exists(buildProfilePath), "Windows build profile present", $"Missing build profile at {buildProfilePath}");
            if (!File.Exists(buildProfilePath))
            {
                return;
            }

            string content = File.ReadAllText(buildProfilePath);
            int bootstrap = content.IndexOf("Assets/_TPS/Scenes/Bootstrap/Bootstrap.unity");
            int core = content.IndexOf("Assets/_TPS/Scenes/Core/Core.unity");
            int town = content.IndexOf("Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity");
            int battle = content.IndexOf("Assets/_TPS/Scenes/Battle/BTL_Standard.unity");
            report.Expect(bootstrap >= 0, "Bootstrap present in Windows build profile", "Bootstrap missing from Windows build profile.");
            report.Expect(core > bootstrap && bootstrap >= 0, "Core ordered after Bootstrap", "Core is not ordered after Bootstrap in Windows build profile.");
            report.Expect(town > core && core >= 0, "Town ordered after Core", "ZN_Town_AsterHarbor is not ordered after Core in Windows build profile.");
            report.Expect(battle > town && town >= 0, "Battle ordered after Town", "BTL_Standard is not ordered after ZN_Town_AsterHarbor in Windows build profile.");
        }

        private static void ValidateScene(string scenePath, System.Action<Scene, AuditReport> validate, AuditReport report)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                CountMissingScripts(scene, report);
                validate(scene, report);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateContentCatalog(AuditReport report)
        {
            const string catalogPath = "Assets/_TPS/Data/Phase1/Core/CAT_Phase1Content.asset";
            Phase1ContentCatalog catalog = AssetDatabase.LoadAssetAtPath<Phase1ContentCatalog>(catalogPath);
            report.Expect(catalog != null, "Phase1 content catalog loaded", $"Missing content catalog at {catalogPath}");
            if (catalog == null)
            {
                return;
            }

            ContentValidationResult validation = PhaseContentValidator.ValidateCatalog(catalog);
            for (int i = 0; i < validation.Errors.Count; i++)
            {
                report.Error(validation.Errors[i]);
            }

            for (int i = 0; i < validation.Warnings.Count; i++)
            {
                report.Warning(validation.Warnings[i]);
            }
        }

        private static void ValidateCoreScene(Scene scene, AuditReport report)
        {
            GameObject root = FindDeep(scene, "CoreServices");
            report.Expect(root != null, "CoreServices found", "CoreServices object missing from Core scene.");
            if (root == null)
            {
                return;
            }

            ValidateSingleComponent<SceneLoader>(root, report, "CoreServices");
            ValidateSingleComponent<TPS.Runtime.Time.WorldClock>(root, report, "CoreServices");
            ValidateSingleComponent<TPS.Runtime.Weather.WeatherSystem>(root, report, "CoreServices");
            ValidateSingleComponent<TPS.Runtime.Spawn.PlayerSpawnSystem>(root, report, "CoreServices");
            ValidateSingleComponent<GameStateManager>(root, report, "CoreServices");
            ValidateSingleComponent<TPS.Runtime.SaveLoad.SaveLoadManager>(root, report, "CoreServices");
            ValidateSingleComponent<QuestService>(root, report, "CoreServices");
            ValidateSingleComponent<DialogueStateService>(root, report, "CoreServices");
            ValidateSingleComponent<PartyService>(root, report, "CoreServices");
            ValidateSingleComponent<ProgressionService>(root, report, "CoreServices");
            ValidateSingleComponent<EconomyService>(root, report, "CoreServices");
            ValidateSingleComponent<EncounterService>(root, report, "CoreServices");
            ValidateSingleComponent<InventoryService>(root, report, "CoreServices");
            ValidateSingleComponent<ZoneStateService>(root, report, "CoreServices");
            ValidateSingleComponent<RewardService>(root, report, "CoreServices");
            ValidateSingleComponent<StateResolver>(root, report, "CoreServices");
            ValidateSingleComponent<Phase1RuntimeHUD>(root, report, "CoreServices");
            ValidateSingleComponent<Phase1SmokeRunner>(root, report, "CoreServices");

            ValidateObjectReference(root.GetComponent<QuestService>(), "_contentCatalog", report, "QuestService");
            ValidateObjectReference(root.GetComponent<DialogueStateService>(), "_contentCatalog", report, "DialogueStateService");
            ValidateObjectReference(root.GetComponent<PartyService>(), "_contentCatalog", report, "PartyService");
            ValidateObjectReference(root.GetComponent<ProgressionService>(), "_contentCatalog", report, "ProgressionService");
            ValidateObjectReference(root.GetComponent<EconomyService>(), "_contentCatalog", report, "EconomyService");
            ValidateObjectReference(root.GetComponent<EncounterService>(), "_contentCatalog", report, "EncounterService");
            ValidateObjectReference(root.GetComponent<Phase1RuntimeHUD>(), "_contentCatalog", report, "Phase1RuntimeHUD");
            ValidateObjectReference(root.GetComponent<Phase1SmokeRunner>(), "_contentCatalog", report, "Phase1SmokeRunner");
        }

        private static void ValidateWorldScene(Scene scene, AuditReport report)
        {
            GameObject npc = FindDeep(scene, "PF_NPC_Test_Citizen");
            report.Expect(npc != null, "Town NPC found", "PF_NPC_Test_Citizen missing from world scene.");
            if (npc != null)
            {
                ValidateSingleComponent<NPCSchedule>(npc, report, npc.name);
                ValidateSingleComponent<DialogueAnchor>(npc, report, npc.name);
                ValidateSerializedString(npc.GetComponent<NPCSchedule>(), "_npcId", report, "NPCSchedule");
                ValidateObjectReference(npc.GetComponent<DialogueAnchor>(), "_dialogueDefinition", report, "DialogueAnchor");
            }

            GameObject shop = FindDeep(scene, "BLD_Shop_Block");
            report.Expect(shop != null, "Merchant block found", "BLD_Shop_Block missing from world scene.");
            if (shop != null)
            {
                ValidateSingleComponent<MerchantAnchor>(shop, report, shop.name);
                ValidateObjectReference(shop.GetComponent<MerchantAnchor>(), "_shopDefinition", report, "MerchantAnchor");
            }

            GameObject tavern = FindDeep(scene, "BLD_Tavern_Block");
            report.Expect(tavern != null, "Inn block found", "BLD_Tavern_Block missing from world scene.");
            if (tavern != null)
            {
                ValidateSingleComponent<InnAnchor>(tavern, report, tavern.name);
            }

            GameObject trigger = FindDeep(scene, "PF_EncounterTrigger_Test");
            report.Expect(trigger != null, "Zone encounter trigger found", "PF_EncounterTrigger_Test missing from world scene.");
            if (trigger != null)
            {
                ValidateSingleComponent<EncounterAnchor>(trigger, report, trigger.name);
                ValidateSerializedString(trigger.GetComponent<EncounterAnchor>(), "_zoneId", report, "PF_EncounterTrigger_Test EncounterAnchor");
            }

            GameObject bossAnchor = FindDeep(scene, "ENC_SubBoss_Anchor");
            report.Expect(bossAnchor != null, "Sub-boss anchor found", "ENC_SubBoss_Anchor missing from world scene.");
            if (bossAnchor != null)
            {
                ValidateSingleComponent<EncounterAnchor>(bossAnchor, report, bossAnchor.name);
                ValidateObjectReference(bossAnchor.GetComponent<EncounterAnchor>(), "_directEncounter", report, "Sub-boss EncounterAnchor");
            }

            GameObject dockNpc = FindDeep(scene, "NPC_DockQuartermaster");
            report.Expect(dockNpc != null, "Dock quartermaster found", "NPC_DockQuartermaster missing from world scene.");
            if (dockNpc != null)
            {
                ValidateSingleComponent<NPCSchedule>(dockNpc, report, dockNpc.name);
                ValidateSingleComponent<DialogueAnchor>(dockNpc, report, dockNpc.name);
                ValidateObjectReference(dockNpc.GetComponent<DialogueAnchor>(), "_dialogueDefinition", report, "Dock quartermaster DialogueAnchor");
            }

            GameObject dockEncounter = FindDeep(scene, "ENC_DockRainMites_Anchor");
            report.Expect(dockEncounter != null, "Dock encounter anchor found", "ENC_DockRainMites_Anchor missing from world scene.");
            if (dockEncounter != null)
            {
                ValidateSingleComponent<EncounterAnchor>(dockEncounter, report, dockEncounter.name);
                ValidateObjectReference(dockEncounter.GetComponent<EncounterAnchor>(), "_directEncounter", report, "Dock encounter anchor");
            }

            GameObject dockBannerController = FindDeep(scene, "PRP_DockSupplyBanner_Controller");
            report.Expect(dockBannerController != null, "Dock banner controller found", "PRP_DockSupplyBanner_Controller missing from world scene.");
            if (dockBannerController != null)
            {
                ValidateSingleComponent<ConditionalActivator>(dockBannerController, report, dockBannerController.name);
            }
        }

        private static void ValidateBattleScene(Scene scene, AuditReport report)
        {
            GameObject battleRoot = FindDeep(scene, "BattleRoot");
            report.Expect(battleRoot != null, "BattleRoot found", "BattleRoot missing from battle scene.");
            if (battleRoot != null)
            {
                ValidateSingleComponent<BattleWorldBridge>(battleRoot, report, battleRoot.name);
            }
        }

        private static void ValidatePrefabs(AuditReport report)
        {
            ValidatePrefabByName("PF_NPC_Test_Citizen", report);
            ValidatePrefabByName("PF_EncounterTrigger_Test", report);
        }

        private static void ValidatePrefabByName(string prefabName, AuditReport report)
        {
            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            if (guids.Length == 0)
            {
                report.Warning($"Prefab audit skipped because {prefabName} prefab was not found as a direct asset.");
                return;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            report.Expect(prefab != null, $"{prefabName} prefab loaded", $"Could not load prefab asset {assetPath}");
            if (prefab == null)
            {
                return;
            }

            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
            report.Expect(missing == 0, $"{prefabName} prefab has no missing scripts", $"{prefabName} prefab has {missing} missing script reference(s).");
        }

        private static void CountMissingScripts(Scene scene, AuditReport report)
        {
            int totalMissing = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                totalMissing += CountMissingScriptsRecursive(roots[i]);
            }

            report.Expect(totalMissing == 0, $"{scene.name} has no missing scripts", $"{scene.name} has {totalMissing} missing script reference(s).");
        }

        private static int CountMissingScriptsRecursive(GameObject root)
        {
            int total = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                total += CountMissingScriptsRecursive(transform.GetChild(i).gameObject);
            }

            return total;
        }

        private static void ValidateSingleComponent<T>(GameObject target, AuditReport report, string context) where T : Component
        {
            T[] components = target.GetComponents<T>();
            report.Expect(components.Length == 1, $"{context} has one {typeof(T).Name}", $"{context} should have exactly one {typeof(T).Name}, found {components.Length}.");
        }

        private static void ValidateObjectReference(Object target, string propertyName, AuditReport report, string context)
        {
            if (target == null)
            {
                report.Error($"{context} missing target object during audit.");
                return;
            }

            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            report.Expect(property != null, $"{context} exposes {propertyName}", $"{context} missing serialized property {propertyName}.");
            if (property != null)
            {
                report.Expect(property.objectReferenceValue != null, $"{context}.{propertyName} assigned", $"{context}.{propertyName} is null.");
            }
        }

        private static void ValidateSerializedString(Object target, string propertyName, AuditReport report, string context)
        {
            if (target == null)
            {
                report.Error($"{context} missing target object during audit.");
                return;
            }

            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            report.Expect(property != null, $"{context} exposes {propertyName}", $"{context} missing serialized property {propertyName}.");
            if (property != null)
            {
                report.Expect(!string.IsNullOrWhiteSpace(property.stringValue), $"{context}.{propertyName} assigned", $"{context}.{propertyName} is empty.");
            }
        }

        private static GameObject FindDeep(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject found = FindChild(roots[i], name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindChild(GameObject root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject found = FindChild(transform.GetChild(i).gameObject, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
