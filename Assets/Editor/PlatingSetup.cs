using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Le monta al prefab del plato su cartel de receta y le pasa la lista de platos
    /// conocidos, para que sepa deducir qué está saliendo y qué le falta.
    /// </summary>
    public static class PlatingSetup
    {
        private const string PlatePrefabPath = "Assets/02_Prefabs/Plate_Item.prefab";
        private const string DishesFolder = "Assets/03_SO/Platos";

        [MenuItem("Kitchen/Setup Plating (recetas en el plato)")]
        public static void SetupPlating()
        {
            Debug.Log("[PlatingSetup] Configurando emplatado...");

            List<DishData> recetas = CargarRecetas();
            MigrarRecetas(recetas);
            PrepararPrefabsServidos(recetas);
            if (recetas.Count == 0)
            {
                Debug.LogWarning($"[PlatingSetup] No hay ningún DishData en {DishesFolder}. " +
                                 "El plato no podrá deducir ninguna receta.");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath) == null)
            {
                Debug.LogError($"[PlatingSetup] No existe {PlatePrefabPath}. " +
                               "Corre antes 'Kitchen > Setup Kitchen Mechanics'.");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(PlatePrefabPath))
            {
                GameObject root = scope.prefabContentsRoot;

                var plate = root.GetComponent<PlateItem>();
                if (plate == null)
                {
                    Debug.LogError("[PlatingSetup] El prefab del plato no tiene PlateItem.");
                    return;
                }

                // El plato de emplatado es una estacion fija: se monta encima, no se lleva.
                // Lo unico agarrable es el plato terminado del punto de recogida.
                var grabViejo = root.GetComponent<XRGrabInteractable>();
                if (grabViejo != null)
                {
                    Object.DestroyImmediate(grabViejo, true);
                    Debug.Log("[PlatingSetup] Quitado el XRGrabInteractable del plato de emplatado.");
                }

                PlateCounter counter = ConstruirCartelSiFalta(root);

                var so = new SerializedObject(plate);
                so.FindProperty("contador").objectReferenceValue = counter;

                // El plato se repone a si mismo: el prefab se referencia desde dentro del
                // propio prefab, que es legal y evita tener que buscarlo en runtime.
                so.FindProperty("platoVacioPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);

                SerializedProperty lista = so.FindProperty("recetasConocidas");
                lista.ClearArray();
                for (int i = 0; i < recetas.Count; i++)
                {
                    lista.InsertArrayElementAtIndex(i);
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = recetas[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();

            CrearPuntoDeRecogida();

            var nombres = new List<string>();
            foreach (var r in recetas) nombres.Add(r.nombre);
            Debug.Log($"[PlatingSetup] Listo. Recetas que el plato reconoce: {string.Join(", ", nombres)}.");
        }

        /// <summary>
        /// Silpancho de verdad: milanesa y papas fritas, arroz hervido, y encima la
        /// ensalada cruda de tomate y cebolla picados. Solo se aplica si la receta aún
        /// está como la dejó la migración (todo crudo y entero), para no pisar tus ajustes.
        /// </summary>
        private static readonly (string ingrediente, TipoCorte corte, bool cocido, MetodoCoccion metodo)[] RecetaSilpancho =
        {
            ("Carne",   TipoCorte.Ninguno, true,  MetodoCoccion.Freir),
            ("Papa",    TipoCorte.Rodajas, true,  MetodoCoccion.Freir),
            ("Arroz",   TipoCorte.Ninguno, true,  MetodoCoccion.Hervir),
            ("Huevo",   TipoCorte.Ninguno, true,  MetodoCoccion.Freir),
            ("Tomate",  TipoCorte.Cubitos, false, MetodoCoccion.Hervir),
            ("Cebolla", TipoCorte.Rodajas, false, MetodoCoccion.Hervir),
        };

        private static void MigrarRecetas(List<DishData> recetas)
        {
            foreach (var plato in recetas)
            {
                if (plato == null) continue;

                if (plato.MigrarRecetaLegada())
                {
                    Debug.Log($"[PlatingSetup] '{plato.nombre}': receta migrada al nuevo formato " +
                              $"({plato.receta.Count} ingredientes, todos crudos y enteros).");
                }

                if (plato.nombre == "Silpancho") AplicarRecetaSilpancho(plato);

                EditorUtility.SetDirty(plato);
            }
        }

        private static void AplicarRecetaSilpancho(DishData plato)
        {
            // Si alguien ya le puso estados a mano, no se toca.
            foreach (var r in plato.receta)
            {
                if (r != null && (r.corte != TipoCorte.Ninguno || r.debeEstarCocido)) return;
            }

            foreach (var (nombre, corte, cocido, metodo) in RecetaSilpancho)
            {
                var req = plato.receta.Find(r => r != null && r.ingrediente != null && r.ingrediente.nombre == nombre);
                if (req == null)
                {
                    Debug.LogWarning($"[PlatingSetup] El Silpancho no lleva '{nombre}' en su receta; se omite.");
                    continue;
                }
                req.corte = corte;
                req.debeEstarCocido = cocido;
                req.metodo = metodo;
            }

            Debug.Log("[PlatingSetup] Silpancho: carne frita, papa en rodajas frita, arroz hervido, " +
                      "huevo frito, tomate en cubitos crudo, cebolla en rodajas cruda.");
        }

        /// <summary>
        /// Marca la mesa contigua a la de emplatado como sitio donde aparecen los platos
        /// terminados. Solo lo coloca al crearlo: si lo mueves, se respeta.
        /// </summary>
        private static void CrearPuntoDeRecogida()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/First Scene.unity", OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            if (Object.FindAnyObjectByType<DishPickupPoint>() != null)
            {
                Debug.Log("[PlatingSetup] Ya hay un punto de recogida; se deja donde está.");
                return;
            }

            // Counter_Island_01 es la mesa justo al lado de AssemblyStation.
            GameObject mesa = GameObject.Find("Counter_Island_01") ?? GameObject.Find("AssemblyStation");
            if (mesa == null)
            {
                Debug.LogWarning("[PlatingSetup] No encontré una mesa donde poner el punto de recogida.");
                return;
            }

            var go = new GameObject("PuntoDeRecogida");
            go.transform.SetParent(mesa.transform, true);
            go.transform.position = mesa.transform.position + new Vector3(-0.25f, 0.50f, 0f);
            go.transform.rotation = Quaternion.identity;
            go.AddComponent<DishPickupPoint>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PlatingSetup] Punto de recogida creado sobre '{mesa.name}'. " +
                      "Muévelo en la Hierarchy si quieres otro sitio.");
        }

        private const string ServidosFolder = "Assets/02_Prefabs/PlatosPrefabs";

        /// <summary>
        /// Deja un prefab agarrable por cada plato. El modelo crudo (.glb) no tiene ni
        /// collider ni Rigidbody, así que al terminar una receta aparecía algo imposible
        /// de levantar. Aquí se le monta la física una vez y 'platoPrefab' pasa a apuntar
        /// a ese prefab en vez de al modelo suelto.
        /// </summary>
        private static void PrepararPrefabsServidos(List<DishData> recetas)
        {
            if (!AssetDatabase.IsValidFolder(ServidosFolder))
            {
                AssetDatabase.CreateFolder("Assets/02_Prefabs", "PlatosPrefabs");
            }

            foreach (var plato in recetas)
            {
                if (plato == null) continue;
                if (plato.platoPrefab == null)
                {
                    Debug.LogWarning($"[PlatingSetup] '{plato.nombre}' no tiene 'platoPrefab'; " +
                                     "al completarse no aparecerá nada.");
                    continue;
                }

                string destino = $"{ServidosFolder}/{plato.nombre}_Servido.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(destino) == null)
                {
                    var temp = (GameObject)PrefabUtility.InstantiatePrefab(plato.platoPrefab);
                    PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    temp.name = plato.nombre;
                    PrefabUtility.SaveAsPrefabAsset(temp, destino);
                    Object.DestroyImmediate(temp);
                    Debug.Log($"[PlatingSetup] Prefab servido creado en {destino}");
                }

                using (var scope = new PrefabUtility.EditPrefabContentsScope(destino))
                {
                    ConfigurarPlatoServido(scope.prefabContentsRoot, plato);
                }

                plato.platoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(destino);
                EditorUtility.SetDirty(plato);
            }

            AssetDatabase.SaveAssets();
        }

        private static void ConfigurarPlatoServido(GameObject root, DishData plato)
        {
            var col = root.GetComponent<BoxCollider>();
            if (col == null) col = root.AddComponent<BoxCollider>();

            // Caja ajustada a la malla: el modelo ya trae el plato, así que esto envuelve
            // el conjunto entero y se puede apuntar desde cualquier lado.
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds mundo = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) mundo.Encapsulate(renderers[i].bounds);

                Vector3 lossy = root.transform.lossyScale;
                col.center = root.transform.InverseTransformPoint(mundo.center);
                col.size = new Vector3(
                    mundo.size.x / Mathf.Max(Mathf.Abs(lossy.x), 0.0001f),
                    mundo.size.y / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f),
                    mundo.size.z / Mathf.Max(Mathf.Abs(lossy.z), 0.0001f));
            }
            col.isTrigger = false;

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.9f;
            rb.angularDamping = 6f;
            rb.linearDamping = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            if (root.GetComponent<XRGrabInteractable>() == null)
            {
                root.AddComponent<XRGrabInteractable>();
            }

            var marca = root.GetComponent<ServedDish>();
            if (marca == null) marca = root.AddComponent<ServedDish>();
            var so = new SerializedObject(marca);
            so.FindProperty("plato").objectReferenceValue = plato;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<DishData> CargarRecetas()
        {
            var recetas = new List<DishData>();
            if (!AssetDatabase.IsValidFolder(DishesFolder)) return recetas;

            foreach (string guid in AssetDatabase.FindAssets("t:DishData", new[] { DishesFolder }))
            {
                var dish = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(guid));
                if (dish != null) recetas.Add(dish);
            }
            return recetas;
        }

        /// <summary>
        /// El cartel se construye con GameObjects sueltos dentro del propio prefab del plato,
        /// no como prefab aparte: anidar prefabs dentro de un EditPrefabContentsScope da
        /// problemas, y este cartel no se reutiliza en ningún otro sitio.
        /// </summary>
        private static PlateCounter ConstruirCartelSiFalta(GameObject root)
        {
            Transform existente = root.transform.Find("PlateCounter");
            if (existente != null)
            {
                var yaEsta = existente.GetComponent<PlateCounter>();
                if (yaEsta != null) return yaEsta;
                Object.DestroyImmediate(existente.gameObject);
            }

            var canvasGo = new GameObject("PlateCounter",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 130);
            canvasGo.transform.localPosition = new Vector3(0f, 0.26f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            Text titulo = CrearTexto(panel.transform, "Titulo", 34, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.50f), new Vector2(1f, 1f));
            Text faltan = CrearTexto(panel.transform, "Faltantes", 24, TextAnchor.UpperCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0.50f));
            faltan.color = new Color(0.9f, 0.9f, 0.9f);

            var counter = canvasGo.AddComponent<PlateCounter>();
            var so = new SerializedObject(counter);
            so.FindProperty("raiz").objectReferenceValue = panel;
            so.FindProperty("titulo").objectReferenceValue = titulo;
            so.FindProperty("faltantes").objectReferenceValue = faltan;
            so.ApplyModifiedPropertiesWithoutUndo();

            return counter;
        }

        private static Text CrearTexto(Transform padre, string nombre, int tamano,
                                       TextAnchor alineacion, Vector2 anclaMin, Vector2 anclaMax)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(padre, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anclaMin;
            rt.anchorMax = anclaMax;
            rt.offsetMin = new Vector2(10f, 4f);
            rt.offsetMax = new Vector2(-10f, -4f);

            var t = go.GetComponent<Text>();
            t.alignment = alineacion;
            t.color = Color.white;
            t.fontSize = tamano;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }
    }
}
