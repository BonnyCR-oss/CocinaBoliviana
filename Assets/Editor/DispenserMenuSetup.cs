using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Construye el prefab de menú flotante (IngredientSelectorMenu), asegura que "First Scene"
    /// tenga la infraestructura de UI para VR (EventSystem + raycasters, copiada de "Main Menu"),
    /// deja los tres cajones de la despensa (Papas / Carne / Verduras) configurados como
    /// dispensadores con selección múltiple, y conecta un menú de tipo de corte en cada tabla.
    /// </summary>
    public static class DispenserMenuSetup
    {
        private const string FirstScenePath = "Assets/Scenes/First Scene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
        private const string MenuPrefabPath = "Assets/02_Prefabs/UI/IngredientSelectorMenu.prefab";

        /// <summary>Altura del menú sobre el cajón, en metros.</summary>
        private const float AlturaMenuCajon = 0.35f;

        /// <summary>
        /// Altura del menú sobre la tabla, en metros. Más alto que el de los cajones a
        /// propósito: el trigger de detección de la tabla llega a 30 cm y, como ItemDispenser
        /// pone Physics.queriesHitTriggers = true, el rayo del control chocaría con él antes
        /// de alcanzar el panel.
        /// </summary>
        private const float AlturaMenuTabla = 0.45f;

        [MenuItem("Kitchen/Setup Ingredient Selector Menu")]
        public static void SetupAll()
        {
            Debug.Log("[DispenserMenuSetup] Configurando menú de selección de ingredientes...");

            GameObject menuPrefab = BuildMenuPrefabIfNeeded();
            if (menuPrefab == null)
            {
                Debug.LogError("[DispenserMenuSetup] No se pudo crear el prefab del menú.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(FirstScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[DispenserMenuSetup] No se pudo abrir {FirstScenePath}");
                return;
            }

            EnsureEventSystem(scene);
            SetupDispensers(menuPrefab);
            SetupCuttingBoardMenus(menuPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DispenserMenuSetup] Listo. Cajones con menú de ingredientes, tablas con menú de corte.");
        }

        private static GameObject BuildMenuPrefabIfNeeded()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            if (existing != null) return existing;

            string folder = "Assets/02_Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/02_Prefabs", "UI");
            }

            var canvasGo = new GameObject("IngredientSelectorMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(400, 340);
            canvasGo.transform.localScale = Vector3.one * 0.001f;
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var containerGo = new GameObject("Container", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            containerGo.transform.SetParent(panelGo.transform, false);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(360, 300);
            var vlg = containerGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(15, 15, 15, 15);
            var fitter = containerGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var buttonGo = new GameObject("ButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.transform.SetParent(containerGo.transform, false);
            buttonGo.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f, 1f);
            var le = buttonGo.GetComponent<LayoutElement>();
            le.preferredHeight = 64f;
            le.preferredWidth = 330f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.text = "Ingrediente";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;

            var menuComp = canvasGo.AddComponent<IngredientSelectorMenu>();
            var so = new SerializedObject(menuComp);
            so.FindProperty("panelRoot").objectReferenceValue = panelGo;
            so.FindProperty("buttonContainer").objectReferenceValue = containerRt;
            so.FindProperty("buttonTemplate").objectReferenceValue = buttonGo.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, MenuPrefabPath);
            Object.DestroyImmediate(canvasGo);

            Debug.Log($"[DispenserMenuSetup] Prefab de menú creado en {MenuPrefabPath}");
            return savedPrefab;
        }

        private static void EnsureEventSystem(Scene targetScene)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            Scene mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);
            if (!mainMenuScene.IsValid())
            {
                Debug.LogWarning("[DispenserMenuSetup] No se pudo abrir Main Menu para copiar el EventSystem.");
                return;
            }

            GameObject sourceEventSystem = null;
            foreach (var root in mainMenuScene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<EventSystem>(true);
                if (found != null) { sourceEventSystem = found.gameObject; break; }
            }

            if (sourceEventSystem != null)
            {
                EditorSceneManager.SetActiveScene(targetScene);
                GameObject copy = Object.Instantiate(sourceEventSystem);
                copy.name = "EventSystem";
                SceneManager.MoveGameObjectToScene(copy, targetScene);
                Debug.Log("[DispenserMenuSetup] EventSystem copiado desde Main Menu a First Scene.");
            }
            else
            {
                Debug.LogWarning("[DispenserMenuSetup] Main Menu no tiene un EventSystem para copiar.");
            }

            EditorSceneManager.CloseScene(mainMenuScene, true);
            EditorSceneManager.SetActiveScene(targetScene);
        }

        /// <summary>
        /// Qué ingredientes ofrece cada cajón. El reparto sigue el nombre del cajón, no el campo
        /// 'tipo' del IngredientData: ahí Papa, Tomate, Cebolla y Huevo están los cuatro marcados
        /// como Verdura, así que agrupar por tipo no distingue nada.
        /// </summary>
        private static readonly (string crate, string[] ingredientes)[] Reparto =
        {
            ("Cajon_Papas",    new[] { "Papa", "Arroz" }),
            ("Cajon_Carne",    new[] { "Carne", "Huevo" }),
            ("Cajon_Verduras", new[] { "Tomate", "Cebolla" }),
        };

        private static void SetupDispensers(GameObject menuPrefab)
        {
            foreach (var (crateName, nombresIngredientes) in Reparto)
            {
                GameObject crate = GameObject.Find(crateName);
                if (crate == null)
                {
                    Debug.LogWarning($"[DispenserMenuSetup] No se encontró '{crateName}' en la escena.");
                    continue;
                }

                var dispenser = crate.GetComponent<ItemDispenser>();
                if (dispenser == null)
                {
                    Debug.LogWarning($"[DispenserMenuSetup] '{crateName}' no tiene ItemDispenser. " +
                                     "¿Corriste antes 'Kitchen > Setup Kitchen Mechanics'?");
                    continue;
                }

                GameObject menuInstance = AttachMenu(crate, menuPrefab, AlturaMenuCajon);

                var so = new SerializedObject(dispenser);
                so.FindProperty("menu").objectReferenceValue = menuInstance.GetComponent<IngredientSelectorMenu>();

                SerializedProperty opcionesProp = so.FindProperty("opcionesIngredientes");
                opcionesProp.ClearArray();
                int i = 0;
                foreach (string nombre in nombresIngredientes)
                {
                    string assetPath = $"Assets/03_SO/Ingredientes/{nombre}.asset";
                    var ingrediente = AssetDatabase.LoadAssetAtPath<IngredientData>(assetPath);
                    if (ingrediente == null)
                    {
                        Debug.LogWarning($"[DispenserMenuSetup] No existe {assetPath}, se omite en '{crateName}'.");
                        continue;
                    }
                    if (ingrediente.prefab == null)
                    {
                        // Sin esto el menú abre pero no dispensa nada: ItemDispenser spawnea
                        // IngredientData.prefab, no el prefab del dispensador.
                        Debug.LogWarning($"[DispenserMenuSetup] '{nombre}' no tiene 'prefab' asignado en su " +
                                         "IngredientData; el menú lo mostrará pero no dispensará nada.");
                    }

                    opcionesProp.InsertArrayElementAtIndex(i);
                    opcionesProp.GetArrayElementAtIndex(i).objectReferenceValue = ingrediente;
                    i++;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[DispenserMenuSetup] '{crateName}' ofrece: {string.Join(", ", nombresIngredientes)}.");
            }
        }

        /// <summary>
        /// Cada tabla de cortar lleva su propio menú para elegir el tipo de corte al acoplar
        /// un ingrediente con más de un corte posible (ej. Cebolla: Rodajas o Cubitos).
        /// </summary>
        private static void SetupCuttingBoardMenus(GameObject menuPrefab)
        {
            var boards = Object.FindObjectsByType<CuttingBoard>(FindObjectsInactive.Include);
            if (boards.Length == 0)
            {
                Debug.LogWarning("[DispenserMenuSetup] No hay ninguna CuttingBoard en la escena.");
                return;
            }

            foreach (var board in boards)
            {
                GameObject menuInstance = AttachMenu(board.gameObject, menuPrefab, AlturaMenuTabla);

                var so = new SerializedObject(board);
                so.FindProperty("corteMenu").objectReferenceValue = menuInstance.GetComponent<IngredientSelectorMenu>();
                so.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log($"[DispenserMenuSetup] Menú de corte conectado en '{board.gameObject.name}'.");
            }
        }

        /// <summary>
        /// Cuelga (o reutiliza) la instancia del menú sobre el cajón, compensando la escala.
        /// </summary>
        private static GameObject AttachMenu(GameObject host, GameObject menuPrefab, float alturaEnMetros)
        {
            Transform existingMenu = host.transform.Find("IngredientSelectorMenu");
            if (existingMenu != null)
            {
                // Ya está puesto: se respeta dónde lo dejaste. Recolocarlo en cada corrida
                // borraba los ajustes hechos a mano en la escena.
                return existingMenu.gameObject;
            }

            var menuInstance = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, host.transform);

            // Tanto los cajones (0.36 / 0.16 / 0.55) como las tablas (0.45 / 0.02 / 0.35)
            // tienen escala NO uniforme. Sin compensarla el menú hereda esa distorsión
            // y queda microscópico y deforme.
            const float targetWorldScale = 0.001f;
            Vector3 parentLossy = host.transform.lossyScale;
            menuInstance.transform.localScale = new Vector3(
                targetWorldScale / Mathf.Max(parentLossy.x, 0.0001f),
                targetWorldScale / Mathf.Max(parentLossy.y, 0.0001f),
                targetWorldScale / Mathf.Max(parentLossy.z, 0.0001f));
            menuInstance.transform.localPosition = new Vector3(0f, alturaEnMetros / Mathf.Max(parentLossy.y, 0.0001f), 0f);
            menuInstance.transform.localRotation = Quaternion.identity;

            return menuInstance;
        }
    }
}
