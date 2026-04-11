using UnityEditor;

namespace TPS.Editor
{
    public static class Phase1AutomationBridge
    {
        [MenuItem("Tools/TPS/Phase 1/Automation/Enter Play Mode")]
        public static void EnterPlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }
        }

        [MenuItem("Tools/TPS/Phase 1/Automation/Exit Play Mode")]
        public static void ExitPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }
    }
}
