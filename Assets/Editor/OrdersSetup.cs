using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Monta el sistema de pedidos: el gestor, el tablero de comandas en la pared oeste
    /// sobre la estación de entrega, y el mostrador que recibe los platos.
    /// </summary>
    public static class OrdersSetup
    {
        private const string ScenePath = "Assets/Scenes/First Scene.unity";
        private const string DepartamentosFolder = "Assets/03_SO/Departamentos";

        [MenuItem("Kitchen/Setup Orders (pedidos y entrega)")]
        public static void SetupOrders()
        {
            Debug.Log("[OrdersSetup] Configurando pedidos...");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[OrdersSetup] No se pudo abrir {ScenePath}");
                return;
            }

            GameObject entrega = GameObject.Find("DeliveryStation");
            if (entrega == null)
            {
                Debug.LogError("[OrdersSetup] No encontré 'DeliveryStation' en la escena.");
                return;
            }

            OrderBoard tablero = ConstruirTableroSiFalta(entrega);
            ConfigurarGestor(tablero);
            ConfigurarMostrador(entrega);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[OrdersSetup] Listo. Tablero en la pared oeste y entrega en la cinta.");
        }

        private static void ConfigurarGestor(OrderBoard tablero)
        {
            GameObject go = GameObject.Find("OrderManager");
            if (go == null) go = new GameObject("OrderManager");

            var manager = go.GetComponent<OrderManager>();
            if (manager == null) manager = go.AddComponent<OrderManager>();

            var so = new SerializedObject(manager);
            so.FindProperty("tablero").objectReferenceValue = tablero;

            // El departamento es el menú del que salen los pedidos.
            if (so.FindProperty("departamento").objectReferenceValue == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:DepartmentData", new[] { DepartamentosFolder });
                if (guids.Length > 0)
                {
                    var dep = AssetDatabase.LoadAssetAtPath<DepartmentData>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    so.FindProperty("departamento").objectReferenceValue = dep;
                    Debug.Log($"[OrdersSetup] Departamento asignado: '{dep.nombre}' " +
                              $"({dep.comidas.Count} comida(s), {dep.refrescos.Count} refresco(s)).");
                }
                else
                {
                    Debug.LogWarning($"[OrdersSetup] No hay ningún DepartmentData en {DepartamentosFolder}; " +
                                     "asigna uno a mano en el OrderManager o no habrá pedidos.");
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurarMostrador(GameObject entrega)
        {
            var counter = entrega.GetComponent<DeliveryCounter>();
            if (counter == null) counter = entrega.AddComponent<DeliveryCounter>();

            var so = new SerializedObject(counter);
            Transform campana = entrega.transform.Find("Campana_Servicio");
            if (campana != null) so.FindProperty("campana").objectReferenceValue = campana;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[OrdersSetup] DeliveryCounter listo en 'DeliveryStation'." +
                      (campana == null ? " (sin Campana_Servicio, no habrá saltito)" : ""));
        }

        // Paleta: carbón, ámbar de acento y papel crema para los tickets.
        private static readonly Color ColorFondo = new Color(0.075f, 0.082f, 0.10f, 0.96f);
        private static readonly Color ColorAcento = new Color(0.96f, 0.68f, 0.18f, 1f);
        private static readonly Color ColorTicket = new Color(0.97f, 0.96f, 0.93f, 1f);
        private static readonly Color ColorTinta = new Color(0.13f, 0.13f, 0.15f, 1f);

        private const float AltoCabecera = 104f;
        private const float AltoTicket = 200f;
        private const float AltoFila = 62f;
        private const float AltoBarra = 16f;
        private const float AnchoFranja = 10f;

        /// <summary>
        /// Tablero en la pared oeste, encima de la entrega. Solo se coloca al crearlo:
        /// si lo mueves, se respeta.
        /// </summary>
        private static OrderBoard ConstruirTableroSiFalta(GameObject entrega)
        {
            // Si ya hay uno, se comprueba que sea de esta versión. Antes se respetaba a
            // ciegas y un tablero viejo nunca recibía las mejoras.
            Vector3 posicionPrevia = Vector3.zero;
            Quaternion rotacionPrevia = Quaternion.identity;
            Vector3 escalaPrevia = Vector3.one;
            bool habiaUno = false;

            var existente = Object.FindAnyObjectByType<OrderBoard>();
            if (existente != null)
            {
                if (existente.transform.Find("Version_4") != null)
                {
                    Debug.Log("[OrdersSetup] Ya hay un tablero al día; se deja como está.");
                    return existente;
                }

                habiaUno = true;
                posicionPrevia = existente.transform.position;
                rotacionPrevia = existente.transform.rotation;
                escalaPrevia = existente.transform.localScale;
                Undo.DestroyObjectImmediate(existente.gameObject);
                Debug.Log("[OrdersSetup] Tablero desactualizado; se rehace conservando su sitio.");
            }

            var canvasGo = new GameObject("TableroDePedidos",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(820, 900);
            canvasGo.transform.localScale = Vector3.one * 0.0016f;

            new GameObject("Version_4", typeof(RectTransform)).transform.SetParent(canvasGo.transform, false);

            if (habiaUno)
            {
                canvasGo.transform.SetPositionAndRotation(posicionPrevia, rotacionPrevia);
                canvasGo.transform.localScale = escalaPrevia;
            }
            else
            {
                canvasGo.transform.position = new Vector3(-4.85f, 2.05f, entrega.transform.position.z);
                canvasGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }

            var fondo = NuevoPanel(canvasGo.transform, "Fondo", ColorFondo, redondeado: true);
            Estirar(fondo.GetComponent<RectTransform>());

            // Cabecera ámbar: da jerarquía y separa el título de los tickets.
            var cabecera = NuevoPanel(fondo.transform, "Cabecera", ColorAcento, redondeado: true);
            AnclarArriba(cabecera.GetComponent<RectTransform>(), AltoCabecera, 0f, 0f);

            var titulo = NuevoTexto(cabecera.transform, "Titulo", "PEDIDOS", 50, TextAlignmentOptions.Left);
            titulo.color = new Color(0.12f, 0.08f, 0.02f);
            titulo.fontStyle = FontStyles.Bold;
            titulo.characterSpacing = 10f;
            var tRt = titulo.GetComponent<RectTransform>();
            Estirar(tRt);
            tRt.offsetMin = new Vector2(34f, 0f);
            tRt.offsetMax = new Vector2(-34f, 0f);

            var puntos = NuevoTexto(cabecera.transform, "Puntos", "0", 50, TextAlignmentOptions.Right);
            puntos.color = new Color(0.12f, 0.08f, 0.02f);
            puntos.fontStyle = FontStyles.Bold;
            var pRt = puntos.GetComponent<RectTransform>();
            Estirar(pRt);
            pRt.offsetMin = new Vector2(34f, 0f);
            pRt.offsetMax = new Vector2(-34f, 0f);

            var vacio = NuevoTexto(fondo.transform, "SinPedidos", "Sin pedidos", 32, TextAlignmentOptions.Center);
            vacio.color = new Color(0.42f, 0.43f, 0.47f);
            vacio.fontStyle = FontStyles.Italic;
            var vRt = vacio.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0f, 0.40f);
            vRt.anchorMax = new Vector2(1f, 0.55f);
            Margen(vRt, 20f);

            var listaGo = new GameObject("Lista", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listaGo.transform.SetParent(fondo.transform, false);
            var lRt = listaGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(22f, 22f);
            lRt.offsetMax = new Vector2(-22f, -(AltoCabecera + 18f));

            var vlg = listaGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            GameObject plantilla = ConstruirPlantillaTicket(listaGo.transform);

            var board = canvasGo.AddComponent<OrderBoard>();
            var so = new SerializedObject(board);
            so.FindProperty("contenedor").objectReferenceValue = lRt;
            so.FindProperty("plantillaTicket").objectReferenceValue = plantilla;
            so.FindProperty("textoPuntos").objectReferenceValue = puntos;
            so.FindProperty("textoVacio").objectReferenceValue = vacio;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[OrdersSetup] Tablero creado en la pared oeste sobre la entrega.");
            return board;
        }

        /// <summary>
        /// El ticket: tarjeta de papel con una franja lateral de color según la urgencia,
        /// una fila por elemento (icono + nombre) y una barra de tiempo fina abajo.
        ///
        /// Todo en PÍXELES exactos. OrderBoard busca los hijos por nombre ("Franja", "Fila0",
        /// "Fila0/Icono", "Fila0/Texto", "Barra/Relleno"): no se renombran a la ligera.
        /// </summary>
        private static GameObject ConstruirPlantillaTicket(Transform padre)
        {
            var ticket = NuevoPanel(padre, "PlantillaTicket", ColorTicket, redondeado: true);
            ticket.AddComponent<LayoutElement>().preferredHeight = AltoTicket;

            // Franja de urgencia pegada al borde izquierdo: se lee de un vistazo aunque no
            // te fijes en la barra fina de abajo.
            var franja = NuevoPanel(ticket.transform, "Franja", ColorAcento, redondeado: true);
            var frRt = franja.GetComponent<RectTransform>();
            frRt.anchorMin = new Vector2(0f, 0f);
            frRt.anchorMax = new Vector2(0f, 1f);
            frRt.pivot = new Vector2(0f, 0.5f);
            frRt.sizeDelta = new Vector2(AnchoFranja, -20f);
            frRt.anchoredPosition = new Vector2(10f, 0f);

            const float margenIzq = AnchoFranja + 28f;
            float anchoUtil = -(margenIzq + 22f);
            float centrado = (margenIzq - 22f) * 0.5f;

            for (int i = 0; i < OrderBoard.FilasPorTicket; i++)
            {
                var fila = new GameObject("Fila" + i, typeof(RectTransform));
                fila.transform.SetParent(ticket.transform, false);
                var fRt = fila.GetComponent<RectTransform>();
                fRt.anchorMin = new Vector2(0f, 1f);
                fRt.anchorMax = new Vector2(1f, 1f);
                fRt.pivot = new Vector2(0.5f, 1f);
                fRt.sizeDelta = new Vector2(anchoUtil, AltoFila);
                fRt.anchoredPosition = new Vector2(centrado, -(14f + i * (AltoFila + 6f)));

                // Cuadro del icono con fondo propio, para que el sprite del plato se vea
                // limpio aunque venga con fondo blanco.
                var marco = NuevoPanel(fila.transform, "Icono", Color.white, redondeado: false);
                var iRt = marco.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0f, 0f);
                iRt.anchorMax = new Vector2(0f, 1f);
                iRt.pivot = new Vector2(0f, 0.5f);
                iRt.sizeDelta = new Vector2(AltoFila, 0f);
                iRt.anchoredPosition = Vector2.zero;
                marco.GetComponent<Image>().preserveAspect = true;

                var texto = NuevoTexto(fila.transform, "Texto", "Plato", 34, TextAlignmentOptions.Left);
                texto.color = ColorTinta;
                texto.fontStyle = FontStyles.Bold;
                var txRt = texto.GetComponent<RectTransform>();
                Estirar(txRt);
                txRt.offsetMin = new Vector2(AltoFila + 18f, 0f);
                txRt.offsetMax = new Vector2(-10f, 0f);
            }

            var barra = NuevoPanel(ticket.transform, "Barra", new Color(0f, 0f, 0f, 0.12f), redondeado: true);
            var bRt = barra.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.sizeDelta = new Vector2(anchoUtil, AltoBarra);
            bRt.anchoredPosition = new Vector2(centrado, 14f);

            var relleno = NuevoPanel(barra.transform, "Relleno", ColorAcento, redondeado: true);
            Estirar(relleno.GetComponent<RectTransform>());
            var img = relleno.GetComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;

            return ticket;
        }

        /// <summary>Banda de alto fijo pegada al borde superior del padre.</summary>
        private static void AnclarArriba(RectTransform rt, float alto, float desdeArriba, float margenLateral)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-margenLateral * 2f, alto);
            rt.anchoredPosition = new Vector2(0f, -desdeArriba);
        }

        private static GameObject NuevoPanel(Transform padre, string nombre, Color color, bool redondeado)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);

            var img = go.GetComponent<Image>();
            img.color = color;
            if (redondeado)
            {
                // El sprite integrado de Unity trae esquinas redondeadas y bordes 9-slice,
                // así que escala sin deformarse.
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
            }
            return go;
        }

        private static TextMeshProUGUI NuevoTexto(Transform padre, string nombre, string contenido,
                                                  int tamano, TextAlignmentOptions alineacion)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);

            // TextMeshPro y no Text: en world space el texto legacy se ve borroso al
            // acercarse, y TMP es nítido a cualquier distancia por ser SDF.
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = contenido;
            t.fontSize = tamano;
            t.alignment = alineacion;
            t.color = Color.white;
            t.overflowMode = TextOverflowModes.Truncate;
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

        private static void Margen(RectTransform rt, float m)
        {
            rt.offsetMin = new Vector2(m, m);
            rt.offsetMax = new Vector2(-m, -m);
        }
    }
}
