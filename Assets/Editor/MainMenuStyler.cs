using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Configura y embellece la escena Main Menu:
    /// 1. Cierra el cuarto (pared trasera y techo) para que al girarse en VR no se vea vacío.
    /// 2. Aplica una paleta cohesiva y minimalista en tonos cafés/mocha a todo el menú.
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuStyler
    {
        private const string MainMenuScenePath = "Assets/Scenes/Main Menu.unity";
        private const string AutoRunSessionKey = "MainMenuStyler_AutoRun_v1";

        static MainMenuStyler()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (SessionState.GetBool(AutoRunSessionKey, false)) return;

            Scene active = EditorSceneManager.GetActiveScene();
            if (active.path == MainMenuScenePath)
            {
                SessionState.SetBool(AutoRunSessionKey, true);
                ApplyStyleAndCompleteRoom(active);
            }
        }

        [MenuItem("Kitchen/Style Main Menu & Complete Room")]
        public static void StyleMainMenuMenu()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            bool wasAnother = active.path != MainMenuScenePath;

            Scene menuScene = wasAnother
                ? EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single)
                : active;

            if (!menuScene.IsValid())
            {
                Debug.LogError($"[MainMenuStyler] No se pudo abrir {MainMenuScenePath}");
                return;
            }

            ApplyStyleAndCompleteRoom(menuScene);
        }

        public static void ApplyStyleAndCompleteRoom(Scene scene)
        {
            Debug.Log("[MainMenuStyler] Aplicando estilo café minimalista y completando habitación...");

            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Menu_Wall.mat");

            // 1. COMPLETAR LA HABITACIÓN (Pared trasera y techo)
            var envGo = GameObject.Find("Environment");
            Transform envParent = (envGo != null) ? envGo.transform : null;

            // Pared Trasera (detrás del jugador)
            var backWall = GameObject.Find("Back_Wall");
            if (backWall == null)
            {
                backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backWall.name = "Back_Wall";
                if (envParent != null) backWall.transform.SetParent(envParent, false);

                backWall.transform.localPosition = new Vector3(0f, 2f, -1.5f);
                backWall.transform.localScale = new Vector3(6.4f, 4f, 0.2f);
                backWall.transform.localRotation = Quaternion.identity;

                if (wallMat != null) backWall.GetComponent<MeshRenderer>().material = wallMat;
                Debug.Log("[MainMenuStyler] Pared trasera (Back_Wall) agregada.");
            }

            // Techo de la habitación
            var ceiling = GameObject.Find("Ceiling");
            if (ceiling == null)
            {
                ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.name = "Ceiling";
                if (envParent != null) ceiling.transform.SetParent(envParent, false);

                ceiling.transform.localPosition = new Vector3(0f, 4f, 1.5f);
                ceiling.transform.localScale = new Vector3(6.4f, 0.1f, 6.2f);
                ceiling.transform.localRotation = Quaternion.identity;

                if (wallMat != null) ceiling.GetComponent<MeshRenderer>().material = wallMat;
                Debug.Log("[MainMenuStyler] Techo (Ceiling) agregado.");
            }

            // Puerta decorativa rústica en la pared trasera para dar ambiente de restaurante
            var doorGo = GameObject.Find("Back_Door_Frame");
            if (doorGo == null)
            {
                doorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorGo.name = "Back_Door_Frame";
                if (envParent != null) doorGo.transform.SetParent(envParent, false);

                doorGo.transform.localPosition = new Vector3(0f, 1.15f, -1.39f);
                doorGo.transform.localScale = new Vector3(1.35f, 2.3f, 0.05f);
                doorGo.transform.localRotation = Quaternion.identity;

                var doorRend = doorGo.GetComponent<MeshRenderer>();
                // Material marrón madera oscura
                Material doorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                doorMat.color = new Color(0.22f, 0.15f, 0.10f); // Café espresso
                doorRend.material = doorMat;
            }

            // 2. PALETA DE COLORES CAFÉ MINIMALISTA PARA LA UI
            Color colorFondoCard = new Color(0.96f, 0.94f, 0.90f, 0.98f);       // Crema suave rústico
            Color colorHeader = new Color(0.22f, 0.15f, 0.10f, 1f);             // Café tostado oscuro / espresso
            Color colorTitulo = new Color(0.98f, 0.88f, 0.65f, 1f);             // Oro café con leche
            Color colorSubtitulo = new Color(0.85f, 0.78f, 0.70f, 0.9f);         // Crema suave

            // Colores unificados para todos los botones
            Color colorBotonNormal = new Color(0.38f, 0.25f, 0.17f, 1f);        // Mocha cálido (#61402B)
            Color colorBotonHover = new Color(0.50f, 0.35f, 0.24f, 1f);         // Caramelo (#80593D)
            Color colorBotonPressed = new Color(0.20f, 0.13f, 0.08f, 1f);       // Espresso oscuro (#332114)
            Color colorBotonSelected = new Color(0.46f, 0.32f, 0.22f, 1f);      // Caramelo medio

            Color colorTextoBoton = new Color(0.99f, 0.98f, 0.95f, 1f);          // Blanco marfil
            Color colorDescBoton = new Color(0.88f, 0.82f, 0.75f, 0.92f);        // Crema latte

            // Aplicar al fondo del panel principal (Card)
            var cardImage = GameObject.Find("Card")?.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = colorFondoCard;
            }

            // Aplicar al encabezado (Header)
            var headerGo = GameObject.Find("Header");
            if (headerGo != null)
            {
                var headerImg = headerGo.GetComponent<Image>();
                if (headerImg != null) headerImg.color = colorHeader;

                var titleTxt = headerGo.transform.Find("Title")?.GetComponent<Text>();
                if (titleTxt != null) titleTxt.color = colorTitulo;

                var subTxt = headerGo.transform.Find("Subtitle")?.GetComponent<Text>();
                if (subTxt != null) subTxt.color = colorSubtitulo;

                var badgeTxt = headerGo.transform.Find("Badge")?.GetComponent<Text>();
                if (badgeTxt != null) badgeTxt.color = new Color(0.78f, 0.70f, 0.62f, 0.9f);
            }

            // Aplicar a los 3 botones: NuevaPartida, ContinuarPartida, Salir
            string[] botonesNombres = new string[] { "Btn_NuevaPartida", "Btn_ContinuarPartida", "Btn_Salir" };
            foreach (var btnName in botonesNombres)
            {
                var btnGo = GameObject.Find(btnName);
                if (btnGo == null) continue;

                // Color de la imagen base
                var btnImg = btnGo.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = colorBotonNormal;
                }

                // Estados del botón (Button.colors)
                var btnComp = btnGo.GetComponent<Button>();
                if (btnComp != null)
                {
                    var cb = btnComp.colors;
                    cb.normalColor = colorBotonNormal;
                    cb.highlightedColor = colorBotonHover;
                    cb.pressedColor = colorBotonPressed;
                    cb.selectedColor = colorBotonSelected;
                    btnComp.colors = cb;
                }

                // Textos internos del botón
                var labelTxt = btnGo.transform.Find("Label")?.GetComponent<Text>();
                if (labelTxt != null)
                {
                    labelTxt.color = colorTextoBoton;
                    labelTxt.raycastTarget = false;
                }

                var descTxt = btnGo.transform.Find("Description")?.GetComponent<Text>();
                if (descTxt != null)
                {
                    descTxt.color = colorDescBoton;
                    descTxt.raycastTarget = false;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[MainMenuStyler] ¡Main Menu actualizado exitosamente con cuarto completo y paleta café minimalista!");
        }
    }
}
