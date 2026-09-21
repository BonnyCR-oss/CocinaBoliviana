using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Deja la olla y el sartén de la escena listos para cocinar, y rellena los datos de
    /// cocción de cada ingrediente.
    ///
    /// Busca los objetos POR NOMBRE en la escena, así que sigue funcionando aunque los muevas
    /// o los cambies de estación. No crea geometría ni borra nada tuyo.
    /// </summary>
    public static class CookingSetup
    {
        private const string ScenePath = "Assets/Scenes/First Scene.unity";
        private const string CounterPrefabPath = "Assets/02_Prefabs/UI/CookingCounter.prefab";

        /// <summary>Nombre del objeto en la escena y con qué método cocina.</summary>
        private static readonly (string objeto, MetodoCoccion metodo)[] Recipientes =
        {
            ("olla", MetodoCoccion.Hervir),
            ("sarten", MetodoCoccion.Freir),
        };

        /// <summary>
        /// Qué admite cada ingrediente. Solo se tocan los que ya tienen 'sePuedeCocinar'
        /// marcado: Tomate y Cebolla están como no cocinables y se respeta.
        /// </summary>
        private static readonly (string asset, MetodoCoccion[] metodos, float tiempo, float margen)[] Coccion =
        {
            ("Papa",  new[] { MetodoCoccion.Hervir, MetodoCoccion.Freir }, 10f, 7f),
            ("Arroz", new[] { MetodoCoccion.Hervir },                      12f, 8f),
            ("Carne", new[] { MetodoCoccion.Freir },                        9f, 5f),
            ("Huevo", new[] { MetodoCoccion.Freir },                        6f, 4f),
        };

        [MenuItem("Kitchen/Setup Cooking (olla y sartén)")]
        public static void SetupCooking()
        {
            Debug.Log("[CookingSetup] Configurando cocción...");

            ConfigurarIngredientes();

            GameObject counterPrefab = EnsureCounterPrefab();
            if (counterPrefab == null) return;

            ConfigurarRecipientes(counterPrefab);
        }

        private static void ConfigurarIngredientes()
        {
            foreach (var (asset, metodos, tiempo, margen) in Coccion)
            {
                string path = $"Assets/03_SO/Ingredientes/{asset}.asset";
                var data = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
                if (data == null)
                {
                    Debug.LogWarning($"[CookingSetup] No existe {path}, se omite.");
                    continue;
                }

                if (!data.sePuedeCocinar)
                {
                    Debug.LogWarning($"[CookingSetup] '{asset}' tiene 'sePuedeCocinar' desmarcado; " +
                                     "no se le ponen métodos. Márcalo si quieres que se cocine.");
                    continue;
                }

                data.metodosCoccion.Clear();
                data.metodosCoccion.AddRange(metodos);
                data.tiempoCoccion = tiempo;
                data.margenAntesDeQuemarse = margen;
                EditorUtility.SetDirty(data);

                Debug.Log($"[CookingSetup] '{asset}': {string.Join(" / ", metodos)}, listo en {tiempo}s, se quema {margen}s después.");
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Cartel de progreso: canvas world-space con barra rellenable y texto.</summary>
        private static GameObject EnsureCounterPrefab()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(CounterPrefabPath);
            if (existente != null) return existente;

            if (!AssetDatabase.IsValidFolder("Assets/02_Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/02_Prefabs", "UI");
            }

            var canvasGo = new GameObject("CookingCounter",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(300, 110);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var texto = new GameObject("Etiqueta", typeof(RectTransform), typeof(Text));
            texto.transform.SetParent(panel.transform, false);
            var textoRt = texto.GetComponent<RectTransform>();
            textoRt.anchorMin = new Vector2(0f, 0.42f);
            textoRt.anchorMax = Vector2.one;
            textoRt.offsetMin = new Vector2(10f, 0f);
            textoRt.offsetMax = new Vector2(-10f, -8f);
            var label = texto.GetComponent<Text>();
            label.text = "Cocinando";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;

            var fondoBarra = new GameObject("BarraFondo", typeof(RectTransform), typeof(Image));
            fondoBarra.transform.SetParent(panel.transform, false);
            var fondoRt = fondoBarra.GetComponent<RectTransform>();
            fondoRt.anchorMin = new Vector2(0f, 0.10f);
            fondoRt.anchorMax = new Vector2(1f, 0.36f);
            fondoRt.offsetMin = new Vector2(14f, 0f);
            fondoRt.offsetMax = new Vector2(-14f, 0f);
            fondoBarra.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            var relleno = new GameObject("BarraRelleno", typeof(RectTransform), typeof(Image));
            relleno.transform.SetParent(fondoBarra.transform, false);
            var rellenoRt = relleno.GetComponent<RectTransform>();
            rellenoRt.anchorMin = Vector2.zero;
            rellenoRt.anchorMax = Vector2.one;
            rellenoRt.offsetMin = Vector2.zero;
            rellenoRt.offsetMax = Vector2.zero;
            var barra = relleno.GetComponent<Image>();
            barra.color = new Color(1f, 0.75f, 0.2f);
            // Sin sprite, fillAmount no hace nada: Unity necesita algo que recortar.
            barra.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            barra.type = Image.Type.Filled;
            barra.fillMethod = Image.FillMethod.Horizontal;
            barra.fillOrigin = (int)Image.OriginHorizontal.Left;
            barra.fillAmount = 0f;

            var counter = canvasGo.AddComponent<CookingCounter>();
            var so = new SerializedObject(counter);
            so.FindProperty("raiz").objectReferenceValue = panel;
            so.FindProperty("barra").objectReferenceValue = barra;
            so.FindProperty("etiqueta").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, CounterPrefabPath);
            Object.DestroyImmediate(canvasGo);

            Debug.Log($"[CookingSetup] Cartel de cocción creado en {CounterPrefabPath}");
            return prefab;
        }

        private static void ConfigurarRecipientes(GameObject counterPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CookingSetup] No se pudo abrir {ScenePath}");
                return;
            }

            bool algoCambio = false;

            foreach (var (nombre, metodo) in Recipientes)
            {
                GameObject go = GameObject.Find(nombre);
                if (go == null)
                {
                    Debug.LogWarning($"[CookingSetup] No se encontró '{nombre}' en la escena. " +
                                     "Colócalo y vuelve a correr esto.");
                    continue;
                }

                var vessel = go.GetComponent<CookingVessel>();
                if (vessel == null) vessel = go.AddComponent<CookingVessel>();

                var so = new SerializedObject(vessel);
                so.FindProperty("metodo").enumValueIndex = (int)metodo;

                // El punto donde se apila la comida, solo al crearlo.
                Transform punto = go.transform.Find("PuntoContenido");
                if (punto == null)
                {
                    var puntoGo = new GameObject("PuntoContenido");
                    puntoGo.transform.SetParent(go.transform, false);
                    puntoGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    punto = puntoGo.transform;
                }
                so.FindProperty("puntoContenido").objectReferenceValue = punto;

                // El cartel, igual: si ya está se respeta dónde lo dejaste.
                Transform cartel = go.transform.Find("CookingCounter");
                if (cartel == null)
                {
                    var instancia = (GameObject)PrefabUtility.InstantiatePrefab(counterPrefab, go.transform);
                    instancia.name = "CookingCounter";
                    instancia.transform.localPosition = new Vector3(0f, 0.30f, 0f);
                    instancia.transform.localRotation = Quaternion.identity;
                    cartel = instancia.transform;
                }
                so.FindProperty("contador").objectReferenceValue = cartel.GetComponent<CookingCounter>();

                // El fuego más cercano, para que se encienda solo al cocinar.
                BurnerFlame masCercano = null;
                float mejorDist = float.MaxValue;
                foreach (var f in Object.FindObjectsByType<BurnerFlame>(FindObjectsInactive.Include))
                {
                    float d = Vector3.Distance(f.transform.position, go.transform.position);
                    if (d < mejorDist) { mejorDist = d; masCercano = f; }
                }
                if (masCercano != null && mejorDist < 1f)
                {
                    so.FindProperty("fuego").objectReferenceValue = masCercano;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                algoCambio = true;

                string conFuego = (masCercano != null && mejorDist < 1f) ? $", fuego '{masCercano.name}'" : ", SIN fuego cerca";
                Debug.Log($"[CookingSetup] '{nombre}' -> {metodo}{conFuego}.");
            }

            if (algoCambio)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
