using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CocinaBoliviana;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Generador del prefab LevelManager y configurador del sistema de puntuación Overcooked
    /// para First Scene y cualquier nivel futuro (Niveles 2, 3, 4).
    /// </summary>
    [InitializeOnLoad]
    public static class LevelSetup
    {
        private const string FirstScenePath = "Assets/Scenes/First Scene.unity";
        private const string PrefabFolder = "Assets/02_Prefabs/LevelManagement";
        private const string PrefabPath = "Assets/02_Prefabs/LevelManagement/LevelManager.prefab";
        private const string AutoRunSessionKey = "LevelSetup_AutoRun_Completed_v4";

        static LevelSetup()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (SessionState.GetBool(AutoRunSessionKey, false)) return;

            bool prefabExists = System.IO.File.Exists(PrefabPath);
            var existingInScene = Object.FindAnyObjectByType<LevelManager>();
            bool isIncomplete = existingInScene != null && existingInScene.transform.Find("LevelScreenHUD_Canvas") == null;

            if (!prefabExists || existingInScene == null || isIncomplete)
            {
                SessionState.SetBool(AutoRunSessionKey, true);
                SetupFirstScene();
            }
        }

        [MenuItem("Kitchen/Setup Level Score System (First Scene)")]
        public static void SetupFirstScene()
        {
            GameObject prefab = BuildLevelManagerPrefab();
            if (prefab == null) return;

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != FirstScenePath)
            {
                scene = EditorSceneManager.OpenScene(FirstScenePath, OpenSceneMode.Single);
            }

            AttachToScene(prefab, scene);
        }

        [MenuItem("Kitchen/Setup Level Score System (Active Scene)")]
        public static void SetupActiveScene()
        {
            GameObject prefab = BuildLevelManagerPrefab();
            if (prefab == null) return;

            Scene scene = EditorSceneManager.GetActiveScene();
            AttachToScene(prefab, scene);
        }

        private static void AttachToScene(GameObject prefab, Scene scene)
        {
            if (!scene.IsValid())
            {
                Debug.LogError("[LevelSetup] La escena no es válida.");
                return;
            }

            var existing = Object.FindAnyObjectByType<LevelManager>();
            if (existing != null)
            {
                // Si la instancia existente no tiene el Canvas de la UI, la eliminamos para reemplazarla por la completa
                if (existing.transform.Find("LevelScreenHUD_Canvas") == null)
                {
                    Debug.Log($"[LevelSetup] Reemplazando instancia incompleta de LevelManager en '{scene.name}'...");
                    Object.DestroyImmediate(existing.gameObject);
                    existing = null;
                }
            }

            if (existing == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "LevelManager";
                Debug.Log($"[LevelSetup] Instancia de LevelManager con HUD agregada a la escena '{scene.name}'.");
            }
            else
            {
                Debug.Log($"[LevelSetup] LevelManager con HUD ya existe en la escena '{scene.name}'.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LevelSetup] ¡Sistema de puntuación Overcooked configurado y guardado en '{scene.name}'!");
        }

        [MenuItem("Kitchen/Rebuild LevelManager Prefab Only")]
        public static GameObject BuildLevelManagerPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/02_Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "02_Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/02_Prefabs", "LevelManagement");
            }

            Sprite roundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VRTemplateAssets/Sprites/UI/Round Radius 10.png");
            Sprite outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VRTemplateAssets/Sprites/UI/Round Radius 10 Outline.png");
            Font mainFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf");
            if (mainFont == null) mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            AudioClip sfxExito = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/06_SFX/exito.mp3");
            AudioClip sfxPop = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_22_click.wav");
            AudioClip sfxCorte = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/06_SFX/corte.mp3");

            // 1. Root GameObject: LevelManager
            var rootGo = new GameObject("LevelManager");
            var audioSource = rootGo.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            var lmComp = rootGo.AddComponent<LevelManager>();

            // Configurar SerializedObject de LevelManager
            var lmSo = new SerializedObject(lmComp);
            lmSo.FindProperty("nombreNivel").stringValue = "Nivel 1 - Cochabamba";
            lmSo.FindProperty("duracionNivel").floatValue = 150f;
            lmSo.FindProperty("segundosCuentaAtras").floatValue = 3.5f;
            lmSo.FindProperty("objetivoPuntos1Estrella").intValue = 80;
            lmSo.FindProperty("objetivoPuntos2Estrellas").intValue = 150;
            lmSo.FindProperty("objetivoPuntos3Estrellas").intValue = 220;
            lmSo.FindProperty("penalizacionPedidoPerdido").intValue = 20;
            lmSo.FindProperty("bonusPorNivelRacha").intValue = 10;
            lmSo.FindProperty("maxNivelRacha").intValue = 4;

            if (sfxPop != null) lmSo.FindProperty("sonidoCuentaAtras").objectReferenceValue = sfxPop;
            if (sfxExito != null) lmSo.FindProperty("sonidoSilbatoInicio").objectReferenceValue = sfxExito;
            if (sfxExito != null) lmSo.FindProperty("sonidoEntregaExitosa").objectReferenceValue = sfxExito;
            if (sfxCorte != null) lmSo.FindProperty("sonidoPedidoPerdido").objectReferenceValue = sfxCorte;
            if (sfxExito != null) lmSo.FindProperty("sonidoVictoria").objectReferenceValue = sfxExito;
            if (sfxCorte != null) lmSo.FindProperty("sonidoDerrota").objectReferenceValue = sfxCorte;
            lmSo.ApplyModifiedPropertiesWithoutUndo();

            // 2. Canvas Screen-Space: LevelScreenHUD
            var canvasGo = new GameObject("LevelScreenHUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(rootGo.transform, false);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) canvasGo.layer = uiLayer;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            // Compatibilidad total con mandos VR (ray interactor)
            canvasGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var hudComp = canvasGo.AddComponent<LevelScreenHUD>();
            var hudSo = new SerializedObject(hudComp);

            // ==========================================
            // 3. BARRA SUPERIOR (TOP HUD)
            // ==========================================
            var topBarGo = new GameObject("TopHUDBar", typeof(RectTransform));
            topBarGo.transform.SetParent(canvasGo.transform, false);
            var topBarRt = topBarGo.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 1f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.anchoredPosition = new Vector2(0f, -12f);
            topBarRt.sizeDelta = new Vector2(-60f, 85f);

            // 3.1 Temporizador (Izquierda)
            var timerCardGo = new GameObject("TimerCard", typeof(RectTransform), typeof(Image));
            timerCardGo.transform.SetParent(topBarGo.transform, false);
            var timerCardRt = timerCardGo.GetComponent<RectTransform>();
            timerCardRt.anchorMin = new Vector2(0f, 0.5f);
            timerCardRt.anchorMax = new Vector2(0f, 0.5f);
            timerCardRt.pivot = new Vector2(0f, 0.5f);
            timerCardRt.anchoredPosition = new Vector2(10f, 0f);
            timerCardRt.sizeDelta = new Vector2(250f, 75f);
            var timerImg = timerCardGo.GetComponent<Image>();
            if (roundSprite != null) { timerImg.sprite = roundSprite; timerImg.type = Image.Type.Sliced; }
            timerImg.color = new Color(0.08f, 0.10f, 0.14f, 0.90f);

            var timerTextGo = new GameObject("TextoTiempo", typeof(RectTransform), typeof(Text));
            timerTextGo.transform.SetParent(timerCardGo.transform, false);
            var timerTextRt = timerTextGo.GetComponent<RectTransform>();
            timerTextRt.anchorMin = new Vector2(0f, 0.25f);
            timerTextRt.anchorMax = new Vector2(1f, 1f);
            timerTextRt.offsetMin = new Vector2(15f, 0f);
            timerTextRt.offsetMax = new Vector2(-15f, 0f);
            var timerTxt = timerTextGo.GetComponent<Text>();
            timerTxt.text = "02:30";
            timerTxt.font = mainFont;
            timerTxt.fontSize = 32;
            timerTxt.fontStyle = FontStyle.Bold;
            timerTxt.alignment = TextAnchor.MiddleCenter;
            timerTxt.color = Color.white;

            var timerBarBgGo = new GameObject("BarraTiempoBG", typeof(RectTransform), typeof(Image));
            timerBarBgGo.transform.SetParent(timerCardGo.transform, false);
            var timerBarBgRt = timerBarBgGo.GetComponent<RectTransform>();
            timerBarBgRt.anchorMin = new Vector2(0.08f, 0.12f);
            timerBarBgRt.anchorMax = new Vector2(0.92f, 0.22f);
            timerBarBgRt.offsetMin = Vector2.zero;
            timerBarBgRt.offsetMax = Vector2.zero;
            timerBarBgGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            var timerBarGo = new GameObject("BarraTiempoRelleno", typeof(RectTransform), typeof(Image));
            timerBarGo.transform.SetParent(timerBarBgGo.transform, false);
            var timerBarRt = timerBarGo.GetComponent<RectTransform>();
            timerBarRt.anchorMin = Vector2.zero;
            timerBarRt.anchorMax = Vector2.one;
            timerBarRt.offsetMin = Vector2.zero;
            timerBarRt.offsetMax = Vector2.zero;
            var timerBarImg = timerBarGo.GetComponent<Image>();
            timerBarImg.type = Image.Type.Filled;
            timerBarImg.fillMethod = Image.FillMethod.Horizontal;
            timerBarImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerBarImg.fillAmount = 1f;
            timerBarImg.color = new Color(0.2f, 0.85f, 0.45f);

            // 3.2 Puntuación y Estrellas (Centro)
            var scoreCardGo = new GameObject("ScoreCard", typeof(RectTransform), typeof(Image));
            scoreCardGo.transform.SetParent(topBarGo.transform, false);
            var scoreCardRt = scoreCardGo.GetComponent<RectTransform>();
            scoreCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            scoreCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            scoreCardRt.pivot = new Vector2(0.5f, 0.5f);
            scoreCardRt.anchoredPosition = Vector2.zero;
            scoreCardRt.sizeDelta = new Vector2(360f, 75f);
            var scoreImg = scoreCardGo.GetComponent<Image>();
            if (roundSprite != null) { scoreImg.sprite = roundSprite; scoreImg.type = Image.Type.Sliced; }
            scoreImg.color = new Color(0.08f, 0.10f, 0.14f, 0.90f);

            var scoreTextGo = new GameObject("TextoPuntos", typeof(RectTransform), typeof(Text));
            scoreTextGo.transform.SetParent(scoreCardGo.transform, false);
            var scoreTextRt = scoreTextGo.GetComponent<RectTransform>();
            scoreTextRt.anchorMin = new Vector2(0f, 0.35f);
            scoreTextRt.anchorMax = new Vector2(1f, 1f);
            scoreTextRt.offsetMin = new Vector2(10f, 0f);
            scoreTextRt.offsetMax = new Vector2(-10f, 0f);
            var scoreTxt = scoreTextGo.GetComponent<Text>();
            scoreTxt.text = "0 PTS";
            scoreTxt.font = mainFont;
            scoreTxt.fontSize = 32;
            scoreTxt.fontStyle = FontStyle.Bold;
            scoreTxt.alignment = TextAnchor.MiddleCenter;
            scoreTxt.color = new Color(1.0f, 0.82f, 0.22f); // Oro brillante

            var starTextGo = new GameObject("TextoEstrellas", typeof(RectTransform), typeof(Text));
            starTextGo.transform.SetParent(scoreCardGo.transform, false);
            var starTextRt = starTextGo.GetComponent<RectTransform>();
            starTextRt.anchorMin = new Vector2(0f, 0f);
            starTextRt.anchorMax = new Vector2(1f, 0.40f);
            starTextRt.offsetMin = new Vector2(10f, 4f);
            starTextRt.offsetMax = new Vector2(-10f, 0f);
            var starTxt = starTextGo.GetComponent<Text>();
            starTxt.text = "☆☆☆  (Meta: 80)";
            starTxt.font = mainFont;
            starTxt.fontSize = 18;
            starTxt.alignment = TextAnchor.MiddleCenter;
            starTxt.color = new Color(0.85f, 0.90f, 0.95f);

            // 3.3 Medidor de Racha (Derecha)
            var rachaGo = new GameObject("RachaCard", typeof(RectTransform), typeof(Image));
            rachaGo.transform.SetParent(topBarGo.transform, false);
            var rachaRt = rachaGo.GetComponent<RectTransform>();
            rachaRt.anchorMin = new Vector2(1f, 0.5f);
            rachaRt.anchorMax = new Vector2(1f, 0.5f);
            rachaRt.pivot = new Vector2(1f, 0.5f);
            rachaRt.anchoredPosition = new Vector2(-10f, 0f);
            rachaRt.sizeDelta = new Vector2(250f, 75f);
            var rachaImg = rachaGo.GetComponent<Image>();
            if (roundSprite != null) { rachaImg.sprite = roundSprite; rachaImg.type = Image.Type.Sliced; }
            rachaImg.color = new Color(0.85f, 0.35f, 0.08f, 0.92f); // Naranja fuego

            var rachaTextGo = new GameObject("TextoRacha", typeof(RectTransform), typeof(Text));
            rachaTextGo.transform.SetParent(rachaGo.transform, false);
            var rachaTextRt = rachaTextGo.GetComponent<RectTransform>();
            rachaTextRt.anchorMin = new Vector2(0f, 0.4f);
            rachaTextRt.anchorMax = new Vector2(1f, 1f);
            rachaTextRt.offsetMin = new Vector2(10f, 0f);
            rachaTextRt.offsetMax = new Vector2(-10f, 0f);
            var rachaTxt = rachaTextGo.GetComponent<Text>();
            rachaTxt.text = "RACHA x2";
            rachaTxt.font = mainFont;
            rachaTxt.fontSize = 24;
            rachaTxt.fontStyle = FontStyle.Bold;
            rachaTxt.alignment = TextAnchor.MiddleCenter;
            rachaTxt.color = Color.white;

            var bonusTextGo = new GameObject("TextoBonus", typeof(RectTransform), typeof(Text));
            bonusTextGo.transform.SetParent(rachaGo.transform, false);
            var bonusTextRt = bonusTextGo.GetComponent<RectTransform>();
            bonusTextRt.anchorMin = new Vector2(0f, 0f);
            bonusTextRt.anchorMax = new Vector2(1f, 0.45f);
            bonusTextRt.offsetMin = new Vector2(10f, 4f);
            bonusTextRt.offsetMax = new Vector2(-10f, 0f);
            var bonusTxt = bonusTextGo.GetComponent<Text>();
            bonusTxt.text = "+10 PROPINA";
            bonusTxt.font = mainFont;
            bonusTxt.fontSize = 18;
            bonusTxt.fontStyle = FontStyle.Bold;
            bonusTxt.alignment = TextAnchor.MiddleCenter;
            bonusTxt.color = new Color(1.0f, 0.95f, 0.6f);

            // Racha oculta al inicio (aparece al encadenar entregas)
            rachaGo.SetActive(false);

            // ==========================================
            // 4. TEXTO FLOTANTE DE FEEDBACK (+/- PUNTOS)
            // ==========================================
            var feedbackGo = new GameObject("FeedbackPuntos", typeof(RectTransform), typeof(Text));
            feedbackGo.transform.SetParent(canvasGo.transform, false);
            var feedbackRt = feedbackGo.GetComponent<RectTransform>();
            feedbackRt.anchorMin = new Vector2(0.5f, 0.5f);
            feedbackRt.anchorMax = new Vector2(0.5f, 0.5f);
            feedbackRt.pivot = new Vector2(0.5f, 0.5f);
            feedbackRt.anchoredPosition = new Vector2(0f, 140f);
            feedbackRt.sizeDelta = new Vector2(500f, 60f);
            var feedbackTxt = feedbackGo.GetComponent<Text>();
            feedbackTxt.text = "+35 PTS (¡Racha x2!)";
            feedbackTxt.font = mainFont;
            feedbackTxt.fontSize = 36;
            feedbackTxt.fontStyle = FontStyle.Bold;
            feedbackTxt.alignment = TextAnchor.MiddleCenter;
            feedbackTxt.color = Color.green;
            feedbackGo.SetActive(false);

            // ==========================================
            // 5. BANNER INICIAL (CUENTA REGRESIVA)
            // ==========================================
            var startBannerGo = new GameObject("StartBanner", typeof(RectTransform), typeof(Image));
            startBannerGo.transform.SetParent(canvasGo.transform, false);
            var startBannerRt = startBannerGo.GetComponent<RectTransform>();
            startBannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            startBannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            startBannerRt.pivot = new Vector2(0.5f, 0.5f);
            startBannerRt.anchoredPosition = Vector2.zero;
            startBannerRt.sizeDelta = new Vector2(720f, 340f);
            var startBannerImg = startBannerGo.GetComponent<Image>();
            if (roundSprite != null) { startBannerImg.sprite = roundSprite; startBannerImg.type = Image.Type.Sliced; }
            startBannerImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

            var startBorderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            startBorderGo.transform.SetParent(startBannerGo.transform, false);
            var startBorderRt = startBorderGo.GetComponent<RectTransform>();
            startBorderRt.anchorMin = Vector2.zero;
            startBorderRt.anchorMax = Vector2.one;
            startBorderRt.offsetMin = Vector2.zero;
            startBorderRt.offsetMax = Vector2.zero;
            var startBorderImg = startBorderGo.GetComponent<Image>();
            if (outlineSprite != null) { startBorderImg.sprite = outlineSprite; startBorderImg.type = Image.Type.Sliced; }
            startBorderImg.color = new Color(0.24f, 0.52f, 0.95f, 0.45f);

            var levelNameGo = new GameObject("TextoNombreNivel", typeof(RectTransform), typeof(Text));
            levelNameGo.transform.SetParent(startBannerGo.transform, false);
            var levelNameRt = levelNameGo.GetComponent<RectTransform>();
            levelNameRt.anchorMin = new Vector2(0f, 0.68f);
            levelNameRt.anchorMax = new Vector2(1f, 0.95f);
            levelNameRt.offsetMin = new Vector2(20f, 0f);
            levelNameRt.offsetMax = new Vector2(-20f, 0f);
            var levelNameTxt = levelNameGo.GetComponent<Text>();
            levelNameTxt.text = "NIVEL 1 - COCHABAMBA";
            levelNameTxt.font = mainFont;
            levelNameTxt.fontSize = 32;
            levelNameTxt.fontStyle = FontStyle.Bold;
            levelNameTxt.alignment = TextAnchor.MiddleCenter;
            levelNameTxt.color = new Color(0.35f, 0.75f, 1f);

            var goalTextGo = new GameObject("TextoObjetivoInicial", typeof(RectTransform), typeof(Text));
            goalTextGo.transform.SetParent(startBannerGo.transform, false);
            var goalTextRt = goalTextGo.GetComponent<RectTransform>();
            goalTextRt.anchorMin = new Vector2(0f, 0.50f);
            goalTextRt.anchorMax = new Vector2(1f, 0.70f);
            goalTextRt.offsetMin = new Vector2(20f, 0f);
            goalTextRt.offsetMax = new Vector2(-20f, 0f);
            var goalTxt = goalTextGo.GetComponent<Text>();
            goalTxt.text = "OBJETIVO: 80 PUNTOS (1★)   |   TIEMPO: 02:30";
            goalTxt.font = mainFont;
            goalTxt.fontSize = 20;
            goalTxt.alignment = TextAnchor.MiddleCenter;
            goalTxt.color = new Color(0.85f, 0.90f, 0.95f);

            var countdownTextGo = new GameObject("TextoCuentaAtras", typeof(RectTransform), typeof(Text));
            countdownTextGo.transform.SetParent(startBannerGo.transform, false);
            var countdownTextRt = countdownTextGo.GetComponent<RectTransform>();
            countdownTextRt.anchorMin = new Vector2(0f, 0.05f);
            countdownTextRt.anchorMax = new Vector2(1f, 0.55f);
            countdownTextRt.offsetMin = Vector2.zero;
            countdownTextRt.offsetMax = Vector2.zero;
            var countdownTxt = countdownTextGo.GetComponent<Text>();
            countdownTxt.text = "3";
            countdownTxt.font = mainFont;
            countdownTxt.fontSize = 76;
            countdownTxt.fontStyle = FontStyle.Bold;
            countdownTxt.alignment = TextAnchor.MiddleCenter;
            countdownTxt.color = new Color(1.0f, 0.82f, 0.22f);

            // ==========================================
            // 6. MODAL DE FIN DE NIVEL (RESULTADOS)
            // ==========================================
            var endModalGo = new GameObject("EndLevelModal", typeof(RectTransform), typeof(Image));
            endModalGo.transform.SetParent(canvasGo.transform, false);
            var endModalRt = endModalGo.GetComponent<RectTransform>();
            endModalRt.anchorMin = Vector2.zero;
            endModalRt.anchorMax = Vector2.one;
            endModalRt.offsetMin = Vector2.zero;
            endModalRt.offsetMax = Vector2.zero;
            endModalGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f); // Fondo oscuro semitransparente

            var endCardGo = new GameObject("DialogCard", typeof(RectTransform), typeof(Image));
            endCardGo.transform.SetParent(endModalGo.transform, false);
            var endCardRt = endCardGo.GetComponent<RectTransform>();
            endCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            endCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            endCardRt.pivot = new Vector2(0.5f, 0.5f);
            endCardRt.anchoredPosition = Vector2.zero;
            endCardRt.sizeDelta = new Vector2(680f, 520f);
            var endCardImg = endCardGo.GetComponent<Image>();
            if (roundSprite != null) { endCardImg.sprite = roundSprite; endCardImg.type = Image.Type.Sliced; }
            endCardImg.color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

            var endBorderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            endBorderGo.transform.SetParent(endCardGo.transform, false);
            var endBorderRt = endBorderGo.GetComponent<RectTransform>();
            endBorderRt.anchorMin = Vector2.zero;
            endBorderRt.anchorMax = Vector2.one;
            endBorderRt.offsetMin = Vector2.zero;
            endBorderRt.offsetMax = Vector2.zero;
            var endBorderImg = endBorderGo.GetComponent<Image>();
            if (outlineSprite != null) { endBorderImg.sprite = outlineSprite; endBorderImg.type = Image.Type.Sliced; }
            endBorderImg.color = new Color(0.4f, 0.55f, 0.8f, 0.35f);

            // Título Fin
            var endTitleGo = new GameObject("TextoTituloFin", typeof(RectTransform), typeof(Text));
            endTitleGo.transform.SetParent(endCardGo.transform, false);
            var endTitleRt = endTitleGo.GetComponent<RectTransform>();
            endTitleRt.anchorMin = new Vector2(0f, 0.78f);
            endTitleRt.anchorMax = new Vector2(1f, 0.95f);
            endTitleRt.offsetMin = new Vector2(20f, 0f);
            endTitleRt.offsetMax = new Vector2(-20f, 0f);
            var endTitleTxt = endTitleGo.GetComponent<Text>();
            endTitleTxt.text = "¡NIVEL COMPLETADO!";
            endTitleTxt.font = mainFont;
            endTitleTxt.fontSize = 38;
            endTitleTxt.fontStyle = FontStyle.Bold;
            endTitleTxt.alignment = TextAnchor.MiddleCenter;
            endTitleTxt.color = new Color(1.0f, 0.84f, 0.2f);

            // Subtítulo
            var endSubGo = new GameObject("TextoSubtituloFin", typeof(RectTransform), typeof(Text));
            endSubGo.transform.SetParent(endCardGo.transform, false);
            var endSubRt = endSubGo.GetComponent<RectTransform>();
            endSubRt.anchorMin = new Vector2(0f, 0.68f);
            endSubRt.anchorMax = new Vector2(1f, 0.78f);
            endSubRt.offsetMin = new Vector2(20f, 0f);
            endSubRt.offsetMax = new Vector2(-20f, 0f);
            var endSubTxt = endSubGo.GetComponent<Text>();
            endSubTxt.text = "¡Excelente servicio en Cochabamba!";
            endSubTxt.font = mainFont;
            endSubTxt.fontSize = 20;
            endSubTxt.alignment = TextAnchor.MiddleCenter;
            endSubTxt.color = new Color(0.85f, 0.90f, 0.95f);

            // Estrellas Fin
            var endStarsGo = new GameObject("TextoEstrellasFin", typeof(RectTransform), typeof(Text));
            endStarsGo.transform.SetParent(endCardGo.transform, false);
            var endStarsRt = endStarsGo.GetComponent<RectTransform>();
            endStarsRt.anchorMin = new Vector2(0f, 0.50f);
            endStarsRt.anchorMax = new Vector2(1f, 0.68f);
            endStarsRt.offsetMin = Vector2.zero;
            endStarsRt.offsetMax = Vector2.zero;
            var endStarsTxt = endStarsGo.GetComponent<Text>();
            endStarsTxt.text = "★★★";
            endStarsTxt.font = mainFont;
            endStarsTxt.fontSize = 58;
            endStarsTxt.alignment = TextAnchor.MiddleCenter;
            endStarsTxt.color = new Color(1.0f, 0.85f, 0.2f);

            // Resumen de Stats
            var statsGo = new GameObject("TextoStats", typeof(RectTransform), typeof(Text));
            statsGo.transform.SetParent(endCardGo.transform, false);
            var statsRt = statsGo.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.1f, 0.20f);
            statsRt.anchorMax = new Vector2(0.9f, 0.50f);
            statsRt.offsetMin = Vector2.zero;
            statsRt.offsetMax = Vector2.zero;
            var statsTxt = statsGo.GetComponent<Text>();
            statsTxt.text = "Puntos Finales: <b>120</b> (Meta: 80)\nPlatos Entregados: <b>4</b>\nPedidos Perdidos: <b>0</b>\nMejor Racha: <b>x3</b>";
            statsTxt.font = mainFont;
            statsTxt.fontSize = 22;
            statsTxt.lineSpacing = 1.3f;
            statsTxt.alignment = TextAnchor.MiddleCenter;
            statsTxt.color = Color.white;

            // Botones de Acción
            var btnReintentarGo = new GameObject("BotonReintentar", typeof(RectTransform), typeof(Image), typeof(Button));
            btnReintentarGo.transform.SetParent(endCardGo.transform, false);
            var btnReintentarRt = btnReintentarGo.GetComponent<RectTransform>();
            btnReintentarRt.anchorMin = new Vector2(0.12f, 0.06f);
            btnReintentarRt.anchorMax = new Vector2(0.48f, 0.17f);
            btnReintentarRt.offsetMin = Vector2.zero;
            btnReintentarRt.offsetMax = Vector2.zero;
            var btnReintentarImg = btnReintentarGo.GetComponent<Image>();
            if (roundSprite != null) { btnReintentarImg.sprite = roundSprite; btnReintentarImg.type = Image.Type.Sliced; }
            btnReintentarImg.color = new Color(0.10f, 0.65f, 0.40f, 0.95f); // Verde esmeralda
            var btnReintentarComp = btnReintentarGo.GetComponent<Button>();

            var btnReintentarTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnReintentarTextGo.transform.SetParent(btnReintentarGo.transform, false);
            var btnReintentarTextRt = btnReintentarTextGo.GetComponent<RectTransform>();
            btnReintentarTextRt.anchorMin = Vector2.zero;
            btnReintentarTextRt.anchorMax = Vector2.one;
            btnReintentarTextRt.offsetMin = Vector2.zero;
            btnReintentarTextRt.offsetMax = Vector2.zero;
            var btnReintentarTxt = btnReintentarTextGo.GetComponent<Text>();
            btnReintentarTxt.text = "REINTENTAR  [R]";
            btnReintentarTxt.font = mainFont;
            btnReintentarTxt.fontSize = 22;
            btnReintentarTxt.fontStyle = FontStyle.Bold;
            btnReintentarTxt.alignment = TextAnchor.MiddleCenter;
            btnReintentarTxt.color = Color.white;
            btnReintentarTxt.raycastTarget = false; // Permite que el click llegue al botón directamente

            var btnMenuGo = new GameObject("BotonMenuPrincipal", typeof(RectTransform), typeof(Image), typeof(Button));
            btnMenuGo.transform.SetParent(endCardGo.transform, false);
            var btnMenuRt = btnMenuGo.GetComponent<RectTransform>();
            btnMenuRt.anchorMin = new Vector2(0.52f, 0.06f);
            btnMenuRt.anchorMax = new Vector2(0.88f, 0.17f);
            btnMenuRt.offsetMin = Vector2.zero;
            btnMenuRt.offsetMax = Vector2.zero;
            var btnMenuImg = btnMenuGo.GetComponent<Image>();
            if (roundSprite != null) { btnMenuImg.sprite = roundSprite; btnMenuImg.type = Image.Type.Sliced; }
            btnMenuImg.color = new Color(0.20f, 0.28f, 0.40f, 0.95f); // Pizarra azul
            var btnMenuComp = btnMenuGo.GetComponent<Button>();

            var btnMenuTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnMenuTextGo.transform.SetParent(btnMenuGo.transform, false);
            var btnMenuTextRt = btnMenuTextGo.GetComponent<RectTransform>();
            btnMenuTextRt.anchorMin = Vector2.zero;
            btnMenuTextRt.anchorMax = Vector2.one;
            btnMenuTextRt.offsetMin = Vector2.zero;
            btnMenuTextRt.offsetMax = Vector2.zero;
            var btnMenuTxt = btnMenuTextGo.GetComponent<Text>();
            btnMenuTxt.text = "MENÚ PRINCIPAL  [M]";
            btnMenuTxt.font = mainFont;
            btnMenuTxt.fontSize = 20;
            btnMenuTxt.fontStyle = FontStyle.Bold;
            btnMenuTxt.alignment = TextAnchor.MiddleCenter;
            btnMenuTxt.color = Color.white;
            btnMenuTxt.raycastTarget = false; // Permite que el click llegue al botón directamente

            // Ocultar modal final por defecto
            endModalGo.SetActive(false);

            // ==========================================
            // 7. CONECTAR REFERENCIAS EN LEVELSCREENHUD
            // ==========================================
            hudSo.FindProperty("topBarRoot").objectReferenceValue = topBarGo;
            hudSo.FindProperty("textoTiempo").objectReferenceValue = timerTxt;
            hudSo.FindProperty("barraTiempo").objectReferenceValue = timerBarImg;
            hudSo.FindProperty("textoPuntos").objectReferenceValue = scoreTxt;
            hudSo.FindProperty("barraProgresoEstrellas").objectReferenceValue = null;
            hudSo.FindProperty("textoEstrellasHUD").objectReferenceValue = starTxt;

            hudSo.FindProperty("rachaRoot").objectReferenceValue = rachaGo;
            hudSo.FindProperty("textoRacha").objectReferenceValue = rachaTxt;
            hudSo.FindProperty("textoBonus").objectReferenceValue = bonusTxt;

            hudSo.FindProperty("startBannerRoot").objectReferenceValue = startBannerGo;
            hudSo.FindProperty("textoNombreNivel").objectReferenceValue = levelNameTxt;
            hudSo.FindProperty("textoObjetivoInicial").objectReferenceValue = goalTxt;
            hudSo.FindProperty("textoCuentaAtras").objectReferenceValue = countdownTxt;

            hudSo.FindProperty("textoFeedbackPuntos").objectReferenceValue = feedbackTxt;

            hudSo.FindProperty("endModalRoot").objectReferenceValue = endModalGo;
            hudSo.FindProperty("textoTituloFin").objectReferenceValue = endTitleTxt;
            hudSo.FindProperty("textoSubtituloFin").objectReferenceValue = endSubTxt;
            hudSo.FindProperty("textoEstrellasFin").objectReferenceValue = endStarsTxt;
            hudSo.FindProperty("textoResumenStats").objectReferenceValue = statsTxt;
            hudSo.FindProperty("botonReintentar").objectReferenceValue = btnReintentarComp;
            hudSo.FindProperty("botonMenuPrincipal").objectReferenceValue = btnMenuComp;

            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Guardar prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath);
            Object.DestroyImmediate(rootGo);

            Debug.Log($"[LevelSetup] Prefab LevelManager creado exitosamente en {PrefabPath}");
            return savedPrefab;
        }
    }
}
