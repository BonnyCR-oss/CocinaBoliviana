using System.Collections;
using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Mostrador de entrega. Detecta un plato terminado dejado encima y se lo pasa al
    /// <see cref="OrderManager"/> para ver si alguien lo había pedido.
    ///
    /// Incluye feedback visual y sonoro completo:
    /// - Sonido de éxito (campanazo/chime) y error (rechazo)
    /// - Salto de la campana de servicio (con soporte para <see cref="ServiceBell"/>)
    /// - Partículas de celebración / chispas doradas en acierto
    /// - Luz de estado que destella verde esmeralda al entregar o ámbar al rechazar
    /// </summary>
    public class DeliveryCounter : MonoBehaviour
    {
        [Header("Zona de Entrega")]
        [Tooltip("Caja (en METROS de mundo) sobre el mostrador donde se detecta el plato.")]
        [SerializeField] private Vector3 zonaDeteccion = new Vector3(0.7f, 0.4f, 0.7f);

        [Tooltip("Altura del centro de esa caja sobre el mostrador, en metros.")]
        [SerializeField] private float zonaAltura = 0.6f;

        [Header("Feedback Sonoro")]
        [SerializeField] private AudioClip sonidoAcierto;
        [SerializeField] private AudioClip sonidoRechazo;

        [Header("Feedback Visual")]
        [Tooltip("La campana de servicio. Da un saltito al entregar bien.")]
        [SerializeField] private Transform campana;

        [Tooltip("Partículas que estallan al entregar un pedido con éxito.")]
        [SerializeField] private ParticleSystem particulasExito;

        [Tooltip("Luz de feedback sobre el mostrador que destella según el resultado.")]
        [SerializeField] private Light luzFeedback;

        private AudioSource audioSource;
        private float finRebote;
        private Vector3 campanaEnReposo;
        private ServiceBell serviceBell;
        private Color luzColorBase = new Color(1f, 0.95f, 0.85f);
        private float luzIntensidadBase = 2f;
        private Coroutine corrutinaLuz;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            if (campana == null)
            {
                campana = transform.Find("Campana_Servicio");
            }

            if (campana != null)
            {
                campanaEnReposo = campana.localPosition;
                serviceBell = campana.GetComponent<ServiceBell>();
                if (serviceBell == null)
                {
                    serviceBell = campana.gameObject.AddComponent<ServiceBell>();
                }
            }

            if (luzFeedback == null)
            {
                Transform extractor = transform.Find("Extractor_Entrega");
                if (extractor != null)
                {
                    Transform luzT = extractor.Find("Luz_Extractor");
                    if (luzT != null) luzFeedback = luzT.GetComponent<Light>();
                }
            }

            if (luzFeedback != null)
            {
                luzColorBase = luzFeedback.color;
                luzIntensidadBase = luzFeedback.intensity;
            }

            if (sonidoAcierto == null)
            {
#if UNITY_EDITOR
                sonidoAcierto = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/06_SFX/exito.mp3");
#endif
                if (sonidoAcierto == null) sonidoAcierto = CrearSonidoAciertoSintetizado();
            }

            if (sonidoRechazo == null)
            {
#if UNITY_EDITOR
                sonidoRechazo = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/06_SFX/corte.mp3");
#endif
                if (sonidoRechazo == null) sonidoRechazo = CrearSonidoRechazoSintetizado();
            }

            if (particulasExito == null)
            {
                Transform partT = transform.Find("Particulas_Entrega");
                if (partT != null)
                {
                    particulasExito = partT.GetComponent<ParticleSystem>();
                }
                else
                {
                    particulasExito = CrearParticulasCelebracion();
                }
            }
        }

        private static AudioClip CrearSonidoAciertoSintetizado()
        {
            int frecuencia = 44100;
            float duracion = 0.5f;
            int totalMuestras = (int)(frecuencia * duracion);
            float[] muestras = new float[totalMuestras];

            float[] notas = new float[] { 1046.5f, 1318.5f, 1567.98f };
            for (int i = 0; i < totalMuestras; i++)
            {
                float t = (float)i / frecuencia;
                float valor = 0f;
                for (int n = 0; n < notas.Length; n++)
                {
                    float retardoNota = n * 0.08f;
                    if (t >= retardoNota)
                    {
                        float tn = t - retardoNota;
                        valor += Mathf.Sin(2f * Mathf.PI * notas[n] * tn) * Mathf.Exp(-tn * 9f);
                    }
                }
                muestras[i] = Mathf.Clamp(valor * 0.5f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Acierto_Sintetizado", totalMuestras, 1, frecuencia, false);
            clip.SetData(muestras, 0);
            return clip;
        }

        private static AudioClip CrearSonidoRechazoSintetizado()
        {
            int frecuencia = 44100;
            float duracion = 0.35f;
            int totalMuestras = (int)(frecuencia * duracion);
            float[] muestras = new float[totalMuestras];

            for (int i = 0; i < totalMuestras; i++)
            {
                float t = (float)i / frecuencia;
                float envolvente = Mathf.Exp(-t * 10f);
                float f = (t < 0.15f) ? 220f : 175f;
                float valor = Mathf.PingPong(t * f * 4f, 2f) - 1f;
                muestras[i] = valor * envolvente * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Rechazo_Sintetizado", totalMuestras, 1, frecuencia, false);
            clip.SetData(muestras, 0);
            return clip;
        }

        private ParticleSystem CrearParticulasCelebracion()
        {
            var go = new GameObject("Particulas_Entrega");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.75f, 0.5f);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1.0f;
            main.startLifetime = 0.85f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new Color(1f, 0.88f, 0.35f, 1f);
            main.gravityModifier = -0.15f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.7f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var rend = go.GetComponent<ParticleSystemRenderer>();
            var s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (s != null && rend != null) rend.sharedMaterial = new Material(s) { color = new Color(1f, 0.88f, 0.35f, 1f) };

            return ps;
        }

        private const float IntervaloSondeo = 0.1f;
        private float proximoSondeo;

        private void FixedUpdate()
        {
            if (Time.time < proximoSondeo) return;
            proximoSondeo = Time.time + IntervaloSondeo;

            Vector3 centro = transform.position + Vector3.up * zonaAltura;
            Collider[] dentro = Physics.OverlapBox(centro, zonaDeteccion * 0.5f, transform.rotation,
                                                   ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in dentro)
            {
                var plato = col.GetComponentInParent<ServedDish>();
                if (plato == null || plato.Plato == null) continue;

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

                if (serviceBell != null)
                {
                    serviceBell.Ring();
                }
                else
                {
                    finRebote = Time.time + 0.35f;
                }

                if (particulasExito != null)
                {
                    particulasExito.Play();
                }

                if (luzFeedback != null)
                {
                    DestellarLuz(new Color(0.2f, 1f, 0.4f), 3.5f, 1.0f);
                }

                Destroy(plato.gameObject);
            }
            else
            {
                Sonar(sonidoRechazo);

                if (luzFeedback != null)
                {
                    DestellarLuz(new Color(1f, 0.3f, 0.1f), 2.8f, 0.6f);
                }

                Debug.Log($"[DeliveryCounter] {plato.Plato.nombre} rechazado: ningún pedido lo espera.");
            }
        }

        private void DestellarLuz(Color colorDestello, float intensidadDestello, float duracion)
        {
            if (luzFeedback == null) return;
            if (corrutinaLuz != null) StopCoroutine(corrutinaLuz);
            corrutinaLuz = StartCoroutine(AnimarLuz(colorDestello, intensidadDestello, duracion));
        }

        private IEnumerator AnimarLuz(Color colorDestello, float intensidadDestello, float duracion)
        {
            float elapsed = 0f;
            while (elapsed < duracion)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duracion;
                luzFeedback.color = Color.Lerp(colorDestello, luzColorBase, t);
                luzFeedback.intensity = Mathf.Lerp(intensidadDestello, luzIntensidadBase, t);
                yield return null;
            }

            luzFeedback.color = luzColorBase;
            luzFeedback.intensity = luzIntensidadBase;
            corrutinaLuz = null;
        }

        private void Sonar(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip, 0.95f);
        }

        private void Update()
        {
            if (campana == null || serviceBell != null) return;

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
