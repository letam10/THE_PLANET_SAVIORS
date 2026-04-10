using UnityEngine;

namespace TPS.Runtime.World
{
    public enum EnvironmentGeneratedCategory
    {
        Root = 0,
        Blockout = 1,
        Building = 2,
        Prop = 3,
        Vegetation = 4,
        Ambient = 5,
        Debug = 6
    }

    public sealed class EnvironmentGeneratedMarker : MonoBehaviour
    {
        [SerializeField] private string _generationId = "aster_harbor_environment";
        [SerializeField] private EnvironmentGeneratedCategory _category = EnvironmentGeneratedCategory.Root;
        [SerializeField] private bool _replaceSafe = true;
        [SerializeField] private bool _preserveManualChildren = true;
        [SerializeField] private string _notes = "Generated environment scaffold. Replace visuals by adding custom children and preserving this slot object.";

        public string GenerationId => _generationId;
        public EnvironmentGeneratedCategory Category => _category;
        public bool ReplaceSafe => _replaceSafe;
        public bool PreserveManualChildren => _preserveManualChildren;
        public string Notes => _notes;
    }
}
