using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Guía de recetas del nivel: al pulsar Y en el mando izquierdo (o la tecla I en el
    /// simulador) aparece delante del jugador un panel con cada plato del departamento y
    /// cómo va cada ingrediente: entero o cortado, crudo o cocido, y de qué forma.
    ///
    /// Se lee en vivo de los DishData, así que si cambias una receta la guía cambia sola.
    ///
    /// No hace falta ponerlo en las escenas: se crea solo en cualquier escena que tenga un
    /// OrderManager (los tres niveles y la First Scene). Si prefieres colocarlo a mano, basta
    /// con un GameObject con este componente y no se duplica.
    /// </summary>
    public class RecipeGuide : MonoBehaviour
    {
        [Header("Controles")]
        [Tooltip("Botones que abren y cierran la guía. Y del mando izquierdo; I en teclado " +
                 "para el XR Device Simulator.")]
        [SerializeField] private string[] bindings =
        {
            "<XRController>{LeftHand}/secondaryButton",
            "<Keyboard>/i",
        };

        [Header("Colocación")]
        [Tooltip("Distancia delante de la cabeza a la que aparece, en metros.")]
        [SerializeField] private float distancia = 0.9f;

        [Tooltip("Cuánto por debajo de los ojos, en metros. Un poco abajo se lee sin forzar el cuello.")]
        [SerializeField] private float bajarRespectoOjos = 0.08f;

        [Header("Aspecto")]
        [SerializeField] private int tamanoTexto = 26;
        [SerializeField] private Color colorFondo = new Color(0.07f, 0.08f, 0.11f, 0.94f);
        [SerializeField] private Color colorPlato = new Color(1f, 0.82f, 0.45f);

        private const float EscalaCanvas = 0.001f; // 1 px = 1 mm
        private const float Ancho = 780f;
        private const float AltoLinea = 34f;

        private InputAction accion;
        private GameObject canvasGo;
        private RectTransform panel;
        private Text texto;

        public bool Abierta => canvasGo != null && canvasGo.activeSelf;

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
            // Solo en cocinas: el menú principal no tiene pedidos ni recetas que enseñar.
            if (FindAnyObjectByType<OrderManager>() == null) return;
            if (FindAnyObjectByType<RecipeGuide>() != null) return;

            new GameObject("GuiaRecetas").AddComponent<RecipeGuide>();
        }

        // ------------------------------------------------------------------- ciclo

        private void OnEnable()
        {
            accion = new InputAction("GuiaRecetas", InputActionType.Button);
            foreach (string b in bindings)
            {
                if (!string.IsNullOrEmpty(b)) accion.AddBinding(b);
            }
            accion.performed += _ => Alternar();
            accion.Enable();
        }

        private void OnDisable()
        {
            if (accion == null) return;
            accion.Disable();
            accion.Dispose();
            accion = null;
        }

        public void Alternar()
        {
            if (Abierta) Cerrar();
            else Abrir();
        }

        public void Abrir()
        {
            if (canvasGo == null) Construir();

            RellenarTexto();
            ColocarDelanteDelJugador();
            canvasGo.SetActive(true);
        }

        public void Cerrar()
        {
            if (canvasGo != null) canvasGo.SetActive(false);
        }

        // --------------------------------------------------------------- contenido

        private void RellenarTexto()
        {
            var manager = OrderManager.Instancia;
            DepartmentData depto = (manager != null) ? manager.Departamento : null;

            var sb = new StringBuilder();
            int lineas = 0;

            string titulo = (depto != null && !string.IsNullOrEmpty(depto.nombre))
                ? $"GUÍA DE RECETAS · {depto.nombre.ToUpperInvariant()}"
                : "GUÍA DE RECETAS";
            sb.Append($"<size={tamanoTexto + 8}><b>{titulo}</b></size>\n\n");
            lineas += 3;

            if (depto == null)
            {
                sb.Append("No hay departamento asignado en este nivel.\n");
                lineas++;
            }
            else
            {
                if (depto.comidas != null)
                {
                    foreach (var plato in depto.comidas) lineas += EscribirPlato(sb, plato);
                }

                if (depto.comidas == null || depto.comidas.Count == 0)
                {
                    // Sin comidas tampoco hay pedidos de comida: mejor que se note aquí.
                    sb.Append($"<color=#FF8A7A>{depto.nombre} no tiene comidas asignadas: revisa la " +
                              "lista 'Comidas' de su DepartmentData.</color>\n\n");
                    lineas += 2;
                    Debug.LogWarning($"[RecipeGuide] '{depto.name}' no tiene comidas; solo se pedirán refrescos.");
                }

                if (depto.refrescos != null)
                {
                    foreach (var refresco in depto.refrescos)
                    {
                        if (refresco == null) continue;
                        sb.Append($"<b><color=#{ColorUtility.ToHtmlStringRGB(colorPlato)}>{refresco.nombre}</color></b>" +
                                  $"  <color=#9AA3AE>({refresco.puntos} pts)</color>\n");
                        sb.Append("    • Se sirve en el dispensador de refrescos\n\n");
                        lineas += 3;
                    }
                }
            }

            sb.Append("<color=#9AA3AE><i>Pulsa Y (mando izquierdo) para cerrar</i></color>");
            lineas++;

            texto.text = sb.ToString();
            panel.sizeDelta = new Vector2(Ancho, 60f + lineas * AltoLinea);
        }

        /// <returns>Cuántas líneas ocupó, para dimensionar el panel.</returns>
        private int EscribirPlato(StringBuilder sb, DishData plato)
        {
            if (plato == null) return 0;
            int lineas = 0;

            sb.Append($"<b><color=#{ColorUtility.ToHtmlStringRGB(colorPlato)}>{plato.nombre}</color></b>" +
                      $"  <color=#9AA3AE>({plato.puntos} pts · {plato.tiempoLimite:0} s)</color>\n");
            lineas++;

            if (plato.receta != null)
            {
                foreach (var req in plato.receta)
                {
                    if (req == null || req.ingrediente == null) continue;
                    sb.Append($"    • {req.ingrediente.nombre} — {Estado(req)}\n");
                    lineas++;
                }
            }

            sb.Append(plato.entregaDirecta
                ? "    <color=#7FD18B>→ Se entrega directo en el mostrador, sin emplatar</color>\n"
                : "    <color=#7FD18B>→ Se arma en el plato de emplatado</color>\n");
            sb.Append('\n');
            return lineas + 2;
        }

        /// <summary>"en rodajas, frito", "entero, crudo"...</summary>
        private static string Estado(IngredienteRequerido req)
        {
            var partes = new List<string>(2);

            partes.Add(req.corte switch
            {
                TipoCorte.Rodajas => "en rodajas",
                TipoCorte.Cubitos => "en cubitos",
                TipoCorte.Bastones => "en bastones",
                _ => "entero",
            });

            partes.Add(!req.debeEstarCocido ? "crudo" : req.metodo switch
            {
                MetodoCoccion.Freir => "frito (sartén)",
                MetodoCoccion.Asar => "asado (parrilla)",
                _ => "hervido (olla)",
            });

            return string.Join(", ", partes);
        }

        // ------------------------------------------------------------------ interfaz

        private void Construir()
        {
            canvasGo = new GameObject("GuiaRecetas_Canvas", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            // Por delante de los carteles de cocción y los menús de los cajones.
            canvas.sortingOrder = 50;
            canvasGo.transform.localScale = Vector3.one * EscalaCanvas;

            var fondo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            fondo.transform.SetParent(canvasGo.transform, false);
            panel = fondo.GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(Ancho, 400f);
            var img = fondo.GetComponent<Image>();
            img.color = colorFondo;
            img.raycastTarget = false; // solo se lee: que no tape el rayo a lo que haya detrás

            var textoGo = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            textoGo.transform.SetParent(fondo.transform, false);
            var rt = textoGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(34f, 22f);
            rt.offsetMax = new Vector2(-34f, -22f);

            texto = textoGo.GetComponent<Text>();
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = tamanoTexto;
            texto.color = Color.white;
            texto.supportRichText = true;
            texto.alignment = TextAnchor.UpperLeft;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.lineSpacing = 1.05f;
            texto.raycastTarget = false;

            canvasGo.SetActive(false);
        }

        /// <summary>
        /// Aparece delante y se queda quieto en el mundo, no pegado a la cara: un panel que
        /// sigue a la cabeza marea en VR. Para moverlo, se cierra y se vuelve a abrir.
        /// </summary>
        private void ColocarDelanteDelJugador()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 adelante = cam.transform.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.001f) adelante = cam.transform.up; // mirando al suelo
            adelante.y = 0f;
            adelante.Normalize();

            Vector3 pos = cam.transform.position + adelante * distancia + Vector3.down * bajarRespectoOjos;
            canvasGo.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(adelante));
        }
    }
}
