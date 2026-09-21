using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CocinaBoliviana
{
    /// <summary>
    /// Devuelve una herramienta a su sitio al soltarla, para que no se pierda por el suelo.
    ///
    /// Está pensado para el cuchillo, pero sirve para cualquier objeto agarrable que deba
    /// tener un lugar fijo.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ToolHome : MonoBehaviour
    {
        [Tooltip("Dónde vuelve al soltarlo. Si se deja vacío, usa la posición en la que " +
                 "arrancó la escena.")]
        [SerializeField] private Transform sitio;

        [Tooltip("Desactívalo si en algún momento quieres poder dejar la herramienta " +
                 "en cualquier parte.")]
        [SerializeField] private bool volverAlSoltar = true;

        private XRGrabInteractable grab;
        private Rigidbody rb;
        private Vector3 posicionInicial;
        private Quaternion rotacionInicial;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            posicionInicial = transform.position;
            rotacionInicial = transform.rotation;

            if (grab != null) grab.selectExited.AddListener(OnSoltado);
        }

        private void OnDestroy()
        {
            if (grab != null) grab.selectExited.RemoveListener(OnSoltado);
        }

        private void OnSoltado(SelectExitEventArgs args)
        {
            if (volverAlSoltar) Volver();
        }

        /// <summary>
        /// Lo deja quieto en su sitio. Queda kinemático a propósito: si no, rodaría o se
        /// caería de la mesa, que es justo lo que se quiere evitar.
        /// </summary>
        public void Volver()
        {
            Vector3 destino = (sitio != null) ? sitio.position : posicionInicial;
            Quaternion giro = (sitio != null) ? sitio.rotation : rotacionInicial;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            transform.SetPositionAndRotation(destino, giro);
        }
    }
}
