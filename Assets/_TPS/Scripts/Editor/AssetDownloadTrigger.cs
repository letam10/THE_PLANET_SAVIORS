using UnityEditor;
using TPS.Editor.Tools;
using System.Threading.Tasks;

namespace TPS.Editor.Scratch
{
    [InitializeOnLoad]
    public static class AssetDownloadTrigger
    {
        static AssetDownloadTrigger()
        {
            AutoTrigger();
        }

        public static async void AutoTrigger()
        {
            // Only run once
            string flag = "Assets/_TPS/Scripts/Editor/.download_triggered";
            if (System.IO.File.Exists(flag)) return;
            System.IO.File.Create(flag).Close();

            UnityEngine.Debug.Log("AssetDownloadTrigger: Background sync started for Space-Kit and Sci-Fi-Sounds...");
            
            // Call the downloader window logic
            var window = EditorWindow.GetWindow<AssetDownloaderWindow>(true, "Syncing Assets...", false);
            await window.DownloadAsync("https://github.com/KenneyNL/Space-Kit/archive/refs/heads/master.zip");
            await window.DownloadAsync("https://github.com/KenneyNL/Sci-Fi-Sounds/archive/refs/heads/master.zip");
            
            UnityEngine.Debug.Log("AssetDownloadTrigger: Sync complete. Check D:\\CODE GAME\\Resources");
            window.Close();
        }
    }
}
