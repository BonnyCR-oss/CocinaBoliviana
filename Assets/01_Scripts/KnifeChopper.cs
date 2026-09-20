using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Collider))]
    public class KnifeChopper : MonoBehaviour
    {
        [Header("Chop Settings")]
        [SerializeField] private float minHitCooldown = 0.2f;
        [SerializeField] private float hapticIntensity = 0.6f;
        [SerializeField] private float hapticDuration = 0.1f;

        private XRGrabInteractable grabInteractable;
        private float lastHitTime = -10f;

        private void Awake()
        {
            grabInteractable = GetComponentInParent<XRGrabInteractable>();
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other) => CheckAndChop(other);
        private void OnTriggerStay(Collider other) => CheckAndChop(other);
        private void OnCollisionEnter(Collision collision) => CheckAndChop(collision.collider);
        private void OnCollisionStay(Collision collision) => CheckAndChop(collision.collider);

        private void CheckAndChop(Collider other)
        {
            if (other == null) return;
            if (Time.time - lastHitTime < minHitCooldown) return;

            // 1. Find cutting board directly or through ingredient
            CuttingBoard board = other.GetComponentInParent<CuttingBoard>();
            if (board == null)
            {
                IngredientItem ingredient = other.GetComponentInParent<IngredientItem>();
                if (ingredient != null)
                {
                    board = ingredient.CurrentBoard ?? ingredient.GetComponentInParent<CuttingBoard>();
                    if (board == null)
                    {
                        // Proximity check in case of loose unparented ingredient sitting on board
                        var allBoards = Object.FindObjectsByType<CuttingBoard>();
                        foreach (var b in allBoards)
                        {
                            if (Vector3.Distance(b.transform.position, ingredient.transform.position) < 0.45f)
                            {
                                board = b;
                                break;
                            }
                        }
                    }
                }
            }

            if (board != null && board.HasIngredient && !board.CurrentIngredient.IsCut)
            {
                lastHitTime = Time.time;
                board.RegisterKnifeHit();
                SendHapticFeedback();
            }
        }

        private void SendHapticFeedback()
        {
            if (grabInteractable != null && grabInteractable.isSelected)
            {
                foreach (var interactor in grabInteractable.interactorsSelecting)
                {
                    if (interactor is XRBaseInputInteractor inputInteractor)
                    {
                        inputInteractor.SendHapticImpulse(hapticIntensity, hapticDuration);
                    }
                }
            }
        }
    }
}
