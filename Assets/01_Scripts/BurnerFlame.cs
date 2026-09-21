using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Fuego de una hornalla: partículas + una luz que parpadea.
    ///
    /// Solo es el efecto visual. No sabe nada de cocinar; cuando exista la lógica de cocción,
    /// esta llamará a <see cref="SetEncendido"/> para prenderlo y apagarlo.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class BurnerFlame : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Light flameLight;

        [Header("Parpadeo")]
        [Tooltip("Intensidad base de la luz.")]
        [SerializeField] private float intensidadBase = 1.6f;

        [Tooltip("Cuánto sube y baja la intensidad respecto a la base.")]
        [SerializeField] private float amplitudParpadeo = 0.45f;

        [Tooltip("Velocidad del parpadeo. Más alto = más nervioso.")]
        [SerializeField] private float velocidadParpadeo = 9f;

        [Header("Estado inicial")]
        [SerializeField] private bool encendidoAlEmpezar = true;

        private ParticleSystem particulas;
        private float semilla;

        public bool Encendido { get; private set; }

        private void Awake()
        {
            particulas = GetComponent<ParticleSystem>();
            if (flameLight == null) flameLight = GetComponentInChildren<Light>(true);

            // Sin esto, dos hornallas contiguas parpadean idéntico y se nota el truco.
            semilla = Random.Range(0f, 100f);

            SetEncendido(encendidoAlEmpezar);
        }

        public void SetEncendido(bool valor)
        {
            Encendido = valor;

            if (particulas != null)
            {
                if (valor) particulas.Play(true);
                else particulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (flameLight != null)
            {
                flameLight.enabled = valor;
            }
        }

        private void Update()
        {
            if (!Encendido || flameLight == null) return;

            // Perlin en vez de Random: da un vaivén continuo, no un temblor de ruido blanco.
            float ruido = Mathf.PerlinNoise(semilla, Time.time * velocidadParpadeo);
            flameLight.intensity = intensidadBase + (ruido - 0.5f) * 2f * amplitudParpadeo;
        }
    }
}
