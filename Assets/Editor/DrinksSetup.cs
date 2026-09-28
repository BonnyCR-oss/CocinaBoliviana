using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Monta la estación de refrescos: el prefab del vaso con su líquido, el chorro de
    /// partículas y el dispensador en la escena.
    ///
    /// Qué refresco sale NO se decide aquí: el dispensador lo saca de los 'refrescos' del
    /// DepartmentData del nivel. Cochabamba y La Paz dan mocochinchi, Santa Cruz da somo,
    /// y basta con crear esos departamentos.
    /// </summary>
    public static class DrinksSetup
    {
        private const string ScenePath = "Assets/00_Scenes/First Scene.unity";
        private const string VasoModelPath = "Assets/04_Models/DrinksModel/VasoPlasticoModel.glb";
        // El vaso lo hiciste tu, con su tamano ya ajustado. Aqui solo se le anaden los
        // componentes que le faltan; ni la escala ni el collider se tocan.
        private const string VasoPrefabPath = "Assets/02_Prefabs/DrinksPrebas/VasoPlasticoModel.prefab";
        private const string EstacionName = "DrinkStation";

        [MenuItem("Kitchen/Setup Drinks (refrescos y llenado)")]
        public static void SetupDrinks()
        {
            Debug.Log("[DrinksSetup] Configurando refrescos...");

            MuestrearColoresDeRefresco();

            GameObject vaso = ConstruirVasoSiFalta();
            if (vaso == null) return;

            ConfigurarEstacion(vaso);
        }

        private const string RefrescosFolder = "Assets/03_SO/Refresco";

        /// <summary>
        /// Saca el color del liquido del material del propio modelo de cada bebida, para que
        /// el chorro y el vaso caigan exactamente del color del mocochinchi o del somo sin
        /// tener que acertarlo a ojo.
        ///
        /// Si el material es practicamente blanco significa que el color vive en la textura y
        /// no en el material: entonces no se toca nada y se deja el que tenga puesto.
        /// </summary>
        private static void MuestrearColoresDeRefresco()
        {
            if (!AssetDatabase.IsValidFolder(RefrescosFolder)) return;

            foreach (string guid in AssetDatabase.FindAssets("t:DishData", new[] { RefrescosFolder }))
            {
                var bebida = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(guid));
                if (bebida == null || bebida.platoPrefab == null) continue;

                if (!TryColorDelMaterial(bebida.platoPrefab, out Color color))
                {
                    Debug.Log($"[DrinksSetup] '{bebida.nombre}': su material no define un color base " +
                              "(estara en la textura). Ajusta 'Color Liquido' a mano en el asset.");
                    continue;
                }

                bebida.colorLiquido = color;
                EditorUtility.SetDirty(bebida);
                Debug.Log($"[DrinksSetup] '{bebida.nombre}': color del liquido tomado de su material.");
            }

            AssetDatabase.SaveAssets();
        }

        private static bool TryColorDelMaterial(GameObject prefab, out Color color)
        {
            color = Color.white;

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                Material mat = r.sharedMaterial;
                if (mat == null) continue;

                // Los .glb vienen por glTFast y usan "baseColorFactor"; los materiales hechos
                // en Unity usan "_BaseColor" (URP) o "_Color" (built-in).
                foreach (string prop in new[] { "baseColorFactor", "_BaseColor", "_Color" })
                {
                    if (!mat.HasProperty(prop)) continue;

                    Color c = mat.GetColor(prop);
                    // Un blanco puro significa que el color lo pone la textura, no el material.
                    if (c.r > 0.95f && c.g > 0.95f && c.b > 0.95f) continue;

                    color = new Color(c.r, c.g, c.b, 1f);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// El vaso: modelo + un cilindro de líquido dentro que crece al llenarse, y lo
        /// necesario para poder agarrarlo.
        /// </summary>
        private static GameObject ConstruirVasoSiFalta()
        {
            var existente = AssetDatabase.LoadAssetAtPath<GameObject>(VasoPrefabPath);
            if (existente != null)
            {
                using (var scope = new PrefabUtility.EditPrefabContentsScope(VasoPrefabPath))
                {
                    Configurar(scope.prefabContentsRoot);
                }
                return AssetDatabase.LoadAssetAtPath<GameObject>(VasoPrefabPath);
            }

            var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(VasoModelPath);
            if (modelo == null)
            {
                Debug.LogError($"[DrinksSetup] No encontré {VasoModelPath}");
                return null;
            }

            var temp = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            temp.name = "VasoPlastico";
            Configurar(temp);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, VasoPrefabPath);
            Object.DestroyImmediate(temp);

            Debug.Log($"[DrinksSetup] Vaso creado en {VasoPrefabPath}");
            return prefab;
        }

        private static void Configurar(GameObject root)
        {
            // Collider ajustado a la malla, como los ingredientes.
            // Solo se crea si falta. Si ya le pusiste uno a mano, se respeta tal cual.
            var col = root.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = root.AddComponent<BoxCollider>();

                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds mundo = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) mundo.Encapsulate(renderers[i].bounds);

                    Vector3 lossy = root.transform.lossyScale;
                    col.center = root.transform.InverseTransformPoint(mundo.center);
                    col.size = new Vector3(
                        mundo.size.x / Mathf.Max(Mathf.Abs(lossy.x), 0.0001f),
                        mundo.size.y / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f),
                        mundo.size.z / Mathf.Max(Mathf.Abs(lossy.z), 0.0001f));
                }
                col.isTrigger = false;
            }

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = root.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                rb.angularDamping = 6f;
                rb.linearDamping = 0.6f;
            }

            if (root.GetComponent<XRGrabInteractable>() == null) root.AddComponent<XRGrabInteractable>();

            // El liquido: un cilindro que crece en Y. Se genera aqui para no depender de
            // que el modelo del vaso traiga uno.
            Transform liquido = root.transform.Find("Liquido");
            if (liquido == null)
            {
                var cil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cil.name = "Liquido";
                cil.transform.SetParent(root.transform, false);
                Object.DestroyImmediate(cil.GetComponent<Collider>());
                liquido = cil.transform;
            }
            liquido.localPosition = Vector3.zero;
            liquido.localRotation = Quaternion.identity;

            var cup = root.GetComponent<DrinkCup>();
            if (cup == null) cup = root.AddComponent<DrinkCup>();
            var so = new SerializedObject(cup);
            so.FindProperty("liquido").objectReferenceValue = liquido;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ojo: si rehaces el prefab del vaso desde el modelo, estos componentes se
            // pierden y hay que volver a correr este setup.
            Debug.Log($"[DrinksSetup] Vaso '{root.name}' listo: DrinkCup, Liquido y " +
                      "XRGrabInteractable puestos. Collider, Rigidbody y escala sin tocar.");
        }

        private static void ConfigurarEstacion(GameObject vasoPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            GameObject estacion = GameObject.Find(EstacionName);
            if (estacion == null)
            {
                Debug.LogError($"[DrinksSetup] No encontré '{EstacionName}' en la escena.");
                return;
            }

            var disp = estacion.GetComponent<DrinkDispenser>();
            if (disp == null) disp = estacion.AddComponent<DrinkDispenser>();

            ParticleSystem chorro = ConstruirChorroSiFalta(estacion);

            // Punto de llenado: justo bajo el grifo. Ahi se acopla el vaso al meterlo.
            Transform llenado = estacion.transform.Find("PuntoLlenado");
            if (llenado == null)
            {
                var go = new GameObject("PuntoLlenado");
                go.transform.SetParent(estacion.transform, true);
                go.transform.position = estacion.transform.position + new Vector3(0f, 0.06f, 0f);
                go.transform.rotation = Quaternion.identity;
                llenado = go.transform;
            }

            // El panel de "Servir" reusa el mismo menu flotante de los cajones: ya trae el
            // canvas world-space y el raycaster que funciona con el rayo del control.
            // Sitio del refresco terminado, al lado del grifo.
            Transform servido = estacion.transform.Find("PuntoServido");
            if (servido == null)
            {
                var go = new GameObject("PuntoServido");
                go.transform.SetParent(estacion.transform, true);
                go.transform.position = estacion.transform.position + new Vector3(0.3f, 0.06f, 0f);
                go.transform.rotation = Quaternion.identity;
                servido = go.transform;
            }

            IngredientSelectorMenu panel = AdjuntarPanel(estacion);

            var so = new SerializedObject(disp);
            so.FindProperty("chorro").objectReferenceValue = chorro;
            so.FindProperty("puntoLlenado").objectReferenceValue = llenado;
            so.FindProperty("menu").objectReferenceValue = panel;
            so.FindProperty("puntoServido").objectReferenceValue = servido;

            // El sonido del chorro solo se asigna si esta vacio, para no pisar el que
            // hayas puesto tu a mano.
            if (so.FindProperty("sonidoServir").objectReferenceValue == null)
            {
                AudioClip agua = BuscarSonidoDeAgua();
                if (agua != null)
                {
                    so.FindProperty("sonidoServir").objectReferenceValue = agua;
                    Debug.Log($"[DrinksSetup] Sonido del chorro: '{agua.name}'.");
                }
                else
                {
                    Debug.Log("[DrinksSetup] Sin sonido de agua. Deja tu archivo en " +
                              "Assets/06_SFX con 'agua', 'water', 'dispenser', 'liquido' o " +
                              "'servir' en el nombre y vuelve a correr esto, o arrástralo al " +
                              "campo 'Sonido Servir' del DrinkStation.");
                }
            }
            so.FindProperty("vasoVacioPrefab").objectReferenceValue = vasoPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DrinksSetup] '{EstacionName}' lista. Mueve 'PuntoVaso' y ajusta la zona " +
                      "del grifo en el Inspector si no cuadran con tu modelo.");
        }

        /// <summary>
        /// Busca en 06_SFX un clip que suene a agua. Por nombre y no por una ruta fija para
        /// que valga cualquier archivo que dejes ahi, se llame como se llame el formato.
        /// </summary>
        private static AudioClip BuscarSonidoDeAgua()
        {
            string[] pistas = { "agua", "water", "dispenser", "liquido", "servir", "pour" };

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/06_SFX" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();

                foreach (string pista in pistas)
                {
                    if (nombre.Contains(pista)) return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                }
            }
            return null;
        }

        /// <summary>
        /// Cuelga el menu flotante sobre la estacion, compensando la escala del padre para
        /// que no herede su distorsion.
        /// </summary>
        private static IngredientSelectorMenu AdjuntarPanel(GameObject estacion)
        {
            const string MenuPrefabPath = "Assets/02_Prefabs/UI/IngredientSelectorMenu.prefab";

            Transform existente = estacion.transform.Find("IngredientSelectorMenu");
            if (existente != null) return existente.GetComponent<IngredientSelectorMenu>();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[DrinksSetup] Falta {MenuPrefabPath}; corre antes " +
                                 "'Kitchen > Setup Ingredient Selector Menu'. Sin panel, el " +
                                 "vaso se llenara solo al ponerlo.");
                return null;
            }

            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, estacion.transform);
            instancia.name = "IngredientSelectorMenu";

            const float escalaMundo = 0.001f;
            const float alturaMundo = 0.45f;
            Vector3 lossy = estacion.transform.lossyScale;
            instancia.transform.localScale = new Vector3(
                escalaMundo / Mathf.Max(Mathf.Abs(lossy.x), 0.0001f),
                escalaMundo / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f),
                escalaMundo / Mathf.Max(Mathf.Abs(lossy.z), 0.0001f));
            instancia.transform.localPosition = new Vector3(0f, alturaMundo / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f), 0f);
            instancia.transform.localRotation = Quaternion.identity;

            return instancia.GetComponent<IngredientSelectorMenu>();
        }

        /// <summary>Chorro de líquido cayendo: partículas finas, estiradas y rápidas.</summary>
        private static ParticleSystem ConstruirChorroSiFalta(GameObject estacion)
        {
            Transform existente = estacion.transform.Find("Chorro");
            if (existente != null) return existente.GetComponent<ParticleSystem>();

            var go = new GameObject("Chorro");
            go.transform.SetParent(estacion.transform, true);
            go.transform.position = estacion.transform.position + new Vector3(0f, 0.28f, 0f);
            go.transform.rotation = Quaternion.identity;

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 0.25f;
            main.startSpeed = 1.1f;
            main.startSize = 0.012f;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.playOnAwake = false;
            main.startColor = new Color(0.78f, 0.45f, 0.12f, 0.85f);

            var emission = ps.emission;
            emission.rateOverTime = 90f;

            // Cono muy estrecho apuntando hacia abajo: un chorro, no una ducha.
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 2f;
            shape.radius = 0.008f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            // Estiradas en la direccion de caida: es lo que las hace leer como liquido.
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 2.5f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Water.mat");
            if (mat != null) renderer.sharedMaterial = mat;

            Debug.Log("[DrinksSetup] Chorro creado. Súbelo o bájalo en la Hierarchy para " +
                      "alinearlo con el grifo de tu modelo.");
            return ps;
        }
    }
}
