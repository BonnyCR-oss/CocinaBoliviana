using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

        /// <summary>
        /// Tablero en la pared oeste, encima de la entrega. Solo se coloca al crearlo:
        /// si lo mueves, se respeta.
        /// </summary>
        private static OrderBoard ConstruirTableroSiFalta(GameObject entrega)
        {
            // Si ya hay uno, se comprueba que su plantilla sea la de esta version. Antes se
            // respetaba a ciegas, asi que un tablero viejo nunca recibia las mejoras y habia
            // que acordarse de borrarlo a mano.
            Vector3 posicionPrevia = Vector3.zero;
            Quaternion rotacionPrevia = Quaternion.identity;
            Vector3 escalaPrevia = Vector3.one;
            bool habiaUno = false;

            var existente = Object.FindAnyObjectByType<OrderBoard>();
            if (existente != null)
            {
                bool actualizado = existente.transform.Find("Version_3") != null;
                if (actualizado)
                {
                    Debug.Log("[OrdersSetup] Ya hay un tablero al día; se deja como está.");
                    return existente;
                }

                // Se conserva donde lo hayas puesto, solo se rehace su contenido.
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
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 700);
            canvasGo.transform.localScale = Vector3.one * 0.0018f;

            // Pegado a la pared oeste (x = -4.95), mirando al centro de la cocina.
            // Y = -90, no 90: un canvas se lee cuando su forward (+Z) apunta AL CONTRARIO
            // del observador. Con 90 el +Z va hacia +X, o sea hacia el jugador, y se ve
            // la cara de atras: todo oscuro y el texto ilegible.
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

            // Marcador de version: si falta, el setup sabe que el tablero es viejo y lo rehace.
            new GameObject("Version_3", typeof(RectTransform)).transform.SetParent(canvasGo.transform, false);

            var fondo = NuevoPanel(canvasGo.transform, "Fondo", new Color(0.09f, 0.09f, 0.11f, 0.92f));
            Estirar(fondo.GetComponent<RectTransform>());

            Text titulo = NuevoTexto(fondo.transform, "Titulo", "PEDIDOS", 54, TextAnchor.MiddleCenter);
            var tRt = titulo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.88f);
            tRt.anchorMax = new Vector2(1f, 1f);
            Margen(tRt, 20f);

            Text puntos = NuevoTexto(fondo.transform, "Puntos", "Puntos: 0", 38, TextAnchor.MiddleCenter);
            puntos.color = new Color(1f, 0.85f, 0.4f);
            var pRt = puntos.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0.78f);
            pRt.anchorMax = new Vector2(1f, 0.88f);
            Margen(pRt, 20f);

            Text vacio = NuevoTexto(fondo.transform, "SinPedidos", "Sin pedidos", 34, TextAnchor.MiddleCenter);
            vacio.color = new Color(0.6f, 0.6f, 0.6f);
            var vRt = vacio.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0f, 0.35f);
            vRt.anchorMax = new Vector2(1f, 0.5f);
            Margen(vRt, 20f);

            var listaGo = new GameObject("Lista", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listaGo.transform.SetParent(fondo.transform, false);
            var lRt = listaGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f);
            lRt.anchorMax = new Vector2(1f, 0.78f);
            Margen(lRt, 24f);
            var vlg = listaGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            // Sin estas dos, el ticket se queda con los 100 px por defecto de un
            // RectTransform nuevo y el nombre del plato no cabe: solo se ve la vineta.
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

        private const float AltoTicket = 230f;
        private const float AltoFila = 66f;
        private const float AltoBarra = 26f;

        /// <summary>
        /// La plantilla de ticket: dos filas de icono + nombre, y debajo la barra de tiempo.
        ///
        /// Todo en PIXELES exactos, no en porcentajes con margen. Antes la barra se anclaba al
        /// 4-18% de la altura (26 px) y encima se le restaban 14 arriba y 14 abajo: quedaba de
        /// altura NEGATIVA y no se dibujaba. A las filas les pasaba lo mismo y la fuente no
        /// cabia en la linea, asi que solo se veia el icono.
        ///
        /// OrderBoard busca los hijos por nombre ("Fila0", "Fila0/Icono", "Fila0/Texto",
        /// "Barra/Relleno"), asi que esos nombres no se pueden cambiar a la ligera.
        /// </summary>
        private static GameObject ConstruirPlantillaTicket(Transform padre)
        {
            var ticket = NuevoPanel(padre, "PlantillaTicket", new Color(1f, 0.98f, 0.9f, 0.96f));
            var le = ticket.AddComponent<LayoutElement>();
            le.preferredHeight = AltoTicket;

            for (int i = 0; i < OrderBoard.FilasPorTicket; i++)
            {
                var fila = new GameObject("Fila" + i, typeof(RectTransform));
                fila.transform.SetParent(ticket.transform, false);

                var fRt = fila.GetComponent<RectTransform>();
                AnclarArriba(fRt, AltoFila, desdeArriba: 12f + i * (AltoFila + 8f), margenLateral: 16f);

                var icono = new GameObject("Icono", typeof(RectTransform), typeof(Image));
                icono.transform.SetParent(fila.transform, false);
                var iRt = icono.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0f, 0f);
                iRt.anchorMax = new Vector2(0f, 1f);
                iRt.pivot = new Vector2(0f, 0.5f);
                iRt.sizeDelta = new Vector2(AltoFila, 0f); // cuadrado, del alto de la fila
                iRt.anchoredPosition = Vector2.zero;
                icono.GetComponent<Image>().preserveAspect = true;

                // Fuente holgada dentro de la fila: 34 px de texto en 66 px de alto.
                Text texto = NuevoTexto(fila.transform, "Texto", "Plato", 34, TextAnchor.MiddleLeft);
                texto.color = new Color(0.12f, 0.12f, 0.12f);
                var txRt = texto.GetComponent<RectTransform>();
                txRt.anchorMin = Vector2.zero;
                txRt.anchorMax = Vector2.one;
                txRt.offsetMin = new Vector2(AltoFila + 14f, 0f); // hueco del icono
                txRt.offsetMax = new Vector2(-8f, 0f);
            }

            var barra = NuevoPanel(ticket.transform, "Barra", new Color(0f, 0f, 0f, 0.18f));
            AnclarAbajo(barra.GetComponent<RectTransform>(), AltoBarra, desdeAbajo: 14f, margenLateral: 16f);

            var relleno = NuevoPanel(barra.transform, "Relleno", new Color(0.35f, 0.85f, 0.4f));
            Estirar(relleno.GetComponent<RectTransform>());
            var img = relleno.GetComponent<Image>();
            // Sin sprite, fillAmount no recorta nada.
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
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

        /// <summary>Banda de alto fijo pegada al borde inferior del padre.</summary>
        private static void AnclarAbajo(RectTransform rt, float alto, float desdeAbajo, float margenLateral)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-margenLateral * 2f, alto);
            rt.anchoredPosition = new Vector2(0f, desdeAbajo);
        }

        private static GameObject NuevoPanel(Transform padre, string nombre, Color color)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(padre, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text NuevoTexto(Transform padre, string nombre, string contenido, int tamano, TextAnchor alineacion)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(padre, false);
            var t = go.GetComponent<Text>();
            t.text = contenido;
            t.fontSize = tamano;
            t.alignment = alineacion;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
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
