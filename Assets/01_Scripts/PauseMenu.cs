using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CocinaBoliviana
{
    /// <summary>
    /// Menú de pausa a mitad de partida: Continuar, Reiniciar nivel o Salir al menú.
    /// Se abre y se cierra con X del mando izquierdo (P o Esc en el simulador).
    ///
    /// Al pausar se congela el tiempo del juego (Time.timeScale = 0): se para el reloj del
    /// nivel, la cuenta atrás de los pedidos y la cocción, y se silencia el audio. La
    /// interfaz y los mandos siguen funcionando.
    ///
    /// Salir al menú deja guardado este nivel como "en curso": 'Continuar' del menú
    /// principal vuelve a él, empezando de nuevo.
    ///
    /// Igual que la guía de recetas, se crea solo en cualquier escena con LevelManager.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Controles")]
        [Tooltip("X del mando izquierdo. La Y ya abre la guía de recetas.")]
        [SerializeField] private string[] bindings =
        {
            "<XRController>{LeftHand}/primaryButton",
            "<Keyboard>/p",
            "<Keyboard>/escape",
        };

        [Header("Colocación")]
        [SerializeField] private float distancia = 0.85f;
        [SerializeField] private float bajarRespectoOjos = 0.05f;

        private const float Escala = 0.001f;

        private InputAction accion;
        private GameObject canvasGo;
        private Text subtitulo;
        private float timeScaleAntes = 1f;

        public static bool Pausado { get; private set; }

        // ------------------------------------------------------------ auto-instalación

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Instalar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            // Cambiar de escena desde la pausa no debe dejar el juego congelado ni mudo.
            Desbloquear();

            if (FindAnyObjectByType<LevelManager>() == null) return;
            if (FindAnyObjectByType<PauseMenu>() != null) return;

            new GameObject("MenuPausa").AddComponent<PauseMenu>();
        }

        private static void Desbloquear()
        {
            Pausado = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // ------------------------------------------------------------------- ciclo

        private void OnEnable()
        {
            accion = new InputAction("Pausa", InputActionType.Button);
            foreach (string b in bindings)
            {
                if (!string.IsNullOrEmpty(b)) accion.AddBinding(b);
            }
            accion.performed += _ => Alternar();
            accion.Enable();
        }

        private void OnDisable()
        {
            if (accion != null)
            {
                accion.Disable();
                accion.Dispose();
                accion = null;
            }
            if (Pausado) Desbloquear();
        }

        public void Alternar()
        {
            if (Pausado) Reanudar();
            else Pausar();
        }

        public void Pausar()
        {
            var lm = LevelManager.Instance;
            // Solo en plena partida: durante la cuenta atrás o con la pantalla final ya
            // abierta (que tiene sus propios botones) no tiene sentido.
            if (lm == null || lm.Estado != LevelState.Playing) return;

            if (canvasGo == null) Construir();

            if (subtitulo != null) subtitulo.text = lm.NombreNivel;
            Colocar();
            canvasGo.SetActive(true);

            timeScaleAntes = (Time.timeScale > 0f) ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Pausado = true;
        }

        public void Reanudar()
        {
            if (canvasGo != null) canvasGo.SetActive(false);
            Time.timeScale = timeScaleAntes;
            AudioListener.pause = false;
            Pausado = false;
        }

        private void ReiniciarNivel()
        {
            Desbloquear();
            if (LevelManager.Instance != null) LevelManager.Instance.ReiniciarNivel();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void SalirAlMenu()
        {
            Desbloquear();
            var lm = LevelManager.Instance;
            if (lm != null)
            {
                // Ya se guardó al empezar el nivel, pero se refuerza: es la promesa de este botón.
                GameProgressManager.GuardarNivelEnCurso(lm.NumeroNivel);
                lm.IrAlMenuPrincipal();
            }
            else
            {
                SceneManager.LoadScene(GameProgressManager.EscenaMenuPrincipal);
            }
        }

        // ------------------------------------------------------------------ interfaz

        private void Construir()
        {
            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            canvasGo = new GameObject("MenuPausa_Canvas", typeof(RectTransform), typeof(Canvas),
                                      typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // por delante de la guía y de los carteles
            var rtCanvas = canvasGo.GetComponent<RectTransform>();
            rtCanvas.sizeDelta = new Vector2(520f, 470f);
            canvasGo.transform.localScale = Vector3.one * Escala;

            var fondo = CrearImagen(canvasGo.transform, "Fondo", new Color(0.06f, 0.07f, 0.10f, 0.96f));
            Estirar(fondo.rectTransform, 0f, 1f);

            Text titulo = CrearTexto(fondo.transform, "Titulo", "PAUSA", 52, fuente, FontStyle.Bold);
            titulo.color = new Color(1f, 0.82f, 0.45f);
            Estirar(titulo.rectTransform, 0.80f, 0.97f);

            subtitulo = CrearTexto(fondo.transform, "Subtitulo", "", 26, fuente, FontStyle.Normal);
            subtitulo.color = new Color(0.75f, 0.78f, 0.82f);
            Estirar(subtitulo.rectTransform, 0.71f, 0.80f);

            CrearBoton(fondo.transform, "Continuar", new Color(0.20f, 0.55f, 0.30f), 0.50f, 0.66f, fuente, Reanudar);
            CrearBoton(fondo.transform, "Reiniciar nivel", new Color(0.70f, 0.50f, 0.15f), 0.30f, 0.46f, fuente, ReiniciarNivel);
            CrearBoton(fondo.transform, "Salir al menú", new Color(0.65f, 0.20f, 0.18f), 0.10f, 0.26f, fuente, SalirAlMenu);

            Text pie = CrearTexto(fondo.transform, "Pie", "X (mando izquierdo) para volver", 20, fuente, FontStyle.Italic);
            pie.color = new Color(0.6f, 0.62f, 0.66f);
            Estirar(pie.rectTransform, 0.01f, 0.09f);

            canvasGo.SetActive(false);
        }

        private void CrearBoton(Transform padre, string texto, Color color, float yMin, float yMax,
                                Font fuente, UnityEngine.Events.UnityAction alPulsar)
        {
            Image img = CrearImagen(padre, "Btn_" + texto, color);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.12f, yMin);
            rt.anchorMax = new Vector2(0.88f, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var boton = img.gameObject.AddComponent<Button>();
            boton.targetGraphic = img;
            var colores = boton.colors;
            colores.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colores.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            boton.colors = colores;
            boton.onClick.AddListener(alPulsar);

            Text t = CrearTexto(img.transform, "Texto", texto, 32, fuente, FontStyle.Bold);
            Estirar(t.rectTransform, 0f, 1f);
        }

        private static Image CrearImagen(Transform padre, string nombre, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text CrearTexto(Transform padre, string nombre, string texto, int tamano, Font fuente, FontStyle estilo)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(padre, false);
            var t = go.GetComponent<Text>();
            t.text = texto;
            t.font = fuente;
            t.fontSize = tamano;
            t.fontStyle = estilo;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Ocupa todo el ancho y la franja vertical [yMin, yMax] del padre.</summary>
        private static void Estirar(RectTransform rt, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(0f, yMin);
            rt.anchorMax = new Vector2(1f, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Delante de la cabeza, quieto en el mundo (con el juego congelado no hace falta seguirla).</summary>
        private void Colocar()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 adelante = cam.transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.001f) adelante = cam.transform.up;
            adelante.y = 0f;
            adelante.Normalize();

            Vector3 pos = cam.transform.position + adelante * distancia + Vector3.down * bajarRespectoOjos;
            canvasGo.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(adelante));
        }
    }
}
