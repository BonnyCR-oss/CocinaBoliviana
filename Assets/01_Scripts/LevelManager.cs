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
        [Tooltip("Número de nivel en la campaña (1=Cbba, 2=La Paz, 3=Santa Cruz).")]
        [SerializeField] private int numeroNivel = 1;

        [Tooltip("Nivel por defecto al dar Play directamente en esta escena. Si vienes del " +
                 "menú, manda el que hayas elegido allí.")]
        [SerializeField] private LevelData nivelPorDefecto;

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

        [Tooltip("Puntos que se restan por cada ingrediente tirado con 'Retirar todo' del plato.")]
        [SerializeField] private int penalizacionPorIngredienteDesperdiciado = 5;

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
        public int NumeroNivel => numeroNivel;
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

        /// <summary>Récord de puntos de este nivel antes de la partida actual.</summary>
        public int RecordAnterior { get; private set; }

        /// <summary>true si la partida que acaba de terminar superó el récord.</summary>
        public bool EsNuevoRecord { get; private set; }

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

            AplicarNivel();

            TiempoRestante = duracionNivel;
            Puntos = 0;
            RachaActual = 0;
            RachaMaxima = 0;
            PlatosEntregados = 0;
            PedidosPerdidos = 0;
        }

        /// <summary>
        /// Vuelca el LevelData de ESTA escena sobre los campos del Inspector.
        ///
        /// Cada nivel tiene su propia escena, así que manda el 'Nivel Por Defecto' de la
        /// escena: da igual lo que viniera en LevelSelection desde el menú o desde el nivel
        /// anterior. Solo una escena sin nivel propio (la First Scene de pruebas) usa el
        /// elegido en el menú.
        /// </summary>
        private void AplicarNivel()
        {
            LevelData nivel = (nivelPorDefecto != null) ? nivelPorDefecto : LevelSelection.Elegido;
            LevelSelection.Elegido = nivel;

            if (nivel == null) return;

            nombreNivel = nivel.nombreNivel;
            duracionNivel = nivel.duracionNivel;
            objetivoPuntos1Estrella = nivel.objetivoPuntos1Estrella;
            objetivoPuntos2Estrellas = nivel.objetivoPuntos2Estrellas;
            objetivoPuntos3Estrellas = nivel.objetivoPuntos3Estrellas;

            // El número sale del asset. Antes se deducía buscando "1", "2" o "3" en el
            // nombre, y renombrar un nivel lo rompía. Queda como respaldo para assets viejos.
            if (nivel.numeroNivel >= GameProgressManager.NivelMinimo) numeroNivel = nivel.numeroNivel;
            else if (nivel.nombreNivel.Contains("1") || nivel.nombreNivel.Contains("Cochabamba")) numeroNivel = 1;
            else if (nivel.nombreNivel.Contains("2") || nivel.nombreNivel.Contains("La Paz")) numeroNivel = 2;
            else if (nivel.nombreNivel.Contains("3") || nivel.nombreNivel.Contains("Santa Cruz")) numeroNivel = 3;

            // El departamento se lo pasa al OrderManager, que es de donde lo leen tambien
            // el dispensador de refrescos y las banderas de las paredes.
            if (nivel.departamento != null)
            {
                var pedidos = FindAnyObjectByType<OrderManager>();
                if (pedidos != null) pedidos.SetDepartamento(nivel.departamento);
            }

            Debug.Log($"[LevelManager] Nivel cargado: {nombreNivel} (Nivel #{numeroNivel}) " +
                      $"({(nivel.departamento != null ? nivel.departamento.nombre : "sin departamento")}).");
        }

        private void Start()
        {
            // Este es ahora el nivel "en curso": si sales al menú, 'Continuar' vuelve aquí.
            GameProgressManager.GuardarNivelEnCurso(numeroNivel);

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
            // El récord se guarda ANTES de avisar: la pantalla final lo lee al abrirse.
            RecordAnterior = GameProgressManager.ObtenerRecordPuntos(numeroNivel);
            EsNuevoRecord = GameProgressManager.GuardarResultado(numeroNivel, Puntos, EstrellasConseguidas);

            Estado = LevelState.Finished;
            OnStateChanged?.Invoke(Estado);

            Debug.Log($"[LevelManager] Nivel terminado. Puntos: {Puntos} / {objetivoPuntos1Estrella}. Superado: {NivelSuperado}. Estrellas: {EstrellasConseguidas}");

            if (NivelSuperado)
            {
                Sonar(sonidoVictoria != null ? sonidoVictoria : sonidoEntregaExitosa);

                // Si superó el nivel y no es el último, guardar avance para el siguiente nivel
                if (numeroNivel < GameProgressManager.NivelMaximo)
                {
                    GameProgressManager.GuardarNivel(numeroNivel + 1);
                    // Superado: 'Continuar' ya lleva al siguiente, no a repetir este.
                    GameProgressManager.GuardarNivelEnCurso(numeroNivel + 1);
                }
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

        /// <summary>
        /// Llamado por el plato de emplatado al vaciarlo con 'Retirar todo'. Resta por cada
        /// ingrediente tirado, sin bajar de 0. No corta la racha: equivocarse armando un plato
        /// no es lo mismo que dejar caducar un pedido.
        /// </summary>
        /// <returns>Los puntos que se restaron de verdad (0 si no había que quitar).</returns>
        public int RegistrarDesperdicio(int ingredientes)
        {
            if (Estado != LevelState.Playing || ingredientes <= 0) return 0;

            int puntosPrevios = Puntos;
            Puntos = Mathf.Max(0, Puntos - ingredientes * penalizacionPorIngredienteDesperdiciado);
            int delta = Puntos - puntosPrevios;

            Sonar(sonidoPedidoPerdido);
            Debug.Log($"[LevelManager] Desperdicio: {ingredientes} ingrediente(s) tirados. {delta} pts. Total: {Puntos}");

            OnScoreChanged?.Invoke(Puntos, delta, RachaActual);
            return -delta;
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
            LevelSelection.Elegido = nivelPorDefecto;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void CargarSiguienteNivel()
        {
            Time.timeScale = 1f;
            int siguiente = numeroNivel + 1;
            if (siguiente <= GameProgressManager.NivelMaximo)
            {
                // Solo se desbloquea habiendo superado este. El botón ya se oculta si no,
                // pero así ninguna otra llamada puede saltarse un nivel.
                if (NivelSuperado) GameProgressManager.GuardarNivel(siguiente);

                if (!GameProgressManager.EstaDesbloqueado(siguiente))
                {
                    Debug.LogWarning($"[LevelManager] El nivel {siguiente} aún está bloqueado.");
                    IrAlMenuPrincipal();
                    return;
                }

                // Limpiar la referencia estática previa para que la nueva escena tome limpiamente su nivel
                LevelSelection.Elegido = null;

                string escena = GameProgressManager.ObtenerNombreEscenaNivel(siguiente);
                Debug.Log($"[LevelManager] Avanzando al siguiente nivel ({siguiente}): '{escena}'");
                SceneManager.LoadScene(escena);
            }
            else
            {
                LevelSelection.Elegido = null;
                IrAlMenuPrincipal();
            }
        }

        public void IrAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            LevelSelection.Elegido = null;
            SceneManager.LoadScene(GameProgressManager.EscenaMenuPrincipal);
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
