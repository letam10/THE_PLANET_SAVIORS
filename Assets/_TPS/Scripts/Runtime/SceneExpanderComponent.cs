#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace TPS.Editor
{
    [ExecuteAlways]
    public class SceneExpanderComponent : MonoBehaviour
    {
        private static readonly string[] TargetScenes = new string[]
        {
            "Assets/_TPS/Scenes/World/ZN_Settlement_Gullwatch.unity",
            "Assets/_TPS/Scenes/World/ZN_Settlement_RedCedar.unity",
            "Assets/_TPS/Scenes/World/ZN_Town_AsterHarbor.unity",
            "Assets/_TPS/Scenes/Dungeons/DG_QuarryRuins.unity",
            "Assets/_TPS/Scenes/Dungeons/DG_TideCaverns.unity"
        };

        private void OnEnable()
        {
            if (Application.isPlaying) return;

            string triggerPath = "Assets/_TPS/Scripts/Editor/.scene_expand_pending";
            if (File.Exists(triggerPath))
            {
                File.Delete(triggerPath);
                Debug.Log("SceneExpanderComponent: Trigger detected! Expanding 5 target scenes...");
                ExpandTargetScenes();
                
                // Self-destruct component
                EditorApplication.delayCall += () => 
                {
                    if (this != null)
                        DestroyImmediate(this.gameObject);
                };
            }
        }

        public static void ExpandTargetScenes()
        {
            int count = 0;
            foreach (string scenePath in TargetScenes)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"Scene not found: {scenePath}");
                    continue;
                }

                // Additive load to not break the current scene sequence
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                
                InjectScaffolding(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true); // remove from hierarchy
                count++;
            }

            Debug.Log($"Successfully expanded {count} scenes via Additive process!");
        }

        private static void InjectScaffolding(Scene scene)
        {
            // Find or create "Environment" root inside specific scene
            GameObject envRoot = null;
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.name == "Environment")
                {
                    envRoot = r;
                    break;
                }
            }

            if (envRoot == null)
            {
                envRoot = new GameObject("Environment");
                SceneManager.MoveGameObjectToScene(envRoot, scene);
            }

            // Create scaffolding parent
            Transform scaffoldRoot = envRoot.transform.Find("MultiLayerScaffolding_x3");
            if (scaffoldRoot != null)
            {
                DestroyImmediate(scaffoldRoot.gameObject); // Clean up old if regenerating
            }

            GameObject newScaffold = new GameObject("MultiLayerScaffolding_x3");
            newScaffold.transform.SetParent(envRoot.transform);

            // Layer 0 - Expanded Base (x3 area)
            CreateLayer(newScaffold.transform, "Layer_0_Base", new Vector3(0, 0, 0), new Vector3(150, 1, 150), Color.gray);
            
            // Layer 1 - Upper Walkways
            CreateLayer(newScaffold.transform, "Layer_1_Mid", new Vector3(0, 15, 0), new Vector3(100, 1, 100), new Color(0.6f, 0.6f, 0.6f));
            
            // Layer 2 - High Towers / Peaks
            CreateLayer(newScaffold.transform, "Layer_2_High", new Vector3(0, 30, 0), new Vector3(50, 1, 50), new Color(0.4f, 0.4f, 0.4f));

            // Generate some connecting ramps (crude)
            CreateRamp(newScaffold.transform, "Ramp_0_1", new Vector3(25, 7.5f, 0), new Vector3(20, 1, 80), new Vector3(0, 0, 20f));
            CreateRamp(newScaffold.transform, "Ramp_1_2", new Vector3(-15, 22.5f, -15), new Vector3(20, 1, 60), new Vector3(45f, 0, 0));
        }

        private static void CreateLayer(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            layer.name = name;
            layer.transform.SetParent(parent);
            layer.transform.localPosition = localPos;
            layer.transform.localScale = scale;

            Renderer r = layer.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                Material mat = new Material(r.sharedMaterial);
                mat.color = color;
                r.sharedMaterial = mat;
            }
        }

        private static void CreateRamp(Transform parent, string name, Vector3 localPos, Vector3 scale, Vector3 eulerAngles)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.transform.SetParent(parent);
            ramp.transform.localPosition = localPos;
            ramp.transform.localScale = scale;
            ramp.transform.localEulerAngles = eulerAngles;
        }
    }
}
#endif
