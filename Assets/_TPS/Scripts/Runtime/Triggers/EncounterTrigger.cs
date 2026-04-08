using UnityEngine;
using TPS.Runtime.Core;

namespace TPS.Runtime.Triggers
{
    /// <summary>
    /// When the player enters this trigger volume, loads the specified battle scene
    /// via <see cref="SceneLoader"/>. Requires BoxCollider with IsTrigger = true.
    /// </summary>
    public sealed class EncounterTrigger : MonoBehaviour
    {
        [SerializeField] private string _battleSceneName = "BTL_Standard";
        [SerializeField] private bool _triggerOnce = true;

        private bool _hasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggerOnce && _hasTriggered) return;
            if (!other.CompareTag("Player")) return;
            if (SceneLoader.Instance == null)
            {
                Debug.LogError("EncounterTrigger: SceneLoader.Instance is null.");
                return;
            }

            _hasTriggered = true;
            Debug.Log($"EncounterTrigger: Loading battle scene '{_battleSceneName}'");
            SceneLoader.Instance.StartCoroutine(SceneLoader.Instance.LoadContentSceneAsync(_battleSceneName));
        }
    }
}
