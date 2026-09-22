using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Administrador de música de fondo para niveles de cocina.
    /// Reproduce una lista de canciones con volumen moderado/bajo (configurable),
    /// selección aleatoria sin repeticiones consecutivas y transiciones suaves (fade).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusicManager : MonoBehaviour
    {
        public static BackgroundMusicManager Instance { get; private set; }

        [Header("Playlist")]
        [Tooltip("Lista de canciones de fondo que se reproducirán durante el nivel.")]
        [SerializeField] private List<AudioClip> playlist = new List<AudioClip>();

        [Header("Configuración de Volumen y Reproducción")]
        [Range(0f, 1f)]
        [Tooltip("Volumen maestro de la música de fondo. Recomendado entre 0.15 y 0.25 para no tapar efectos de cocina.")]
        [SerializeField] private float musicVolume = 0.22f;

        [Tooltip("Si es true, el orden de las canciones es aleatorio sin repetir la misma canción dos veces seguidas.")]
        [SerializeField] private bool shuffle = true;

        [Tooltip("Iniciar la reproducción automáticamente al cargar la escena.")]
        [SerializeField] private bool autoPlayOnStart = true;

        [Tooltip("Duración del desvanecimiento suave (fade) al iniciar o cambiar de canción (segundos).")]
        [SerializeField] private float fadeDuration = 2.0f;

        [Tooltip("Pausa de silencio entre canciones al terminar una pista (segundos).")]
        [SerializeField] private float pauseBetweenTracks = 1.0f;

        [Header("Persistencia")]
        [Tooltip("Si es true, la música continúa sonando al cambiar de escena.")]
        [SerializeField] private bool persistBetweenScenes = false;

        private AudioSource audioSource;
        private int lastTrackIndex = -1;
        private Coroutine playbackRoutine;
        private Coroutine fadeRoutine;
        private bool isPaused = false;
        private bool isFading = false;

        public float Volume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (audioSource != null && !isFading)
                {
                    audioSource.volume = musicVolume;
                }
            }
        }

        public bool IsPlaying => audioSource != null && audioSource.isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (persistBetweenScenes)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Instance = this;

            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // En VR la música de fondo debe ser 2D (spatialBlend = 0) para sonar uniforme en ambos oídos
            audioSource.spatialBlend = 0f;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
        }

        private void Start()
        {
            if (autoPlayOnStart && playlist != null && playlist.Count > 0)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Inicia o reanuda la reproducción del playlist.
        /// </summary>
        public void Play()
        {
            if (playlist == null || playlist.Count == 0)
            {
                Debug.LogWarning("[BackgroundMusicManager] No hay canciones en el playlist.");
                return;
            }

            if (isPaused && audioSource != null)
            {
                audioSource.UnPause();
                isPaused = false;
                return;
            }

            if (playbackRoutine != null) StopCoroutine(playbackRoutine);
            playbackRoutine = StartCoroutine(PlaylistLoopRoutine());
        }

        /// <summary>
        /// Pausa la música actual sin reiniciar la pista.
        /// </summary>
        public void PauseMusic()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
                isPaused = true;
            }
        }

        /// <summary>
        /// Reanuda la música si estaba pausada.
        /// </summary>
        public void ResumeMusic()
        {
            if (audioSource != null && isPaused)
            {
                audioSource.UnPause();
                isPaused = false;
            }
            else if (!IsPlaying)
            {
                Play();
            }
        }

        /// <summary>
        /// Detiene la música con un desvanecimiento suave (fade out).
        /// </summary>
        public void StopMusic(float fadeOutSeconds = 1.5f)
        {
            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOutAndStop(fadeOutSeconds));
        }

        /// <summary>
        /// Salta a la siguiente canción inmediatamente.
        /// </summary>
        public void NextTrack()
        {
            if (playbackRoutine != null) StopCoroutine(playbackRoutine);
            playbackRoutine = StartCoroutine(PlaylistLoopRoutine());
        }

        private IEnumerator PlaylistLoopRoutine()
        {
            while (true)
            {
                AudioClip nextClip = PickNextClip();
                if (nextClip == null) yield break;

                audioSource.clip = nextClip;
                audioSource.Play();
                Debug.Log($"[BackgroundMusicManager] Reproduciendo '{nextClip.name}' (Volumen: {musicVolume:F2}, Shuffle: {shuffle})");

                // Fade In inicial
                yield return FadeRoutine(0f, musicVolume, fadeDuration);

                // Esperar a que la canción esté próxima a terminar
                float waitTime = Mathf.Max(0.1f, nextClip.length - fadeDuration);
                float timer = 0f;
                while (timer < waitTime)
                {
                    if (!isPaused) timer += Time.deltaTime;
                    yield return null;
                }

                // Fade Out al final de la canción
                yield return FadeRoutine(audioSource.volume, 0f, fadeDuration);

                audioSource.Stop();

                // Pausa sutil entre canciones antes de iniciar la siguiente
                if (pauseBetweenTracks > 0f)
                {
                    yield return new WaitForSeconds(pauseBetweenTracks);
                }
            }
        }

        private AudioClip PickNextClip()
        {
            if (playlist == null || playlist.Count == 0) return null;

            // Filtro para excluir elementos nulos en el inspector
            var validClips = new List<int>();
            for (int i = 0; i < playlist.Count; i++)
            {
                if (playlist[i] != null) validClips.Add(i);
            }

            if (validClips.Count == 0) return null;
            if (validClips.Count == 1)
            {
                lastTrackIndex = validClips[0];
                return playlist[lastTrackIndex];
            }

            int chosenIndex;
            if (shuffle)
            {
                // Elegir aleatoriamente evitando repetir la última canción
                int attempts = 10;
                do
                {
                    int randomIndex = Random.Range(0, validClips.Count);
                    chosenIndex = validClips[randomIndex];
                    attempts--;
                } while (chosenIndex == lastTrackIndex && attempts > 0);
            }
            else
            {
                // Secuencial
                int currentPos = validClips.IndexOf(lastTrackIndex);
                int nextPos = (currentPos + 1) % validClips.Count;
                chosenIndex = validClips[nextPos];
            }

            lastTrackIndex = chosenIndex;
            return playlist[chosenIndex];
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (duration <= 0.01f)
            {
                audioSource.volume = to;
                yield break;
            }

            isFading = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audioSource.volume = Mathf.Lerp(from, to, t);
                yield return null;
            }
            audioSource.volume = to;
            isFading = false;
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                yield return FadeRoutine(audioSource.volume, 0f, duration);
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Agrega una canción a la lista de reproducción en tiempo de ejecución.
        /// </summary>
        public void AddTrack(AudioClip clip)
        {
            if (clip != null && !playlist.Contains(clip))
            {
                playlist.Add(clip);
            }
        }
    }
}
