using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace TPS.Editor
{
    internal static class ExternalAssetImporterTool
    {
        private const string ManifestPath = "Assets/_TPS/Data/Environment/ExternalAssetSourceManifest.asset";
        private const string SourcesDocPath = "Assets/_TPS/Docs/ExternalAssets/SOURCES.md";
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".wav",
            ".ogg",
            ".png",
            ".jpg",
            ".jpeg",
            ".fbx",
            ".obj",
            ".glb"
        };

        [MenuItem("Tools/TPS/Assets/External/Create Or Refresh Source Manifest")]
        private static void CreateOrRefreshManifestMenu()
        {
            ExternalAssetSourceManifest manifest = CreateOrRefreshManifest();
            Selection.activeObject = manifest;
            EditorGUIUtility.PingObject(manifest);
            Debug.Log($"[TPSExternalAssets] Manifest ready at {ManifestPath}");
        }

        [MenuItem("Tools/TPS/Assets/External/Download Enabled Sources (curl/wget)")]
        private static void DownloadEnabledSourcesMenu()
        {
            ExternalAssetSourceManifest manifest = CreateOrRefreshManifest();
            if (manifest == null)
            {
                Debug.LogError("[TPSExternalAssets] Manifest missing.");
                return;
            }

            EnsureFolders();
            int success = 0;
            int failed = 0;

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                ExternalAssetSourceEntry entry = manifest.Entries[i];
                if (entry == null || !entry.Enabled)
                {
                    continue;
                }

                if (!ValidateEntry(entry, out string validationError))
                {
                    failed++;
                    Debug.LogError($"[TPSExternalAssets] {entry?.Id ?? "(null)"} skipped: {validationError}");
                    continue;
                }

                string destinationPath = ToAbsolutePath(entry.DestinationAssetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? Application.dataPath);

                bool downloaded = TryDownloadWithCurl(entry.DownloadUrl, destinationPath);
                if (!downloaded)
                {
                    downloaded = TryDownloadWithWget(entry.DownloadUrl, destinationPath);
                }

                if (!downloaded)
                {
                    failed++;
                    Debug.LogError($"[TPSExternalAssets] Download failed: {entry.Id} -> {entry.DownloadUrl}");
                    continue;
                }

                success++;
                Debug.Log($"[TPSExternalAssets] Downloaded: {entry.Id} -> {entry.DestinationAssetPath}");
            }

            AssetDatabase.Refresh();
            WriteSourcesDoc(manifest);
            AssetDatabase.Refresh();
            Debug.Log($"[TPSExternalAssets] Download complete. Success={success}, Failed={failed}");
        }

        [MenuItem("Tools/TPS/Assets/External/Write SOURCES.md")]
        private static void WriteSourcesDocMenu()
        {
            ExternalAssetSourceManifest manifest = CreateOrRefreshManifest();
            WriteSourcesDoc(manifest);
            AssetDatabase.Refresh();
            Debug.Log($"[TPSExternalAssets] Updated {SourcesDocPath}");
        }

        internal static ExternalAssetSourceManifest CreateOrRefreshManifest()
        {
            EnsureFolders();
            ExternalAssetSourceManifest manifest = AssetDatabase.LoadAssetAtPath<ExternalAssetSourceManifest>(ManifestPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<ExternalAssetSourceManifest>();
                AssetDatabase.CreateAsset(manifest, ManifestPath);
            }

            if (manifest.Entries.Count == 0)
            {
                PopulateDefaultEntries(manifest);
            }

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            return manifest;
        }

        private static void EnsureFolders()
        {
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Data/Environment");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Art/External");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Art/External/Models");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Art/External/Textures");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Audio/External");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Audio/External/SFX");
            Phase1InstallerShared.EnsureFolder("Assets/_TPS/Docs/ExternalAssets");
        }

        private static void PopulateDefaultEntries(ExternalAssetSourceManifest manifest)
        {
            manifest.Entries.Clear();
            manifest.Entries.Add(new ExternalAssetSourceEntry
            {
                Enabled = true,
                Id = "audio_example_ogg",
                DownloadUrl = "https://upload.wikimedia.org/wikipedia/commons/c/c8/Example.ogg",
                SourcePageUrl = "https://commons.wikimedia.org/wiki/File:Example.ogg",
                DestinationAssetPath = "Assets/_TPS/Audio/External/SFX/SFX_Example.ogg",
                License = "CC BY-SA 3.0",
                Author = "Wikimedia contributors",
                Attribution = "Example.ogg from Wikimedia Commons (CC BY-SA 3.0).",
                Notes = "Reference placeholder dialogue/click sound."
            });
            manifest.Entries.Add(new ExternalAssetSourceEntry
            {
                Enabled = true,
                Id = "audio_silence_wav",
                DownloadUrl = "https://raw.githubusercontent.com/anars/blank-audio/master/1-second-of-silence.wav",
                SourcePageUrl = "https://github.com/anars/blank-audio",
                DestinationAssetPath = "Assets/_TPS/Audio/External/SFX/SFX_Silence_1s.wav",
                License = "MIT",
                Author = "anars/blank-audio contributors",
                Attribution = "1-second-of-silence.wav from anars/blank-audio (MIT).",
                Notes = "Utility test clip for audio pipeline validation."
            });
            manifest.Entries.Add(new ExternalAssetSourceEntry
            {
                Enabled = true,
                Id = "texture_png_demo",
                DownloadUrl = "https://upload.wikimedia.org/wikipedia/commons/4/47/PNG_transparency_demonstration_1.png",
                SourcePageUrl = "https://commons.wikimedia.org/wiki/File:PNG_transparency_demonstration_1.png",
                DestinationAssetPath = "Assets/_TPS/Art/External/Textures/TEX_Demo_Png.png",
                License = "CC BY 3.0",
                Author = "Wikimedia contributors",
                Attribution = "PNG transparency demo image from Wikimedia Commons (CC BY 3.0).",
                Notes = "Placeholder UI/environment texture."
            });
            manifest.Entries.Add(new ExternalAssetSourceEntry
            {
                Enabled = true,
                Id = "texture_jpg_demo",
                DownloadUrl = "https://upload.wikimedia.org/wikipedia/commons/a/a9/Example.jpg",
                SourcePageUrl = "https://commons.wikimedia.org/wiki/File:Example.jpg",
                DestinationAssetPath = "Assets/_TPS/Art/External/Textures/TEX_Demo_Jpg.jpg",
                License = "CC BY-SA 3.0",
                Author = "Wikimedia contributors",
                Attribution = "Example.jpg from Wikimedia Commons (CC BY-SA 3.0).",
                Notes = "Placeholder texture for signage/material testing."
            });
            manifest.Entries.Add(new ExternalAssetSourceEntry
            {
                Enabled = true,
                Id = "model_obj_cube",
                DownloadUrl = "https://raw.githubusercontent.com/alecjacobson/common-3d-test-models/master/data/cube.obj",
                SourcePageUrl = "https://github.com/alecjacobson/common-3d-test-models",
                DestinationAssetPath = "Assets/_TPS/Art/External/Models/MDL_TestCube.obj",
                License = "MIT",
                Author = "alecjacobson/common-3d-test-models contributors",
                Attribution = "cube.obj from common-3d-test-models (MIT).",
                Notes = "Replace-safe geometry placeholder."
            });
            manifest.Entries.Add(new ExternalAssetSourceEntry
            {
                Enabled = false,
                Id = "model_glb_duck_optional",
                DownloadUrl = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/main/Models/Duck/glTF-Binary/Duck.glb",
                SourcePageUrl = "https://github.com/KhronosGroup/glTF-Sample-Assets",
                DestinationAssetPath = "Assets/_TPS/Art/External/Models/MDL_Duck.glb",
                License = "CC BY 4.0",
                Author = "Khronos glTF Sample Assets contributors",
                Attribution = "Duck.glb from Khronos glTF Sample Assets (CC BY 4.0).",
                Notes = "Optional. Enable only if project has a working .glb importer."
            });
        }

        private static bool ValidateEntry(ExternalAssetSourceEntry entry, out string error)
        {
            error = string.Empty;
            if (entry == null)
            {
                error = "Entry is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                error = "Missing entry id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.DownloadUrl))
            {
                error = "Missing download URL.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.DestinationAssetPath) || !entry.DestinationAssetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                error = "Destination path must be inside Assets/.";
                return false;
            }

            string extension = Path.GetExtension(entry.DestinationAssetPath);
            if (!AllowedExtensions.Contains(extension))
            {
                error = $"Extension '{extension}' is not whitelisted.";
                return false;
            }

            return true;
        }

        private static bool TryDownloadWithCurl(string url, string destinationPath)
        {
            string arguments = $"--fail --location --retry 2 --connect-timeout 20 --output \"{destinationPath}\" \"{url}\"";
            int exitCode = RunProcess("curl", arguments, out string stdout, out string stderr);
            if (exitCode == 0)
            {
                return true;
            }

            Debug.LogWarning($"[TPSExternalAssets] curl failed ({exitCode}). {stderr}\n{stdout}");
            return false;
        }

        private static bool TryDownloadWithWget(string url, string destinationPath)
        {
            string arguments = $"-O \"{destinationPath}\" \"{url}\"";
            int exitCode = RunProcess("wget", arguments, out string stdout, out string stderr);
            if (exitCode == 0)
            {
                return true;
            }

            Debug.LogWarning($"[TPSExternalAssets] wget failed ({exitCode}). {stderr}\n{stdout}");
            return false;
        }

        private static int RunProcess(string fileName, string arguments, out string stdout, out string stderr)
        {
            stdout = string.Empty;
            stderr = string.Empty;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    stderr = "Process failed to start.";
                    return -1;
                }

                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                stderr = ex.Message;
                return -1;
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static void WriteSourcesDoc(ExternalAssetSourceManifest manifest)
        {
            EnsureFolders();
            var builder = new StringBuilder();
            builder.AppendLine("# External Asset Sources");
            builder.AppendLine();
            builder.AppendLine("This file is generated by `Tools/TPS/Assets/External/Write SOURCES.md`.");
            builder.AppendLine("Only replace-safe placeholder assets should be downloaded into `Assets/_TPS/Art/External` and `Assets/_TPS/Audio/External`.");
            builder.AppendLine();
            builder.AppendLine("| ID | Destination | License | Author | Download URL | Source Page | Attribution |");
            builder.AppendLine("|---|---|---|---|---|---|---|");

            if (manifest != null)
            {
                for (int i = 0; i < manifest.Entries.Count; i++)
                {
                    ExternalAssetSourceEntry entry = manifest.Entries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    builder.AppendLine($"| {EscapePipe(entry.Id)} | {EscapePipe(entry.DestinationAssetPath)} | {EscapePipe(entry.License)} | {EscapePipe(entry.Author)} | {EscapePipe(entry.DownloadUrl)} | {EscapePipe(entry.SourcePageUrl)} | {EscapePipe(entry.Attribution)} |");
                }
            }

            string absolutePath = ToAbsolutePath(SourcesDocPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? Application.dataPath);
            File.WriteAllText(absolutePath, builder.ToString());
        }

        private static string EscapePipe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("|", "\\|");
        }
    }
}
