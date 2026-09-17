using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Prevents the player from falling into the void or escaping the kitchen play area.
    /// Resets player position if Y drops below the floor level.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerVoidGuard : MonoBehaviour
    {
        private static readonly Vector3 SafeSpawnPosition = new Vector3(0f, 0.05f, -1.2f);
        private const float FallThresholdY = -0.15f;
        private CharacterController m_CharacterController;

        private void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (transform.position.y < FallThresholdY)
            {
                Debug.LogWarning($"[PlayerVoidGuard] Player dropped below safe floor level (Y={transform.position.y:F2}). Teleporting back to kitchen!");
                
                if (m_CharacterController != null)
                {
                    m_CharacterController.enabled = false;
                    transform.position = SafeSpawnPosition;
                    m_CharacterController.enabled = true;
                }
                else
                {
                    transform.position = SafeSpawnPosition;
                }

                Physics.SyncTransforms();
            }
        }
    }
}
