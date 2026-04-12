using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPS.Editor
{
    [CreateAssetMenu(fileName = "ExternalAssetSourceManifest", menuName = "TPS/External Assets/Source Manifest")]
    internal sealed class ExternalAssetSourceManifest : ScriptableObject
    {
        [SerializeField] private List<ExternalAssetSourceEntry> _entries = new List<ExternalAssetSourceEntry>();

        public List<ExternalAssetSourceEntry> Entries => _entries;
    }

    [Serializable]
    internal sealed class ExternalAssetSourceEntry
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private string _id = "asset_id";
        [SerializeField] private string _downloadUrl = string.Empty;
        [SerializeField] private string _sourcePageUrl = string.Empty;
        [SerializeField] private string _destinationAssetPath = "Assets/_TPS/Art/External/placeholder.asset";
        [SerializeField] private string _license = "CC-BY 4.0";
        [SerializeField] private string _author = "Unknown";
        [SerializeField] [TextArea] private string _attribution = string.Empty;
        [SerializeField] [TextArea] private string _notes = string.Empty;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string DownloadUrl
        {
            get => _downloadUrl;
            set => _downloadUrl = value;
        }

        public string SourcePageUrl
        {
            get => _sourcePageUrl;
            set => _sourcePageUrl = value;
        }

        public string DestinationAssetPath
        {
            get => _destinationAssetPath;
            set => _destinationAssetPath = value;
        }

        public string License
        {
            get => _license;
            set => _license = value;
        }

        public string Author
        {
            get => _author;
            set => _author = value;
        }

        public string Attribution
        {
            get => _attribution;
            set => _attribution = value;
        }

        public string Notes
        {
            get => _notes;
            set => _notes = value;
        }
    }
}
