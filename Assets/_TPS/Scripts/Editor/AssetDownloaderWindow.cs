using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace TPS.Editor.Tools
{
    public class AssetDownloaderWindow : EditorWindow
    {
        private struct AssetPreset
        {
            public string Name;
            public string URL;
            public AssetPreset(string name, string url) { Name = name; URL = url; }
        }

        private static readonly List<AssetPreset> Presets = new List<AssetPreset>
        {
            new AssetPreset("Space Kit (3D Models)", "https://kenney.nl/media/pages/assets/space-kit/cceeafbd0c-1677698978/kenney_space-kit.zip"),
            new AssetPreset("Sci-Fi Sounds (Audio)", "https://kenney.nl/media/pages/assets/sci-fi-sounds/e3af5f7ed7-1677589334/kenney_sci-fi-sounds.zip"),
            new AssetPreset("UI Audio (Audio)", "https://kenney.nl/media/pages/assets/ui-audio/e8f7a6a7b2-1677589334/kenney_ui-audio.zip"),
            new AssetPreset("Nature Kit (3D Models)", "https://kenney.nl/media/pages/assets/nature-kit/a2b2c3d4e5-1677589334/kenney_nature-kit.zip")
        };

        private string _downloadUrl = "";
        private string _targetDirectory = @"D:\CODE GAME\Resources";
        private bool _isDownloading = false;
        private float _progress = 0f;
        private string _statusMessage = "Ready to build the world.";
        private int _selectedPreset = 0;

        [MenuItem("TPS/Tools/Asset Downloader")]
        public static void ShowWindow()
        {
            GetWindow<AssetDownloaderWindow>("TPS Asset Library");
        }

        [MenuItem("TPS/Tools/Sync All Essential Kits")]
        public static void SyncAllKits()
        {
            var window = GetWindow<AssetDownloaderWindow>(true, "Syncing...", false);
            _ = window.DownloadAllSequence();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(_downloadUrl) && Presets.Count > 0)
            {
                _downloadUrl = Presets[0].URL;
            }
        }

        private void OnGUI()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 18;
            headerStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);

            EditorGUILayout.Space(10);
            GUILayout.Label("TPS SYSTEMIC ASSET LIBRARY", headerStyle);
            GUILayout.Label("Acquire high-quality CC0 assets directly into your production cache.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            _targetDirectory = EditorGUILayout.TextField("Local Resource Cache", _targetDirectory);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Preset Library", EditorStyles.boldLabel);
            
            string[] presetNames = new string[Presets.Count];
            for (int i = 0; i < Presets.Count; i++) presetNames[i] = Presets[i].Name;
            
            int newPreset = EditorGUILayout.Popup("Select Pack", _selectedPreset, presetNames);
            if (newPreset != _selectedPreset)
            {
                _selectedPreset = newPreset;
                _downloadUrl = Presets[_selectedPreset].URL;
            }
            
            _downloadUrl = EditorGUILayout.TextField("URL Override", _downloadUrl);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
            
            if (_isDownloading)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 25);
                EditorGUI.ProgressBar(rect, _progress, _statusMessage);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Download & Extract Selected", GUILayout.Height(30)))
                {
                    _ = DownloadAsync(_downloadUrl);
                }
                
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                if (GUILayout.Button("Flash Sync All Kits", GUILayout.Width(130), GUILayout.Height(30)))
                {
                    _ = DownloadAllSequence();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);
            EditorStyles.helpBox.fontSize = 11;
            EditorGUILayout.HelpBox($"Status: {_statusMessage}", MessageType.Info);
        }

        public async Task DownloadAllSequence()
        {
            foreach (var preset in Presets)
            {
                await DownloadAsync(preset.URL);
                await Task.Delay(500); // Cooldown
            }
            _statusMessage = "All Systemic Assets Synced Successfully.";
        }

        public async Task DownloadAsync(string url)
        {
            _isDownloading = true;
            _progress = 0f;
            _statusMessage = $"Preparing {Path.GetFileNameWithoutExtension(url)}...";

            if (!Directory.Exists(_targetDirectory))
            {
                Directory.CreateDirectory(_targetDirectory);
            }

            string ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext) || ext.Contains("?")) ext = ".zip";
            string fileName = $"{Path.GetFileNameWithoutExtension(url)}_{System.DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            string filePath = Path.Combine(_targetDirectory, fileName);

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SendWebRequest();

                while (!request.isDone)
                {
                    _progress = request.downloadProgress;
                    _statusMessage = $"Downloading {Path.GetFileNameWithoutExtension(url)}... {(_progress * 100):F0}%";
                    Repaint();
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    _statusMessage = $"Error: {request.error}";
                }
                else
                {
                    try
                    {
                        File.WriteAllBytes(filePath, request.downloadHandler.data);
                        _statusMessage = "Exctracting content...";
                        Repaint();
                        
                        string extractFolderName = Path.GetFileNameWithoutExtension(url).Replace("-master", "");
                        string extractPath = Path.Combine(_targetDirectory, extractFolderName);
                        
                        if (Directory.Exists(extractPath))
                        {
                            Directory.Delete(extractPath, true);
                        }
                        Directory.CreateDirectory(extractPath);
                        
                        await Task.Run(() => ZipFile.ExtractToDirectory(filePath, extractPath));
                        File.Delete(filePath); // Cleanup zip
                        
                        _statusMessage = $"Synced: {extractFolderName}";
                    }
                    catch (System.Exception ex)
                    {
                        _statusMessage = $"IO Error: {ex.Message}";
                    }
                }
            }

            _isDownloading = false;
            Repaint();
        }
    }
}
