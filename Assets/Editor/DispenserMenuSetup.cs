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
    /// Construye el prefab de menú flotante estilizado (IngredientSelectorMenu) con botón 'X' de cierre,
    /// estética de cristal oscuro con bordes redondeados y botones centrados.
    /// Conecta los tres cajones de la despensa y la tabla de cortar en "First Scene".
    /// </summary>
    [InitializeOnLoad]
    public static class DispenserMenuSetup
    {
        private const string FirstScenePath = "Assets/Scenes/First Scene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
        private const string MenuPrefabPath = "Assets/02_Prefabs/UI/IngredientSelectorMenu.prefab";
        private const string SessionKey = "DispenserMenuSetup_V6_AutoExecuted";

        /// <summary>Altura del menú sobre el cajón, en metros.</summary>
        private const float AlturaMenuCajon = 0.35f;

        /// <summary>Altura del menú sobre la tabla de corte, en metros.</summary>
        private const float AlturaMenuTabla = 0.45f;

        static DispenserMenuSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(SessionKey, false))
                {
                    SessionState.SetBool(SessionKey, true);
                    SetupAll();
                }
            };
        }

        [MenuItem("Kitchen/Setup Ingredient Selector Menu")]
        public static void SetupAll()
        {
            Debug.Log("[DispenserMenuSetup] Configurando menú de selección estilizado con botón 'X'...");

            GameObject menuPrefab = BuildMenuPrefab();
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

            Debug.Log("[DispenserMenuSetup] Listo. Paneles de cajones y tabla de cortar actualizados con 'X' y diseño centrado.");
        }

        [MenuItem("Kitchen/Rebuild Menu Prefab Only")]
        public static GameObject BuildMenuPrefab()
        {
            string folder = "Assets/02_Prefabs/UI";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/02_Prefabs", "UI");
            }

            Sprite roundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VRTemplateAssets/Sprites/UI/Round Radius 10.png");
            Sprite outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VRTemplateAssets/Sprites/UI/Round Radius 10 Outline.png");
            Font mainFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");
            if (mainFont == null) mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AudioClip popSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button Pop.wav");

            // 1. Canvas Root
            var canvasGo = new GameObject("IngredientSelectorMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(380, 320);
            canvasGo.transform.localScale = Vector3.one * 0.001f;
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

            // 2. Main Panel (Fondo oscuro redondeado)
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var panelImg = panelGo.GetComponent<Image>();
            if (roundSprite != null)
            {
                panelImg.sprite = roundSprite;
                panelImg.type = Image.Type.Sliced;
            }
            panelImg.color = new Color(0.08f, 0.10f, 0.14f, 0.94f); // Cristal oscuro elegante

            // 2.1 Borde sutil del panel
            var outlineGo = new GameObject("PanelBorder", typeof(RectTransform), typeof(Image));
            outlineGo.transform.SetParent(panelGo.transform, false);
            var outlineRt = outlineGo.GetComponent<RectTransform>();
            outlineRt.anchorMin = Vector2.zero;
            outlineRt.anchorMax = Vector2.one;
            outlineRt.offsetMin = Vector2.zero;
            outlineRt.offsetMax = Vector2.zero;
            var outlineImg = outlineGo.GetComponent<Image>();
            if (outlineSprite != null)
            {
                outlineImg.sprite = outlineSprite;
                outlineImg.type = Image.Type.Sliced;
            }
            outlineImg.color = new Color(0.35f, 0.48f, 0.65f, 0.35f);
            outlineImg.raycastTarget = false;

            // 3. Barra Superior (Header con Título y Botón 'X')
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(panelGo.transform, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0, 48);

            // 3.1 Texto de Título
            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(headerGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(18, 0);
            titleRt.offsetMax = new Vector2(-54, 0);
            var titleText = titleGo.GetComponent<Text>();
            titleText.text = "SELECCIONAR";
            titleText.font = mainFont;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.85f, 0.90f, 0.96f, 1f);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.raycastTarget = false;

            // 3.2 Botón de Cierre ('X')
            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(headerGo.transform, false);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 0.5f);
            closeRt.anchorMax = new Vector2(1, 0.5f);
            closeRt.pivot = new Vector2(1, 0.5f);
            closeRt.anchoredPosition = new Vector2(-12, 0);
            closeRt.sizeDelta = new Vector2(34, 34);
            var closeImg = closeGo.GetComponent<Image>();
            if (roundSprite != null)
            {
                closeImg.sprite = roundSprite;
                closeImg.type = Image.Type.Sliced;
            }
            closeImg.color = new Color(0.82f, 0.20f, 0.20f, 0.88f);
            var closeBtn = closeGo.GetComponent<Button>();
            var closeColors = closeBtn.colors;
            closeColors.normalColor = new Color(0.82f, 0.20f, 0.20f, 0.88f);
            closeColors.highlightedColor = new Color(1f, 0.36f, 0.36f, 1f);
            closeColors.pressedColor = new Color(0.60f, 0.12f, 0.12f, 1f);
            closeColors.selectedColor = new Color(1f, 0.36f, 0.36f, 1f);
            closeBtn.colors = closeColors;

            var closeTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRt = closeTextGo.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.offsetMin = Vector2.zero;
            closeTextRt.offsetMax = Vector2.zero;
            var closeText = closeTextGo.GetComponent<Text>();
            closeText.text = "✕";
            closeText.font = mainFont;
            closeText.fontSize = 20;
            closeText.fontStyle = FontStyle.Bold;
            closeText.color = Color.white;
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.raycastTarget = false;

            // 3.3 Línea divisora
            var dividerGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerGo.transform.SetParent(headerGo.transform, false);
            var dividerRt = dividerGo.GetComponent<RectTransform>();
            dividerRt.anchorMin = new Vector2(0, 0);
            dividerRt.anchorMax = new Vector2(1, 0);
            dividerRt.pivot = new Vector2(0.5f, 0);
            dividerRt.anchoredPosition = Vector2.zero;
            dividerRt.sizeDelta = new Vector2(-28, 1);
            var dividerImg = dividerGo.GetComponent<Image>();
            dividerImg.color = new Color(1f, 1f, 1f, 0.12f);
            dividerImg.raycastTarget = false;

            // 4. Contenedor de Botones (Centrado)
            var containerGo = new GameObject("Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
            containerGo.transform.SetParent(panelGo.transform, false);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.offsetMin = new Vector2(16, 16);
            containerRt.offsetMax = new Vector2(-16, -50);
            var vlg = containerGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childAlignment = TextAnchor.MiddleCenter; // ¡Botones completamente centrados!
            vlg.padding = new RectOffset(6, 6, 6, 6);

            // 5. Plantilla de Botón (Estilo tarjeta redondeada)
            var buttonGo = new GameObject("ButtonTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonGo.transform.SetParent(containerGo.transform, false);
            var btnImg = buttonGo.GetComponent<Image>();
            if (roundSprite != null)
            {
                btnImg.sprite = roundSprite;
                btnImg.type = Image.Type.Sliced;
            }
            btnImg.color = new Color(0.14f, 0.19f, 0.27f, 0.95f);
            var btnComp = buttonGo.GetComponent<Button>();
            var btnColors = btnComp.colors;
            btnColors.normalColor = new Color(0.14f, 0.19f, 0.27f, 0.95f);
            btnColors.highlightedColor = new Color(0.24f, 0.50f, 0.92f, 1f); // Resaltado azul al apuntar en VR
            btnColors.pressedColor = new Color(0.12f, 0.32f, 0.70f, 1f);
            btnColors.selectedColor = new Color(0.24f, 0.50f, 0.92f, 1f);
            btnComp.colors = btnColors;

            var le = buttonGo.GetComponent<LayoutElement>();
            le.preferredHeight = 56f;
            le.preferredWidth = 320f;

            // 5.1 Borde sutil del botón
            var btnOutlineGo = new GameObject("ButtonOutline", typeof(RectTransform), typeof(Image));
            btnOutlineGo.transform.SetParent(buttonGo.transform, false);
            var btnOutlineRt = btnOutlineGo.GetComponent<RectTransform>();
            btnOutlineRt.anchorMin = Vector2.zero;
            btnOutlineRt.anchorMax = Vector2.one;
            btnOutlineRt.offsetMin = Vector2.zero;
            btnOutlineRt.offsetMax = Vector2.zero;
            var btnOutlineImg = btnOutlineGo.GetComponent<Image>();
            if (outlineSprite != null)
            {
                btnOutlineImg.sprite = outlineSprite;
                btnOutlineImg.type = Image.Type.Sliced;
            }
            btnOutlineImg.color = new Color(1f, 1f, 1f, 0.10f);
            btnOutlineImg.raycastTarget = false;

            // 5.2 Texto del Botón
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
            text.color = Color.white;
            text.font = mainFont;
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            // 6. Conectar referencias en IngredientSelectorMenu
            var menuComp = canvasGo.AddComponent<IngredientSelectorMenu>();
            var so = new SerializedObject(menuComp);
            so.FindProperty("panelRoot").objectReferenceValue = panelGo;
            so.FindProperty("buttonContainer").objectReferenceValue = containerRt;
            so.FindProperty("buttonTemplate").objectReferenceValue = btnComp;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("titleText").objectReferenceValue = titleText;
            if (popSound != null)
            {
                var openProp = so.FindProperty("openSound");
                if (openProp != null) openProp.objectReferenceValue = popSound;
                var closeProp = so.FindProperty("closeSound");
                if (closeProp != null) closeProp.objectReferenceValue = popSound;
                var selectProp = so.FindProperty("selectSound");
                if (selectProp != null) selectProp.objectReferenceValue = popSound;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, MenuPrefabPath);
            Object.DestroyImmediate(canvasGo);

            Debug.Log($"[DispenserMenuSetup] Prefab estilizado actualizado en {MenuPrefabPath}");
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
                    Debug.LogWarning($"[DispenserMenuSetup] '{crateName}' no tiene ItemDispenser.");
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

                    opcionesProp.InsertArrayElementAtIndex(i);
                    opcionesProp.GetArrayElementAtIndex(i).objectReferenceValue = ingrediente;
                    i++;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[DispenserMenuSetup] '{crateName}' configurado con {string.Join(", ", nombresIngredientes)}.");
            }
        }

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
        /// Instancia o reemplaza el menú flotante preservando su posición y escala relativas.
        /// </summary>
        private static GameObject AttachMenu(GameObject host, GameObject menuPrefab, float alturaEnMetros)
        {
            Transform existingMenu = host.transform.Find("IngredientSelectorMenu");
            Vector3 localPos;
            Quaternion localRot;
            Vector3 localScale;

            if (existingMenu != null)
            {
                localPos = existingMenu.localPosition;
                localRot = existingMenu.localRotation;
                localScale = existingMenu.localScale;
                Object.DestroyImmediate(existingMenu.gameObject);
            }
            else
            {
                const float targetWorldScale = 0.001f;
                Vector3 parentLossy = host.transform.lossyScale;
                localScale = new Vector3(
                    targetWorldScale / Mathf.Max(parentLossy.x, 0.0001f),
                    targetWorldScale / Mathf.Max(parentLossy.y, 0.0001f),
                    targetWorldScale / Mathf.Max(parentLossy.z, 0.0001f));
                localPos = new Vector3(0f, alturaEnMetros / Mathf.Max(parentLossy.y, 0.0001f), 0f);
                localRot = Quaternion.identity;
            }

            var menuInstance = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, host.transform);
            menuInstance.name = "IngredientSelectorMenu";
            menuInstance.transform.localPosition = localPos;
            menuInstance.transform.localRotation = localRot;
            menuInstance.transform.localScale = localScale;

            return menuInstance;
        }
    }
}
