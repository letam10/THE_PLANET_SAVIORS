using UnityEngine;

namespace TPS.Runtime.Spawn
{
    /// <summary>
    /// Marks a position in a scene where the player can be spawned.
    /// Place on any GameObject (e.g. MK_PlayerSpawn, MK_PartySlot_01).
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnId = "Default";

        public string SpawnId => _spawnId;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);

            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.6f,
                $"Spawn: {_spawnId}");
        }
#endif
    }
}
