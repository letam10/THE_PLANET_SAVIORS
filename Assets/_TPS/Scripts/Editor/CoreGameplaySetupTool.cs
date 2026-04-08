using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TPS.Runtime.Spawn;
using TPS.Runtime.Player;
using TPS.Runtime.Interaction;
using TPS.Runtime.Triggers;
using TPS.Runtime.Debugging;
using TPS.Data.Config;

namespace TPS.Editor
{
    /// <summary>
    /// Editor tool for setting up the core gameplay loop.
    /// Every operation is idempotent — running it again changes nothing if already correct.
    /// </summary>
    public class CoreGameplaySetupTool : EditorWindow
    {
        [MenuItem("Tools/TPS/Core Gameplay Setup")]
        static void Open()
        {
            GetWindow<CoreGameplaySetupTool>("TPS Core Setup");
        }

        private Vector2 _scrollPos;

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Prefab Fixes", EditorStyles.boldLabel);
            if (GUILayout.Button("Fix Player Prefab")) FixPlayerPrefab();
            if (GUILayout.Button("Fix Door Prefab")) FixDoorPrefab();
            if (GUILayout.Button("Fix Trigger Prefab")) FixTriggerPrefab();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Setup Core Scene")) SetupCoreScene();
            if (GUILayout.Button("Setup Town Scene")) SetupTownScene();
            if (GUILayout.Button("Setup Battle Scene")) SetupBattleScene();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Config", EditorStyles.boldLabel);
            if (GUILayout.Button("Link Player To Config")) LinkPlayerToConfig();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Verify", EditorStyles.boldLabel);
            if (GUILayout.Button("Verify Build Settings")) VerifyBuildSettings();

            EditorGUILayout.EndScrollView();
        }

        // =====================================================================
        // PREFAB FIXES
        // =====================================================================

        private void FixPlayerPrefab()
        {
            const string path = "Assets/_TPS/Prefabs/Prototypes/PF_Player_Prototype.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError($"Prefab not found: {path}"); return; }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Remove CapsuleCollider (CharacterController has its own collider)
                var capsule = contents.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    DestroyImmediate(capsule);
                    Debug.Log("FixPlayerPrefab: Removed CapsuleCollider.");
                }

                // Ensure CharacterController exists and configure it
                var cc = contents.GetComponent<CharacterController>();
                if (cc == null)
                {
                    cc = contents.AddComponent<CharacterController>();
                    cc.height = 2f;
                    cc.radius = 0.5f;
                    cc.center = new Vector3(0, 1f, 0);
                    Debug.Log("FixPlayerPrefab: Added CharacterController.");
                }
                else
                {
                    // Fix center if at origin (should be at y=1 for capsule height 2)
                    if (cc.center.y < 0.5f)
                    {
                        cc.center = new Vector3(0, 1f, 0);
                        Debug.Log("FixPlayerPrefab: Fixed CharacterController center to (0,1,0).");
                    }
                }

                // Make sure tag is Player
                contents.tag = "Player";

                // Ensure PlayerInput exists
                var pi = contents.GetComponent<PlayerInput>();
                if (pi == null)
                {
                    pi = contents.AddComponent<PlayerInput>();
                    Debug.Log("FixPlayerPrefab: Added PlayerInput.");
                }

                // Try to assign IA_Player asset
                var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/_TPS/Input/IA_Player.inputactions");
                if (inputAsset != null && pi.actions != inputAsset)
                {
                    pi.actions = inputAsset;
                    pi.defaultActionMap = "Player";
                    Debug.Log("FixPlayerPrefab: Assigned IA_Player to PlayerInput.");
                }

                // Ensure PlayerController
                if (contents.GetComponent<PlayerController>() == null)
                {
                    contents.AddComponent<PlayerController>();
                    Debug.Log("FixPlayerPrefab: Added PlayerController.");
                }

                // Ensure PlayerCameraController
                var pcc = contents.GetComponent<PlayerCameraController>();
                if (pcc == null)
                {
                    pcc = contents.AddComponent<PlayerCameraController>();
                    Debug.Log("FixPlayerPrefab: Added PlayerCameraController.");
                }

                // Ensure PlayerInteractionController
                if (contents.GetComponent<PlayerInteractionController>() == null)
                {
                    contents.AddComponent<PlayerInteractionController>();
                    Debug.Log("FixPlayerPrefab: Added PlayerInteractionController.");
                }

                // Create CameraPivot if missing
                Transform cameraPivot = contents.transform.Find("CameraPivot");
                if (cameraPivot == null)
                {
                    var pivotGO = new GameObject("CameraPivot");
                    pivotGO.transform.SetParent(contents.transform);
                    pivotGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                    pivotGO.transform.localRotation = Quaternion.identity;
                    cameraPivot = pivotGO.transform;
                    Debug.Log("FixPlayerPrefab: Created CameraPivot.");
                }

                // Create Main Camera under CameraPivot if missing
                Transform camTransform = cameraPivot.Find("Main Camera");
                Camera playerCam = null;
                if (camTransform == null)
                {
                    var camGO = new GameObject("Main Camera");
                    camGO.transform.SetParent(cameraPivot);
                    camGO.transform.localPosition = new Vector3(0f, 0f, -4f);
                    camGO.transform.localRotation = Quaternion.identity;
                    playerCam = camGO.AddComponent<Camera>();
                    camGO.AddComponent<AudioListener>();
                    camGO.tag = "MainCamera";
                    camTransform = camGO.transform;
                    Debug.Log("FixPlayerPrefab: Created Main Camera under CameraPivot.");
                }
                else
                {
                    playerCam = camTransform.GetComponent<Camera>();
                    if (playerCam == null) playerCam = camTransform.gameObject.AddComponent<Camera>();
                    if (camTransform.GetComponent<AudioListener>() == null)
                        camTransform.gameObject.AddComponent<AudioListener>();
                    camTransform.gameObject.tag = "MainCamera";
                }

                // Wire PlayerCameraController references via SerializedObject
                var soPCC = new SerializedObject(pcc);
                var pivotProp = soPCC.FindProperty("_cameraPivot");
                var camProp = soPCC.FindProperty("_playerCamera");
                if (pivotProp != null) pivotProp.objectReferenceValue = cameraPivot;
                if (camProp != null) camProp.objectReferenceValue = playerCam;
                soPCC.ApplyModifiedPropertiesWithoutUndo();

                // Wire PlayerInteractionController._rayOrigin to camera
                var pic = contents.GetComponent<PlayerInteractionController>();
                if (pic != null)
                {
                    var soPIC = new SerializedObject(pic);
                    var rayProp = soPIC.FindProperty("_rayOrigin");
                    if (rayProp != null) rayProp.objectReferenceValue = camTransform;
                    soPIC.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("FixPlayerPrefab: DONE ✓");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private void FixDoorPrefab()
        {
            const string path = "Assets/_TPS/Prefabs/Prototypes/PF_Door_Test_A.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError($"Prefab not found: {path}"); return; }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var door = contents.GetComponent<DoorPrototype>();
                if (door == null)
                {
                    door = contents.AddComponent<DoorPrototype>();
                    Debug.Log("FixDoorPrefab: Added DoorPrototype.");
                }

                // Wire _doorHinge
                Transform hinge = contents.transform.Find("DoorHinge");
                if (hinge != null)
                {
                    var so = new SerializedObject(door);
                    var hingeProp = so.FindProperty("_doorHinge");
                    if (hingeProp != null && hingeProp.objectReferenceValue != hinge)
                    {
                        hingeProp.objectReferenceValue = hinge;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("FixDoorPrefab: Wired DoorHinge reference.");
                    }
                }
                else
                {
                    Debug.LogWarning("FixDoorPrefab: DoorHinge child not found.");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("FixDoorPrefab: DONE ✓");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private void FixTriggerPrefab()
        {
            const string path = "Assets/_TPS/Prefabs/Prototypes/PF_EncounterTrigger_Test.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError($"Prefab not found: {path}"); return; }

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var trigger = contents.GetComponent<EncounterTrigger>();
                if (trigger == null)
                {
                    trigger = contents.AddComponent<EncounterTrigger>();
                    Debug.Log("FixTriggerPrefab: Added EncounterTrigger.");
                }

                // Set battle scene name via SerializedObject
                var so = new SerializedObject(trigger);
                var sceneProp = so.FindProperty("_battleSceneName");
                if (sceneProp != null && sceneProp.stringValue != "BTL_Standard")
                {
                    sceneProp.stringValue = "BTL_Standard";
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("FixTriggerPrefab: Set battleSceneName = BTL_Standard.");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("FixTriggerPrefab: DONE ✓");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // =====================================================================
        // SCENE SETUP
        // =====================================================================

        private void SetupCoreScene()
        {
            const string path = "Assets/_TPS/Scenes/Core/Core.unity";
            bool wasOpen;
            Scene scene = OpenSceneSafely(path, out wasOpen);
            if (!scene.IsValid()) return;

            try
            {
                GameObject coreServices = FindInScene(scene, "CoreServices");
                if (coreServices == null)
                {
                    Debug.LogError("SetupCoreScene: CoreServices not found.");
                    return;
                }

                AddComponentIfMissing<PlayerSpawnSystem>(coreServices, "PlayerSpawnSystem");
                AddComponentIfMissing<DebugWorldHUD>(coreServices, "DebugWorldHUD");

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("SetupCoreScene: DONE ✓");
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private void SetupTownScene()
        {
            const string path = "Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity";
            bool wasOpen;
            Scene scene = OpenSceneSafely(path, out wasOpen);
            if (!scene.IsValid()) return;

            try
            {
                // Remove hand-placed player instance
                GameObject playerInScene = FindInScene(scene, "PF_Player_Prototype");
                if (playerInScene != null)
                {
                    DestroyImmediate(playerInScene);
                    Debug.Log("SetupTownScene: Removed hand-placed PF_Player_Prototype.");
                }

                // Disable WorldCamera_Test
                GameObject worldCam = FindInScene(scene, "WorldCamera_Test");
                if (worldCam != null && worldCam.activeSelf)
                {
                    worldCam.SetActive(false);
                    Debug.Log("SetupTownScene: Disabled WorldCamera_Test.");
                }

                // Add SpawnPoint to MK_PlayerSpawn
                GameObject spawnMarker = FindInSceneDeep(scene, "MK_PlayerSpawn");
                if (spawnMarker != null)
                {
                    AddSpawnPointIfMissing(spawnMarker, "Default");
                }
                else
                {
                    Debug.LogWarning("SetupTownScene: MK_PlayerSpawn not found.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("SetupTownScene: DONE ✓");
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private void SetupBattleScene()
        {
            const string path = "Assets/_TPS/Scenes/Battle/BTL_Standard.unity";
            bool wasOpen;
            Scene scene = OpenSceneSafely(path, out wasOpen);
            if (!scene.IsValid()) return;

            try
            {
                // Add SpawnPoint to MK_PartySlot_01
                GameObject slot = FindInSceneDeep(scene, "MK_PartySlot_01");
                if (slot != null)
                {
                    AddSpawnPointIfMissing(slot, "Default");
                }
                else
                {
                    Debug.LogWarning("SetupBattleScene: MK_PartySlot_01 not found.");
                }

                // Disable BattleCamera to avoid duplicate camera / AudioListener
                GameObject battleCam = FindInScene(scene, "BattleCamera");
                if (battleCam != null && battleCam.activeSelf)
                {
                    battleCam.SetActive(false);
                    Debug.Log("SetupBattleScene: Disabled BattleCamera.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("SetupBattleScene: DONE ✓");
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // =====================================================================
        // CONFIG LINK
        // =====================================================================

        private void LinkPlayerToConfig()
        {
            const string configPath = "Assets/_TPS/Data/Config/CFG_GameConfig.asset";
            const string prefabPath = "Assets/_TPS/Prefabs/Prototypes/PF_Player_Prototype.prefab";

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(configPath);
            if (config == null) { Debug.LogError($"Config not found: {configPath}"); return; }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogError($"Prefab not found: {prefabPath}"); return; }

            var so = new SerializedObject(config);
            var prop = so.FindProperty("_playerPrefab");
            if (prop == null) { Debug.LogError("LinkPlayerToConfig: _playerPrefab property not found. Compile first?"); return; }

            if (prop.objectReferenceValue == prefab)
            {
                Debug.Log("LinkPlayerToConfig: Already linked. Skipping.");
                return;
            }

            prop.objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("LinkPlayerToConfig: DONE ✓ — PF_Player_Prototype assigned to CFG_GameConfig.");
        }

        // =====================================================================
        // BUILD SETTINGS
        // =====================================================================

        private void VerifyBuildSettings()
        {
            string[] requiredScenes = new string[]
            {
                "Assets/_TPS/Scenes/Bootstrap/Bootstrap.unity",
                "Assets/_TPS/Scenes/Core/Core.unity",
                "Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity",
                "Assets/_TPS/Scenes/Battle/BTL_Standard.unity"
            };

            var existingScenes = EditorBuildSettings.scenes;
            bool allPresent = true;

            foreach (string req in requiredScenes)
            {
                bool found = false;
                foreach (var s in existingScenes)
                {
                    if (s.path == req && s.enabled)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogWarning($"VerifyBuildSettings: MISSING or DISABLED — {req}");
                    allPresent = false;
                }
            }

            if (allPresent)
            {
                Debug.Log("VerifyBuildSettings: All required scenes present ✓");
            }
            else
            {
                Debug.LogWarning("VerifyBuildSettings: Some scenes are missing. Please add them via File > Build Settings.");
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private Scene OpenSceneSafely(string path, out bool wasAlreadyOpen)
        {
            // Check if scene is already open
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path == path)
                {
                    wasAlreadyOpen = true;
                    return s;
                }
            }

            wasAlreadyOpen = false;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }

        private GameObject FindInSceneDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;

                Transform found = FindChildRecursive(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private void AddComponentIfMissing<T>(GameObject target, string label) where T : Component
        {
            if (target.GetComponent<T>() == null)
            {
                target.AddComponent<T>();
                Debug.Log($"Added {label} to {target.name}.");
            }
            else
            {
                Debug.Log($"{label} already on {target.name}. Skipping.");
            }
        }

        private void AddSpawnPointIfMissing(GameObject target, string spawnId)
        {
            var sp = target.GetComponent<SpawnPoint>();
            if (sp == null)
            {
                sp = target.AddComponent<SpawnPoint>();
                Debug.Log($"Added SpawnPoint to {target.name}.");
            }

            // Set spawn id via SerializedObject
            var so = new SerializedObject(sp);
            var idProp = so.FindProperty("_spawnId");
            if (idProp != null && idProp.stringValue != spawnId)
            {
                idProp.stringValue = spawnId;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"Set SpawnPoint.SpawnId = '{spawnId}' on {target.name}.");
            }
        }
    }
}
