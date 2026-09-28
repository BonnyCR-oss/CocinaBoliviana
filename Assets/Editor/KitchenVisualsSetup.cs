using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Sistema integral de mejora gráfica y game feel para Cocina Boliviana:
    /// 1. Generación y aplicación de texturas PBR (madera rústica, acero inoxidable cepillado, baldosas, filtros).
    /// 2. Cajones de ingredientes huacales: listones de madera, postes biselados, herrajes de hierro,
    ///    placas de pizarra identificadoras y producto 3D en su interior (papas, carnes, verduras).
    /// 3. Mesa central de chef: mesón de acero inoxidable con biseles redondeados, patas tubulares metálicas,
    ///    estante inferior con ollas y bandejas gastronorm, tabla de picar de bambú encastrada y toallero lateral.
    /// 4. Extractores de cocina y entrega: campana trapezoidal comercial, ductos con bridas y remaches,
    ///    caja de control con interruptores y manómetro analógico, deflectores baffle y luz cálida de trabajo.
    /// 5. Zona de entrega: riel de comandas con tickets de pedidos, letrero de entrega, bordes de seguridad
    ///    hazard en la cinta y campana de servicio sobre peana de caoba.
    /// 6. Main Menu: recepción rústica, lámparas colgantes cálidas, ambiente de taberna y atrezzo culinario.
    /// </summary>
    public static class KitchenVisualsSetup
    {
        // Texturas
        private const string TexFolder = "Assets/Materials/Textures";
        private const string TexWoodAlbedo = "Assets/Materials/Textures/Tex_RusticWood_Albedo.png";
        private const string TexWoodNormal = "Assets/Materials/Textures/Tex_RusticWood_Normal.png";
        private const string TexSteelAlbedo = "Assets/Materials/Textures/Tex_BrushedSteel_Albedo.png";
        private const string TexSteelNormal = "Assets/Materials/Textures/Tex_BrushedSteel_Normal.png";
        private const string TexFilterAlbedo = "Assets/Materials/Textures/Tex_BaffleFilter_Albedo.png";
        private const string TexFilterNormal = "Assets/Materials/Textures/Tex_BaffleFilter_Normal.png";
        private const string TexTileAlbedo = "Assets/Materials/Textures/Tex_KitchenTile_Albedo.png";
        private const string TexTileNormal = "Assets/Materials/Textures/Tex_KitchenTile_Normal.png";
        private const string TexCuttingBoard = "Assets/Materials/Textures/Tex_CuttingBoard_Albedo.png";
        private const string TexHazard = "Assets/Materials/Textures/Tex_HazardStripe_Albedo.png";
        private const string TexDeliverySign = "Assets/Materials/Textures/Tex_DeliverySign_Albedo.png";

        // Shaders
        private const string ShaderHoodPath = "Assets/Materials/Shaders/BrushedStainlessSteel.shader";
        private const string ShaderFilterPath = "Assets/Materials/Shaders/ExtractorFilterGrill.shader";
        private const string ShaderBellPath = "Assets/Materials/Shaders/BrassBell.shader";

        // Materiales
        private const string MatHoodPath = "Assets/Materials/Mat_Metal_Hood_Brushed.mat";
        private const string MatFilterPath = "Assets/Materials/Mat_Extractor_Filter.mat";
        private const string MatBellPath = "Assets/Materials/Mat_Brass_Bell.mat";
        private const string MatLedPath = "Assets/Materials/Mat_LED_Strip.mat";
        private const string MatParticlePath = "Assets/Materials/Mat_Delivery_Particle.mat";
        private const string MatHumoPath = "Assets/Materials/Mat_Humo.mat";
        private const string MatWoodPath = "Assets/Materials/Mat_Rustic_Wood.mat";
        private const string MatDarkMetalPath = "Assets/Materials/Mat_Dark_Metal.mat";
        private const string MatSteelTablePath = "Assets/Materials/Mat_Kitchen_Island_Steel.mat";
        private const string MatCuttingBoardPath = "Assets/Materials/Mat_Cutting_Board.mat";
        private const string MatHazardPath = "Assets/Materials/Mat_Hazard_Stripe.mat";
        private const string MatDeliverySignPath = "Assets/Materials/Mat_Delivery_Sign.mat";
        private const string MatWallTilePath = "Assets/Materials/Mat_Wall_Tile.mat";

        private const string SfxExitoPath = "Assets/06_SFX/exito.mp3";
        private const string SfxRechazoPath = "Assets/06_SFX/corte.mp3";

        private static readonly string[] EscenasCocina = new[]
        {
            "Assets/00_Scenes/First Scene.unity",
            "Assets/00_Scenes/Nivel 1 - Cochabamba.unity",
            "Assets/00_Scenes/Nivel 2 - La Paz.unity",
            "Assets/00_Scenes/Nivel 3 - Santa Cruz.unity"
        };

        private const string EscenaMainMenu = "Assets/00_Scenes/Main Menu.unity";

        [InitializeOnLoadMethod]
        private static void AutoRunOnCompile()
        {
            if (EditorPrefs.GetBool("Cocina_Visuals_Applied_Final_v1", false)) return;

            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool("Cocina_Visuals_Applied_Final_v1", false)) return;
                EditorPrefs.SetBool("Cocina_Visuals_Applied_Final_v1", true);
                Debug.Log("[KitchenVisualsSetup] Disparando mejora visual integral en todas las escenas...");
                SetupAllScenes();
            };
        }

        [MenuItem("Kitchen/Apply Master Visuals (All Scenes)", priority = 1)]
        public static void SetupAllScenes()
        {
            Debug.Log("[KitchenVisualsSetup] === INICIANDO MEJORA VISUAL INTEGRAL ===");
            string originalScenePath = EditorSceneManager.GetActiveScene().path;

            // 1. Asegurar materiales PBR configurados
            AsegurarMateriales();

            // 3. Aplicar en escenas de cocina
            foreach (string scenePath in EscenasCocina)
            {
                if (!File.Exists(scenePath)) continue;

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) continue;

                AplicarVisualesEnEscenaCocina(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[KitchenVisualsSetup] Escena de cocina '{scene.name}' actualizada y guardada.");
            }

            // 4. Aplicar en Main Menu
            if (File.Exists(EscenaMainMenu))
            {
                Scene menuScene = EditorSceneManager.OpenScene(EscenaMainMenu, OpenSceneMode.Single);
                if (menuScene.IsValid())
                {
                    AplicarVisualesEnMainMenu(menuScene);
                    EditorSceneManager.MarkSceneDirty(menuScene);
                    EditorSceneManager.SaveScene(menuScene);
                    Debug.Log("[KitchenVisualsSetup] Escena 'Main Menu' actualizada y guardada.");
                }
            }

            // Restaurar escena activa previa
            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }

            Debug.Log("[KitchenVisualsSetup] === MEJORA VISUAL INTEGRAL COMPLETADA CON ÉXITO ===");
        }

        public static void AplicarVisualesEnEscena(Scene scene)
        {
            AplicarVisualesEnEscenaCocina(scene);
        }

        public static void AsegurarMateriales()
        {
            // Texturas cargadas
            var texWoodAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexWoodAlbedo);
            var texWoodNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexWoodNormal);
            var texSteelAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexSteelAlbedo);
            var texSteelNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexSteelNormal);
            var texFilterAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexFilterAlbedo);
            var texFilterNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexFilterNormal);
            var texTileAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexTileAlbedo);
            var texTileNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexTileNormal);
            var texCutting = AssetDatabase.LoadAssetAtPath<Texture2D>(TexCuttingBoard);
            var texHazard = AssetDatabase.LoadAssetAtPath<Texture2D>(TexHazard);
            var texDelivery = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDeliverySign);

            Shader shaderLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader shaderHood = AssetDatabase.LoadAssetAtPath<Shader>(ShaderHoodPath) ?? Shader.Find("CocinaBoliviana/BrushedStainlessSteel") ?? shaderLit;
            Shader shaderFilter = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFilterPath) ?? Shader.Find("CocinaBoliviana/ExtractorFilterGrill") ?? shaderLit;
            Shader shaderBell = AssetDatabase.LoadAssetAtPath<Shader>(ShaderBellPath) ?? Shader.Find("CocinaBoliviana/BrassBell") ?? shaderLit;

            // 1. Acero Inoxidable Cepillado para Campanas
            var matHood = GetOrCreateMaterial(MatHoodPath, shaderHood);
            matHood.SetColor("_BaseColor", new Color(0.86f, 0.88f, 0.90f, 1f));
            if (texSteelAlbedo != null) matHood.SetTexture("_MainTex", texSteelAlbedo);
            if (texSteelNormal != null) matHood.SetTexture("_BumpMap", texSteelNormal);
            matHood.SetFloat("_BumpScale", 1.4f);
            matHood.SetFloat("_Metallic", 0.96f);
            matHood.SetFloat("_Smoothness", 0.88f);
            matHood.SetFloat("_Anisotropy", 0.70f);

            // 2. Acero Inoxidable para Mesón de la Isla Central
            var matSteelTable = GetOrCreateMaterial(MatSteelTablePath, shaderHood);
            matSteelTable.SetColor("_BaseColor", new Color(0.90f, 0.92f, 0.94f, 1f));
            if (texSteelAlbedo != null) matSteelTable.SetTexture("_MainTex", texSteelAlbedo);
            if (texSteelNormal != null) matSteelTable.SetTexture("_BumpMap", texSteelNormal);
            matSteelTable.SetFloat("_BumpScale", 1.2f);
            matSteelTable.SetFloat("_Metallic", 0.95f);
            matSteelTable.SetFloat("_Smoothness", 0.85f);
            matSteelTable.SetFloat("_Anisotropy", 0.55f);

            // 3. Deflectores Baffle para Extractores
            var matFilter = GetOrCreateMaterial(MatFilterPath, shaderFilter);
            matFilter.SetColor("_BaseColor", new Color(0.78f, 0.80f, 0.83f, 1f));
            if (texFilterAlbedo != null) matFilter.SetTexture("_MainTex", texFilterAlbedo);
            if (texFilterNormal != null) matFilter.SetTexture("_BumpMap", texFilterNormal);
            matFilter.SetFloat("_BumpScale", 2.0f);
            matFilter.SetFloat("_Metallic", 0.95f);
            matFilter.SetFloat("_Smoothness", 0.80f);

            // 4. Campana de Latón Dorado
            var matBell = GetOrCreateMaterial(MatBellPath, shaderBell);
            matBell.SetColor("_BrassColor", new Color(0.98f, 0.80f, 0.28f, 1f));
            matBell.SetFloat("_Metallic", 0.98f);
            matBell.SetFloat("_Smoothness", 0.95f);

            // 5. Madera Rústica para Cajones y Mostrador
            var matWood = GetOrCreateMaterial(MatWoodPath, shaderLit);
            matWood.SetColor("_BaseColor", Color.white);
            if (texWoodAlbedo != null) matWood.SetTexture("_BaseMap", texWoodAlbedo);
            if (texWoodNormal != null)
            {
                matWood.SetTexture("_BumpMap", texWoodNormal);
                matWood.SetFloat("_BumpScale", 1.8f);
                matWood.EnableKeyword("_NORMALMAP");
            }
            matWood.SetFloat("_Metallic", 0.04f);
            matWood.SetFloat("_Smoothness", 0.38f);

            // 6. Metal Oscuro Industrial (patas, pernos, bisagras, soportes)
            var matDarkMetal = GetOrCreateMaterial(MatDarkMetalPath, shaderLit);
            matDarkMetal.SetColor("_BaseColor", new Color(0.18f, 0.19f, 0.21f, 1f));
            matDarkMetal.SetFloat("_Metallic", 0.88f);
            matDarkMetal.SetFloat("_Smoothness", 0.62f);

            // 7. Tabla de Corte (Bamboo / Butcher block)
            var matCutting = GetOrCreateMaterial(MatCuttingBoardPath, shaderLit);
            if (texCutting != null) matCutting.SetTexture("_BaseMap", texCutting);
            matCutting.SetFloat("_Metallic", 0.02f);
            matCutting.SetFloat("_Smoothness", 0.45f);

            // 8. Franja de Seguridad Industrial (Hazard)
            var matHazard = GetOrCreateMaterial(MatHazardPath, shaderLit);
            if (texHazard != null) matHazard.SetTexture("_BaseMap", texHazard);
            matHazard.SetFloat("_Metallic", 0.1f);
            matHazard.SetFloat("_Smoothness", 0.50f);

            // 9. Letrero de Entrega
            var matDeliverySign = GetOrCreateMaterial(MatDeliverySignPath, shaderLit);
            if (texDelivery != null) matDeliverySign.SetTexture("_BaseMap", texDelivery);
            matDeliverySign.SetColor("_EmissionColor", new Color(0.95f, 0.78f, 0.28f, 1f) * 1.8f);
            matDeliverySign.EnableKeyword("_EMISSION");

            // 10. Baldosas de Pared de Cocina
            var matTile = GetOrCreateMaterial(MatWallTilePath, shaderLit);
            if (texTileAlbedo != null)
            {
                matTile.SetTexture("_BaseMap", texTileAlbedo);
                matTile.SetTextureScale("_BaseMap", new Vector2(4, 3));
            }
            if (texTileNormal != null)
            {
                matTile.SetTexture("_BumpMap", texTileNormal);
                matTile.SetTextureScale("_BumpMap", new Vector2(4, 3));
                matTile.SetFloat("_BumpScale", 2.2f);
                matTile.EnableKeyword("_NORMALMAP");
            }
            matTile.SetFloat("_Smoothness", 0.85f);
            matTile.SetFloat("_Metallic", 0.08f);

            // 11. Barra LED Emisiva
            var matLed = GetOrCreateMaterial(MatLedPath, shaderLit);
            matLed.SetColor("_BaseColor", new Color(1f, 0.98f, 0.92f, 1f));
            matLed.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f, 1f) * 3.2f);
            matLed.EnableKeyword("_EMISSION");
            matLed.SetFloat("_Metallic", 0.1f);
            matLed.SetFloat("_Smoothness", 0.9f);

            // 12. Partículas de Entrega
            Shader shaderParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var matParticle = GetOrCreateMaterial(MatParticlePath, shaderParticle);
            matParticle.SetColor("_BaseColor", new Color(1f, 0.88f, 0.35f, 1f));

            // Materiales de etiquetas de cajones
            CrearMaterialEtiqueta("Mat_Label_Papas.mat", "Tex_CrateLabel_Papas.png", shaderLit);
            CrearMaterialEtiqueta("Mat_Label_Carne.mat", "Tex_CrateLabel_Carne.png", shaderLit);
            CrearMaterialEtiqueta("Mat_Label_Verduras.mat", "Tex_CrateLabel_Verduras.png", shaderLit);

            AssetDatabase.SaveAssets();
        }

        private static Material GetOrCreateMaterial(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null && mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        private static void CrearMaterialEtiqueta(string matName, string texName, Shader shader)
        {
            string matPath = $"Assets/Materials/{matName}";
            string texPath = $"{TexFolder}/{texName}";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            var mat = GetOrCreateMaterial(matPath, shader);
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_Smoothness", 0.25f);
        }

        public static void AplicarVisualesEnEscenaCocina(Scene scene)
        {
            var matHood = AssetDatabase.LoadAssetAtPath<Material>(MatHoodPath);
            var matFilter = AssetDatabase.LoadAssetAtPath<Material>(MatFilterPath);
            var matBell = AssetDatabase.LoadAssetAtPath<Material>(MatBellPath);
            var matLed = AssetDatabase.LoadAssetAtPath<Material>(MatLedPath);
            var matParticle = AssetDatabase.LoadAssetAtPath<Material>(MatParticlePath);
            var matHumo = AssetDatabase.LoadAssetAtPath<Material>(MatHumoPath);
            var matWood = AssetDatabase.LoadAssetAtPath<Material>(MatWoodPath);
            var matDarkMetal = AssetDatabase.LoadAssetAtPath<Material>(MatDarkMetalPath);
            var matSteelTable = AssetDatabase.LoadAssetAtPath<Material>(MatSteelTablePath);
            var matCutting = AssetDatabase.LoadAssetAtPath<Material>(MatCuttingBoardPath);
            var matHazard = AssetDatabase.LoadAssetAtPath<Material>(MatHazardPath);
            var matDeliverySign = AssetDatabase.LoadAssetAtPath<Material>(MatDeliverySignPath);
            var matTile = AssetDatabase.LoadAssetAtPath<Material>(MatWallTilePath);

            var sfxExito = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxExitoPath);
            var sfxRechazo = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxRechazoPath);

            // 1. MEJORAR CAJONES DE INGREDIENTES (Huacales rústicos de mercado)
            MejorarCajon("Cajon_Papas", "Mat_Label_Papas.mat", TipoContenidoCaja.Papas, matWood, matDarkMetal);
            MejorarCajon("Cajon_Carne", "Mat_Label_Carne.mat", TipoContenidoCaja.Carne, matWood, matDarkMetal);
            MejorarCajon("Cajon_Verduras", "Mat_Label_Verduras.mat", TipoContenidoCaja.Verduras, matWood, matDarkMetal);

            // 2. MEJORAR LA MESA CENTRAL (Isla de Chef profesional)
            MejorarMesaCentral(matSteelTable, matDarkMetal, matCutting);

            // 3. MEJORAR EXTRACTORES (Cocina y Entrega)
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var t in allTransforms)
            {
                if (t.name == "Extractor_Cocina")
                {
                    MejorarCampanaExtractor(t.gameObject, matHood, matFilter, matLed, matDarkMetal, esCocina: true, matHumo);
                }
                else if (t.name == "Extractor_Entrega")
                {
                    MejorarCampanaExtractor(t.gameObject, matHood, matFilter, matLed, matDarkMetal, esCocina: false, matHumo: null);
                }
            }

            // 4. MEJORAR ZONA DE ENTREGA
            GameObject deliveryStation = GameObject.Find("DeliveryStation");
            if (deliveryStation != null)
            {
                MejorarZonaEntrega(deliveryStation, matBell, matParticle, matWood, matDarkMetal, matHazard, matDeliverySign, sfxExito, sfxRechazo);
            }

            // 5. El usuario prefiere las paredes originales (Pared.mat), se respetan intactas.

            // 6. COLOR DEL CHORRO DE BEBIDAS SEGÚN EL NIVEL
            GameObject dispenserGo = GameObject.Find("DrinkDispenser") ?? GameObject.Find("DrinkStation");
            if (dispenserGo != null)
            {
                var chorroPs = dispenserGo.GetComponentInChildren<ParticleSystem>();
                if (chorroPs != null)
                {
                    var main = chorroPs.main;
                    bool esSantaCruz = scene.name.IndexOf("Santa Cruz", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    Color colorChorro = esSantaCruz
                        ? new Color(0.96f, 0.90f, 0.58f, 0.88f) // Somó (amarillo crema claro)
                        : new Color(0.58f, 0.35f, 0.16f, 0.88f); // Mocochinchi (café claro)
                    main.startColor = colorChorro;
                }
            }
        }

        // ==========================================
        // 1. CAJONES DE INGREDIENTES HUACALES
        // ==========================================
        private enum TipoContenidoCaja { Papas, Carne, Verduras }

        private static void MejorarCajon(string crateName, string labelMatName, TipoContenidoCaja tipo, Material matWood, Material matMetal)
        {
            GameObject crate = GameObject.Find(crateName);
            if (crate == null) return;

            // Ocultar el cubo monolítico plano original
            var rootRenderer = crate.GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;

            // Eliminar Produce anterior simple si existe
            Transform oldProduce = crate.transform.Find("Produce");
            if (oldProduce != null) Object.DestroyImmediate(oldProduce.gameObject);

            // Contenedor visual nuevo
            Transform visual = crate.transform.Find("Crate_Visual");
            if (visual != null) Object.DestroyImmediate(visual.gameObject);

            visual = new GameObject("Crate_Visual").transform;
            visual.SetParent(crate.transform, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;

            // 1. Base / Fondo del cajón
            CrearPiezaVisual(visual, PrimitiveType.Cube, "Fondo_Madera",
                new Vector3(0f, -0.42f, 0f), new Vector3(0.92f, 0.08f, 0.92f), matWood);

            // 2. Cuatro postes esquineros biselados
            float cornerX = 0.44f;
            float cornerZ = 0.44f;
            CrearPiezaVisual(visual, PrimitiveType.Cube, "Poste_NW", new Vector3(-cornerX, 0f, cornerZ), new Vector3(0.12f, 0.88f, 0.12f), matWood);
            CrearPiezaVisual(visual, PrimitiveType.Cube, "Poste_NE", new Vector3(cornerX, 0f, cornerZ), new Vector3(0.12f, 0.88f, 0.12f), matWood);
            CrearPiezaVisual(visual, PrimitiveType.Cube, "Poste_SW", new Vector3(-cornerX, 0f, -cornerZ), new Vector3(0.12f, 0.88f, 0.12f), matWood);
            CrearPiezaVisual(visual, PrimitiveType.Cube, "Poste_SE", new Vector3(cornerX, 0f, -cornerZ), new Vector3(0.12f, 0.88f, 0.12f), matWood);

            // 3. Tablones horizontales separados (Slats huacal) en 3 niveles de altura
            float[] alturasSlats = new[] { -0.28f, 0.02f, 0.32f };
            for (int i = 0; i < alturasSlats.Length; i++)
            {
                float y = alturasSlats[i];
                // Frontal y Trasero
                CrearPiezaVisual(visual, PrimitiveType.Cube, $"Tablon_Front_{i}", new Vector3(0f, y, -cornerZ), new Vector3(0.96f, 0.22f, 0.04f), matWood);
                CrearPiezaVisual(visual, PrimitiveType.Cube, $"Tablon_Back_{i}", new Vector3(0f, y, cornerZ), new Vector3(0.96f, 0.22f, 0.04f), matWood);
                // Laterales
                CrearPiezaVisual(visual, PrimitiveType.Cube, $"Tablon_Left_{i}", new Vector3(-cornerX, y, 0f), new Vector3(0.04f, 0.22f, 0.96f), matWood);
                CrearPiezaVisual(visual, PrimitiveType.Cube, $"Tablon_Right_{i}", new Vector3(cornerX, y, 0f), new Vector3(0.04f, 0.22f, 0.96f), matWood);
            }

            // 4. Herrajes metálicos esquineros de refuerzo (hierro forjado oscuro)
            float[] alturasHerrajes = new[] { -0.38f, 0.38f };
            foreach (float y in alturasHerrajes)
            {
                CrearPiezaVisual(visual, PrimitiveType.Cube, "Herraje_NW", new Vector3(-cornerX, y, cornerZ), new Vector3(0.14f, 0.14f, 0.14f), matMetal);
                CrearPiezaVisual(visual, PrimitiveType.Cube, "Herraje_NE", new Vector3(cornerX, y, cornerZ), new Vector3(0.14f, 0.14f, 0.14f), matMetal);
                CrearPiezaVisual(visual, PrimitiveType.Cube, "Herraje_SW", new Vector3(-cornerX, y, -cornerZ), new Vector3(0.14f, 0.14f, 0.14f), matMetal);
                CrearPiezaVisual(visual, PrimitiveType.Cube, "Herraje_SE", new Vector3(cornerX, y, -cornerZ), new Vector3(0.14f, 0.14f, 0.14f), matMetal);
            }

            // 5. Placa frontal de identificación (Pizarra oscura con acento rústico)
            Material matLabel = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/{labelMatName}") ?? matWood;
            // Marco de madera para la placa
            CrearPiezaVisual(visual, PrimitiveType.Cube, "Placa_Marco", new Vector3(0f, 0.02f, -cornerZ - 0.025f), new Vector3(0.72f, 0.36f, 0.03f), matWood);
            // Placa con texto y acento
            CrearPiezaVisual(visual, PrimitiveType.Cube, "Placa_Identificadora", new Vector3(0f, 0.02f, -cornerZ - 0.045f), new Vector3(0.66f, 0.30f, 0.02f), matLabel);

            // El interior se mantiene despejado y limpio sin elementos falsos
        }

        // ==========================================
        // 2. MESA CENTRAL (ISLA DE CHEF PROFESIONAL)
        // ==========================================
        private static void MejorarMesaCentral(Material matSteelTable, Material matDarkMetal, Material matCutting)
        {
            string[] estacionesIsla = new[] { "AssemblyStation", "Counter_Island_01", "Counter_Island_02" };

            foreach (var estNombre in estacionesIsla)
            {
                GameObject estacion = GameObject.Find(estNombre);
                if (estacion == null) continue;

                // 1. Asignar material acero inoxidable cepillado de chef a la superficie
                Transform mesa = estacion.transform.Find("Mesa");
                if (mesa != null)
                {
                    var r = mesa.GetComponent<MeshRenderer>();
                    if (r != null && matSteelTable != null) r.sharedMaterial = matSteelTable;
                }

                Transform topTrim = estacion.transform.Find("TopTrim");
                if (topTrim != null)
                {
                    var r = topTrim.GetComponent<MeshRenderer>();
                    if (r != null && matSteelTable != null) r.sharedMaterial = matSteelTable;
                }

                // 2. Añadir detalles de carpintería y herrería profesional
                Transform detalles = estacion.transform.Find("Island_Details");
                if (detalles != null) Object.DestroyImmediate(detalles.gameObject);

                detalles = new GameObject("Island_Details").transform;
                detalles.SetParent(estacion.transform, false);
                detalles.localPosition = Vector3.zero;
                detalles.localRotation = Quaternion.identity;

                // Dimensiones del módulo
                Vector3 mesaScale = mesa != null ? mesa.localScale : new Vector3(1f, 0.9f, 0.8f);
                float halfX = mesaScale.x * 0.46f;
                float halfZ = mesaScale.z * 0.46f;
                float legY = -0.05f;

                // 4 Patas cilíndricas de acero tubular con regatones niveladores en el piso
                float legRadius = 0.045f;
                float legHeight = 0.88f;

                CrearPataTubular(detalles, "Pata_NW", new Vector3(-halfX, legY, halfZ), legRadius, legHeight, matDarkMetal);
                CrearPataTubular(detalles, "Pata_NE", new Vector3(halfX, legY, halfZ), legRadius, legHeight, matDarkMetal);
                CrearPataTubular(detalles, "Pata_SW", new Vector3(-halfX, legY, -halfZ), legRadius, legHeight, matDarkMetal);
                CrearPataTubular(detalles, "Pata_SE", new Vector3(halfX, legY, -halfZ), legRadius, legHeight, matDarkMetal);

                // Estante inferior abierto para bandejas (a 22cm del piso)
                CrearPiezaVisual(detalles, PrimitiveType.Cube, "Estante_Inferior",
                    new Vector3(0f, -0.32f, 0f),
                    new Vector3(mesaScale.x * 0.92f, 0.03f, mesaScale.z * 0.88f), matDarkMetal);

                // Bandejas gastronorm apiladas en el estante inferior (atrezzo)
                CrearPiezaVisual(detalles, PrimitiveType.Cube, "Bandeja_Gastro_01",
                    new Vector3(0f, -0.28f, 0f),
                    new Vector3(mesaScale.x * 0.55f, 0.04f, mesaScale.z * 0.50f), matSteelTable);

                // Bisel frontal redondeado de seguridad sanitaria (bullnose edge)
                CrearPiezaVisual(detalles, PrimitiveType.Cube, "Bisel_Frontal",
                    new Vector3(0f, 0.47f, -halfZ - 0.02f),
                    new Vector3(mesaScale.x * 1.02f, 0.04f, 0.04f), matSteelTable);

                // Accesorios específicos según la estación
                if (estNombre == "Counter_Island_01")
                {
                    // Tabla de cortar de carnicero (Butcher block) integrada en la sección de preparación
                    CrearPiezaVisual(detalles, PrimitiveType.Cube, "Tabla_Picado_Incrustada",
                        new Vector3(0f, 0.485f, 0f),
                        new Vector3(0.58f, 0.035f, 0.52f), matCutting);
                }
                else if (estNombre == "Counter_Island_02")
                {
                    // Barra toallera lateral de acero inoxidable
                    CrearPiezaVisual(detalles, PrimitiveType.Cylinder, "Barra_Toallero",
                        new Vector3(halfX + 0.06f, 0.38f, 0f),
                        new Vector3(0.02f, 0.32f, 0.02f), matSteelTable)
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    // Paño de cocina colgado
                    Material matPano = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    matPano.color = new Color(0.92f, 0.92f, 0.95f);
                    CrearPiezaVisual(detalles, PrimitiveType.Cube, "Pano_Cocina",
                        new Vector3(halfX + 0.07f, 0.26f, 0f),
                        new Vector3(0.02f, 0.22f, 0.24f), matPano);
                }
            }
        }

        private static void CrearPataTubular(Transform parent, string name, Vector3 pos, float radius, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            // Regatón nivelador en la base
            var pie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pie.name = "Regaton";
            pie.transform.SetParent(go.transform, false);
            pie.transform.localPosition = new Vector3(0f, -0.98f, 0f);
            pie.transform.localScale = new Vector3(1.4f, 0.12f, 1.4f);
            var colPie = pie.GetComponent<Collider>();
            if (colPie != null) Object.DestroyImmediate(colPie);
            var rPie = pie.GetComponent<MeshRenderer>();
            if (rPie != null && mat != null) rPie.sharedMaterial = mat;
        }

        // ==========================================
        // 3. EXTRACTORES DE COCINA Y ENTREGA
        // ==========================================
        private static void MejorarCampanaExtractor(GameObject extractor, Material matHood, Material matFilter, Material matLed, Material matDarkMetal, bool esCocina, Material matHumo)
        {
            // 1. Materiales en cuerpo y filtros existentes
            foreach (Transform child in extractor.transform)
            {
                if (child.name.StartsWith("Campana_"))
                {
                    var renderer = child.GetComponent<MeshRenderer>();
                    if (renderer != null && matHood != null) renderer.sharedMaterial = matHood;
                }
                else if (child.name.StartsWith("Filtro_"))
                {
                    var renderer = child.GetComponent<MeshRenderer>();
                    if (renderer != null && matFilter != null) renderer.sharedMaterial = matFilter;
                }
            }

            // 2. Detalles mecánicos nuevos (ductos con bridas y pernos, panel de control)
            Transform detalles = extractor.transform.Find("Detalles_Campana");
            if (detalles != null) Object.DestroyImmediate(detalles.gameObject);

            detalles = new GameObject("Detalles_Campana").transform;
            detalles.SetParent(extractor.transform, false);
            detalles.localPosition = Vector3.zero;
            detalles.localRotation = Quaternion.identity;

            // Brida de unión del ducto al techo con pernos
            CrearPiezaVisual(detalles, PrimitiveType.Cylinder, "Brida_Ducto_Techo",
                new Vector3(0f, 0.85f, 0f), new Vector3(0.42f, 0.04f, 0.42f), matDarkMetal);

            // Anillo intermedio del ducto
            CrearPiezaVisual(detalles, PrimitiveType.Cylinder, "Brida_Ducto_Medio",
                new Vector3(0f, 0.60f, 0f), new Vector3(0.38f, 0.03f, 0.38f), matDarkMetal);

            // Caja de control comercial en el frontal de la campana
            float frontZ = esCocina ? -0.44f : -0.96f;
            var panelControl = CrearPiezaVisual(detalles, PrimitiveType.Cube, "Panel_Control_Extractor",
                new Vector3(0f, -0.08f, frontZ), new Vector3(0.32f, 0.12f, 0.05f), matDarkMetal);

            // Interruptor ON/OFF (LED rojo/verde)
            Material matSwitch = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            matSwitch.color = new Color(0.15f, 0.85f, 0.25f);
            matSwitch.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 0.25f) * 2.5f);
            matSwitch.EnableKeyword("_EMISSION");
            CrearPiezaVisual(detalles, PrimitiveType.Cube, "Boton_Encendido",
                new Vector3(-0.09f, -0.08f, frontZ - 0.035f), new Vector3(0.04f, 0.05f, 0.03f), matSwitch);

            // Manómetro / reloj analógico de presión de vacío
            Material matGauge = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            matGauge.color = new Color(0.95f, 0.95f, 0.95f);
            var dial = CrearPiezaVisual(detalles, PrimitiveType.Cylinder, "Manometro",
                new Vector3(0.08f, -0.08f, frontZ - 0.03f), new Vector3(0.07f, 0.015f, 0.07f), matGauge);
            dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // 3. Barra LED Emisiva
            Transform ledBar = extractor.transform.Find("Barra_LED");
            if (ledBar == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Barra_LED";
                go.transform.SetParent(extractor.transform, false);
                go.transform.localPosition = new Vector3(0f, -0.21f, 0f);
                go.transform.localScale = new Vector3(0.68f, 0.02f, 0.08f);
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
                ledBar = go.transform;
            }
            if (matLed != null)
            {
                var r = ledBar.GetComponent<MeshRenderer>();
                if (r != null) r.sharedMaterial = matLed;
            }

            // 4. Luz cálida de trabajo
            Transform luzTransform = extractor.transform.Find("Luz_Extractor");
            if (luzTransform != null)
            {
                var light = luzTransform.GetComponent<Light>();
                if (light != null)
                {
                    light.type = LightType.Spot;
                    light.color = new Color(1f, 0.94f, 0.84f, 1f); // 3200K halógena cálida
                    light.intensity = esCocina ? 2.8f : 2.6f;
                    light.range = 3.0f;
                    light.spotAngle = 80f;
                    light.innerSpotAngle = 40f;
                    luzTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    luzTransform.localPosition = new Vector3(0f, -0.22f, 0f);
                }
            }

            // 5. Partículas de succión de humo/vapor
            if (esCocina && matHumo != null)
            {
                Transform vaporTransform = extractor.transform.Find("Vapor_Extractor");
                if (vaporTransform == null)
                {
                    var go = new GameObject("Vapor_Extractor");
                    go.transform.SetParent(extractor.transform, false);
                    go.transform.localPosition = new Vector3(0f, -0.65f, 0f);
                    go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                    var ps = go.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.playOnAwake = true;
                    main.loop = true;
                    main.duration = 2.0f;
                    main.startLifetime = 1.4f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
                    main.startColor = new Color(1f, 1f, 1f, 0.14f);

                    var emission = ps.emission;
                    emission.rateOverTime = 8f;

                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(0.65f, 0.45f, 0.1f);

                    var renderer = go.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null) renderer.sharedMaterial = matHumo;
                }
            }
        }

        // ==========================================
        // 4. ZONA DE ENTREGA DE PEDIDOS
        // ==========================================
        private static void MejorarZonaEntrega(GameObject deliveryStation, Material matBell, Material matParticle,
            Material matWood, Material matMetal, Material matHazard, Material matDeliverySign, AudioClip sfxExito, AudioClip sfxRechazo)
        {
            // 1. Campana de servicio sobre peana de caoba pulida
            Transform campana = deliveryStation.transform.Find("Campana_Servicio");
            if (campana != null)
            {
                var r = campana.GetComponent<MeshRenderer>();
                if (r != null && matBell != null) r.sharedMaterial = matBell;

                var col = campana.GetComponent<SphereCollider>();
                if (col == null) col = campana.gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.5f;

                if (campana.GetComponent<ServiceBell>() == null)
                    campana.gameObject.AddComponent<ServiceBell>();

                // Base / peana de madera oscura torneada bajo la campana
                Transform peana = campana.Find("Peana_Madera");
                if (peana == null)
                {
                    var goPeana = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    goPeana.name = "Peana_Madera";
                    goPeana.transform.SetParent(campana, false);
                    goPeana.transform.localPosition = new Vector3(0f, -0.45f, 0f);
                    goPeana.transform.localScale = new Vector3(1.35f, 0.25f, 1.35f);
                    var colP = goPeana.GetComponent<Collider>();
                    if (colP != null) Object.DestroyImmediate(colP);
                    var rendP = goPeana.GetComponent<MeshRenderer>();
                    if (rendP != null && matWood != null) rendP.sharedMaterial = matWood;
                }
            }

            // 2. Franjas de advertencia industrial en los rieles guía de la cinta
            Transform guideRail = deliveryStation.transform.Find("Guide_Rail_Outer");
            if (guideRail != null && matHazard != null)
            {
                var rend = guideRail.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = matHazard;
            }

            // 3. Riel de comandas (Order ticket rail) con comandas de papel colgadas
            Transform passWindow = deliveryStation.transform.Find("Ventana_Entrega");
            if (passWindow != null)
            {
                var rend = passWindow.GetComponent<MeshRenderer>();
                if (rend != null && matWood != null) rend.sharedMaterial = matWood;

                Transform riel = deliveryStation.transform.Find("Riel_Comandas");
                if (riel != null) Object.DestroyImmediate(riel.gameObject);

                riel = new GameObject("Riel_Comandas").transform;
                riel.SetParent(deliveryStation.transform, false);
                riel.localPosition = new Vector3(-0.35f, 1.38f, 0f);

                // Riel horizontal de acero
                CrearPiezaVisual(riel, PrimitiveType.Cube, "Barra_Riel",
                    Vector3.zero, new Vector3(0.04f, 0.03f, 1.25f), matMetal);

                // Letrero luminoso de entrega sobre el riel
                if (matDeliverySign != null)
                {
                    CrearPiezaVisual(riel, PrimitiveType.Cube, "Letrero_Entrega",
                        new Vector3(0f, 0.16f, 0f), new Vector3(0.04f, 0.24f, 0.95f), matDeliverySign);
                }

                // 3 Tickets de comanda de papel colgando del riel
                Material matTicket = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                matTicket.color = new Color(0.98f, 0.96f, 0.88f); // Papel térmico
                matTicket.SetFloat("_Smoothness", 0.15f);

                float[] zTickets = new[] { -0.35f, 0.0f, 0.35f };
                for (int i = 0; i < zTickets.Length; i++)
                {
                    // Clip metálico
                    CrearPiezaVisual(riel, PrimitiveType.Cube, $"Clip_{i}",
                        new Vector3(0.02f, 0f, zTickets[i]), new Vector3(0.025f, 0.04f, 0.04f), matMetal);

                    // Papel de comanda
                    var t = CrearPiezaVisual(riel, PrimitiveType.Cube, $"Ticket_{i}",
                        new Vector3(0.02f, -0.15f, zTickets[i]), new Vector3(0.01f, 0.26f, 0.16f), matTicket);
                    t.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 3f);
                }
            }

            // 4. Partículas y DeliveryCounter
            Transform particulasT = deliveryStation.transform.Find("Particulas_Entrega");
            ParticleSystem particulasExito = null;
            if (particulasT == null)
            {
                var go = new GameObject("Particulas_Entrega");
                go.transform.SetParent(deliveryStation.transform, false);
                go.transform.localPosition = new Vector3(0f, 0.75f, 0.5f);
                go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                particulasExito = go.AddComponent<ParticleSystem>();
                var main = particulasExito.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 1.0f;
                main.startLifetime = 0.85f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
                main.startColor = new Color(1f, 0.88f, 0.35f, 1f);
                main.gravityModifier = -0.15f;

                var emission = particulasExito.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 30) });

                var shape = particulasExito.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && matParticle != null) renderer.sharedMaterial = matParticle;
            }
            else
            {
                particulasExito = particulasT.GetComponent<ParticleSystem>();
            }

            // Conectar DeliveryCounter
            Light luzFeedback = null;
            Transform extractorEntrega = deliveryStation.transform.Find("Extractor_Entrega");
            if (extractorEntrega != null)
            {
                Transform luzT = extractorEntrega.Find("Luz_Extractor");
                if (luzT != null) luzFeedback = luzT.GetComponent<Light>();
            }

            var counter = deliveryStation.GetComponent<DeliveryCounter>();
            if (counter == null) counter = deliveryStation.AddComponent<DeliveryCounter>();

            var so = new SerializedObject(counter);
            if (sfxExito != null) so.FindProperty("sonidoAcierto").objectReferenceValue = sfxExito;
            if (sfxRechazo != null) so.FindProperty("sonidoRechazo").objectReferenceValue = sfxRechazo;
            if (campana != null) so.FindProperty("campana").objectReferenceValue = campana;
            if (particulasExito != null) so.FindProperty("particulasExito").objectReferenceValue = particulasExito;
            if (luzFeedback != null) so.FindProperty("luzFeedback").objectReferenceValue = luzFeedback;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==========================================
        // 5. MAIN MENU ESTILIZADO
        // ==========================================
        public static void AplicarVisualesEnMainMenu(Scene scene)
        {
            var desk = GameObject.Find("Reception_Desk");
            if (desk != null) Object.DestroyImmediate(desk);

            var lamps = GameObject.Find("Menu_Pendant_Lamps");
            if (lamps != null) Object.DestroyImmediate(lamps);

            var extraEnv = GameObject.Find("Environment");
            if (extraEnv != null && extraEnv.transform.childCount == 0) Object.DestroyImmediate(extraEnv);
        }

        // ==========================================
        // UTILIDADES AUXILIARES
        // ==========================================
        private static GameObject CrearPiezaVisual(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            if (mat != null)
            {
                var rend = go.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }
            return go;
        }
    }
}
