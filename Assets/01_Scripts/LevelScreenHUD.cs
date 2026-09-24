using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CocinaBoliviana
{
    /// <summary>
    /// Interfaz de usuario en pantalla (Screen-Space Overlay) estilo Overcooked para el nivel.
    /// Muestra el banner inicial de cuenta regresiva, la barra superior con el temporizador digital,
    /// la puntuación, las estrellas de objetivo, el indicador de racha de combo y la pantalla final de resultados.
    /// </summary>
    public class LevelScreenHUD : MonoBehaviour
    {
        [Header("Barra Superior (HUD en Juego)")]
        [SerializeField] private GameObject topBarRoot;
        [SerializeField] private Text textoTiempo;
        [SerializeField] private Image barraTiempo;
        [SerializeField] private Text textoPuntos;
        [SerializeField] private Image barraProgresoEstrellas;
        [SerializeField] private Text textoEstrellasHUD;

        [Header("Racha / Combo")]
        [SerializeField] private GameObject rachaRoot;
        [SerializeField] private Text textoRacha;
        [SerializeField] private Text textoBonus;

        [Header("Banner Inicial (Cuenta Regresiva)")]
        [SerializeField] private GameObject startBannerRoot;
        [SerializeField] private Text textoNombreNivel;
        [SerializeField] private Text textoObjetivoInicial;
        [SerializeField] private Text textoCuentaAtras;

        [Header("Texto Flotante de Puntos (+ / -)")]
        [SerializeField] private Text textoFeedbackPuntos;

        [Header("Modal de Fin de Nivel")]
        [SerializeField] private GameObject endModalRoot;
        [SerializeField] private Text textoTituloFin;
        [SerializeField] private Text textoSubtituloFin;
        [SerializeField] private Text textoEstrellasFin;
        [SerializeField] private Text textoResumenStats;
        [SerializeField] private Button botonReintentar;
        [SerializeField] private Button botonMenuPrincipal;

        [Header("Colores")]
        [SerializeField] private Color colorTiempoNormal = new Color(0.2f, 0.85f, 0.45f);
        [SerializeField] private Color colorTiempoMedio = new Color(1.0f, 0.80f, 0.25f);
        [SerializeField] private Color colorTiempoCritico = new Color(0.95f, 0.25f, 0.25f);
        [SerializeField] private Color colorRachaActiva = new Color(1.0f, 0.70f, 0.15f);

        private Coroutine feedbackRoutine;
        private LevelManager lm;

        private void Start()
        {
            lm = LevelManager.Instance;
            if (lm == null)
            {
                Debug.LogWarning("[LevelScreenHUD] No se encontró LevelManager en la escena.");
                return;
            }

            // Suscribirse a eventos del LevelManager
            lm.OnTimeChanged += ActualizarTiempo;
            lm.OnScoreChanged += ActualizarPuntuacion;
            lm.OnStreakChanged += ActualizarRacha;
            lm.OnStateChanged += ActualizarEstado;
            lm.OnCountdownTick += ActualizarCuentaAtras;

            // Configurar botones
            if (botonReintentar != null)
            {
                botonReintentar.onClick.RemoveAllListeners();
                botonReintentar.onClick.AddListener(() => lm.ReiniciarNivel());
            }
            if (botonMenuPrincipal != null)
            {
                botonMenuPrincipal.onClick.RemoveAllListeners();
                botonMenuPrincipal.onClick.AddListener(() => lm.IrAlMenuPrincipal());
            }

            // Inicializar textos y pantallas
            if (textoNombreNivel != null) textoNombreNivel.text = lm.NombreNivel.ToUpperInvariant();
            if (textoObjetivoInicial != null)
            {
                int min = Mathf.FloorToInt(lm.DuracionTotal / 60f);
                int sec = Mathf.FloorToInt(lm.DuracionTotal % 60f);
                textoObjetivoInicial.text = $"OBJETIVO: {lm.Objetivo1Estrella} PUNTOS (1★)   |   TIEMPO: {min:00}:{sec:00}";
            }

            if (textoFeedbackPuntos != null) textoFeedbackPuntos.gameObject.SetActive(false);
            if (endModalRoot != null) endModalRoot.SetActive(false);

            // Refrescar estado inicial
            ActualizarPuntuacion(lm.Puntos, 0, lm.RachaActual);
            ActualizarTiempo(lm.DuracionTotal, lm.DuracionTotal);
            ActualizarRacha(lm.RachaActual, 0);
            ActualizarEstado(lm.Estado);
            if (lm.Estado == LevelState.Starting && textoCuentaAtras != null)
            {
                textoCuentaAtras.text = "3";
            }
        }

        private void OnDestroy()
        {
            if (lm != null)
            {
                lm.OnTimeChanged -= ActualizarTiempo;
                lm.OnScoreChanged -= ActualizarPuntuacion;
                lm.OnStreakChanged -= ActualizarRacha;
                lm.OnStateChanged -= ActualizarEstado;
                lm.OnCountdownTick -= ActualizarCuentaAtras;
            }
        }

        private void ActualizarCuentaAtras(string tick)
        {
            if (startBannerRoot != null) startBannerRoot.SetActive(true);
            if (textoCuentaAtras != null)
            {
                textoCuentaAtras.text = tick;
                StartCoroutine(PunchScale(textoCuentaAtras.transform, 1.25f, 0.2f));
            }
        }

        private void ActualizarEstado(LevelState estado)
        {
            switch (estado)
            {
                case LevelState.Starting:
                    if (startBannerRoot != null) startBannerRoot.SetActive(true);
                    if (topBarRoot != null) topBarRoot.SetActive(true);
                    if (endModalRoot != null) endModalRoot.SetActive(false);
                    break;

                case LevelState.Playing:
                    if (startBannerRoot != null) startBannerRoot.SetActive(false);
                    if (topBarRoot != null) topBarRoot.SetActive(true);
                    if (endModalRoot != null) endModalRoot.SetActive(false);
                    break;

                case LevelState.Finished:
                    if (topBarRoot != null) topBarRoot.SetActive(true);
                    MostrarFinDeNivel();
                    break;
            }
        }

        private void ActualizarTiempo(float restante, float total)
        {
            if (textoTiempo != null)
            {
                int min = Mathf.FloorToInt(restante / 60f);
                int sec = Mathf.FloorToInt(restante % 60f);
                textoTiempo.text = $"{min:00}:{sec:00}";

                if (restante <= 30f)
                {
                    // Parpadeo de urgencia en los últimos 30 segundos
                    textoTiempo.color = (Mathf.Sin(Time.time * 8f) > 0f) ? colorTiempoCritico : Color.white;
                }
                else
                {
                    textoTiempo.color = Color.white;
                }
            }

            if (barraTiempo != null && total > 0f)
            {
                float frac = Mathf.Clamp01(restante / total);
                barraTiempo.fillAmount = frac;
                barraTiempo.color = (frac > 0.5f) ? colorTiempoNormal : (frac > 0.2f) ? colorTiempoMedio : colorTiempoCritico;
            }
        }

        private void ActualizarPuntuacion(int puntos, int delta, int racha)
        {
            if (textoPuntos != null)
            {
                textoPuntos.text = $"{puntos} PTS";
            }

            if (lm != null)
            {
                if (barraProgresoEstrellas != null)
                {
                    barraProgresoEstrellas.fillAmount = lm.ObtenerProgresoEstrellas();
                }

                if (textoEstrellasHUD != null)
                {
                    int stars = lm.EstrellasConseguidas;
                    string starsStr = (stars == 3) ? "★★★" : (stars == 2) ? "★★☆" : (stars == 1) ? "★☆☆" : "☆☆☆";
                    textoEstrellasHUD.text = $"{starsStr}  (Meta: {lm.Objetivo1Estrella})";
                }
            }

            // Notificación visual (+ / -)
            if (delta != 0 && textoFeedbackPuntos != null)
            {
                if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
                feedbackRoutine = StartCoroutine(MostrarFeedbackPuntos(delta, racha));
            }
        }

        private void ActualizarRacha(int racha, int bonus)
        {
            if (rachaRoot == null) return;

            if (racha > 1)
            {
                rachaRoot.SetActive(true);
                if (textoRacha != null) textoRacha.text = $"RACHA x{racha}";
                if (textoBonus != null) textoBonus.text = (bonus > 0) ? $"+{bonus} PROPINA" : "EN RACHA";
                StartCoroutine(PunchScale(rachaRoot.transform, 1.15f, 0.15f));
            }
            else
            {
                rachaRoot.SetActive(false);
            }
        }

        private IEnumerator MostrarFeedbackPuntos(int delta, int racha)
        {
            textoFeedbackPuntos.gameObject.SetActive(true);
            if (delta > 0)
            {
                string rachaTag = (racha > 1) ? $"  (x{racha}!)" : "";
                textoFeedbackPuntos.text = $"+{delta} PTS{rachaTag}";
                textoFeedbackPuntos.color = (racha > 1) ? colorRachaActiva : colorTiempoNormal;
            }
            else
            {
                textoFeedbackPuntos.text = $"{delta} PTS  (¡RACHA PERDIDA!)";
                textoFeedbackPuntos.color = colorTiempoCritico;
            }

            var rt = textoFeedbackPuntos.GetComponent<RectTransform>();
            Vector2 posOriginal = rt.anchoredPosition;

            float duracion = 1.6f;
            float elapsed = 0f;
            while (elapsed < duracion)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duracion;
                rt.anchoredPosition = posOriginal + new Vector2(0f, t * 45f);
                yield return null;
            }

            rt.anchoredPosition = posOriginal;
            textoFeedbackPuntos.gameObject.SetActive(false);
        }

        private void MostrarFinDeNivel()
        {
            if (endModalRoot == null || lm == null) return;

            endModalRoot.SetActive(true);

            bool victoria = lm.NivelSuperado;
            int estrellas = lm.EstrellasConseguidas;

            if (textoTituloFin != null)
            {
                textoTituloFin.text = victoria ? "¡NIVEL COMPLETADO!" : "¡TIEMPO AGOTADO!";
                textoTituloFin.color = victoria ? new Color(1f, 0.84f, 0.2f) : new Color(0.95f, 0.35f, 0.35f);
            }

            if (textoSubtituloFin != null)
            {
                textoSubtituloFin.text = victoria
                    ? $"¡Excelente servicio en {lm.NombreNivel}!"
                    : $"Te faltaron {Mathf.Max(0, lm.Objetivo1Estrella - lm.Puntos)} puntos para la meta mínima de 1★.";
            }

            if (textoEstrellasFin != null)
            {
                textoEstrellasFin.text = (estrellas == 3) ? "★★★" : (estrellas == 2) ? "★★☆" : (estrellas == 1) ? "★☆☆" : "☆☆☆";
                textoEstrellasFin.color = (estrellas > 0) ? new Color(1f, 0.85f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
            }

            if (textoResumenStats != null)
            {
                textoResumenStats.text =
                    $"Puntos Finales: <b>{lm.Puntos}</b> (Meta: {lm.Objetivo1Estrella})\n" +
                    $"Platos Entregados: <b>{lm.PlatosEntregados}</b>\n" +
                    $"Pedidos Perdidos: <b>{lm.PedidosPerdidos}</b>\n" +
                    $"Mejor Racha: <b>x{lm.RachaMaxima}</b>";
            }

            // Desbloquear cursor para clicks con ratón en simulator
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Adaptar Canvas para que los mandos VR (ray interactor) puedan interactuar directamente
            var canvas = GetComponent<Canvas>();
            if (canvas != null && Camera.main != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 1.0f;
            }

            if (GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
            {
                gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            }
        }

        private void Update()
        {
            if (endModalRoot != null && endModalRoot.activeSelf && lm != null)
            {
                // Atajos inmediatos por teclado para máxima comodidad
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    Debug.Log("[LevelScreenHUD] Atajo de teclado (R/Espacio) -> Reiniciando nivel...");
                    lm.ReiniciarNivel();
                }
                else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
                {
                    Debug.Log("[LevelScreenHUD] Atajo de teclado (M/Esc) -> Volviendo al Menú Principal...");
                    lm.IrAlMenuPrincipal();
                }
            }
        }

        private IEnumerator PunchScale(Transform tr, float maxScale, float dur)
        {
            if (tr == null) yield break;
            Vector3 original = Vector3.one;
            float mitad = dur * 0.5f;

            float el = 0f;
            while (el < mitad)
            {
                el += Time.deltaTime;
                tr.localScale = Vector3.Lerp(original, original * maxScale, el / mitad);
                yield return null;
            }

            el = 0f;
            while (el < mitad)
            {
                el += Time.deltaTime;
                tr.localScale = Vector3.Lerp(original * maxScale, original, el / mitad);
                yield return null;
            }
            tr.localScale = original;
        }
    }
}
