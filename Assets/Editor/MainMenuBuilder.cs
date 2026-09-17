using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CocinaBoliviana.Editor
{
    [InitializeOnLoad]
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main Menu.unity";
        private const string GameScenePath = "Assets/Scenes/First Scene.unity";
        private const string MaterialsFolder = "Assets/Materials";

        static MainMenuBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                // Check if Main Menu scene needs generation
                bool needsBuild = false;
                if (!File.Exists(ScenePath))
                {
                    needsBuild = true;
                }
                else
                {
                    FileInfo fi = new FileInfo(ScenePath);
                    if (fi.Length < 10000) // Less than 10KB means unpopulated empty scene
                    {
                        needsBuild = true;
                    }
                }

                if (needsBuild || !SessionState.GetBool("MainMenuBuilder_Executed_v4", false))
                {
                    SessionState.SetBool("MainMenuBuilder_Executed_v4", true);
                    BuildMainMenuScene();
                }
            };
        }

        [MenuItem("Kitchen/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            Debug.Log("[MainMenuBuilder] Building Overcooked-style VR Main Menu Scene (v4)...");

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // 1. Ensure Build Settings include both scenes (0: Main Menu, 1: First Scene)
            UpdateBuildSettings();

            // 2. Open scene
            Scene scene;
            if (File.Exists(ScenePath))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            if (!scene.IsValid())
            {
                Debug.LogError($"[MainMenuBuilder] Failed to open scene at {ScenePath}");
                return;
            }

            // Clear any existing root objects to avoid duplication
            GameObject[] oldRoots = scene.GetRootGameObjects();
            foreach (var r in oldRoots)
            {
                Undo.DestroyObjectImmediate(r);
            }

            // 3. Render Settings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.96f, 0.94f, 0.90f);
            RenderSettings.ambientIntensity = 1.15f;

            // 4. Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.97f, 0.90f);
            dirLight.intensity = 1.2f;
            dirLight.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
            lightGo.transform.position = new Vector3(0f, 3.5f, 0f);

            // 5. Materials
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material matFloor = GetOrCreateMaterial("Mat_Menu_Floor", new Color(0.85f, 0.78f, 0.68f), 0.0f, 0.2f, urpShader);
            Material matWall = GetOrCreateMaterial("Mat_Menu_Wall", new Color(0.96f, 0.94f, 0.88f), 0.0f, 0.1f, urpShader);
            Material matWood = GetOrCreateMaterial("Mat_Menu_Wood", new Color(0.55f, 0.38f, 0.26f), 0.0f, 0.3f, urpShader);
            Material matCyan = GetOrCreateMaterial("Mat_Menu_Banner", new Color(0.18f, 0.68f, 0.85f), 0.0f, 0.4f, urpShader);
            Material matMetal = GetOrCreateMaterial("Mat_Metal", new Color(0.60f, 0.66f, 0.70f), 0.85f, 0.65f, urpShader);
            Material matLamp = GetOrCreateMaterial("Mat_Lamp", new Color(1.0f, 0.82f, 0.40f), 0.0f, 0.3f, urpShader);

            // 6. XR Origin (XR Rig)
            GameObject xrOrigin = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
            if (prefab != null)
            {
                xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                xrOrigin.name = "XR Origin (XR Rig)";
            }
            else
            {
                xrOrigin = new GameObject("XR Origin (XR Rig)");
                var camGo = new GameObject("Main Camera");
                camGo.transform.SetParent(xrOrigin.transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            xrOrigin.transform.position = new Vector3(0f, 0.05f, 0f);
            xrOrigin.transform.rotation = Quaternion.identity;

            // 7. XR Interaction Manager & EventSystem
            GameObject xriManager = new GameObject("XR Interaction Manager");
            Type xriMgrType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager, Unity.XR.Interaction.Toolkit");
            if (xriMgrType != null)
            {
                xriManager.AddComponent(xriMgrType);
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            Type inputSystemUiType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiType != null)
            {
                eventSystem.AddComponent(inputSystemUiType);
            }
            else
            {
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 8. Environment Diorama (Overcooked Reception Decor)
            GameObject envRoot = new GameObject("Menu_Environment");

            // Floor (6m x 6m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(envRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0f, 1.5f);
            floor.transform.localScale = new Vector3(6f, 0.1f, 6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = matFloor;

            // Back Wall (Z = 3.6m)
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "Back_Wall";
            backWall.transform.SetParent(envRoot.transform, false);
            backWall.transform.localPosition = new Vector3(0f, 2f, 3.6f);
            backWall.transform.localScale = new Vector3(6.5f, 4f, 0.2f);
            backWall.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            // Left Wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Left_Wall";
            leftWall.transform.SetParent(envRoot.transform, false);
            leftWall.transform.localPosition = new Vector3(-3.1f, 2f, 1.5f);
            leftWall.transform.localScale = new Vector3(0.2f, 4f, 6f);
            leftWall.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            // Right Wall
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Right_Wall";
            rightWall.transform.SetParent(envRoot.transform, false);
            rightWall.transform.localPosition = new Vector3(3.1f, 2f, 1.5f);
            rightWall.transform.localScale = new Vector3(0.2f, 4f, 6f);
            rightWall.GetComponent<MeshRenderer>().sharedMaterial = matWall;

            // Front Counter Table below canvas
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Reception_Table";
            table.transform.SetParent(envRoot.transform, false);
            table.transform.localPosition = new Vector3(0f, 0.45f, 2.2f);
            table.transform.localScale = new Vector3(2.4f, 0.9f, 0.7f);
            table.GetComponent<MeshRenderer>().sharedMaterial = matWood;

            // Table Props: Pot & Chopping board
            GameObject pot1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot1.name = "Deco_Olla";
            pot1.transform.SetParent(table.transform, false);
            pot1.transform.localPosition = new Vector3(-0.35f, 0.58f, 0f);
            pot1.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            pot1.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(pot1.GetComponent<Collider>());

            GameObject board1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board1.name = "Deco_Tabla";
            board1.transform.SetParent(table.transform, false);
            board1.transform.localPosition = new Vector3(0.35f, 0.52f, 0f);
            board1.transform.localScale = new Vector3(0.25f, 0.02f, 0.20f);
            board1.GetComponent<MeshRenderer>().sharedMaterial = matWood;
            UnityEngine.Object.DestroyImmediate(board1.GetComponent<Collider>());

            // Ceiling Pendant Lamps
            AddCeilingLamp(envRoot.transform, "Lamp_Left", new Vector3(-1.3f, 2.7f, 1.8f), matMetal, matLamp);
            AddCeilingLamp(envRoot.transform, "Lamp_Right", new Vector3(1.3f, 2.7f, 1.8f), matMetal, matLamp);

            // 9. Main Menu Controller
            GameObject controllerGo = new GameObject("MainMenuController");
            MainMenuController menuController = controllerGo.AddComponent<MainMenuController>();

            // 10. World Space Canvas UI (Overcooked Aesthetic)
            GameObject canvasGo = new GameObject("Menu_Canvas");
            canvasGo.transform.position = new Vector3(0f, 1.55f, 2.15f);
            canvasGo.transform.rotation = Quaternion.identity;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(850f, 820f);
            rect.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f); // ~1.85m wide x 1.80m tall

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

            // 3D Menu Board Frame (Overcooked vibrant turquoise frame)
            GameObject menuFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            menuFrame.name = "Menu_Board_Frame";
            menuFrame.transform.SetParent(canvasGo.transform, false);
            menuFrame.transform.localPosition = new Vector3(0f, 0f, 8f);
            menuFrame.transform.localScale = new Vector3(880f, 850f, 15f);
            menuFrame.GetComponent<MeshRenderer>().sharedMaterial = matCyan;
            UnityEngine.Object.DestroyImmediate(menuFrame.GetComponent<Collider>());

            // Main Background Panel (Soft warm white/cyan tint)
            GameObject mainPanel = CreateUIPanel("Main_Panel", canvasGo.transform, new Vector2(830f, 800f), new Color(0.93f, 0.97f, 1.0f, 0.98f));
            var mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(35, 35, 25, 25);
            mainLayout.spacing = 18f;
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = false;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            // --- HEADER BANNER (Overcooked Ribbon Style) ---
            GameObject headerBanner = CreateUIPanel("Header_Banner", mainPanel.transform, new Vector2(760f, 135f), new Color(0.12f, 0.64f, 0.88f, 1.0f));
            var hLayout = headerBanner.AddComponent<VerticalLayoutGroup>();
            hLayout.padding = new RectOffset(15, 15, 12, 12);
            hLayout.spacing = 4f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Main Title
            CreateUIText("Title_Text", headerBanner.transform, "COCINA BOLIVIANA", 52, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            // Subtitle
            CreateUIText("Subtitle_Text", headerBanner.transform, "- MENÚ PRINCIPAL -", 22, FontStyle.Bold, new Color(1.0f, 0.93f, 0.65f), TextAnchor.MiddleCenter);

            // --- 3 VERTICAL BUTTON CARDS ---
            // Button 1: NUEVA PARTIDA (Vibrant Emerald Green)
            CreateMenuCardButton("Btn_NuevaPartida", mainPanel.transform,
                "NUEVA PARTIDA", "Comienza tu servicio y cocina las mejores recetas",
                new Color(0.15f, 0.68f, 0.38f), new Color(0.20f, 0.80f, 0.44f), new Color(0.10f, 0.50f, 0.26f),
                menuController, nameof(menuController.NuevaPartida));

            // Button 2: CONTINUAR PARTIDA (Warm Amber Orange)
            CreateMenuCardButton("Btn_ContinuarPartida", mainPanel.transform,
                "CONTINUAR PARTIDA", "Retoma tu cocina y supera tu mejor puntuación",
                new Color(0.92f, 0.55f, 0.15f), new Color(0.98f, 0.65f, 0.20f), new Color(0.78f, 0.42f, 0.10f),
                menuController, nameof(menuController.ContinuarPartida));

            // Button 3: SALIR (Coral Crimson Red)
            CreateMenuCardButton("Btn_Salir", mainPanel.transform,
                "SALIR", "Cerrar el restaurante y salir del juego",
                new Color(0.85f, 0.28f, 0.22f), new Color(0.95f, 0.36f, 0.30f), new Color(0.68f, 0.18f, 0.14f),
                menuController, nameof(menuController.Salir));

            // Footer info
            CreateUIText("Footer_Text", mainPanel.transform, "★ Modo VR · Overcooked Style · Bolivia 2026 ★", 18, FontStyle.Normal, new Color(0.40f, 0.50f, 0.58f), TextAnchor.MiddleCenter);

            // 11. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MainMenuBuilder] Main Menu Scene created with {scene.GetRootGameObjects().Length} root objects and saved successfully at {ScenePath}");
        }

        private static GameObject CreateMenuCardButton(string name, Transform parent, string title, string subtitle, Color normalColor, Color hoverColor, Color pressedColor, MainMenuController controller, string methodName)
        {
            GameObject card = new GameObject(name);
            card.transform.SetParent(parent, false);

            var rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(760f, 145f);

            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 145f;
            le.preferredHeight = 145f;
            le.flexibleHeight = 0f;

            // Card Image
            var img = card.AddComponent<Image>();
            img.color = normalColor;

            // Card Button
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = hoverColor;
            cb.pressedColor = pressedColor;
            cb.selectedColor = hoverColor;
            cb.colorMultiplier = 1.0f;
            btn.colors = cb;

            // BoxCollider for reliable VR raycast / interaction
            var boxCol = card.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(760f, 145f, 2f);
            boxCol.center = Vector3.zero;

            // Hook UnityEvent onClick to Controller method
            var methodInfo = typeof(MainMenuController).GetMethod(methodName, Type.EmptyTypes);
            if (methodInfo != null && controller != null)
            {
                var action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), controller, methodInfo);
                UnityEventTools.AddPersistentListener(btn.onClick, action);
            }

            // Inner Layout
            var innerLayout = card.AddComponent<VerticalLayoutGroup>();
            innerLayout.padding = new RectOffset(25, 25, 18, 18);
            innerLayout.spacing = 6f;
            innerLayout.childAlignment = TextAnchor.MiddleCenter;
            innerLayout.childControlWidth = true;
            innerLayout.childControlHeight = true;

            // Title
            CreateUIText("Title", card.transform, title, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

            // Subtitle
            CreateUIText("Subtitle", card.transform, subtitle, 20, FontStyle.Italic, new Color(1f, 1f, 1f, 0.90f), TextAnchor.MiddleCenter);

            return card;
        }

        private static GameObject CreateUIPanel(string name, Transform parent, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static Text CreateUIText(string name, Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return txt;
        }

        private static void AddCeilingLamp(Transform parent, string name, Vector3 pos, Material matMetal, Material matLamp)
        {
            GameObject lamp = new GameObject(name);
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = pos;

            GameObject cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cord.name = "Cord";
            cord.transform.SetParent(lamp.transform, false);
            cord.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            cord.transform.localScale = new Vector3(0.02f, 0.2f, 0.02f);
            cord.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(cord.GetComponent<Collider>());

            GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shade.name = "Shade";
            shade.transform.SetParent(lamp.transform, false);
            shade.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            shade.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
            shade.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(shade.GetComponent<Collider>());

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            bulb.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            bulb.GetComponent<MeshRenderer>().sharedMaterial = matLamp;
            UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());

            GameObject ptLightGo = new GameObject("Pendant_Light");
            ptLightGo.transform.SetParent(lamp.transform, false);
            ptLightGo.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            Light ptLight = ptLightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = new Color(1.0f, 0.88f, 0.65f);
            ptLight.range = 3.5f;
            ptLight.intensity = 0.8f;
        }

        private static Material GetOrCreateMaterial(string name, Color color, float metallic, float smoothness, Shader shader)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
