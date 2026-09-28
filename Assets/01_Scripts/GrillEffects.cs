using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Efectos de la parrilla: brasas que brillan, chispas, humo que sube y el chisporroteo
    /// de la comida sobre la rejilla.
    ///
    /// Igual que <see cref="BurnerFlame"/>, solo es lo visual y lo sonoro. Quien sabe
    /// cocinar es <see cref="CookingVessel"/>, que llama a <see cref="SetCocinando"/> con el
    /// progreso del ingrediente más avanzado. Con eso el humo avisa sin mirar el cartel:
    /// blanco y suave mientras se dora, gris al estar listo y negro y espeso al quemarse.
    /// </summary>
    public class GrillEffects : MonoBehaviour
    {
        [Header("Partículas")]
        [SerializeField] private ParticleSystem humo;
        [SerializeField] private ParticleSystem chispas;

        [Header("Brasas")]
        [Tooltip("Luz roja bajo la rejilla. Está siempre algo encendida: el carbón no se apaga " +
                 "porque no haya comida encima.")]
        [SerializeField] private Light luzBrasas;

        [SerializeField] private float intensidadReposo = 0.6f;
        [SerializeField] private float intensidadCocinando = 1.8f;
        [SerializeField] private float amplitudParpadeo = 0.35f;
        [SerializeField] private float velocidadParpadeo = 3.5f;

        [Header("Humo según el punto")]
        [SerializeField] private Color humoCocinando = new Color(0.92f, 0.92f, 0.90f, 0.35f);
        [SerializeField] private Color humoListo = new Color(0.65f, 0.63f, 0.60f, 0.45f);
        [SerializeField] private Color humoQuemado = new Color(0.12f, 0.11f, 0.10f, 0.75f);

        [SerializeField] private float humoPorSegundoCocinando = 10f;
        [SerializeField] private float humoPorSegundoQuemado = 28f;

        [Header("Sonido")]
        [Tooltip("Chisporroteo en bucle. Vacío = se genera uno por código al arrancar.")]
        [SerializeField] private AudioClip sonidoChisporroteo;

        [SerializeField, Range(0f, 1f)] private float volumen = 0.45f;

        [Tooltip("Segundos que tarda el sonido en subir o bajar, para que no corte de golpe.")]
        [SerializeField] private float fundido = 0.4f;

        private AudioSource audioSource;
        private float volumenObjetivo;
        private float progreso;
        private float semilla;

        public bool Cocinando { get; private set; }

        private void Awake()
        {
            semilla = Random.Range(0f, 100f);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = sonidoChisporroteo != null ? sonidoChisporroteo : GenerarChisporroteo();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D: se oye de la parrilla, no dentro del casco
            audioSource.minDistance = 0.4f;
            audioSource.maxDistance = 6f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = 0f;

            if (chispas != null) chispas.Play(true); // el carbón chisporrotea siempre, poco
            SetCocinando(false, 0f);
        }

        /// <summary>
        /// <paramref name="progresoCoccion"/> va de 0 a 2, igual que en el cartel:
        /// hasta 1 se está dorando, de 1 a 2 se acerca a quemarse.
        /// </summary>
        public void SetCocinando(bool valor, float progresoCoccion)
        {
            progreso = progresoCoccion;

            if (valor != Cocinando)
            {
                Cocinando = valor;

                if (humo != null)
                {
                    if (valor) humo.Play(true);
                    // StopEmitting a secas: el humo que ya salió termina de subir y se
                    // disipa, en vez de desaparecer de golpe al retirar el sonso.
                    else humo.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                if (valor && audioSource != null && !audioSource.isPlaying) audioSource.Play();
                volumenObjetivo = valor ? volumen : 0f;
            }

            if (valor) AjustarHumo();
            AjustarChispas();
        }

        private void AjustarHumo()
        {
            if (humo == null) return;

            Color color;
            float tasa;
            if (progreso < 1f)
            {
                color = Color.Lerp(humoCocinando, humoListo, progreso);
                tasa = humoPorSegundoCocinando;
            }
            else
            {
                float t = Mathf.Clamp01(progreso - 1f);
                color = Color.Lerp(humoListo, humoQuemado, t);
                tasa = Mathf.Lerp(humoPorSegundoCocinando, humoPorSegundoQuemado, t);
            }

            var main = humo.main;
            main.startColor = color;
            var emision = humo.emission;
            emision.rateOverTime = tasa;
        }

        private void AjustarChispas()
        {
            if (chispas == null) return;

            // Más chispas con comida encima: es la grasa goteando sobre el carbón.
            var emision = chispas.emission;
            emision.rateOverTime = Cocinando ? 14f : 3f;
        }

        private void Update()
        {
            if (luzBrasas != null)
            {
                float baseIntensidad = Cocinando ? intensidadCocinando : intensidadReposo;
                // Perlin lento: las brasas respiran, no titilan como una llama.
                float ruido = Mathf.PerlinNoise(semilla, Time.time * velocidadParpadeo);
                luzBrasas.intensity = baseIntensidad + (ruido - 0.5f) * 2f * amplitudParpadeo;
            }

            if (audioSource != null)
            {
                float paso = (fundido > 0f) ? volumen * Time.deltaTime / fundido : 1f;
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, volumenObjetivo, paso);
                if (volumenObjetivo <= 0f && audioSource.volume <= 0f && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }

        /// <summary>
        /// El proyecto no trae ningún sonido de parrilla, así que se sintetiza: un siseo de
        /// ruido filtrado (la grasa) con chasquidos sueltos encima (el carbón). Dos segundos
        /// en bucle bastan porque los chasquidos caen al azar y no se nota la repetición.
        /// </summary>
        private static AudioClip GenerarChisporroteo()
        {
            const int frecuencia = 22050;
            const float duracion = 2f;
            int muestras = Mathf.RoundToInt(frecuencia * duracion);
            var datos = new float[muestras];
            var rng = new System.Random(1234);

            float filtrado = 0f;
            float chasquido = 0f;

            for (int i = 0; i < muestras; i++)
            {
                float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);

                // Paso alto casero: resta la parte lenta y deja solo el siseo agudo.
                filtrado += (ruido - filtrado) * 0.25f;
                float siseo = (ruido - filtrado) * 0.35f;

                // Ondulación lenta del volumen, como cuando la grasa burbujea a ratos.
                float t = i / (float)frecuencia;
                siseo *= 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 2f * 1.5f);

                // Chasquido: pico que decae muy rápido. ~12 por segundo.
                if (rng.NextDouble() < 12.0 / frecuencia)
                {
                    chasquido = (float)(0.5 + rng.NextDouble() * 0.5);
                }
                float pop = chasquido * (float)(rng.NextDouble() * 2.0 - 1.0);
                chasquido *= 0.985f;

                datos[i] = Mathf.Clamp(siseo + pop * 0.6f, -1f, 1f);
            }

            // Fundido en las puntas para que el bucle no haga clic al empalmar.
            int borde = frecuencia / 50;
            for (int i = 0; i < borde; i++)
            {
                float k = i / (float)borde;
                datos[i] *= k;
                datos[muestras - 1 - i] *= k;
            }

            var clip = AudioClip.Create("Chisporroteo_Parrilla", muestras, 1, frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }
    }
}
