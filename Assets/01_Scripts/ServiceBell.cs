using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CocinaBoliviana
{
    /// <summary>
    /// Campana de servicio sobre el mostrador de entrega.
    /// Puede ser golpeada o pulsada por el jugador (mano VR, rayo o contacto físico)
    /// o activada automáticamente al entregar un plato terminado.
    /// Produce feedback háptico/visual: animación elástica de rebote y sonido metálico de timbre.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ServiceBell : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Clip del timbre. Si está vacío, se sintetiza un campanazo limpio por código.")]
        [SerializeField] private AudioClip timbreClip;
        [Range(0f, 1f)]
        [SerializeField] private float volumen = 0.9f;

        [Header("Animación de Rebote")]
        [SerializeField] private float alturaRebote = 0.025f;
        [SerializeField] private float duracionRebote = 0.35f;

        private AudioSource audioSource;
        private Vector3 posicionReposo;
        private Vector3 escalaReposo;
        private float tiempoFinRebote;
        private float ultimoToque;
        private const float CooldownToque = 0.12f;

        private XRSimpleInteractable interactable;

        private void Awake()
        {
            posicionReposo = transform.localPosition;
            escalaReposo = transform.localScale;

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 10f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            if (timbreClip == null)
            {
                timbreClip = CrearClipCampana();
            }

            // Permitir interacción con mandos VR (Rayo y Poke)
            interactable = GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                interactable = gameObject.AddComponent<XRSimpleInteractable>();
            }

            interactable.selectEntered.AddListener(_ => Ring());
            interactable.activated.AddListener(_ => Ring());
        }

        private void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveAllListeners();
                interactable.activated.RemoveAllListeners();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Responder al contacto con manos o herramientas del jugador
            if (Time.time - ultimoToque < CooldownToque) return;
            Ring();
        }

        /// <summary>
        /// Hace sonar la campana con animación de rebote y sonido metálico.
        /// </summary>
        public void Ring()
        {
            if (Time.time - ultimoToque < CooldownToque) return;
            ultimoToque = Time.time;
            tiempoFinRebote = Time.time + duracionRebote;

            if (audioSource != null && timbreClip != null)
            {
                audioSource.pitch = Random.Range(0.97f, 1.03f);
                audioSource.PlayOneShot(timbreClip, volumen);
            }
        }

        private void Update()
        {
            if (Time.time >= tiempoFinRebote)
            {
                transform.localPosition = posicionReposo;
                transform.localScale = escalaReposo;
                return;
            }

            float t = 1f - ((tiempoFinRebote - Time.time) / duracionRebote); // 0 a 1
            float oscilacion = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t);

            // Rebote vertical y compresión elástica (squash & stretch)
            transform.localPosition = posicionReposo + Vector3.up * (Mathf.Abs(oscilacion) * alturaRebote);
            float deformacion = oscilacion * 0.15f;
            transform.localScale = new Vector3(
                escalaReposo.x * (1f + deformacion),
                escalaReposo.y * (1f - deformacion),
                escalaReposo.z * (1f + deformacion)
            );
        }

        /// <summary>
        /// Sintetiza un armónico de campana metálica brillante (2093 Hz - Do7) con decaimiento natural
        /// para garantizar un timbre rico si no hay archivo externo de audio.
        /// </summary>
        private static AudioClip CrearClipCampana()
        {
            int frecuenciaMuestreo = 44100;
            float duracion = 0.8f;
            int totalMuestras = (int)(frecuenciaMuestreo * duracion);
            float[] muestras = new float[totalMuestras];

            float f0 = 2093f; // Tono fundamental alto de campana de mostrador
            for (int i = 0; i < totalMuestras; i++)
            {
                float t = (float)i / frecuenciaMuestreo;
                float envolvente = Mathf.Exp(-t * 6f); // Decaimiento exponencial

                // Combinación de armónicos metálicos no perfectamente enteros (característica de metal percutido)
                float tono = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.5f
                           + Mathf.Sin(2f * Mathf.PI * f0 * 2.76f * t) * 0.25f
                           + Mathf.Sin(2f * Mathf.PI * f0 * 5.4f * t) * 0.15f
                           + Mathf.Sin(2f * Mathf.PI * f0 * 8.9f * t) * 0.1f;

                muestras[i] = tono * envolvente;
            }

            AudioClip clip = AudioClip.Create("Campana_Sintetizada", totalMuestras, 1, frecuenciaMuestreo, false);
            clip.SetData(muestras, 0);
            return clip;
        }
    }
}
