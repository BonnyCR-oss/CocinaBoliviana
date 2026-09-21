using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
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

        [Tooltip("Velocidad mínima de la hoja (m/s) para que un contacto cuente como golpe. " +
                 "Sin esto, un cuchillo apoyado sobre la tabla corta solo vía OnTriggerStay.")]
        [SerializeField] private float minChopSpeed = 0.25f;

        private XRGrabInteractable grabInteractable;
        private float lastHitTime = -10f;
        private Vector3 lastPosition;
        private float currentSpeed;

        // Última tabla que la hoja tocó. Se recuerda aunque el golpe no cuente, para que
        // el gatillo sepa sobre qué cortar sin tener que volver a resolverla.
        private CuttingBoard boardInContact;

        private void Awake()
        {
            grabInteractable = GetComponentInParent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                Debug.LogWarning($"[KnifeChopper] {name} no encuentra un XRGrabInteractable en sus padres; no podrá cortar.");
            }

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (grabInteractable != null)
            {
                grabInteractable.activated.AddListener(OnActivated);
            }

            lastPosition = transform.position;
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.activated.RemoveListener(OnActivated);
            }
        }

        /// <summary>
        /// Gatillo con el cuchillo en la mano: corta sin necesidad de moverlo. En VR el gesto
        /// natural es bajar la mano, pero con el simulador de teclado el controlador se mueve
        /// demasiado lento para superar 'minChopSpeed'.
        /// </summary>
        private void OnActivated(ActivateEventArgs args)
        {
            if (boardInContact == null) return;
            if (Time.time - lastHitTime < minHitCooldown) return;
            TryChop(boardInContact);
        }

        private void FixedUpdate()
        {
            currentSpeed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            lastPosition = transform.position;
        }

        private void OnTriggerEnter(Collider other) => CheckAndChop(other);
        private void OnTriggerStay(Collider other) => CheckAndChop(other);
        private void OnCollisionEnter(Collision collision) => CheckAndChop(collision.collider);
        private void OnCollisionStay(Collision collision) => CheckAndChop(collision.collider);

        private void OnTriggerExit(Collider other)
        {
            if (other != null && ResolveBoard(other) == boardInContact)
            {
                boardInContact = null;
            }
        }

        private void CheckAndChop(Collider other)
        {
            if (other == null) return;

            CuttingBoard board = ResolveBoard(other);
            if (board == null) return;

            // Se recuerda siempre, aunque este contacto no cuente como golpe: es lo que
            // permite que el gatillo corte sin mover el cuchillo.
            boardInContact = board;

            if (Time.time - lastHitTime < minHitCooldown) return;

            // El cuchillo tiene que estar agarrado Y moviéndose. OnTriggerStay dispara cada
            // frame mientras haya contacto, así que un cuchillo dejado sobre la tabla
            // acumulaba los 3 golpes solo, sin que el jugador hiciera nada.
            if (grabInteractable == null || !grabInteractable.isSelected) return;
            if (currentSpeed < minChopSpeed) return;

            TryChop(board);
        }

        private void TryChop(CuttingBoard board)
        {
            if (board == null || !board.HasIngredient || board.CurrentIngredient.IsCut) return;

            lastHitTime = Time.time;
            board.RegisterKnifeHit();
            SendHapticFeedback();
        }

        /// <summary>
        /// La hoja puede tocar la tabla directamente, el ingrediente encima de ella, o un
        /// ingrediente suelto que quedó apoyado sin emparentarse. Se cubren los tres casos.
        /// </summary>
        private CuttingBoard ResolveBoard(Collider other)
        {
            CuttingBoard board = other.GetComponentInParent<CuttingBoard>();
            if (board != null) return board;

            IngredientItem ingredient = other.GetComponentInParent<IngredientItem>();
            if (ingredient == null) return null;

            board = ingredient.CurrentBoard ?? ingredient.GetComponentInParent<CuttingBoard>();
            if (board != null) return board;

            foreach (var b in Object.FindObjectsByType<CuttingBoard>())
            {
                if (Vector3.Distance(b.transform.position, ingredient.transform.position) < 0.45f)
                {
                    return b;
                }
            }

            return null;
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
