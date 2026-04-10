using System.IO;
using UnityEditor;

namespace TPS.Editor
{
    [InitializeOnLoad]
    public static class Phase1EditorCommandBridge
    {
        private const string CommandFileName = ".phase1_editor_command.txt";

        static Phase1EditorCommandBridge()
        {
            EditorApplication.update += PollCommandFile;
        }

        private static void PollCommandFile()
        {
            string path = GetCommandPath();
            if (!File.Exists(path))
            {
                return;
            }

            string command = File.ReadAllText(path).Trim();
            File.Delete(path);
            if (string.Equals(command, "ENTER_PLAY", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorApplication.isPlaying = true;
            }
            else if (string.Equals(command, "EXIT_PLAY", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static string GetCommandPath()
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), CommandFileName));
        }
    }
}
