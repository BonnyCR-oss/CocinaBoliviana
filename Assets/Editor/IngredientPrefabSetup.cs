using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Completa los prefabs de ingredientes (creados a mano en Assets/02_Prefabs/IngredientePrefabs)
    /// agregando Collider/Rigidbody/XRGrabInteractable/IngredientItem, conectando cada uno a su
    /// IngredientData y llenando la lista "Cortes Posibles" de cada ingrediente crudo.
    /// </summary>
    [InitializeOnLoad]
    public static class IngredientPrefabSetup
    {
        private const string PrefabsFolder = "Assets/02_Prefabs/IngredientePrefabs";
        private const string ScenePath = "Assets/Scenes/First Scene.unity";
        private const float DesiredWorldColliderRadius = 0.06f;

        private class CorteSpec
        {
            public TipoCorte tipo;
            public string prefabPath;
        }

        private class IngredienteSpec
        {
            public string ingredientDataPath;
            public string prefabPath;
            public List<CorteSpec> cortes = new List<CorteSpec>();
        }

        static IngredientPrefabSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("IngredientPrefabSetup_Executed_v1", false))
                {
                    SessionState.SetBool("IngredientPrefabSetup_Executed_v1", true);
                    SetupIngredientPrefabs();
                }
            };
        }

        [MenuItem("Kitchen/Setup Ingredient Prefabs (Cuts & Data)")]
        public static void SetupIngredientPrefabs()
        {
            Debug.Log("[IngredientPrefabSetup] Configurando prefabs de ingredientes...");

            var specs = new List<IngredienteSpec>
            {
                new IngredienteSpec
                {
                    ingredientDataPath = "Assets/03_SO/Ingredientes/Tomate.asset",
                    prefabPath = $"{PrefabsFolder}/tomate.prefab",
                    cortes = { new CorteSpec { tipo = TipoCorte.Cubitos, prefabPath = $"{PrefabsFolder}/TomatePicado.prefab" } }
                },
                new IngredienteSpec
                {
                    ingredientDataPath = "Assets/03_SO/Ingredientes/Cebolla.asset",
                    prefabPath = $"{PrefabsFolder}/cebolla.prefab",
                    cortes =
                    {
                        new CorteSpec { tipo = TipoCorte.Rodajas, prefabPath = $"{PrefabsFolder}/cebollaRodajas.prefab" },
                        new CorteSpec { tipo = TipoCorte.Cubitos, prefabPath = $"{PrefabsFolder}/cebollaCubitos.prefab" },
                    }
                },
                new IngredienteSpec
                {
                    ingredientDataPath = "Assets/03_SO/Ingredientes/Papa.asset",
                    prefabPath = $"{PrefabsFolder}/Papa.prefab",
                    cortes = { new CorteSpec { tipo = TipoCorte.Rodajas, prefabPath = $"{PrefabsFolder}/papaRodajas.prefab" } }
                },
                new IngredienteSpec
                {
                    ingredientDataPath = "Assets/03_SO/Ingredientes/Carne.asset",
                    prefabPath = $"{PrefabsFolder}/carne.prefab",
                    cortes = { new CorteSpec { tipo = TipoCorte.Cubitos, prefabPath = $"{PrefabsFolder}/carneCubitos.prefab" } }
                },
                new IngredienteSpec
                {
                    ingredientDataPath = "Assets/03_SO/Ingredientes/Arroz.asset",
                    prefabPath = $"{PrefabsFolder}/arroz.prefab",
                },
                new IngredienteSpec
                {
                    ingredientDataPath = "Assets/03_SO/Ingredientes/Huevo.asset",
                    prefabPath = $"{PrefabsFolder}/huevo.prefab",
                },
            };

            foreach (var spec in specs)
            {
                IngredientData data = AssetDatabase.LoadAssetAtPath<IngredientData>(spec.ingredientDataPath);
                if (data == null)
                {
                    Debug.LogError($"[IngredientPrefabSetup] No se encontró IngredientData en {spec.ingredientDataPath}");
                    continue;
                }

                EnsureIngredientPrefab(spec.prefabPath, data, isCut: false);

                data.cortesDisponibles.Clear();
                foreach (var corte in spec.cortes)
                {
                    GameObject cutPrefab = EnsureIngredientPrefab(corte.prefabPath, data, isCut: true);
                    if (cutPrefab != null)
                    {
                        data.cortesDisponibles.Add(new ResultadoCorte { tipo = corte.tipo, prefabResultado = cutPrefab });
                    }
                }

                EditorUtility.SetDirty(data);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            FixTomatoDispenserAndLegacyRefs();

            Debug.Log("[IngredientPrefabSetup] Listo. Revisa en el Inspector el tamaño de cada SphereCollider (es una estimación) y ajústalo si el agarre se siente raro.");
        }

        private static GameObject EnsureIngredientPrefab(string prefabPath, IngredientData data, bool isCut)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[IngredientPrefabSetup] No existe el prefab {prefabPath}, se omite.");
                return null;
            }

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                GameObject root = editScope.prefabContentsRoot;

                float scale = root.transform.localScale.x != 0 ? Mathf.Abs(root.transform.localScale.x) : 1f;
                float localRadius = DesiredWorldColliderRadius / scale;

                var collider = root.GetComponent<SphereCollider>();
                if (collider == null) collider = root.AddComponent<SphereCollider>();
                collider.radius = localRadius;
                collider.center = Vector3.zero;

                var rb = root.GetComponent<Rigidbody>();
                if (rb == null) rb = root.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                rb.angularDamping = 0.05f;

                if (root.GetComponent<XRGrabInteractable>() == null)
                {
                    root.AddComponent<XRGrabInteractable>();
                }

                var item = root.GetComponent<IngredientItem>();
                if (item == null) item = root.AddComponent<IngredientItem>();

                var so = new SerializedObject(item);
                so.FindProperty("ingredientName").stringValue = data.nombre;
                so.FindProperty("isCut").boolValue = isCut;
                so.FindProperty("data").objectReferenceValue = data;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static void FixTomatoDispenserAndLegacyRefs()
        {
            GameObject newTomatoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/tomate.prefab");
            if (newTomatoPrefab == null) return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[IngredientPrefabSetup] No se pudo abrir la escena {ScenePath} para reparar el dispensador de tomate.");
                return;
            }

            bool sceneChanged = false;

            GameObject cajonTomate = GameObject.Find("Cajon_Tomate");
            if (cajonTomate != null)
            {
                var dispenser = cajonTomate.GetComponent<ItemDispenser>();
                if (dispenser != null)
                {
                    var so = new SerializedObject(dispenser);
                    so.FindProperty("itemPrefab").objectReferenceValue = newTomatoPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    sceneChanged = true;
                    Debug.Log("[IngredientPrefabSetup] 'Cajon_Tomate' ahora dispensa el nuevo prefab de tomate funcional.");
                }
            }
            else
            {
                Debug.LogWarning("[IngredientPrefabSetup] No se encontró 'Cajon_Tomate' en la escena.");
            }

            // Limpia el fallback legado (apuntaba al TomatePicado_Item.prefab ya borrado) en cada tabla.
            // Ya no se usa: el corte se resuelve vía IngredientData.cortesDisponibles.
            var boards = Object.FindObjectsByType<CuttingBoard>(FindObjectsSortMode.None);
            foreach (var board in boards)
            {
                var so = new SerializedObject(board);
                so.FindProperty("defaultCutPrefab").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            sceneChanged = sceneChanged || boards.Length > 0;

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
