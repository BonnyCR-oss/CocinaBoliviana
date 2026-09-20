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
    /// y deja "Cajon_Tomate" configurado como ejemplo de dispensador con selección múltiple
    /// (Tomate / Cebolla / Papa).
    /// </summary>
    [InitializeOnLoad]
    public static class DispenserMenuSetup
    {
        private const string FirstScenePath = "Assets/Scenes/First Scene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
        private const string MenuPrefabPath = "Assets/02_Prefabs/UI/IngredientSelectorMenu.prefab";

        static DispenserMenuSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("DispenserMenuSetup_Executed_v1", false))
                {
                    SessionState.SetBool("DispenserMenuSetup_Executed_v1", true);
                    SetupAll();
                }
            };
        }

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
            SetupDemoDispenser(menuPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[DispenserMenuSetup] Listo. 'Cajon_Tomate' ahora abre un menú con Tomate/Cebolla/Papa al presionar Grip o Trigger.");
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

        private static void SetupDemoDispenser(GameObject menuPrefab)
        {
            GameObject cajonTomate = GameObject.Find("Cajon_Tomate");
            if (cajonTomate == null)
            {
                Debug.LogWarning("[DispenserMenuSetup] No se encontró 'Cajon_Tomate' en la escena.");
                return;
            }

            var dispenser = cajonTomate.GetComponent<ItemDispenser>();
            if (dispenser == null)
            {
                Debug.LogWarning("[DispenserMenuSetup] 'Cajon_Tomate' no tiene ItemDispenser.");
                return;
            }

            Transform existingMenu = cajonTomate.transform.Find("IngredientSelectorMenu");
            GameObject menuInstance = existingMenu != null
                ? existingMenu.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, cajonTomate.transform);

            // El padre (Cajon_Tomate) tiene escala NO uniforme (ej. 0.36/0.16/0.55).
            // Si no se compensa, el menú hereda esa distorsión y queda microscópico/deforme.
            const float targetWorldScale = 0.001f;
            const float targetWorldHeightOffset = 0.35f;
            Vector3 parentLossy = cajonTomate.transform.lossyScale;
            menuInstance.transform.localScale = new Vector3(
                targetWorldScale / Mathf.Max(parentLossy.x, 0.0001f),
                targetWorldScale / Mathf.Max(parentLossy.y, 0.0001f),
                targetWorldScale / Mathf.Max(parentLossy.z, 0.0001f));
            menuInstance.transform.localPosition = new Vector3(0f, targetWorldHeightOffset / Mathf.Max(parentLossy.y, 0.0001f), 0f);
            menuInstance.transform.localRotation = Quaternion.identity;

            var tomate = AssetDatabase.LoadAssetAtPath<IngredientData>("Assets/03_SO/Ingredientes/Tomate.asset");
            var cebolla = AssetDatabase.LoadAssetAtPath<IngredientData>("Assets/03_SO/Ingredientes/Cebolla.asset");
            var papa = AssetDatabase.LoadAssetAtPath<IngredientData>("Assets/03_SO/Ingredientes/Papa.asset");

            var so = new SerializedObject(dispenser);
            SerializedProperty menuProp = so.FindProperty("menu");
            SerializedProperty opcionesProp = so.FindProperty("opcionesIngredientes");

            menuProp.objectReferenceValue = menuInstance.GetComponent<IngredientSelectorMenu>();

            opcionesProp.ClearArray();
            int i = 0;
            foreach (var ingrediente in new[] { tomate, cebolla, papa })
            {
                if (ingrediente == null) continue;
                opcionesProp.InsertArrayElementAtIndex(i);
                opcionesProp.GetArrayElementAtIndex(i).objectReferenceValue = ingrediente;
                i++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
