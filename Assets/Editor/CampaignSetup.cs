using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Generador y configurador automatizado de la campaña completa de 3 niveles para Cocina Boliviana.
    /// Crea las escenas independientes para Cochabamba (Nivel 1), La Paz (Nivel 2) y Santa Cruz (Nivel 3),
    /// cablea el LevelManager con HUD, OrderManager, dispensadores, banderas y registra todas las escenas
    /// en Build Settings.
    /// </summary>
    public static class CampaignSetup
    {
        private const string FirstScenePath = "Assets/00_Scenes/First Scene.unity";
        private const string MainMenuScenePath = "Assets/00_Scenes/Main Menu.unity";

        private const string Nivel1ScenePath = "Assets/00_Scenes/Nivel 1 - Cochabamba.unity";
        private const string Nivel2ScenePath = "Assets/00_Scenes/Nivel 2 - La Paz.unity";
        private const string Nivel3ScenePath = "Assets/00_Scenes/Nivel 3 - Santa Cruz.unity";

        private const string Nivel1DataPath = "Assets/03_SO/Niveles/Nivel1_Cochabamba.asset";
        private const string Nivel2DataPath = "Assets/03_SO/Niveles/Nivel2_LaPaz.asset";
        private const string Nivel3DataPath = "Assets/03_SO/Niveles/Nivel3_SantaCruz.asset";

        private const string CbbaDeptoPath = "Assets/03_SO/Departamentos/Cochabamba.asset";
        private const string LaPazDeptoPath = "Assets/03_SO/Departamentos/La paz.asset";
        private const string SantaCruzDeptoPath = "Assets/03_SO/Departamentos/SantaCruz.asset";

        [MenuItem("Kitchen/Setup 3-Level Campaign (Full Setup)", priority = 1)]
        public static void RunAll()
        {
            Debug.Log("[CampaignSetup] === INICIANDO CONFIGURACIÓN INTEGRAL DE CAMPAÑA DE 3 NIVELES ===");

            // 1. Reconstruir Prefab actualizado de LevelManager con soporte para 3 botones (Reintentar, Siguiente, Menú)
            Debug.Log("[CampaignSetup] Paso 1: Reconstruyendo LevelManager.prefab...");
            GameObject levelManagerPrefab = LevelSetup.BuildLevelManagerPrefab();
            if (levelManagerPrefab == null)
            {
                Debug.LogError("[CampaignSetup] Error: no se pudo construir LevelManager.prefab");
                return;
            }

            // 2. Asegurar que las 3 escenas existan copiadas de First Scene
            Debug.Log("[CampaignSetup] Paso 2: Creando/actualizando escenas de nivel...");
            PrepareSceneCopy(FirstScenePath, Nivel1ScenePath);
            PrepareSceneCopy(FirstScenePath, Nivel2ScenePath);
            PrepareSceneCopy(FirstScenePath, Nivel3ScenePath);
            AssetDatabase.Refresh();

            // 3. Configurar Nivel 1 - Cochabamba
            Debug.Log("[CampaignSetup] Paso 3: Configurando Nivel 1 - Cochabamba...");
            ConfigureLevelScene(
                scenePath: Nivel1ScenePath,
                numeroNivel: 1,
                nombreNivel: "Nivel 1 - Cochabamba",
                nivelDataPath: Nivel1DataPath,
                deptoDataPath: CbbaDeptoPath,
                papasIngr: new[] { "Papa", "Arroz" },
                carneIngr: new[] { "Carne", "Chorizo", "Huevo" },
                verdurasIngr: new[] { "Tomate", "Cebolla" },
                levelManagerPrefab: levelManagerPrefab
            );

            // 4. Configurar Nivel 2 - La Paz
            Debug.Log("[CampaignSetup] Paso 4: Configurando Nivel 2 - La Paz...");
            ConfigureLevelScene(
                scenePath: Nivel2ScenePath,
                numeroNivel: 2,
                nombreNivel: "Nivel 2 - La Paz",
                nivelDataPath: Nivel2DataPath,
                deptoDataPath: LaPazDeptoPath,
                papasIngr: new[] { "Papa", "Haba" },
                carneIngr: new[] { "Carne", "Queso" },
                verdurasIngr: new[] { "Tomate", "Cebolla" },
                levelManagerPrefab: levelManagerPrefab
            );

            // 5. Configurar Nivel 3 - Santa Cruz
            Debug.Log("[CampaignSetup] Paso 5: Configurando Nivel 3 - Santa Cruz...");
            ConfigureLevelScene(
                scenePath: Nivel3ScenePath,
                numeroNivel: 3,
                nombreNivel: "Nivel 3 - Santa Cruz",
                nivelDataPath: Nivel3DataPath,
                deptoDataPath: SantaCruzDeptoPath,
                papasIngr: new[] { "Arroz", "SonsoCrudo" },
                carneIngr: new[] { "Carne", "Huevo", "Queso" },
                verdurasIngr: new[] { "Tomate", "Cebolla" },
                levelManagerPrefab: levelManagerPrefab
            );

            // 6. Configurar First Scene (para testing directo en editor)
            Debug.Log("[CampaignSetup] Paso 6: Configurando First Scene para testing directo...");
            ConfigureLevelScene(
                scenePath: FirstScenePath,
                numeroNivel: 1,
                nombreNivel: "Nivel 1 - Cochabamba",
                nivelDataPath: Nivel1DataPath,
                deptoDataPath: CbbaDeptoPath,
                papasIngr: new[] { "Papa", "Arroz" },
                carneIngr: new[] { "Carne", "Chorizo", "Huevo" },
                verdurasIngr: new[] { "Tomate", "Cebolla" },
                levelManagerPrefab: levelManagerPrefab
            );

            // 7. Configurar Menú Principal
            Debug.Log("[CampaignSetup] Paso 7: Configurando Main Menu...");
            ConfigureMainMenu();

            // 8. Configurar EditorBuildSettings
            Debug.Log("[CampaignSetup] Paso 8: Registrando escenas en Build Settings...");
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("[CampaignSetup] === ¡CAMPAÑA DE 3 NIVELES CONFIGURADA CON ÉXITO! ===");
        }

        private static void PrepareSceneCopy(string sourcePath, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                Debug.Log($"[CampaignSetup] La escena '{targetPath}' ya existe. Se actualizará.");
                return;
            }

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                File.Copy(sourcePath, targetPath, true);
                Debug.Log($"[CampaignSetup] Copia de archivo directa realizada: '{sourcePath}' -> '{targetPath}'");
            }
            else
            {
                Debug.Log($"[CampaignSetup] Escena copiada vía AssetDatabase: '{targetPath}'");
            }
        }

        private static void ConfigureLevelScene(
            string scenePath,
            int numeroNivel,
            string nombreNivel,
            string nivelDataPath,
            string deptoDataPath,
            string[] papasIngr,
            string[] carneIngr,
            string[] verdurasIngr,
            GameObject levelManagerPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CampaignSetup] No se pudo abrir la escena '{scenePath}'.");
                return;
            }

            // 1. Instalar o actualizar LevelManager
            var existingLm = Object.FindAnyObjectByType<LevelManager>();
            if (existingLm != null)
            {
                Object.DestroyImmediate(existingLm.gameObject);
                existingLm = null;
            }

            var lmInstance = (GameObject)PrefabUtility.InstantiatePrefab(levelManagerPrefab, scene);
            lmInstance.name = "LevelManager";

            var lm = lmInstance.GetComponent<LevelManager>();
            var soLm = new SerializedObject(lm);
            soLm.FindProperty("numeroNivel").intValue = numeroNivel;
            soLm.FindProperty("nombreNivel").stringValue = nombreNivel;

            var nivelData = AssetDatabase.LoadAssetAtPath<LevelData>(nivelDataPath);
            soLm.FindProperty("nivelPorDefecto").objectReferenceValue = nivelData;

            if (nivelData != null)
            {
                soLm.FindProperty("duracionNivel").floatValue = nivelData.duracionNivel;
                soLm.FindProperty("objetivoPuntos1Estrella").intValue = nivelData.objetivoPuntos1Estrella;
                soLm.FindProperty("objetivoPuntos2Estrellas").intValue = nivelData.objetivoPuntos2Estrellas;
                soLm.FindProperty("objetivoPuntos3Estrellas").intValue = nivelData.objetivoPuntos3Estrellas;
            }
            soLm.ApplyModifiedPropertiesWithoutUndo();

            // 2. Configurar OrderManager
            var departmentData = AssetDatabase.LoadAssetAtPath<DepartmentData>(deptoDataPath);
            var om = Object.FindAnyObjectByType<OrderManager>();
            if (om != null)
            {
                var soOm = new SerializedObject(om);
                soOm.FindProperty("departamento").objectReferenceValue = departmentData;
                soOm.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[CampaignSetup] OrderManager configurado con '{departmentData?.nombre}' en '{scene.name}'.");
            }

            // 3. Configurar WallFlags
            var wallFlags = Object.FindObjectsByType<WallFlag>(FindObjectsInactive.Include);
            foreach (var wf in wallFlags)
            {
                var soWf = new SerializedObject(wf);
                var testDeptoProp = soWf.FindProperty("departamentoDePrueba");
                if (testDeptoProp != null)
                {
                    testDeptoProp.objectReferenceValue = departmentData;
                }
                soWf.ApplyModifiedPropertiesWithoutUndo();
            }

            // 4. Configurar opciones de dispensadores de cajones
            SetCrateIngredients("Cajon_Papas", papasIngr);
            SetCrateIngredients("Cajon_Carne", carneIngr);
            SetCrateIngredients("Cajon_Verduras", verdurasIngr);

            // 5. Aplicar mejoras estéticas de extractores, campana e iluminación de entrega
            KitchenVisualsSetup.AsegurarMateriales();
            KitchenVisualsSetup.AplicarVisualesEnEscena(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[CampaignSetup] Escena '{scene.name}' guardada correctamente.");
        }

        private static void SetCrateIngredients(string crateName, string[] ingredientNames)
        {
            GameObject crate = GameObject.Find(crateName);
            if (crate == null)
            {
                Debug.LogWarning($"[CampaignSetup] No se encontró el cajón '{crateName}' en la escena.");
                return;
            }

            var dispenser = crate.GetComponent<ItemDispenser>();
            if (dispenser == null)
            {
                Debug.LogWarning($"[CampaignSetup] '{crateName}' no tiene componente ItemDispenser.");
                return;
            }

            var so = new SerializedObject(dispenser);
            SerializedProperty opcionesProp = so.FindProperty("opcionesIngredientes");
            opcionesProp.ClearArray();

            int index = 0;
            foreach (string nombre in ingredientNames)
            {
                string assetPath = $"Assets/03_SO/Ingredientes/{nombre}.asset";
                var ingData = AssetDatabase.LoadAssetAtPath<IngredientData>(assetPath);
                if (ingData == null)
                {
                    Debug.LogWarning($"[CampaignSetup] No existe '{assetPath}' para '{crateName}'.");
                    continue;
                }

                opcionesProp.InsertArrayElementAtIndex(index);
                opcionesProp.GetArrayElementAtIndex(index).objectReferenceValue = ingData;
                index++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[CampaignSetup] '{crateName}' actualizado con [{string.Join(", ", ingredientNames)}].");
        }

        private static void ConfigureMainMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[CampaignSetup] No se pudo abrir la escena del menú '{MainMenuScenePath}'.");
                return;
            }

            var menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu != null)
            {
                var so = new SerializedObject(menu);
                SerializedProperty lista = so.FindProperty("niveles");
                lista.ClearArray();

                string[] nivelPaths = { Nivel1DataPath, Nivel2DataPath, Nivel3DataPath };
                for (int i = 0; i < nivelPaths.Length; i++)
                {
                    var nivelData = AssetDatabase.LoadAssetAtPath<LevelData>(nivelPaths[i]);
                    lista.InsertArrayElementAtIndex(i);
                    lista.GetArrayElementAtIndex(i).objectReferenceValue = nivelData;
                }

                so.FindProperty("nivelPorDefecto").intValue = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[CampaignSetup] MainMenuController vinculado a los 3 niveles de la campaña.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBuildSettings()
        {
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(Nivel1ScenePath, true),
                new EditorBuildSettingsScene(Nivel2ScenePath, true),
                new EditorBuildSettingsScene(Nivel3ScenePath, true),
                new EditorBuildSettingsScene(FirstScenePath, false) // Escena de desarrollo / respaldo deshabilitada
            };

            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[CampaignSetup] EditorBuildSettings actualizado con las 4 escenas principales.");
        }
    }
}
