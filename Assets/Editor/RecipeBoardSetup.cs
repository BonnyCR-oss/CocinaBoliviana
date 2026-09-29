using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    public static class RecipeBoardSetup
    {
        private static readonly Color ColorFondo = new Color(0.075f, 0.082f, 0.10f, 0.96f);
        private static readonly Color ColorCabecera = new Color(0.96f, 0.68f, 0.18f, 1f);
        private static readonly Color ColorTarjeta = new Color(0.12f, 0.135f, 0.17f, 0.98f);
        private static readonly Color ColorTextoTitulo = new Color(0.12f, 0.08f, 0.02f);
        private static readonly Color ColorPlatoTitulo = new Color(1f, 0.78f, 0.28f);
        private static readonly Color ColorDetalle = new Color(0.55f, 0.85f, 0.58f);

        private const float AnchoTablero = 860f;
        private const float AltoTablero = 920f;
        private const float AltoCabecera = 90f;
        private const float AltoBebida = 56f;

        [MenuItem("Kitchen/Setup Recipe Boards (en todos los niveles)")]
        public static void SetupAllBoards()
        {
            var configNiveles = new (string scenePath, string deptoPath)[]
            {
                ("Assets/00_Scenes/Nivel 1 - Cochabamba.unity", "Assets/03_SO/Departamentos/Cochabamba.asset"),
                ("Assets/00_Scenes/Nivel 2 - La Paz.unity", "Assets/03_SO/Departamentos/La paz.asset"),
                ("Assets/00_Scenes/Nivel 3 - Santa Cruz.unity", "Assets/03_SO/Departamentos/SantaCruz.asset"),
                ("Assets/00_Scenes/First Scene.unity", "Assets/03_SO/Departamentos/Cochabamba.asset")
            };

            foreach (var (scenePath, deptoPath) in configNiveles)
            {
                ConfigurarTableroEnEscena(scenePath, deptoPath);
            }

            Debug.Log("[RecipeBoardSetup] ¡Tableros de recetas configurados con éxito en todas las escenas de cocina!");
        }

        public static void ConfigurarTableroEnEscena(string scenePath, string deptoPath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[RecipeBoardSetup] No se pudo abrir {scenePath}");
                return;
            }

            var depto = AssetDatabase.LoadAssetAtPath<DepartmentData>(deptoPath);
            if (depto == null)
            {
                Debug.LogError($"[RecipeBoardSetup] No se encontró DepartmentData en {deptoPath}");
                return;
            }

            // Buscar si ya existe TableroDeRecetas y eliminarlo para rehacer limpio
            var existente = GameObject.Find("TableroDeRecetas");
            if (existente != null)
            {
                Undo.DestroyObjectImmediate(existente);
            }

            // Crear el Canvas WorldSpace
            var canvasGo = new GameObject("TableroDeRecetas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var canvasScaler = canvasGo.GetComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 1;
            canvasScaler.referencePixelsPerUnit = 100;

            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(AnchoTablero, AltoTablero);
            // Position on West Wall
            canvasGo.transform.position = new Vector3(-4.847f, 1.862f, 1.952f);
            canvasGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Looking East into the kitchen towards player
            canvasGo.transform.localScale = Vector3.one * 0.0018f;

            // Panel de Fondo oscuro
            var fondo = NuevoPanel(canvasGo.transform, "Fondo", ColorFondo, true);
            Estirar(fondo.GetComponent<RectTransform>());

            // Cabecera color ámbar
            var cabecera = NuevoPanel(fondo.transform, "Cabecera", ColorCabecera, true);
            AnclarArriba(cabecera.GetComponent<RectTransform>(), AltoCabecera, 0f, 0f);

            string nombreDepto = !string.IsNullOrEmpty(depto.nombre) ? depto.nombre.ToUpperInvariant() : "COCINA";
            var titulo = NuevoTexto(cabecera.transform, "Titulo", $"GUÍA DE RECETAS · {nombreDepto}", 38, TextAlignmentOptions.Center);
            titulo.color = ColorTextoTitulo;
            titulo.fontStyle = FontStyles.Bold;
            titulo.characterSpacing = 4f;
            Estirar(titulo.GetComponent<RectTransform>());

            // Contenedor vertical de tarjetas de platos
            var contenedorGo = new GameObject("ContenedorTarjetas", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contenedorGo.transform.SetParent(fondo.transform, false);
            var cRt = contenedorGo.GetComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.offsetMin = new Vector2(24f, AltoBebida + 20f);
            cRt.offsetMax = new Vector2(-24f, -(AltoCabecera + 16f));

            var vlg = contenedorGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            // Generar tarjetas para cada plato de este departamento
            GameObject primeraTarjeta = null;
            if (depto.comidas != null)
            {
                foreach (var plato in depto.comidas)
                {
                    if (plato == null) continue;
                    var tarjeta = CrearTarjetaPlato(contenedorGo.transform, plato);
                    if (primeraTarjeta == null) primeraTarjeta = tarjeta;
                }
            }

            // Barra inferior para bebida típica
            var barraBebida = NuevoPanel(fondo.transform, "BarraBebida", new Color(0.14f, 0.16f, 0.20f, 0.95f), true);
            AnclarAbajo(barraBebida.GetComponent<RectTransform>(), AltoBebida, 20f, 14f);

            var textoBebida = NuevoTexto(barraBebida.transform, "TextoRefresco", "", 21, TextAlignmentOptions.Center);
            textoBebida.color = new Color(0.92f, 0.94f, 0.97f);
            Estirar(textoBebida.GetComponent<RectTransform>());

            if (depto.refrescos != null && depto.refrescos.Count > 0)
            {
                var sbRefresco = new System.Text.StringBuilder();
                sbRefresco.Append("<b>BEBIDA TÍPICA:</b> ");
                for (int i = 0; i < depto.refrescos.Count; i++)
                {
                    var r = depto.refrescos[i];
                    if (r == null) continue;
                    if (i > 0) sbRefresco.Append(" · ");
                    sbRefresco.Append($"<color=#{ColorUtility.ToHtmlStringRGB(ColorPlatoTitulo)}>{r.nombre}</color> ({r.puntos} pts)");
                }
                sbRefresco.Append("  →  <i>Servir en vaso desde el dispensador</i>");
                textoBebida.text = sbRefresco.ToString();
            }

            // Agregar y configurar el componente RecipeBoard
            var recipeBoard = canvasGo.AddComponent<RecipeBoard>();
            var so = new SerializedObject(recipeBoard);
            so.FindProperty("departamento").objectReferenceValue = depto;
            so.FindProperty("textoTitulo").objectReferenceValue = titulo;
            so.FindProperty("contenedorTarjetas").objectReferenceValue = contenedorGo.transform;
            if (primeraTarjeta != null)
            {
                so.FindProperty("plantillaTarjetaPlato").objectReferenceValue = primeraTarjeta;
            }
            so.FindProperty("textoRefresco").objectReferenceValue = textoBebida;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RecipeBoardSetup] Escena guardada con éxito: {scene.name}");
        }

        private static GameObject CrearTarjetaPlato(Transform padre, DishData plato)
        {
            var tarjetaGo = NuevoPanel(padre, $"Tarjeta_{plato.nombre}", ColorTarjeta, true);
            var tRt = tarjetaGo.GetComponent<RectTransform>();

            // Calcular altura adecuada según cantidad de ingredientes
            int numIngredientes = (plato.receta != null) ? plato.receta.Count : 0;
            float alturaTarjeta = 64f + (numIngredientes * 27f) + 38f;
            tRt.sizeDelta = new Vector2(0f, alturaTarjeta);

            // Borde lateral ámbar decorativo
            var bordeAcento = NuevoPanel(tarjetaGo.transform, "BordeAcento", ColorCabecera, true);
            var bRt = bordeAcento.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(6f, 0f);
            bRt.sizeDelta = new Vector2(8f, -16f);

            // Título del plato
            var tituloPlato = NuevoTexto(tarjetaGo.transform, "TituloPlato",
                $"<b>{plato.nombre.ToUpperInvariant()}</b>  <size=22><color=#A0AAB8>({plato.puntos} pts · {plato.tiempoLimite:0}s)</color></size>",
                28, TextAlignmentOptions.Left);
            tituloPlato.color = ColorPlatoTitulo;
            var titRt = tituloPlato.GetComponent<RectTransform>();
            titRt.anchorMin = new Vector2(0f, 1f);
            titRt.anchorMax = new Vector2(1f, 1f);
            titRt.pivot = new Vector2(0.5f, 1f);
            titRt.anchoredPosition = new Vector2(26f, -12f);
            titRt.sizeDelta = new Vector2(-40f, 34f);

            // Lista de ingredientes formateados
            var textoReceta = NuevoTexto(tarjetaGo.transform, "TextoReceta", "", 21, TextAlignmentOptions.TopLeft);
            textoReceta.color = Color.white;
            textoReceta.lineSpacing = 6f;
            var recRt = textoReceta.GetComponent<RectTransform>();
            recRt.anchorMin = new Vector2(0f, 1f);
            recRt.anchorMax = new Vector2(1f, 1f);
            recRt.pivot = new Vector2(0.5f, 1f);
            recRt.anchoredPosition = new Vector2(26f, -48f);
            recRt.sizeDelta = new Vector2(-40f, numIngredientes * 27f);

            if (plato.receta != null)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var req in plato.receta)
                {
                    if (req == null || req.ingrediente == null) continue;
                    string estadoStr = RecipeBoard.FormatearEstado(req);
                    sb.AppendLine($"• <b>{req.ingrediente.nombre}</b>: <color=#{ColorUtility.ToHtmlStringRGB(ColorDetalle)}>{estadoStr}</color>");
                }
                textoReceta.text = sb.ToString().TrimEnd();
            }

            // Indicador de emplatado / entrega
            string entregaStr = plato.entregaDirecta
                ? "<color=#F5AE2E>⚡ ENTREGA DIRECTA EN MOSTRADOR (sin plato)</color>"
                : "<color=#7FD18B>🍽️ ARMAR EN PLATO DE EMPLATADO</color>";

            var textoEntrega = NuevoTexto(tarjetaGo.transform, "TextoEntrega", entregaStr, 19, TextAlignmentOptions.Left);
            textoEntrega.fontStyle = FontStyles.Bold;
            var entRt = textoEntrega.GetComponent<RectTransform>();
            entRt.anchorMin = new Vector2(0f, 0f);
            entRt.anchorMax = new Vector2(1f, 0f);
            entRt.pivot = new Vector2(0.5f, 0f);
            entRt.anchoredPosition = new Vector2(26f, 10f);
            entRt.sizeDelta = new Vector2(-40f, 26f);

            return tarjetaGo;
        }

        private static GameObject NuevoPanel(Transform padre, string nombre, Color color, bool redondeado)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            if (redondeado)
            {
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = false;
            return go;
        }

        private static TextMeshProUGUI NuevoTexto(Transform padre, string nombre, string contenido,
                                                  int tamano, TextAlignmentOptions alineacion)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = contenido;
            t.fontSize = tamano;
            t.alignment = alineacion;
            t.color = Color.white;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AnclarArriba(RectTransform rt, float alto, float margenX, float desdeArriba)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-margenX * 2f, alto);
            rt.anchoredPosition = new Vector2(0f, -desdeArriba);
        }

        private static void AnclarAbajo(RectTransform rt, float alto, float margenX, float desdeAbajo)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-margenX * 2f, alto);
            rt.anchoredPosition = new Vector2(0f, desdeAbajo);
        }
    }
}
