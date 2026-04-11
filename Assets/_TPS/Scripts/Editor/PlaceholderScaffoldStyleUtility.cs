using System.IO;
using TPS.Runtime.World;
using UnityEditor;
using UnityEngine;

namespace TPS.Editor
{
    internal static class PlaceholderScaffoldStyleUtility
    {
        private const string MaterialFolder = "Assets/_TPS/Data/Environment/GeneratedMaterials";

        public static void ApplyStyle(GameObject target, EnvironmentGeneratedCategory category, string name, string notes)
        {
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetMaterialFor(category, name);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            SimplePlaceholderMotion motion = target.GetComponent<SimplePlaceholderMotion>();
            if (motion != null)
            {
                Object.DestroyImmediate(motion);
            }

            string notesLower = notes != null ? notes.ToLowerInvariant() : string.Empty;

            if (category == EnvironmentGeneratedCategory.Ambient)
            {
                motion = target.AddComponent<SimplePlaceholderMotion>();
                SerializedObject so = new SerializedObject(motion);
                bool creature = name.IndexOf("Creature", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Dog", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Cat", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Bird", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool walker = notesLower.Contains("worker") || notesLower.Contains("lookout") || notesLower.Contains("watcher") || notesLower.Contains("shelter") || notesLower.Contains("wander");
                so.FindProperty("_mode").enumValueIndex = (int)(creature || walker ? SimplePlaceholderMotionMode.Pace : SimplePlaceholderMotionMode.Bob);
                so.FindProperty("_speed").floatValue = creature ? 0.8f : walker ? 0.55f : 1.1f;
                so.FindProperty("_amplitude").floatValue = creature ? 0.08f : walker ? 0.06f : 0.1f;
                so.FindProperty("_paceOffset").vector3Value = creature ? new Vector3(0.35f, 0f, 0.35f) : walker ? new Vector3(0.5f, 0f, 0.25f) : new Vector3(0.2f, 0f, 0.2f);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(motion);
            }
            else if (category == EnvironmentGeneratedCategory.Prop && (name.IndexOf("Lantern", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Plate", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Spire", System.StringComparison.OrdinalIgnoreCase) >= 0 || notesLower.Contains("awning") || notesLower.Contains("banner")))
            {
                motion = target.AddComponent<SimplePlaceholderMotion>();
                SerializedObject so = new SerializedObject(motion);
                so.FindProperty("_mode").enumValueIndex = (int)SimplePlaceholderMotionMode.Sway;
                so.FindProperty("_speed").floatValue = 0.65f;
                so.FindProperty("_amplitude").floatValue = 0.06f;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(motion);
            }
        }

        private static Material GetMaterialFor(EnvironmentGeneratedCategory category, string name)
        {
            if (name.IndexOf("Roof", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Body", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                category = EnvironmentGeneratedCategory.Building;
            }
            else if (name.IndexOf("Trunk", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Canopy", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Bush", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Grass", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                category = EnvironmentGeneratedCategory.Vegetation;
            }
            else if (name.IndexOf("Actor", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Creature", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                category = EnvironmentGeneratedCategory.Ambient;
            }

            string paletteId;
            Color color;

            switch (category)
            {
                case EnvironmentGeneratedCategory.Blockout:
                    paletteId = "blockout";
                    color = name.IndexOf("DOCK", System.StringComparison.OrdinalIgnoreCase) >= 0
                        ? new Color(0.36f, 0.29f, 0.21f, 1f)
                        : new Color(0.55f, 0.49f, 0.38f, 1f);
                    break;
                case EnvironmentGeneratedCategory.Building:
                    if (name.IndexOf("Roof", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        paletteId = "roof";
                        color = new Color(0.61f, 0.27f, 0.18f, 1f);
                    }
                    else if (name.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        paletteId = "door";
                        color = new Color(0.19f, 0.12f, 0.08f, 1f);
                    }
                    else
                    {
                        paletteId = "building";
                        color = new Color(0.83f, 0.77f, 0.66f, 1f);
                    }
                    break;
                case EnvironmentGeneratedCategory.Vegetation:
                    paletteId = "vegetation";
                    color = name.IndexOf("Trunk", System.StringComparison.OrdinalIgnoreCase) >= 0
                        ? new Color(0.34f, 0.22f, 0.14f, 1f)
                        : new Color(0.28f, 0.47f, 0.25f, 1f);
                    break;
                case EnvironmentGeneratedCategory.Ambient:
                    paletteId = "ambient";
                    color = name.IndexOf("Creature", System.StringComparison.OrdinalIgnoreCase) >= 0
                        ? new Color(0.76f, 0.62f, 0.34f, 1f)
                        : new Color(0.22f, 0.69f, 0.84f, 1f);
                    break;
                case EnvironmentGeneratedCategory.Debug:
                    paletteId = "debug";
                    color = new Color(0.2f, 0.85f, 0.95f, 1f);
                    break;
                default:
                    paletteId = "prop";
                    color = name.IndexOf("Post", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Crate", System.StringComparison.OrdinalIgnoreCase) >= 0
                        ? new Color(0.42f, 0.31f, 0.18f, 1f)
                        : new Color(0.72f, 0.66f, 0.5f, 1f);
                    break;
            }

            return LoadOrCreateMaterial(paletteId, color);
        }

        private static Material LoadOrCreateMaterial(string materialName, Color color)
        {
            EnsureFolders();
            string assetPath = Path.Combine(MaterialFolder, $"MAT_{materialName}.mat").Replace('\\', '/');
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_TPS/Data/Environment"))
            {
                AssetDatabase.CreateFolder("Assets/_TPS/Data", "Environment");
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/_TPS/Data/Environment", "GeneratedMaterials");
            }
        }
    }
}
