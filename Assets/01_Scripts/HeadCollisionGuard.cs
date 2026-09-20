using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Keeps the player's head from visually clipping through kitchen geometry.
    /// HMD tracking (a real headset, or the XR Interaction Simulator's WASD device-move) sets the
    /// camera's local offset directly and never goes through the CharacterController, so normal
    /// locomotion collision never catches it. This pushes the whole rig away from any overlap
    /// detected at the head instead.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class HeadCollisionGuard : MonoBehaviour
    {
        [SerializeField] private float radius = 0.18f;

        private Transform m_RigRoot;
        private SphereCollider m_Collider;
        private CharacterController m_RigController;
        private readonly Collider[] m_Overlaps = new Collider[8];

        public void Initialize(Transform rigRoot)
        {
            m_RigRoot = rigRoot;
            m_RigController = rigRoot != null ? rigRoot.GetComponent<CharacterController>() : null;
        }

        private void Awake()
        {
            m_Collider = GetComponent<SphereCollider>();
            m_Collider.isTrigger = true;
            m_Collider.center = Vector3.zero;
            m_Collider.radius = radius;
        }

        private void LateUpdate()
        {
            if (m_RigRoot == null) return;

            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, m_Overlaps, ~0, QueryTriggerInteraction.Ignore);

            Vector3 totalPush = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Collider other = m_Overlaps[i];
                if (other == null || other == m_Collider || other.transform.IsChildOf(m_RigRoot)) continue;

                if (Physics.ComputePenetration(
                        m_Collider, transform.position, transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 direction, out float distance))
                {
                    Vector3 push = direction * distance;
                    push.y = 0f;
                    totalPush += push;
                }
            }

            if (totalPush.sqrMagnitude > 0f)
            {
                MoveRig(totalPush);
            }
        }

        private void MoveRig(Vector3 delta)
        {
            if (m_RigController != null)
            {
                m_RigController.enabled = false;
                m_RigRoot.position += delta;
                m_RigController.enabled = true;
            }
            else
            {
                m_RigRoot.position += delta;
            }
        }
    }
}
