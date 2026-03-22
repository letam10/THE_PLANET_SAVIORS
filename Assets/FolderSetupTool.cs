using UnityEngine;
using UnityEditor;
using System.IO;

public class FolderSetupTool : MonoBehaviour
{
    [MenuItem("Tools/Setup TPS Folder Structure")]
    public static void SetupFolders()
    {
        string[] folders = new string[]
        {
            "Assets/_TPS/Art/Characters",
            "Assets/_TPS/Art/Environments",
            "Assets/_TPS/Art/Props",
            "Assets/_TPS/Art/Vehicles",
            "Assets/_TPS/Art/Mounts",
            "Assets/_TPS/Art/UI",
            "Assets/_TPS/Art/VFX",
            "Assets/_TPS/Audio/Music",
            "Assets/_TPS/Audio/SFX",
            "Assets/_TPS/Audio/Voice",
            "Assets/_TPS/Audio/Ambient",
            "Assets/_TPS/Materials",
            "Assets/_TPS/Prefabs/Core",
            "Assets/_TPS/Prefabs/Player",
            "Assets/_TPS/Prefabs/NPC",
            "Assets/_TPS/Prefabs/Interactables",
            "Assets/_TPS/Prefabs/Mounts",
            "Assets/_TPS/Prefabs/Vehicles",
            "Assets/_TPS/Prefabs/Combat",
            "Assets/_TPS/Prefabs/UI",
            "Assets/_TPS/Prefabs/VFX",
            "Assets/_TPS/Scenes/Bootstrap",
            "Assets/_TPS/Scenes/Core",
            "Assets/_TPS/Scenes/Menu",
            "Assets/_TPS/Scenes/World",
            "Assets/_TPS/Scenes/Dungeons",
            "Assets/_TPS/Scenes/Battle",
            "Assets/_TPS/Scenes/Test",
            "Assets/_TPS/Scripts/Runtime/Bootstrap",
            "Assets/_TPS/Scripts/Runtime/Core",
            "Assets/_TPS/Scripts/Runtime/World",
            "Assets/_TPS/Scripts/Runtime/Time",
            "Assets/_TPS/Scripts/Runtime/Weather",
            "Assets/_TPS/Scripts/Runtime/Exploration",
            "Assets/_TPS/Scripts/Runtime/Traversal",
            "Assets/_TPS/Scripts/Runtime/Interaction",
            "Assets/_TPS/Scripts/Runtime/NPC",
            "Assets/_TPS/Scripts/Runtime/Dialogue",
            "Assets/_TPS/Scripts/Runtime/Quest",
            "Assets/_TPS/Scripts/Runtime/Combat",
            "Assets/_TPS/Scripts/Runtime/SaveLoad",
            "Assets/_TPS/Scripts/Runtime/UI",
            "Assets/_TPS/Scripts/Runtime/Audio",
            "Assets/_TPS/Scripts/Runtime/Tools",
            "Assets/_TPS/Scripts/Editor",
            "Assets/_TPS/Data/Config",
            "Assets/_TPS/Data/Characters",
            "Assets/_TPS/Data/Enemies",
            "Assets/_TPS/Data/Skills",
            "Assets/_TPS/Data/Weapons",
            "Assets/_TPS/Data/Items",
            "Assets/_TPS/Data/Zones",
            "Assets/_TPS/Data/Dungeons",
            "Assets/_TPS/Data/Weather",
            "Assets/_TPS/Data/Dialogue",
            "Assets/_TPS/Data/Quests",
            "Assets/_TPS/Data/Schedules",
            "Assets/_TPS/Data/Events",
            "Assets/_TPS/Data/MiniGames",
            "Assets/_TPS/UI",
            "Assets/_TPS/Shaders",
            "Assets/_TPS/Localization",
            "Assets/_TPS/Docs"
        };

        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("TPS Folder Structure created successfully!");
    }
}