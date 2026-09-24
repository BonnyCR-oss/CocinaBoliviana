using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    public enum LevelState
    {
        Starting,   // Cuenta regresiva 3.. 2.. 1.. ¡A Cocinar!
        Playing,    // Tiempo corriendo, pedidos activos y cocción en marcha
        Finished    // Fin del nivel: evaluación de estrellas y pantalla de resultados
    }

    /// <summary>
    /// Controlador principal del nivel y sistema de puntuación al estilo Overcooked.
    /// Gestiona la duración del nivel, objetivos de estrellas, sistema de racha (combo bonus),
    /// penalización por pedidos perdidos (sin puntos negativos) y el flujo de estados.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Información del Nivel")]
        [Tooltip("Nombre descriptivo para la pantalla de inicio.")]
        [SerializeField] private string nombreNivel = "Nivel 1 - Cochabamba";

        [Tooltip("Duración total del nivel en segundos (ej. 150s = 2:30 min, 180s = 3:00 min).")]
        [SerializeField] private float duracionNivel = 150f;

        [Tooltip("Segundos de la cuenta atrás inicial antes de empezar a jugar.")]
        [SerializeField] private float segundosCuentaAtras = 3.5f;

        [Header("Objetivos de Puntuación (Estrellas)")]
        [Tooltip("Puntos requeridos para 1 Estrella (Mínimo necesario para superar el nivel).")]
        [SerializeField] private int objetivoPuntos1Estrella = 80;

        [Tooltip("Puntos requeridos para 2 Estrellas.")]
        [SerializeField] private int objetivoPuntos2Estrellas = 150;

        [Tooltip("Puntos requeridos para 3 Estrellas.")]
        [SerializeField] private int objetivoPuntos3Estrellas = 220;

        [Header("Racha y Bonus de Propina (Overcooked)")]
        [Tooltip("Puntos restados cuando un pedido caduca sin ser entregado.")]
        [SerializeField] private int penalizacionPedidoPerdido = 20;

        [Tooltip("Puntos de propina extra que se suman por cada nivel de racha.")]
        [SerializeField] private int bonusPorNivelRacha = 10;

        [Tooltip("Racha máxima acumulable para bonus.")]
        [SerializeField] private int maxNivelRacha = 4;

        [Header("Audio")]
        [SerializeField] private AudioClip sonidoCuentaAtras;
        [SerializeField] private AudioClip sonidoSilbatoInicio;
        [SerializeField] private AudioClip sonidoEntregaExitosa;
        [SerializeField] private AudioClip sonidoPedidoPerdido;
        [SerializeField] private AudioClip sonidoVictoria;
        [SerializeField] private AudioClip sonidoDerrota;

        private AudioSource audioSource;
        private Coroutine levelLoopRoutine;

        // Propiedades de estado
        public LevelState Estado { get; private set; } = LevelState.Starting;
        public bool IsPlaying => Estado == LevelState.Playing;
        public float TiempoRestante { get; private set; }
        public float DuracionTotal => duracionNivel;
        public string NombreNivel => nombreNivel;

        // Puntuación y estadísticas
        public int Puntos { get; private set; }
        public int RachaActual { get; private set; }
        public int RachaMaxima { get; private set; }
        public int PlatosEntregados { get; private set; }
        public int PedidosPerdidos { get; private set; }

        public int Objetivo1Estrella => objetivoPuntos1Estrella;
        public int Objetivo2Estrellas => objetivoPuntos2Estrellas;
        public int Objetivo3Estrellas => objetivoPuntos3Estrellas;

        public bool NivelSuperado => Puntos >= objetivoPuntos1Estrella;
        public int EstrellasConseguidas => CalcularEstrellas();

        // Eventos para UI y sistemas externos
        public event Action<int, int, int> OnScoreChanged;      // (puntosActuales, deltaPuntos, racha)
        public event Action<float, float> OnTimeChanged;        // (tiempoRestante, duracionTotal)
        public event Action<int, int> OnStreakChanged;          // (rachaActual, bonusActual)
        public event Action<LevelState> OnStateChanged;         // (nuevoEstado)
        public event Action<string> OnCountdownTick;            // ("3", "2", "1", "¡A COCINAR!")

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D en cascos
            }

            TiempoRestante = duracionNivel;
            Puntos = 0;
            RachaActual = 0;
            RachaMaxima = 0;
            PlatosEntregados = 0;
            PedidosPerdidos = 0;
        }

        private void Start()
        {
            levelLoopRoutine = StartCoroutine(LevelLoop());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private IEnumerator LevelLoop()
        {
            // Esperar un frame inicial para asegurar que todos los sistemas de UI se hayan suscrito
            yield return null;

            // 1. Fase de Inicio / Cuenta Regresiva
            Estado = LevelState.Starting;
            OnStateChanged?.Invoke(Estado);

            float tiempoPorPaso = segundosCuentaAtras / 4f;

            OnCountdownTick?.Invoke("3");
            Sonar(sonidoCuentaAtras);
            yield return new WaitForSeconds(tiempoPorPaso);

            OnCountdownTick?.Invoke("2");
            Sonar(sonidoCuentaAtras);
            yield return new WaitForSeconds(tiempoPorPaso);

            OnCountdownTick?.Invoke("1");
            Sonar(sonidoCuentaAtras);
            yield return new WaitForSeconds(tiempoPorPaso);

            OnCountdownTick?.Invoke("¡A COCINAR!");
            Sonar(sonidoSilbatoInicio);
            yield return new WaitForSeconds(tiempoPorPaso);

            // 2. Fase de Juego
            Estado = LevelState.Playing;
            OnStateChanged?.Invoke(Estado);
            TiempoRestante = duracionNivel;

            while (TiempoRestante > 0f)
            {
                TiempoRestante -= Time.deltaTime;
                if (TiempoRestante < 0f) TiempoRestante = 0f;

                OnTimeChanged?.Invoke(TiempoRestante, duracionNivel);
                yield return null;
            }

            // 3. Fase de Fin de Nivel
            Estado = LevelState.Finished;
            OnStateChanged?.Invoke(Estado);

            Debug.Log($"[LevelManager] Nivel terminado. Puntos: {Puntos} / {objetivoPuntos1Estrella}. Superado: {NivelSuperado}. Estrellas: {EstrellasConseguidas}");

            if (NivelSuperado)
            {
                Sonar(sonidoVictoria != null ? sonidoVictoria : sonidoEntregaExitosa);
            }
            else
            {
                Sonar(sonidoDerrota != null ? sonidoDerrota : sonidoPedidoPerdido);
            }
        }

        /// <summary>
        /// Llamado por OrderManager al completar la entrega de un pedido.
        /// Aplica la recompensa base más el bonus por racha activa.
        /// </summary>
        public void RegistrarEntregaExitosa(int recompensaBase, DishData plato)
        {
            if (Estado != LevelState.Playing) return;

            RachaActual++;
            if (RachaActual > RachaMaxima) RachaMaxima = RachaActual;

            int nivelBonus = Mathf.Min(RachaActual - 1, maxNivelRacha);
            int bonus = (nivelBonus > 0) ? (nivelBonus * bonusPorNivelRacha) : 0;
            int totalSumar = recompensaBase + bonus;

            Puntos += totalSumar;
            PlatosEntregados++;

            Sonar(sonidoEntregaExitosa);

            Debug.Log($"[LevelManager] Entrega exitosa de '{plato?.nombre}'. Base: {recompensaBase} + Bonus Racha (x{RachaActual}): {bonus} = +{totalSumar} pts. Total: {Puntos}");

            OnScoreChanged?.Invoke(Puntos, totalSumar, RachaActual);
            OnStreakChanged?.Invoke(RachaActual, bonus);
        }

        /// <summary>
        /// Llamado por OrderManager cuando un pedido expira sin ser entregado.
        /// Corta la racha y aplica penalización de puntos sin permitir valores negativos.
        /// </summary>
        public void RegistrarPedidoPerdido(PedidoActivo pedido)
        {
            if (Estado != LevelState.Playing) return;

            int rachaPrevia = RachaActual;
            RachaActual = 0; // Se corta la racha
            PedidosPerdidos++;

            int puntosPrevios = Puntos;
            // Regla explícita: nunca puntos negativos
            Puntos = Mathf.Max(0, Puntos - penalizacionPedidoPerdido);
            int deltaPerdido = Puntos - puntosPrevios; // número negativo o 0

            Sonar(sonidoPedidoPerdido);

            Debug.Log($"[LevelManager] Pedido perdido. Racha cortada (era x{rachaPrevia}). Penalización: {penalizacionPedidoPerdido} pts. Puntos actuales: {Puntos}");

            OnScoreChanged?.Invoke(Puntos, deltaPerdido, RachaActual);
            OnStreakChanged?.Invoke(RachaActual, 0);
        }

        public int CalcularEstrellas()
        {
            if (Puntos >= objetivoPuntos3Estrellas) return 3;
            if (Puntos >= objetivoPuntos2Estrellas) return 2;
            if (Puntos >= objetivoPuntos1Estrella) return 1;
            return 0;
        }

        public float ObtenerProgresoEstrellas()
        {
            if (objetivoPuntos3Estrellas <= 0) return 0f;
            return Mathf.Clamp01((float)Puntos / objetivoPuntos3Estrellas);
        }

        public void ReiniciarNivel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void IrAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
        }

        private void Sonar(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, 0.9f);
            }
        }
    }
}
