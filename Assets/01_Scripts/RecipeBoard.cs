using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Tablero físico de recetas montado en la pared de la cocina (World-Space).
    /// Muestra al jugador los platos disponibles en el nivel actual y el paso a paso
    /// de preparación para cada ingrediente (corte, método de cocción y forma de entrega).
    /// </summary>
    public class RecipeBoard : MonoBehaviour
    {
        [Header("Datos")]
        [Tooltip("Departamento del que se leen las recetas. Si se deja vacío, se lee automáticamente de OrderManager o LevelManager.")]
        [SerializeField] private DepartmentData departamento;

        [Header("Referencias UI")]
        [SerializeField] private TextMeshProUGUI textoTitulo;
        [SerializeField] private Transform contenedorTarjetas;
        [SerializeField] private GameObject plantillaTarjetaPlato;
        [SerializeField] private TextMeshProUGUI textoRefresco;

        [Header("Colores")]
        [SerializeField] private Color colorFondo = new Color(0.075f, 0.082f, 0.10f, 0.96f);
        [SerializeField] private Color colorCabecera = new Color(0.96f, 0.68f, 0.18f, 1f);
        [SerializeField] private Color colorTarjeta = new Color(0.12f, 0.135f, 0.17f, 0.98f);
        [SerializeField] private Color colorTextoTitulo = new Color(0.12f, 0.08f, 0.02f);
        [SerializeField] private Color colorPlato = new Color(1f, 0.78f, 0.28f);
        [SerializeField] private Color colorIngrediente = Color.white;
        [SerializeField] private Color colorDetalle = new Color(0.55f, 0.85f, 0.58f);

        // ------------------------------------------------------------- Auto-instalación en Runtime
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstalarEnCocina()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedCheck;
            SceneManager.sceneLoaded += OnSceneLoadedCheck;
            CheckAndBuild(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoadedCheck(Scene escena, LoadSceneMode modo)
        {
            CheckAndBuild(escena);
        }

        private static void CheckAndBuild(Scene escena)
        {
            // Solo en escenas con OrderManager (cocinas)
            if (FindAnyObjectByType<OrderManager>() == null) return;
            if (FindAnyObjectByType<RecipeBoard>() != null) return;

            ConstruirTableroRuntime();
        }

        // ------------------------------------------------------------- Ciclo de Vida
        private void Start()
        {
            Refrescar();
        }

        public void SetDepartamento(DepartmentData nuevoDepto)
        {
            departamento = nuevoDepto;
            Refrescar();
        }

        public void Refrescar()
        {
            DepartmentData depto = departamento;
            if (depto == null && OrderManager.Instancia != null)
            {
                depto = OrderManager.Instancia.Departamento;
            }
            if (depto == null && LevelSelection.Elegido != null)
            {
                depto = LevelSelection.Elegido.departamento;
            }

            if (depto == null) return;

            if (textoTitulo != null)
            {
                string nombreDepto = !string.IsNullOrEmpty(depto.nombre) ? depto.nombre.ToUpperInvariant() : "COCINA";
                textoTitulo.text = $"GUÍA DE RECETAS · {nombreDepto}";
            }

            // Refresco de comidas si hay contenedor dinámico
            if (contenedorTarjetas != null && plantillaTarjetaPlato != null)
            {
                // Limpiar instancias previas excepto la plantilla
                foreach (Transform child in contenedorTarjetas)
                {
                    if (child.gameObject != plantillaTarjetaPlato)
                        Destroy(child.gameObject);
                }
                plantillaTarjetaPlato.SetActive(false);

                if (depto.comidas != null)
                {
                    foreach (var plato in depto.comidas)
                    {
                        if (plato == null) continue;
                        CrearTarjetaPlato(plato);
                    }
                }
            }

            // Bebidas
            if (textoRefresco != null && depto.refrescos != null && depto.refrescos.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("<b>BEBIDA TÍPICA:</b> ");
                for (int i = 0; i < depto.refrescos.Count; i++)
                {
                    var r = depto.refrescos[i];
                    if (r == null) continue;
                    if (i > 0) sb.Append(" · ");
                    sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(colorPlato)}>{r.nombre}</color> ({r.puntos} pts)");
                }
                sb.Append("  →  <i>Servir en vaso desde el dispensador</i>");
                textoRefresco.text = sb.ToString();
            }
        }

        private void CrearTarjetaPlato(DishData plato)
        {
            GameObject tarjeta = Instantiate(plantillaTarjetaPlato, contenedorTarjetas);
            tarjeta.SetActive(true);

            var textoNombre = tarjeta.transform.Find("TituloPlato")?.GetComponent<TextMeshProUGUI>();
            var textoReceta = tarjeta.transform.Find("TextoReceta")?.GetComponent<TextMeshProUGUI>();
            var textoEntrega = tarjeta.transform.Find("TextoEntrega")?.GetComponent<TextMeshProUGUI>();

            if (textoNombre != null)
            {
                textoNombre.text = $"<b>{plato.nombre.ToUpperInvariant()}</b>  <size=20><color=#9AA3AE>({plato.puntos} pts · {plato.tiempoLimite:0}s)</color></size>";
            }

            if (textoReceta != null)
            {
                var sb = new StringBuilder();
                if (plato.receta != null)
                {
                    foreach (var req in plato.receta)
                    {
                        if (req == null || req.ingrediente == null) continue;
                        string estadoStr = FormatearEstado(req);
                        sb.AppendLine($"• <b>{req.ingrediente.nombre}</b>: <color=#{ColorUtility.ToHtmlStringRGB(colorDetalle)}>{estadoStr}</color>");
                    }
                }
                textoReceta.text = sb.ToString().TrimEnd();
            }

            if (textoEntrega != null)
            {
                textoEntrega.text = plato.entregaDirecta
                    ? "<color=#F5AE2E>⚡ ENTREGA DIRECTA EN MOSTRADOR (sin plato)</color>"
                    : "<color=#7FD18B>🍽️ ARMAR EN PLATO DE EMPLATADO</color>";
            }
        }

        public static string FormatearEstado(IngredienteRequerido req)
        {
            string corteStr = req.corte switch
            {
                TipoCorte.Rodajas => "en rodajas",
                TipoCorte.Cubitos => "en cubitos",
                TipoCorte.Bastones => "en bastones",
                _ => "entero"
            };

            string coccionStr = !req.debeEstarCocido ? "crudo" : req.metodo switch
            {
                MetodoCoccion.Freir => "frito (sartén)",
                MetodoCoccion.Asar => "asado (parrilla)",
                _ => "hervido (olla)"
            };

            return $"{corteStr}, {coccionStr}";
        }

        // ------------------------------------------------------------- Constructor en Runtime
        public static RecipeBoard ConstruirTableroRuntime()
        {
            var canvasGo = new GameObject("TableroDeRecetas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1;
            scaler.referencePixelsPerUnit = 100;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860f, 920f);
            canvasGo.transform.position = new Vector3(-4.847f, 1.862f, 1.952f);
            canvasGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0018f;

            // Fondo
            var fondoGo = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
            fondoGo.transform.SetParent(canvasGo.transform, false);
            var fRt = fondoGo.GetComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;
            var fImg = fondoGo.GetComponent<Image>();
            fImg.color = new Color(0.075f, 0.082f, 0.10f, 0.96f);
            fImg.raycastTarget = false;

            // Cabecera
            var cabeceraGo = new GameObject("Cabecera", typeof(RectTransform), typeof(Image));
            cabeceraGo.transform.SetParent(fondoGo.transform, false);
            var cabRt = cabeceraGo.GetComponent<RectTransform>();
            cabRt.anchorMin = new Vector2(0f, 1f); cabRt.anchorMax = new Vector2(1f, 1f);
            cabRt.pivot = new Vector2(0.5f, 1f);
            cabRt.sizeDelta = new Vector2(0f, 90f);
            cabRt.anchoredPosition = Vector2.zero;
            var cabImg = cabeceraGo.GetComponent<Image>();
            cabImg.color = new Color(0.96f, 0.68f, 0.18f, 1f);
            cabImg.raycastTarget = false;

            // Texto Título
            var tituloGo = new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI));
            tituloGo.transform.SetParent(cabeceraGo.transform, false);
            var titRt = tituloGo.GetComponent<RectTransform>();
            titRt.anchorMin = Vector2.zero; titRt.anchorMax = Vector2.one;
            titRt.offsetMin = Vector2.zero; titRt.offsetMax = Vector2.zero;
            var titTmp = tituloGo.GetComponent<TextMeshProUGUI>();
            titTmp.text = "GUÍA DE RECETAS";
            titTmp.fontSize = 38;
            titTmp.alignment = TextAlignmentOptions.Center;
            titTmp.color = new Color(0.12f, 0.08f, 0.02f);
            titTmp.fontStyle = FontStyles.Bold;
            titTmp.characterSpacing = 4f;
            titTmp.raycastTarget = false;

            // Contenedor de Tarjetas
            var contGo = new GameObject("ContenedorTarjetas", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contGo.transform.SetParent(fondoGo.transform, false);
            var cRt = contGo.GetComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = new Vector2(24f, 76f);
            cRt.offsetMax = new Vector2(-24f, -106f);

            var vlg = contGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            // Plantilla Tarjeta
            var plantillaGo = new GameObject("PlantillaTarjeta", typeof(RectTransform), typeof(Image));
            plantillaGo.transform.SetParent(contGo.transform, false);
            var pRt = plantillaGo.GetComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(0f, 200f);
            var pImg = plantillaGo.GetComponent<Image>();
            pImg.color = new Color(0.12f, 0.135f, 0.17f, 0.98f);
            pImg.raycastTarget = false;

            // Borde ámbar en tarjeta
            var bordeGo = new GameObject("BordeAcento", typeof(RectTransform), typeof(Image));
            bordeGo.transform.SetParent(plantillaGo.transform, false);
            var bRt = bordeGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f); bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(6f, 0f);
            bRt.sizeDelta = new Vector2(8f, -16f);
            bordeGo.GetComponent<Image>().color = new Color(0.96f, 0.68f, 0.18f, 1f);

            // TituloPlato
            var tPlatoGo = new GameObject("TituloPlato", typeof(RectTransform), typeof(TextMeshProUGUI));
            tPlatoGo.transform.SetParent(plantillaGo.transform, false);
            var tpRt = tPlatoGo.GetComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0f, 1f); tpRt.anchorMax = new Vector2(1f, 1f);
            tpRt.pivot = new Vector2(0.5f, 1f);
            tpRt.anchoredPosition = new Vector2(26f, -12f);
            tpRt.sizeDelta = new Vector2(-40f, 34f);
            var tpTmp = tPlatoGo.GetComponent<TextMeshProUGUI>();
            tpTmp.fontSize = 28;
            tpTmp.color = new Color(1f, 0.78f, 0.28f);
            tpTmp.raycastTarget = false;

            // TextoReceta
            var tRecGo = new GameObject("TextoReceta", typeof(RectTransform), typeof(TextMeshProUGUI));
            tRecGo.transform.SetParent(plantillaGo.transform, false);
            var trRt = tRecGo.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0f, 1f); trRt.anchorMax = new Vector2(1f, 1f);
            trRt.pivot = new Vector2(0.5f, 1f);
            trRt.anchoredPosition = new Vector2(26f, -48f);
            trRt.sizeDelta = new Vector2(-40f, 120f);
            var trTmp = tRecGo.GetComponent<TextMeshProUGUI>();
            trTmp.fontSize = 21;
            trTmp.color = Color.white;
            trTmp.textWrappingMode = TextWrappingModes.Normal;
            trTmp.lineSpacing = 6f;
            trTmp.raycastTarget = false;

            // TextoEntrega
            var tEntGo = new GameObject("TextoEntrega", typeof(RectTransform), typeof(TextMeshProUGUI));
            tEntGo.transform.SetParent(plantillaGo.transform, false);
            var teRt = tEntGo.GetComponent<RectTransform>();
            teRt.anchorMin = new Vector2(0f, 0f); teRt.anchorMax = new Vector2(1f, 0f);
            teRt.pivot = new Vector2(0.5f, 0f);
            teRt.anchoredPosition = new Vector2(26f, 10f);
            teRt.sizeDelta = new Vector2(-40f, 26f);
            var teTmp = tEntGo.GetComponent<TextMeshProUGUI>();
            teTmp.fontSize = 19;
            teTmp.fontStyle = FontStyles.Bold;
            teTmp.raycastTarget = false;

            // Barra inferior bebida
            var barraBebidaGo = new GameObject("BarraBebida", typeof(RectTransform), typeof(Image));
            barraBebidaGo.transform.SetParent(fondoGo.transform, false);
            var bbRt = barraBebidaGo.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f); bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.sizeDelta = new Vector2(-40f, 56f);
            bbRt.anchoredPosition = new Vector2(0f, 14f);
            barraBebidaGo.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.20f, 0.95f);

            var textoRefrescoGo = new GameObject("TextoRefresco", typeof(RectTransform), typeof(TextMeshProUGUI));
            textoRefrescoGo.transform.SetParent(barraBebidaGo.transform, false);
            var trefRt = textoRefrescoGo.GetComponent<RectTransform>();
            trefRt.anchorMin = Vector2.zero; trefRt.anchorMax = Vector2.one;
            trefRt.offsetMin = Vector2.zero; trefRt.offsetMax = Vector2.zero;
            var trefTmp = textoRefrescoGo.GetComponent<TextMeshProUGUI>();
            trefTmp.fontSize = 21;
            trefTmp.color = new Color(0.92f, 0.94f, 0.97f);
            trefTmp.alignment = TextAlignmentOptions.Center;
            trefTmp.raycastTarget = false;

            // Componente RecipeBoard
            var board = canvasGo.AddComponent<RecipeBoard>();
            board.textoTitulo = titTmp;
            board.contenedorTarjetas = contGo.transform;
            board.plantillaTarjetaPlato = plantillaGo;
            board.textoRefresco = trefTmp;

            board.Refrescar();
            return board;
        }
    }
}
