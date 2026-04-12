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
        private string _downloadUrl = "https://example.com/assets.zip";
        private string _targetDirectory = @"D:\CODE GAME\Resources";
        private bool _isDownloading = false;
        private float _progress = 0f;
        private string _statusMessage = "Ready";

        [MenuItem("TPS/Tools/Asset Downloader")]
        public static void ShowWindow()
        {
            GetWindow<AssetDownloaderWindow>("Asset Downloader");
        }

        private void OnGUI()
        {
            GUILayout.Label("Open-Source Asset Downloader", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _downloadUrl = EditorGUILayout.TextField("Asset URL (.zip or .rar)", _downloadUrl);
            _targetDirectory = EditorGUILayout.TextField("Target Directory", _targetDirectory);

            EditorGUILayout.Space();
            
            if (_isDownloading)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, _progress, "Downloading...");
            }
            else
            {
                if (GUILayout.Button("Download & Save"))
                {
                    _ = DownloadAsync();
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label($"Status: {_statusMessage}", EditorStyles.wordWrappedLabel);
        }

        private async Task DownloadAsync()
        {
            _isDownloading = true;
            _progress = 0f;
            _statusMessage = "Initializing download...";

            if (!Directory.Exists(_targetDirectory))
            {
                Directory.CreateDirectory(_targetDirectory);
            }

            string ext = Path.GetExtension(_downloadUrl);
            if (string.IsNullOrEmpty(ext)) ext = ".zip";
            string fileName = $"AssetPack_{System.DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            string filePath = Path.Combine(_targetDirectory, fileName);

            using (UnityWebRequest request = UnityWebRequest.Get(_downloadUrl))
            {
                request.SendWebRequest();

                while (!request.isDone)
                {
                    _progress = request.downloadProgress;
                    _statusMessage = $"Downloading... {(_progress * 100):F1}%";
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
                        _statusMessage = $"Saved to: {filePath}";
                        
                        if (ext.ToLower() == ".zip")
                        {
                            _statusMessage += "\nExtracting .zip file...";
                            Repaint();
                            
                            string extractPath = Path.Combine(_targetDirectory, Path.GetFileNameWithoutExtension(fileName));
                            if (!Directory.Exists(extractPath))
                            {
                                Directory.CreateDirectory(extractPath);
                            }
                            await Task.Run(() => ZipFile.ExtractToDirectory(filePath, extractPath));
                            _statusMessage = $"Success. Extracted to: {extractPath}";
                        }
                        else
                        {
                            _statusMessage += $"\nFile saved. Note: Auto-extraction only supports .zip.";
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _statusMessage = $"File Error: {ex.Message}";
                    }
                }
            }

            _isDownloading = false;
            Repaint();
        }
    }
}
