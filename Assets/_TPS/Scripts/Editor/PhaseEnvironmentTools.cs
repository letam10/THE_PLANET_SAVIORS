using System;
using System.Collections.Generic;
using TPS.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPS.Editor
{
    internal static class PhaseEnvironmentTools
    {
        private const string WorldScenePath = "Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity";
        private const string GenerationId = "aster_harbor_environment";
        private const string GeneratedRootName = "ENV_AsterHarbor_Generated";
        private const string BlockoutContainerName = "ENV_Blockout";
        private const string BuildingsContainerName = "ENV_Buildings";
        private const string PropsContainerName = "ENV_Props";
        private const string VegetationContainerName = "ENV_Vegetation";
        private const string AmbientContainerName = "ENV_Ambient";
        private const string DebugContainerName = "ENV_Debug";

        private readonly struct ExclusionZone
        {
            public ExclusionZone(Vector3 center, float radius, string label)
            {
                Center = center;
                Radius = radius;
                Label = label;
            }

            public Vector3 Center { get; }
            public float Radius { get; }
            public string Label { get; }
        }

        private sealed class EnvironmentContext
        {
            public Scene Scene;
            public GameObject WorldRoot;
            public Vector3 SquareCenter;
            public Vector3 TavernMarker;
            public Vector3 ShopPosition;
            public Vector3 TavernPosition;
            public Vector3 HarborPosition;
            public Vector3 DockNpcPosition;
            public Vector3 EncounterFringe;
            public readonly List<ExclusionZone> Exclusions = new List<ExclusionZone>();
        }

        public sealed class EnvironmentValidationResult
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
        }

        [MenuItem("Tools/TPS/Environment/Generate AsterHarbor Blockout")]
        private static void GenerateAsterHarborBlockoutMenu()
        {
            RebuildSceneEnvironment(includeAmbient: false, includeVegetation: false);
        }

        [MenuItem("Tools/TPS/Environment/Rebuild Replace-Safe Environment")]
        private static void RebuildReplaceSafeEnvironmentMenu()
        {
            RebuildSceneEnvironment(includeAmbient: true, includeVegetation: true);
        }

        [MenuItem("Tools/TPS/Environment/Rebuild Ambient Layer")]
        private static void RebuildAmbientLayerMenu()
        {
            RebuildSceneEnvironment(includeAmbient: true, includeVegetation: false, ambientOnly: true);
        }

        [MenuItem("Tools/TPS/Environment/Validate Replace-Safe Layout")]
        public static void ValidateReplaceSafeLayoutMenu()
        {
            EnvironmentValidationResult result = ValidateEnvironmentLayout();
            LogValidationResult(result, "[TPSEnvironment]");
        }

        public static void EnsureAsterHarborEnvironment(Scene scene, GameObject worldRoot, bool includeAmbient = true, bool includeVegetation = true)
        {
            if (!scene.IsValid() || !scene.isLoaded || worldRoot == null)
            {
                return;
            }

            EnvironmentContext context = BuildContext(scene, worldRoot);
            GameObject generatedRoot = EnsureManagedNode(worldRoot.transform, GeneratedRootName, EnvironmentGeneratedCategory.Root, "Top-level generated environment root.");
            generatedRoot.transform.localPosition = Vector3.zero;
            generatedRoot.transform.localRotation = Quaternion.identity;
            generatedRoot.transform.localScale = Vector3.one;

            GameObject blockout = EnsureManagedNode(generatedRoot.transform, BlockoutContainerName, EnvironmentGeneratedCategory.Blockout, "Replace-safe blockout surfaces and path ribbons.");
            GameObject buildings = EnsureManagedNode(generatedRoot.transform, BuildingsContainerName, EnvironmentGeneratedCategory.Building, "Replace-safe placeholder building slots.");
            GameObject props = EnsureManagedNode(generatedRoot.transform, PropsContainerName, EnvironmentGeneratedCategory.Prop, "Replace-safe prop group slots.");
            GameObject vegetation = EnsureManagedNode(generatedRoot.transform, VegetationContainerName, EnvironmentGeneratedCategory.Vegetation, "Replace-safe vegetation and clutter slots.");
            GameObject ambient = EnsureManagedNode(generatedRoot.transform, AmbientContainerName, EnvironmentGeneratedCategory.Ambient, "Replace-safe ambient crowd and creature slots.");
            GameObject debug = EnsureManagedNode(generatedRoot.transform, DebugContainerName, EnvironmentGeneratedCategory.Debug, "Environment debug and district labels.");

            BuildBlockout(blockout.transform);
            BuildBuildings(buildings.transform);
            BuildProps(props.transform, context);
            if (includeVegetation)
            {
                BuildVegetation(vegetation.transform, context);
            }

            if (includeAmbient)
            {
                BuildAmbient(ambient.transform);
            }

            BuildDebug(debug.transform, context);
        }

        public static EnvironmentValidationResult ValidateEnvironmentLayout()
        {
            Scene scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Additive);
            try
            {
                return ValidateEnvironmentLayout(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static EnvironmentValidationResult ValidateEnvironmentLayout(Scene scene)
        {
            var result = new EnvironmentValidationResult();
            GameObject worldRoot = FindDeep(scene, "WorldRoot");
            if (worldRoot == null)
            {
                result.Errors.Add("WorldRoot missing from AsterHarbor scene.");
                return result;
            }

            GameObject generatedRoot = FindChild(worldRoot, GeneratedRootName);
            if (generatedRoot == null)
            {
                result.Errors.Add($"{GeneratedRootName} missing under WorldRoot.");
                return result;
            }

            ValidateManagedContainer(generatedRoot, BlockoutContainerName, result);
            ValidateManagedContainer(generatedRoot, BuildingsContainerName, result);
            ValidateManagedContainer(generatedRoot, PropsContainerName, result);
            ValidateManagedContainer(generatedRoot, VegetationContainerName, result);
            ValidateManagedContainer(generatedRoot, AmbientContainerName, result);
            ValidateManagedContainer(generatedRoot, DebugContainerName, result);

            EnvironmentContext context = BuildContext(scene, worldRoot);
            ValidateExclusions(FindChild(generatedRoot, BuildingsContainerName), result, context, 0.75f);
            ValidateExclusions(FindChild(generatedRoot, PropsContainerName), result, context, 0.45f, "Merchant block", "Inn block", "Tavern marker");
            ValidateExclusions(FindChild(generatedRoot, VegetationContainerName), result, context, 0.5f);
            ValidateExclusions(FindChild(generatedRoot, AmbientContainerName), result, context, 0.65f, "Inn block", "Square marker");

            return result;
        }

        private static void RebuildSceneEnvironment(bool includeAmbient, bool includeVegetation, bool ambientOnly = false)
        {
            Scene scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject worldRoot = FindDeep(scene, "WorldRoot");
                if (worldRoot == null)
                {
                    Debug.LogError("[TPSEnvironment] WorldRoot not found. Environment rebuild aborted.");
                    return;
                }

                if (ambientOnly)
                {
                    EnvironmentContext context = BuildContext(scene, worldRoot);
                    GameObject generatedRoot = EnsureManagedNode(worldRoot.transform, GeneratedRootName, EnvironmentGeneratedCategory.Root, "Top-level generated environment root.");
                    GameObject ambient = EnsureManagedNode(generatedRoot.transform, AmbientContainerName, EnvironmentGeneratedCategory.Ambient, "Replace-safe ambient crowd and creature slots.");
                    BuildAmbient(ambient.transform);
                    BuildDebug(EnsureManagedNode(generatedRoot.transform, DebugContainerName, EnvironmentGeneratedCategory.Debug, "Environment debug and district labels.").transform, context);
                }
                else
                {
                    EnsureAsterHarborEnvironment(scene, worldRoot, includeAmbient, includeVegetation);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                LogValidationResult(ValidateEnvironmentLayout(scene), "[TPSEnvironment]");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static EnvironmentContext BuildContext(Scene scene, GameObject worldRoot)
        {
            GameObject shop = FindDeep(scene, "BLD_Shop_Block");
            GameObject tavern = FindDeep(scene, "BLD_Tavern_Block");
            GameObject square = FindDeep(scene, "MK_Square_01");
            GameObject tavernMarker = FindDeep(scene, "MK_Tavern_01");
            GameObject gate = FindDeep(scene, "BLD_Gate_Block");
            GameObject dockNpc = FindDeep(scene, "NPC_DockQuartermaster");
            GameObject dockEncounter = FindDeep(scene, "ENC_DockRainMites_Anchor");
            GameObject patrolEncounter = FindDeep(scene, "PF_EncounterTrigger_Test");
            GameObject captainNpc = FindDeep(scene, "PF_NPC_Test_Citizen");
            GameObject bossAnchor = FindDeep(scene, "ENC_SubBoss_Anchor");

            var context = new EnvironmentContext
            {
                Scene = scene,
                WorldRoot = worldRoot,
                SquareCenter = square != null ? square.transform.position : new Vector3(0f, 0f, 2f),
                TavernMarker = tavernMarker != null ? tavernMarker.transform.position : new Vector3(-4f, 0f, 4f),
                ShopPosition = shop != null ? shop.transform.position : new Vector3(6f, 0f, 6f),
                TavernPosition = tavern != null ? tavern.transform.position : new Vector3(-6f, 0f, 5f),
                HarborPosition = dockNpc != null ? dockNpc.transform.position + new Vector3(3f, -1.5f, -1f) : new Vector3(11f, 0f, 3f),
                DockNpcPosition = dockNpc != null ? dockNpc.transform.position : new Vector3(10f, 0f, 4f),
                EncounterFringe = gate != null ? gate.transform.position : new Vector3(0f, 0f, -8f)
            };

            AddExclusion(context, captainNpc, 1.4f, "Quest NPC");
            AddExclusion(context, dockNpc, 1.4f, "Dock quest NPC");
            AddExclusion(context, shop, 2f, "Merchant block");
            AddExclusion(context, tavern, 2f, "Inn block");
            AddExclusion(context, patrolEncounter, 1.8f, "Zone encounter trigger");
            AddExclusion(context, bossAnchor, 1.6f, "Boss encounter anchor");
            AddExclusion(context, dockEncounter, 1.6f, "Dock encounter anchor");
            AddExclusion(context, square, 1.2f, "Square marker");
            AddExclusion(context, tavernMarker, 0.8f, "Tavern marker");
            return context;
        }

        private static void BuildBlockout(Transform container)
        {
            BuildPrimitiveSlot(container, "PAD_Square", EnvironmentGeneratedCategory.Blockout, new Vector3(0f, 0.05f, 3f), new Vector3(16f, 0.2f, 11f), PrimitiveType.Cube, "Main square and smoke-safe plaza surface.");
            BuildPrimitiveSlot(container, "PAD_Harbor", EnvironmentGeneratedCategory.Blockout, new Vector3(11f, 0.05f, 3f), new Vector3(13f, 0.2f, 10f), PrimitiveType.Cube, "Harbor work area placeholder pad.");
            BuildPrimitiveSlot(container, "PAD_Residential", EnvironmentGeneratedCategory.Blockout, new Vector3(-11f, 0.05f, 4f), new Vector3(11f, 0.2f, 9f), PrimitiveType.Cube, "Residential strip placeholder pad.");
            BuildPrimitiveSlot(container, "PAD_Frontier", EnvironmentGeneratedCategory.Blockout, new Vector3(0f, 0.05f, -7.5f), new Vector3(7f, 0.2f, 5f), PrimitiveType.Cube, "Encounter fringe pad near the gate.");
            BuildPrimitiveSlot(container, "PATH_MainAvenue", EnvironmentGeneratedCategory.Blockout, new Vector3(0f, 0.11f, -1.5f), new Vector3(4f, 0.12f, 17f), PrimitiveType.Cube, "Primary path from gate to square.");
            BuildPrimitiveSlot(container, "PATH_CrossStreet", EnvironmentGeneratedCategory.Blockout, new Vector3(0f, 0.11f, 5.5f), new Vector3(18f, 0.12f, 3.5f), PrimitiveType.Cube, "Cross street connecting tavern frontage, square, and harbor.");
            BuildPrimitiveSlot(container, "PATH_HarborApproach", EnvironmentGeneratedCategory.Blockout, new Vector3(8f, 0.11f, 3f), new Vector3(8f, 0.12f, 3f), PrimitiveType.Cube, "Harbor approach path.");
            BuildPrimitiveSlot(container, "DOCK_MainPier", EnvironmentGeneratedCategory.Blockout, new Vector3(15f, 0.16f, 1.5f), new Vector3(4f, 0.25f, 9f), PrimitiveType.Cube, "Main pier placeholder.");
            BuildPrimitiveSlot(container, "DOCK_SidePier", EnvironmentGeneratedCategory.Blockout, new Vector3(17f, 0.16f, 6.5f), new Vector3(3f, 0.25f, 5f), PrimitiveType.Cube, "Side pier placeholder.");
        }

        private static void BuildBuildings(Transform container)
        {
            BuildHouseSlot(container, "BLD_RowHouse_West_A", new Vector3(-13f, 0f, 8f), new Vector3(3.4f, 3.2f, 3.4f), "Residential landmark house west.");
            BuildHouseSlot(container, "BLD_RowHouse_West_B", new Vector3(-16.5f, 0f, 4.5f), new Vector3(3.1f, 3f, 3.2f), "Residential support house west.");
            BuildHouseSlot(container, "BLD_RowHouse_West_C", new Vector3(-14f, 0f, 0.5f), new Vector3(3f, 2.8f, 3.3f), "Residential support house near encounter fringe.");
            BuildHouseSlot(container, "BLD_MarketStall_Row_A", new Vector3(2f, 0f, 9.5f), new Vector3(2.5f, 2.2f, 2f), "Small commercial frontage scaffold.");
            BuildHouseSlot(container, "BLD_MarketStall_Row_B", new Vector3(1f, 0f, 7.75f), new Vector3(2.2f, 2.1f, 1.8f), "Small market stall scaffold.");
            BuildHouseSlot(container, "BLD_HarborShed_A", new Vector3(12.5f, 0f, 7.5f), new Vector3(4f, 2.8f, 3f), "Harbor shed scaffold.");
            BuildHouseSlot(container, "BLD_HarborShed_B", new Vector3(15.5f, 0f, -1.5f), new Vector3(3.2f, 2.5f, 2.5f), "Dockside storage shed scaffold.");
        }

        private static void BuildProps(Transform container, EnvironmentContext context)
        {
            BuildCrateCluster(container, "PRP_HarborCrates_A", new Vector3(13.2f, 0f, 6.8f), 4, "Harbor cargo clutter near quartermaster.");
            BuildCrateCluster(container, "PRP_HarborCrates_B", new Vector3(16.8f, 0f, -2.2f), 3, "Harbor cargo clutter near pier.");
            BuildFenceRun(container, "PRP_ResidentialFence", new Vector3(-9.5f, 0f, 1.5f), 7, 1.5f, Quaternion.identity, "Residential edge fence line.");
            BuildFenceRun(container, "PRP_HarborPosts", new Vector3(18.2f, 0f, -2f), 5, 1.25f, Quaternion.Euler(0f, 90f, 0f), "Harbor mooring post line.");
            BuildLamp(container, "PRP_SquareLamp_A", new Vector3(-2f, 0f, 6.5f), "Square readability lamp.");
            BuildLamp(container, "PRP_SquareLamp_B", new Vector3(2.5f, 0f, 6.5f), "Square readability lamp.");
            BuildLamp(container, "PRP_HarborLamp", new Vector3(9.2f, 0f, -0.8f), "Harbor readability lamp.");
            BuildSign(container, "PRP_TavernSign", context.TavernPosition + new Vector3(2.2f, 0.1f, 0f), "Tavern frontage sign scaffold.");
            BuildSign(container, "PRP_ShopSign", context.ShopPosition + new Vector3(-2.2f, 0.1f, 0f), "Shop frontage sign scaffold.");
            BuildLandmark(container, "PRP_HarborBeacon", new Vector3(19f, 0f, 3.5f), "Harbor landmark beacon for scene readability.");
            BuildLandmark(container, "PRP_GateBannerFrame", new Vector3(0f, 0f, -10.5f), "Encounter fringe landmark frame.");
        }

        private static void BuildVegetation(Transform container, EnvironmentContext context)
        {
            ClearManagedChildren(container);
            Vector3[] treeSlots =
            {
                new Vector3(-18f, 0f, 8.5f), new Vector3(-18f, 0f, 1.5f), new Vector3(-8.5f, 0f, 9f),
                new Vector3(8.5f, 0f, 9.5f), new Vector3(19f, 0f, 7f), new Vector3(19f, 0f, -1f)
            };

            for (int i = 0; i < treeSlots.Length; i++)
            {
                if (!IsNearExclusion(context, treeSlots[i], 1.25f))
                {
                    BuildTree(container, $"VEG_Tree_{i + 1:00}", treeSlots[i], $"Replace-safe tree slot {i + 1}.");
                }
            }

            Vector3[] clutterSlots =
            {
                new Vector3(-15f, 0f, 10.5f), new Vector3(-12f, 0f, 10f), new Vector3(-7f, 0f, 8.8f),
                new Vector3(6.5f, 0f, 9.6f), new Vector3(11f, 0f, 9.2f), new Vector3(17f, 0f, 8.4f),
                new Vector3(18.5f, 0f, 4.5f), new Vector3(18.2f, 0f, -2.5f), new Vector3(7f, 0f, -2f),
                new Vector3(-8.5f, 0f, -1.5f), new Vector3(-16.5f, 0f, -2.2f), new Vector3(-13.2f, 0f, 6.2f)
            };

            int clutterIndex = 1;
            for (int i = 0; i < clutterSlots.Length; i++)
            {
                if (!IsNearExclusion(context, clutterSlots[i], 1f) && !IsNearRoad(clutterSlots[i]))
                {
                    BuildClutter(container, $"VEG_Clutter_{clutterIndex:00}", clutterSlots[i], clutterIndex % 3 == 0, "Replace-safe clutter slot.");
                    clutterIndex++;
                }
            }
        }

        private static void BuildAmbient(Transform container)
        {
            ClearManagedChildren(container);
            BuildAmbientPair(container, "AMB_SquareChat_A", new Vector3(-2.8f, 0f, 1.2f), Quaternion.Euler(0f, 20f, 0f), "Ambient pair chatting in the square.");
            BuildAmbientPair(container, "AMB_SquareChat_B", new Vector3(-0.8f, 0f, 0.4f), Quaternion.Euler(0f, -15f, 0f), "Ambient pair chatting near the market frontage.");
            BuildAmbientSolo(container, "AMB_HarborLookout_A", new Vector3(15.8f, 0f, 7.2f), "Ambient lookout facing the harbor.");
            BuildAmbientSolo(container, "AMB_HarborLookout_B", new Vector3(18.8f, 0f, 2.4f), "Ambient lookout near the pier edge.");
            BuildAmbientSolo(container, "AMB_TavernRest_A", new Vector3(-8.5f, 0f, 6.8f), "Ambient villager resting near the tavern.");
            BuildAmbientSolo(container, "AMB_RainShelter_A", new Vector3(-1.8f, 0f, 6.8f), "Ambient rain shelter point near tavern awning.");
            BuildAmbientCreature(container, "AMB_Cat_Dock", new Vector3(9.2f, 0f, 1.5f), PrimitiveType.Sphere, "Ambient dock cat placeholder.");
            BuildAmbientCreature(container, "AMB_Bird_Post_A", new Vector3(18.2f, 1.6f, -2.2f), PrimitiveType.Sphere, "Ambient bird placeholder.");
            BuildAmbientCreature(container, "AMB_Bird_Post_B", new Vector3(19.4f, 1.7f, 5.6f), PrimitiveType.Sphere, "Ambient bird placeholder.");
        }

        private static void BuildDebug(Transform container, EnvironmentContext context)
        {
            BuildPrimitiveSlot(container, "DBG_SquareLandmark", EnvironmentGeneratedCategory.Debug, context.SquareCenter + new Vector3(0f, 1.8f, 0f), new Vector3(0.35f, 3.5f, 0.35f), PrimitiveType.Cylinder, "Scene readability landmark for the square.");
            BuildPrimitiveSlot(container, "DBG_HarborLandmark", EnvironmentGeneratedCategory.Debug, context.HarborPosition + new Vector3(0f, 2f, 0f), new Vector3(0.4f, 4f, 0.4f), PrimitiveType.Cylinder, "Scene readability landmark for the harbor.");
            BuildPrimitiveSlot(container, "DBG_ResidentialLandmark", EnvironmentGeneratedCategory.Debug, new Vector3(-14f, 1.9f, 4f), new Vector3(0.35f, 3.8f, 0.35f), PrimitiveType.Cylinder, "Scene readability landmark for the residential edge.");
        }

        private static void BuildHouseSlot(Transform container, string name, Vector3 position, Vector3 size, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Building, position, notes);
            if (HasManualChildren(slot))
            {
                return;
            }

            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Body", PrimitiveType.Cube, new Vector3(0f, size.y * 0.5f, 0f), size, EnvironmentGeneratedCategory.Building, notes);
            CreatePrimitiveVisual(slot.transform, "GEN_Roof", PrimitiveType.Cube, new Vector3(0f, size.y + 0.35f, 0f), new Vector3(size.x + 0.4f, 0.35f, size.z + 0.6f), EnvironmentGeneratedCategory.Building, "Roof placeholder.");
            CreatePrimitiveVisual(slot.transform, "GEN_Door", PrimitiveType.Cube, new Vector3(0f, 0.9f, size.z * 0.5f + 0.05f), new Vector3(0.8f, 1.8f, 0.2f), EnvironmentGeneratedCategory.Building, "Door readout marker.");
        }

        private static void BuildCrateCluster(Transform container, string name, Vector3 position, int crateCount, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Prop, position, notes);
            if (HasManualChildren(slot))
            {
                return;
            }

            ClearManagedChildren(slot.transform);
            for (int i = 0; i < crateCount; i++)
            {
                float offsetX = (i % 2) * 0.9f;
                float offsetZ = (i / 2) * 0.8f;
                CreatePrimitiveVisual(slot.transform, $"GEN_Crate_{i + 1}", PrimitiveType.Cube, new Vector3(offsetX, 0.35f, offsetZ), new Vector3(0.7f, 0.7f, 0.7f), EnvironmentGeneratedCategory.Prop, "Cargo crate placeholder.");
            }
        }

        private static void BuildFenceRun(Transform container, string name, Vector3 startPosition, int count, float spacing, Quaternion rotation, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Prop, startPosition, notes, rotation);
            if (HasManualChildren(slot))
            {
                return;
            }

            ClearManagedChildren(slot.transform);
            for (int i = 0; i < count; i++)
            {
                CreatePrimitiveVisual(slot.transform, $"GEN_Post_{i + 1}", PrimitiveType.Cylinder, new Vector3(i * spacing, 0.6f, 0f), new Vector3(0.16f, 0.6f, 0.16f), EnvironmentGeneratedCategory.Prop, "Fence post placeholder.");
            }

            for (int i = 0; i < count - 1; i++)
            {
                CreatePrimitiveVisual(slot.transform, $"GEN_Rail_{i + 1}", PrimitiveType.Cube, new Vector3(i * spacing + spacing * 0.5f, 0.9f, 0f), new Vector3(spacing, 0.12f, 0.12f), EnvironmentGeneratedCategory.Prop, "Fence rail placeholder.");
            }
        }

        private static void BuildLamp(Transform container, string name, Vector3 position, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Prop, position, notes);
            if (HasManualChildren(slot))
            {
                return;
            }

            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Post", PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 0f), new Vector3(0.14f, 1.4f, 0.14f), EnvironmentGeneratedCategory.Prop, "Lamp post placeholder.");
            CreatePrimitiveVisual(slot.transform, "GEN_Lantern", PrimitiveType.Sphere, new Vector3(0f, 2.95f, 0f), new Vector3(0.45f, 0.45f, 0.45f), EnvironmentGeneratedCategory.Prop, "Lantern placeholder.");
        }

        private static void BuildSign(Transform container, string name, Vector3 position, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Prop, position, notes);
            if (HasManualChildren(slot))
            {
                return;
            }

            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Post", PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f), new Vector3(0.12f, 1f, 0.12f), EnvironmentGeneratedCategory.Prop, "Sign post placeholder.");
            CreatePrimitiveVisual(slot.transform, "GEN_Plate", PrimitiveType.Cube, new Vector3(0f, 2.05f, 0f), new Vector3(1.8f, 0.8f, 0.2f), EnvironmentGeneratedCategory.Prop, "Sign plate placeholder.");
        }

        private static void BuildLandmark(Transform container, string name, Vector3 position, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Prop, position, notes);
            if (HasManualChildren(slot))
            {
                return;
            }

            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Base", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 1.2f), EnvironmentGeneratedCategory.Prop, "Landmark base placeholder.");
            CreatePrimitiveVisual(slot.transform, "GEN_Spire", PrimitiveType.Cylinder, new Vector3(0f, 2.8f, 0f), new Vector3(0.3f, 2.2f, 0.3f), EnvironmentGeneratedCategory.Prop, "Landmark spire placeholder.");
        }

        private static void BuildTree(Transform container, string name, Vector3 position, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Vegetation, position, notes);
            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f), new Vector3(0.35f, 1f, 0.35f), EnvironmentGeneratedCategory.Vegetation, "Tree trunk placeholder.");
            CreatePrimitiveVisual(slot.transform, "GEN_Canopy", PrimitiveType.Sphere, new Vector3(0f, 2.8f, 0f), new Vector3(2f, 2f, 2f), EnvironmentGeneratedCategory.Vegetation, "Tree canopy placeholder.");
        }

        private static void BuildClutter(Transform container, string name, Vector3 position, bool bushHeavy, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Vegetation, position, notes);
            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Stone", PrimitiveType.Sphere, new Vector3(0f, 0.18f, 0f), new Vector3(0.55f, 0.3f, 0.45f), EnvironmentGeneratedCategory.Vegetation, "Small stone placeholder.");
            CreatePrimitiveVisual(slot.transform, "GEN_Grass", PrimitiveType.Cylinder, new Vector3(0.4f, 0.14f, 0.2f), new Vector3(0.12f, 0.15f, 0.12f), EnvironmentGeneratedCategory.Vegetation, "Grass tuft placeholder.");
            if (bushHeavy)
            {
                CreatePrimitiveVisual(slot.transform, "GEN_Bush", PrimitiveType.Sphere, new Vector3(-0.35f, 0.35f, 0.1f), new Vector3(0.8f, 0.7f, 0.8f), EnvironmentGeneratedCategory.Vegetation, "Bush placeholder.");
            }
        }

        private static void BuildAmbientPair(Transform container, string name, Vector3 position, Quaternion rotation, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Ambient, position, notes, rotation);
            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Actor_A", PrimitiveType.Capsule, new Vector3(-0.5f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), EnvironmentGeneratedCategory.Ambient, "Ambient actor placeholder.");
            CreatePrimitiveVisual(slot.transform, "GEN_Actor_B", PrimitiveType.Capsule, new Vector3(0.5f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), EnvironmentGeneratedCategory.Ambient, "Ambient actor placeholder.");
        }

        private static void BuildAmbientSolo(Transform container, string name, Vector3 position, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Ambient, position, notes);
            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Actor", PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.8f, 1f, 0.8f), EnvironmentGeneratedCategory.Ambient, "Ambient actor placeholder.");
        }

        private static void BuildAmbientCreature(Transform container, string name, Vector3 position, PrimitiveType type, string notes)
        {
            GameObject slot = EnsureSlot(container, name, EnvironmentGeneratedCategory.Ambient, position, notes);
            ClearManagedChildren(slot.transform);
            Vector3 scale = type == PrimitiveType.Sphere ? new Vector3(0.35f, 0.35f, 0.35f) : new Vector3(0.5f, 0.5f, 0.5f);
            CreatePrimitiveVisual(slot.transform, "GEN_Creature", type, Vector3.zero, scale, EnvironmentGeneratedCategory.Ambient, notes);
        }

        private static void BuildPrimitiveSlot(Transform container, string name, EnvironmentGeneratedCategory category, Vector3 position, Vector3 primitiveScale, PrimitiveType primitiveType, string notes)
        {
            GameObject slot = EnsureSlot(container, name, category, position, notes);
            if (HasManualChildren(slot))
            {
                return;
            }

            ClearManagedChildren(slot.transform);
            CreatePrimitiveVisual(slot.transform, "GEN_Visual", primitiveType, Vector3.zero, primitiveScale, category, notes);
        }

        private static GameObject EnsureSlot(Transform container, string name, EnvironmentGeneratedCategory category, Vector3 localPosition, string notes, Quaternion? localRotation = null)
        {
            GameObject slot = EnsureManagedNode(container, name, category, notes);
            slot.transform.localPosition = localPosition;
            slot.transform.localRotation = localRotation ?? Quaternion.identity;
            slot.transform.localScale = Vector3.one;
            return slot;
        }

        private static GameObject EnsureManagedNode(Transform parent, string name, EnvironmentGeneratedCategory category, string notes)
        {
            GameObject target = FindImmediateChild(parent, name);
            if (target == null)
            {
                target = new GameObject(name);
                target.transform.SetParent(parent, false);
            }

            EnvironmentGeneratedMarker marker = target.GetComponent<EnvironmentGeneratedMarker>();
            if (marker == null)
            {
                marker = target.AddComponent<EnvironmentGeneratedMarker>();
            }

            SerializedObject so = new SerializedObject(marker);
            so.FindProperty("_generationId").stringValue = GenerationId;
            so.FindProperty("_category").enumValueIndex = (int)category;
            so.FindProperty("_replaceSafe").boolValue = true;
            so.FindProperty("_preserveManualChildren").boolValue = true;
            so.FindProperty("_notes").stringValue = notes;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(marker);
            return target;
        }

        private static GameObject CreatePrimitiveVisual(Transform parent, string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, EnvironmentGeneratedCategory category, string notes)
        {
            GameObject target = GameObject.CreatePrimitive(primitiveType);
            target.name = name;
            target.transform.SetParent(parent, false);
            target.transform.localPosition = localPosition;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = localScale;

            EnvironmentGeneratedMarker marker = target.AddComponent<EnvironmentGeneratedMarker>();
            SerializedObject so = new SerializedObject(marker);
            so.FindProperty("_generationId").stringValue = GenerationId;
            so.FindProperty("_category").enumValueIndex = (int)category;
            so.FindProperty("_replaceSafe").boolValue = true;
            so.FindProperty("_preserveManualChildren").boolValue = false;
            so.FindProperty("_notes").stringValue = notes;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(marker);
            return target;
        }

        private static bool HasManualChildren(GameObject slot)
        {
            Transform transform = slot.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                EnvironmentGeneratedMarker marker = child.GetComponent<EnvironmentGeneratedMarker>();
                if (marker == null || !string.Equals(marker.GenerationId, GenerationId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ClearManagedChildren(Transform container)
        {
            var toDestroy = new List<GameObject>();
            for (int i = 0; i < container.childCount; i++)
            {
                GameObject child = container.GetChild(i).gameObject;
                EnvironmentGeneratedMarker marker = child.GetComponent<EnvironmentGeneratedMarker>();
                if (marker != null && string.Equals(marker.GenerationId, GenerationId, StringComparison.Ordinal))
                {
                    toDestroy.Add(child);
                }
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(toDestroy[i]);
            }
        }

        private static void ValidateManagedContainer(GameObject generatedRoot, string containerName, EnvironmentValidationResult result)
        {
            GameObject target = FindImmediateChild(generatedRoot.transform, containerName);
            if (target == null)
            {
                result.Errors.Add($"{containerName} missing under {GeneratedRootName}.");
                return;
            }

            EnvironmentGeneratedMarker marker = target.GetComponent<EnvironmentGeneratedMarker>();
            if (marker == null || !string.Equals(marker.GenerationId, GenerationId, StringComparison.Ordinal))
            {
                result.Errors.Add($"{containerName} is missing an EnvironmentGeneratedMarker with generation id '{GenerationId}'.");
            }
        }

        private static void ValidateExclusions(GameObject container, EnvironmentValidationResult result, EnvironmentContext context, float extraRadius, params string[] ignoredLabels)
        {
            if (container == null)
            {
                return;
            }

            foreach (Transform child in container.transform)
            {
                Vector3 position = child.position;
                position.y = 0f;
                for (int i = 0; i < context.Exclusions.Count; i++)
                {
                    ExclusionZone zone = context.Exclusions[i];
                    if (Array.IndexOf(ignoredLabels, zone.Label) >= 0)
                    {
                        continue;
                    }

                    if (Vector3.Distance(position, zone.Center) < zone.Radius + extraRadius)
                    {
                        result.Errors.Add($"{child.name} is too close to exclusion zone '{zone.Label}'.");
                        break;
                    }
                }
            }
        }

        private static bool IsNearExclusion(EnvironmentContext context, Vector3 position, float extraRadius)
        {
            Vector3 test = position;
            test.y = 0f;
            for (int i = 0; i < context.Exclusions.Count; i++)
            {
                ExclusionZone zone = context.Exclusions[i];
                if (Vector3.Distance(test, zone.Center) < zone.Radius + extraRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNearRoad(Vector3 position)
        {
            return Mathf.Abs(position.x) < 2.8f && position.z > -10f && position.z < 8.5f
                || Mathf.Abs(position.z - 5.5f) < 2f && position.x > -10f && position.x < 10f
                || Mathf.Abs(position.z - 3f) < 1.8f && position.x > 5f && position.x < 12.5f;
        }

        private static void AddExclusion(EnvironmentContext context, GameObject target, float radius, string label)
        {
            if (target == null)
            {
                return;
            }

            Vector3 center = target.transform.position;
            center.y = 0f;
            context.Exclusions.Add(new ExclusionZone(center, radius, label));
        }

        private static void LogValidationResult(EnvironmentValidationResult result, string prefix)
        {
            if (result.Errors.Count > 0)
            {
                for (int i = 0; i < result.Errors.Count; i++)
                {
                    Debug.LogError($"{prefix} {result.Errors[i]}");
                }
            }

            for (int i = 0; i < result.Warnings.Count; i++)
            {
                Debug.LogWarning($"{prefix} {result.Warnings[i]}");
            }

            if (result.Errors.Count == 0)
            {
                Debug.Log($"{prefix} Environment validation passed with {result.Warnings.Count} warning(s).");
            }
        }

        private static GameObject FindDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject found = FindChild(root, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindChild(GameObject parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent.transform)
            {
                GameObject found = FindChild(child.gameObject, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindImmediateChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
