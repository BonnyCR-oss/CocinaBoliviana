using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Mostrador de entrega. Detecta un plato terminado dejado encima y se lo pasa al
    /// <see cref="OrderManager"/> para ver si alguien lo había pedido.
    ///
    /// Detecta por OverlapBox y no por trigger, como el resto de estaciones: un trigger lo
    /// bastante grande para detectar acaba envolviendo al plato y robándole el rayo del control.
    /// </summary>
    public class DeliveryCounter : MonoBehaviour
    {
        [Header("Zona de Entrega")]
        [Tooltip("Caja (en METROS de mundo) sobre el mostrador donde se detecta el plato.")]
        [SerializeField] private Vector3 zonaDeteccion = new Vector3(0.7f, 0.4f, 0.7f);

        [Tooltip("Altura del centro de esa caja sobre el mostrador, en metros.")]
        [SerializeField] private float zonaAltura = 0.6f;

        [Header("Feedback")]
        [SerializeField] private AudioClip sonidoAcierto;
        [SerializeField] private AudioClip sonidoRechazo;

        [Tooltip("La campana de servicio. Da un saltito al entregar bien.")]
        [SerializeField] private Transform campana;

        private AudioSource audioSource;
        private float finRebote;
        private Vector3 campanaEnReposo;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            if (campana != null) campanaEnReposo = campana.localPosition;
        }

        private void FixedUpdate()
        {
            Vector3 centro = transform.position + Vector3.up * zonaAltura;
            Collider[] dentro = Physics.OverlapBox(centro, zonaDeteccion * 0.5f, transform.rotation,
                                                   ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in dentro)
            {
                var plato = col.GetComponentInParent<ServedDish>();
                if (plato == null || plato.Plato == null) continue;

                // Mientras lo sostenga no se le arranca de la mano: se entrega al soltarlo.
                var grab = plato.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                if (grab != null && grab.isSelected) continue;

                Procesar(plato);
                return;
            }
        }

        private void Procesar(ServedDish plato)
        {
            var manager = OrderManager.Instancia;
            if (manager == null)
            {
                Debug.LogWarning("[DeliveryCounter] No hay OrderManager en la escena; no se puede entregar.");
                return;
            }

            if (manager.Entregar(plato.Plato))
            {
                Sonar(sonidoAcierto);
                finRebote = Time.time + 0.35f;
                Destroy(plato.gameObject);
            }
            else
            {
                // No se destruye: el plato está bien hecho, simplemente nadie lo pidió.
                // Que se quede permite guardarlo por si entra un pedido que lo pida.
                Sonar(sonidoRechazo);
                Debug.Log($"[DeliveryCounter] {plato.Plato.nombre} rechazado: ningún pedido lo espera.");
            }
        }

        private void Sonar(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, 0.9f);
        }

        private void Update()
        {
            if (campana == null) return;

            // Saltito de la campana al acertar, como feedback sin sonido.
            // Se parte de su posicion original: poner la Y a cero la hundiria en la mesa.
            float alto = (Time.time < finRebote) ? Mathf.Abs(Mathf.Sin(Time.time * 25f)) * 0.03f : 0f;
            campana.localPosition = campanaEnReposo + Vector3.up * alto;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * zonaAltura, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, zonaDeteccion);
        }
    }
}
