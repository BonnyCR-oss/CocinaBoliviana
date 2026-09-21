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

            ClearLegacyCuttingBoardRefs();

            Debug.Log("[IngredientPrefabSetup] Listo. Los colliders se ajustan solos a la malla de cada ingrediente.");
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

                // Caja ajustada a la malla, no esfera. Una esfera RUEDA por definición: los
                // ingredientes no paraban quietos al soltarlos. Además el radio de antes era
                // un valor fijo estimado, igual para una papa que para un grano de arroz.
                var esferaVieja = root.GetComponent<SphereCollider>();
                if (esferaVieja != null) Object.DestroyImmediate(esferaVieja, true);

                var collider = root.GetComponent<BoxCollider>();
                if (collider == null) collider = root.AddComponent<BoxCollider>();

                if (TryGetLocalBounds(root, out Vector3 centro, out Vector3 tamano))
                {
                    collider.center = centro;
                    collider.size = tamano;
                }
                else
                {
                    Debug.LogWarning($"[IngredientPrefabSetup] {prefabPath} no tiene Renderer; " +
                                     "se le deja un collider por defecto.");
                    float scale = root.transform.localScale.x != 0 ? Mathf.Abs(root.transform.localScale.x) : 1f;
                    collider.size = Vector3.one * (DesiredWorldColliderRadius * 2f / scale);
                    collider.center = Vector3.zero;
                }
                collider.sharedMaterial = EnsurePhysicsMaterial();

                var rb = root.GetComponent<Rigidbody>();
                if (rb == null) rb = root.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                // El 0.05 de antes era casi nulo y los dejaba girando eternamente.
                rb.angularDamping = 6f;
                rb.linearDamping = 0.6f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

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

        /// <summary>
        /// Limpia el fallback legado 'defaultCutPrefab' de cada tabla de cortar: apuntaba al
        /// TomatePicado_Item.prefab ya borrado, y hoy el corte se resuelve vía
        /// IngredientData.cortesDisponibles.
        /// </summary>
        private const string PhysicsMaterialPath = "Assets/Materials/Fisica_Ingrediente.physicsMaterial";

        /// <summary>
        /// Mucha fricción y cero rebote: sin esto los ingredientes patinan y botan por el
        /// mostrador en vez de quedarse donde los sueltas.
        /// </summary>
        private static PhysicsMaterial EnsurePhysicsMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysicsMaterialPath);
            if (mat == null)
            {
                mat = new PhysicsMaterial("Fisica_Ingrediente");
                AssetDatabase.CreateAsset(mat, PhysicsMaterialPath);
                Debug.Log($"[IngredientPrefabSetup] Material de física creado en {PhysicsMaterialPath}");
            }

            mat.dynamicFriction = 0.85f;
            mat.staticFriction = 0.95f;
            mat.bounciness = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Maximum;
            mat.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Caja que envuelve a todos los Renderer del prefab, expresada en el espacio local
        /// de la raíz. Así cada ingrediente lleva un collider de su tamaño real en vez de
        /// uno estimado igual para todos.
        /// </summary>
        private static bool TryGetLocalBounds(GameObject root, out Vector3 centro, out Vector3 tamano)
        {
            centro = Vector3.zero;
            tamano = Vector3.one;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;

            Bounds mundo = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                mundo.Encapsulate(renderers[i].bounds);
            }

            Vector3 lossy = root.transform.lossyScale;
            centro = root.transform.InverseTransformPoint(mundo.center);
            tamano = new Vector3(
                mundo.size.x / Mathf.Max(Mathf.Abs(lossy.x), 0.0001f),
                mundo.size.y / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f),
                mundo.size.z / Mathf.Max(Mathf.Abs(lossy.z), 0.0001f));
            return true;
        }

        private static void ClearLegacyCuttingBoardRefs()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[IngredientPrefabSetup] No se pudo abrir la escena {ScenePath}.");
                return;
            }

            var boards = Object.FindObjectsByType<CuttingBoard>(FindObjectsInactive.Exclude);
            if (boards.Length == 0) return;

            foreach (var board in boards)
            {
                var so = new SerializedObject(board);
                so.FindProperty("defaultCutPrefab").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[IngredientPrefabSetup] Limpiado 'defaultCutPrefab' en {boards.Length} tabla(s).");
        }
    }
}
