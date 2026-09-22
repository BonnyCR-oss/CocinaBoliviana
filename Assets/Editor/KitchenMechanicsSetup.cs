using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    public static class KitchenMechanicsSetup
    {
        private const string ScenePath = "Assets/Scenes/First Scene.unity";
        private const string PrefabsFolder = "Assets/02_Prefabs";
        private const string TomatoGlbPath = "Assets/04_Models/IngredientesModel/tomate.glb";
        private const string TomatePicadoGlbPath = "Assets/04_Models/IngredientesModel/TomatePicado.glb";
        private const string PlatePrefabPath = "Assets/04_Models/Plate.prefab";
        private const string KnifePrefabPath = "Assets/02_Prefabs/KnifeGRP.prefab";

        private const string TomatoPrefabPath = "Assets/02_Prefabs/Tomato_Item.prefab";
        private const string TomatePicadoPrefabPath = "Assets/02_Prefabs/TomatePicado_Item.prefab";
        private const string PlateItemPrefabPath = "Assets/02_Prefabs/Plate_Item.prefab";
        private const string KnifeToolPrefabPath = "Assets/02_Prefabs/Knife_Tool.prefab";

        /// <summary>Cajones de la despensa, tal como se llaman en la escena.</summary>
        internal static readonly string[] CrateNames = { "Cajon_Papas", "Cajon_Carne", "Cajon_Verduras" };

        [MenuItem("Kitchen/Setup Kitchen Mechanics (First Scene)")]
        public static void SetupFirstSceneMechanics()
        {
            Debug.Log("[KitchenMechanicsSetup] Setting up Kitchen Mechanics in First Scene.unity...");

            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "02_Prefabs");
            }

            // 1. Build and verify Prefabs
            var tomatePicadoPrefab = CreateTomatePicadoPrefab();
            var tomatoPrefab = CreateTomatoPrefab(tomatePicadoPrefab);
            var platePrefab = CreatePlatePrefab();
            var knifePrefab = CreateKnifePrefab();

            AssetDatabase.SaveAssets();

            // 2. Setup First Scene exclusively
            SetupFirstSceneOnly(tomatoPrefab, tomatePicadoPrefab, platePrefab, knifePrefab);

            AssetDatabase.SaveAssets();
            Debug.Log("[KitchenMechanicsSetup] All Kitchen Mechanics successfully configured in First Scene.unity!");
        }

        private static GameObject CreateTomatoPrefab(GameObject cutPrefab)
        {
            // Si el prefab ya existe se devuelve tal cual: reconstruirlo desde cero borraba
            // los ajustes hechos a mano (ángulo del AttachPoint, colliders, etc.).
            // Para forzar un rebuild, borra el archivo y vuelve a correr el setup.
            var yaExiste = AssetDatabase.LoadAssetAtPath<GameObject>(TomatoPrefabPath);
            if (yaExiste != null) return yaExiste;

            GameObject root = new GameObject("Tomato_Item");

            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.08f;
            col.center = Vector3.zero;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.35f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.AddComponent<XRGrabInteractable>();
            // El 'movementType' y el resto del tacto del agarre los fija GrabFeelSetup
            // (paso 7 del bootstrap); no se tocan aquí para no pelearse por el mismo campo.
            grab.throwOnDetach = true;

            var item = root.AddComponent<IngredientItem>();
            SetPrivateField(item, "ingredientName", "Tomate");
            SetPrivateField(item, "isCut", false);
            if (cutPrefab != null)
            {
                SetPrivateField(item, "cutPrefab", cutPrefab);
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TomatoGlbPath);
            if (modelAsset != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                visual.transform.localPosition = new Vector3(0f, -0.015f, 0f);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TomatoPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[KitchenMechanicsSetup] Built {TomatoPrefabPath}");
            return prefab;
        }

        private static GameObject CreateTomatePicadoPrefab()
        {
            // Si el prefab ya existe se devuelve tal cual: reconstruirlo desde cero borraba
            // los ajustes hechos a mano (ángulo del AttachPoint, colliders, etc.).
            // Para forzar un rebuild, borra el archivo y vuelve a correr el setup.
            var yaExiste = AssetDatabase.LoadAssetAtPath<GameObject>(TomatePicadoPrefabPath);
            if (yaExiste != null) return yaExiste;

            GameObject root = new GameObject("TomatePicado_Item");

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.16f, 0.08f, 0.16f);
            col.center = new Vector3(0f, 0.04f, 0f);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.AddComponent<XRGrabInteractable>();
            // El 'movementType' y el resto del tacto del agarre los fija GrabFeelSetup
            // (paso 7 del bootstrap); no se tocan aquí para no pelearse por el mismo campo.
            grab.throwOnDetach = true;

            var item = root.AddComponent<IngredientItem>();
            SetPrivateField(item, "ingredientName", "TomatePicado");
            SetPrivateField(item, "isCut", true);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(TomatePicadoGlbPath);
            if (modelAsset != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
                visual.transform.localPosition = Vector3.zero;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TomatePicadoPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[KitchenMechanicsSetup] Built {TomatePicadoPrefabPath}");
            return prefab;
        }

        private static GameObject CreatePlatePrefab()
        {
            // Si el prefab ya existe se devuelve tal cual: reconstruirlo desde cero borraba
            // los ajustes hechos a mano (ángulo del AttachPoint, colliders, etc.).
            // Para forzar un rebuild, borra el archivo y vuelve a correr el setup.
            var yaExiste = AssetDatabase.LoadAssetAtPath<GameObject>(PlateItemPrefabPath);
            if (yaExiste != null) return yaExiste;

            var basePlate = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);
            GameObject root;
            if (basePlate != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(basePlate);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.transform.localScale = new Vector3(0.4f, 0.02f, 0.4f);
            }

            root.name = "Plate_Item";

            var baseCol = root.GetComponent<BoxCollider>();
            if (baseCol == null) baseCol = root.AddComponent<BoxCollider>();
            baseCol.size = new Vector3(0.38f, 0.035f, 0.38f);
            baseCol.center = new Vector3(0f, 0.017f, 0f);
            baseCol.isTrigger = false;

            SphereCollider snapCol = null;
            var cols = root.GetComponents<SphereCollider>();
            foreach (var c in cols)
            {
                if (c.isTrigger) { snapCol = c; break; }
            }
            if (snapCol == null) snapCol = root.AddComponent<SphereCollider>();
            snapCol.radius = 0.22f;
            snapCol.center = new Vector3(0f, 0.07f, 0f);
            snapCol.isTrigger = true;

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.8f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.GetComponent<XRGrabInteractable>();
            if (grab == null) grab = root.AddComponent<XRGrabInteractable>();
            // El 'movementType' y el resto del tacto del agarre los fija GrabFeelSetup
            // (paso 7 del bootstrap); no se tocan aquí para no pelearse por el mismo campo.
            grab.throwOnDetach = true;

            // Un plato se lleva horizontal aunque gires la muñeca: sigue la posición de la
            // mano pero NO su rotación. Sin esto el plato se inclina con el control y la
            // comida encima queda de canto.
            grab.trackRotation = false;

            // Se agarra por el centro de la base, como un mozo llevando la bandeja. El pivote
            // del prefab está en la base del disco, así que el attach va en el origen.
            Transform plateAttach = root.transform.Find("AttachPoint");
            if (plateAttach == null)
            {
                var attachGo = new GameObject("AttachPoint");
                attachGo.transform.SetParent(root.transform, false);
                plateAttach = attachGo.transform;
            }
            plateAttach.localPosition = Vector3.zero;
            plateAttach.localRotation = Quaternion.identity;
            plateAttach.localScale = Vector3.one;
            grab.attachTransform = plateAttach;

            Transform foodSnap = root.transform.Find("FoodSnapPoint");
            if (foodSnap == null)
            {
                var snapGo = new GameObject("FoodSnapPoint");
                snapGo.transform.SetParent(root.transform, false);
                snapGo.transform.localPosition = new Vector3(0f, 0.035f, 0f);
                foodSnap = snapGo.transform;
            }

            var plateComp = root.GetComponent<PlateItem>();
            if (plateComp == null) plateComp = root.AddComponent<PlateItem>();
            SetPrivateField(plateComp, "foodSnapPoint", foodSnap);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlateItemPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[KitchenMechanicsSetup] Built {PlateItemPrefabPath}");
            return prefab;
        }

        private static GameObject CreateKnifePrefab()
        {
            // Si el prefab ya existe se devuelve tal cual: reconstruirlo desde cero borraba
            // los ajustes hechos a mano (ángulo del AttachPoint, colliders, etc.).
            // Para forzar un rebuild, borra el archivo y vuelve a correr el setup.
            var yaExiste = AssetDatabase.LoadAssetAtPath<GameObject>(KnifeToolPrefabPath);
            if (yaExiste != null) return yaExiste;

            var baseKnife = AssetDatabase.LoadAssetAtPath<GameObject>(KnifePrefabPath);
            GameObject root;
            if (baseKnife != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(baseKnife);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else
            {
                root = new GameObject("Knife_Tool");
            }

            root.name = "Knife_Tool";

            var handleCol = root.GetComponent<BoxCollider>();
            if (handleCol == null) handleCol = root.AddComponent<BoxCollider>();
            handleCol.size = new Vector3(0.045f, 0.045f, 0.16f);
            handleCol.center = new Vector3(0f, 0f, 0.08f);
            handleCol.isTrigger = false;

            var rb = root.GetComponent<Rigidbody>();
            if (rb == null) rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.GetComponent<XRGrabInteractable>();
            if (grab == null) grab = root.AddComponent<XRGrabInteractable>();
            // El 'movementType' y el resto del tacto del agarre los fija GrabFeelSetup
            // (paso 7 del bootstrap); no se tocan aquí para no pelearse por el mismo campo.
            grab.throwOnDetach = true;

            Transform bladeTrigger = root.transform.Find("Blade_Trigger");
            if (bladeTrigger == null)
            {
                var triggerGo = new GameObject("Blade_Trigger");
                triggerGo.transform.SetParent(root.transform, false);
                triggerGo.transform.localPosition = new Vector3(0f, -0.01f, -0.10f);
                bladeTrigger = triggerGo.transform;
            }

            var bladeCol = bladeTrigger.GetComponent<BoxCollider>();
            if (bladeCol == null) bladeCol = bladeTrigger.gameObject.AddComponent<BoxCollider>();
            bladeCol.size = new Vector3(0.04f, 0.10f, 0.24f);
            bladeCol.center = Vector3.zero;
            bladeCol.isTrigger = true;

            var chopper = bladeTrigger.GetComponent<KnifeChopper>();
            if (chopper == null) chopper = bladeTrigger.gameObject.AddComponent<KnifeChopper>();

            // Punto de agarre. Sin esto XRI alinea el PIVOTE RAÍZ del cuchillo con la mano, y
            // como el mango va hacia +Z y la hoja hacia -Z, el filo terminaba apuntando al
            // jugador. XRI hace coincidir el forward (+Z) de este transform con el forward del
            // attach del control, así que se gira 180° en Y para que lo que salga de la mano
            // sea la hoja y no el mango.
            // Si el ángulo no convence, este es el transform a mover: 'AttachPoint' dentro de
            // Assets/02_Prefabs/Knife_Tool.prefab.
            Transform attach = root.transform.Find("AttachPoint");
            if (attach == null)
            {
                var attachGo = new GameObject("AttachPoint");
                attachGo.transform.SetParent(root.transform, false);
                attach = attachGo.transform;
            }
            attach.localPosition = new Vector3(0f, 0f, 0.08f); // centro del mango
            attach.localRotation = Quaternion.Euler(0f, 180f, 0f);
            attach.localScale = Vector3.one;
            grab.attachTransform = attach;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, KnifeToolPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[KitchenMechanicsSetup] Built {KnifeToolPrefabPath}");
            return prefab;
        }

        private static void SetupFirstSceneOnly(GameObject tomatoPrefab, GameObject tomatePicadoPrefab, GameObject platePrefab, GameObject knifePrefab)
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[KitchenMechanicsSetup] Scene not found at {ScenePath}");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            Debug.Log($"[KitchenMechanicsSetup] Configuring {ScenePath}...");

            // A. Ensure XR Interaction Manager exists
            GameObject xrManagerGo = GameObject.Find("XR Interaction Manager");
            if (xrManagerGo == null)
            {
                xrManagerGo = new GameObject("XR Interaction Manager");
                xrManagerGo.AddComponent<XRInteractionManager>();
                Debug.Log("[KitchenMechanicsSetup] Created XR Interaction Manager in scene.");
            }

            // B. Cajones de la despensa: cada uno lleva su propio ItemDispenser.
            // La geometría de la escena los trae sin collider, así que aquí se les pone uno sólido
            // (no trigger) para que el rayo del control los golpee de forma fiable.
            // La lista de ingredientes de cada cajón la llena DispenserMenuSetup.
            GameObject pantry = GameObject.Find("Pantry");
            if (pantry != null)
            {
                var pCol = pantry.GetComponent<Collider>();
                if (pCol != null) pCol.enabled = false;
            }

            foreach (string crateName in CrateNames)
            {
                GameObject crate = GameObject.Find(crateName);
                if (crate == null)
                {
                    Debug.LogWarning($"[KitchenMechanicsSetup] '{crateName}' no está en la escena.");
                    continue;
                }

                var col = crate.GetComponent<BoxCollider>();
                if (col == null) col = crate.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 1.2f, 1.5f);
                col.center = new Vector3(0f, 0f, 0.25f);
                col.isTrigger = false; // Solid collider so rays hit it reliably!

                var dispenser = crate.GetComponent<ItemDispenser>();
                if (dispenser == null) dispenser = crate.AddComponent<ItemDispenser>();
                SetPrivateField(dispenser, "cooldownTime", 0.35f);

                if (!dispenser.colliders.Contains(col))
                {
                    dispenser.colliders.Add(col);
                }

                Debug.Log($"[KitchenMechanicsSetup] '{crateName}': collider sólido + ItemDispenser listos.");
            }

            // C. CleanPlates: solo decoracion.
            // Ya NO reparte platos. El diseno es que haya UN unico plato de emplatado: al
            // completarse una receta, el plato servido se va al punto de recogida y
            // PlateItem deja uno limpio en su sitio. Un dispensador aqui llenaba la cocina
            // de platos vacios y encima le robaba el rayo al plato bueno, porque estan
            // practicamente en el mismo punto.
            GameObject cleanPlates = GameObject.Find("CleanPlates");
            if (cleanPlates != null)
            {
                var dispensadorViejo = cleanPlates.GetComponent<ItemDispenser>();
                if (dispensadorViejo != null)
                {
                    Undo.DestroyObjectImmediate(dispensadorViejo);
                    Debug.Log("[KitchenMechanicsSetup] Quitado el ItemDispenser de 'CleanPlates': " +
                              "el plato de emplatado se repone solo.");
                }

                // Sin collider tampoco intercepta el rayo del control.
                var colViejo = cleanPlates.GetComponent<BoxCollider>();
                if (colViejo != null) Undo.DestroyObjectImmediate(colViejo);
            }
            else
            {
                Debug.LogWarning("[KitchenMechanicsSetup] CleanPlates not found in scene!");
            }

            // D. Cutting Stations & Cutting Boards
            string[] cuttingStationNames = { "CuttingStation_01" };
            foreach (var stationName in cuttingStationNames)
            {
                GameObject station = GameObject.Find(stationName);
                if (station != null)
                {
                    Transform tabla = station.transform.Find("Tabla");
                    GameObject boardGo = (tabla != null) ? tabla.gameObject : station;

                    // La Tabla tiene escala (0.45, 0.02, 0.35). Definir colliders en espacio
                    // LOCAL los deja deformados: los 0.4 de alto de antes eran 8 mm reales.
                    // Todo se declara en metros de mundo y se convierte.
                    Vector3 lossy = boardGo.transform.lossyScale;
                    Vector3 AMundo(Vector3 metros) => new Vector3(
                        metros.x / Mathf.Max(Mathf.Abs(lossy.x), 0.0001f),
                        metros.y / Mathf.Max(Mathf.Abs(lossy.y), 0.0001f),
                        metros.z / Mathf.Max(Mathf.Abs(lossy.z), 0.0001f));

                    // Collider SÓLIDO: es la superficie donde se apoyan los ingredientes.
                    // La Tabla viene de la escena sin collider, así que hay que ponérselo o
                    // todo la atraviesa y queda hundido sobre la Mesa. La detección NO usa
                    // este collider: va por OverlapBox dentro de CuttingBoard.
                    var col = boardGo.GetComponent<BoxCollider>();
                    if (col == null) col = boardGo.AddComponent<BoxCollider>();
                    col.size = AMundo(new Vector3(0.45f, 0.02f, 0.35f));
                    col.center = Vector3.zero;
                    col.isTrigger = false;

                    // Resto de un intento anterior: la superficie sólida ahora es el propio
                    // collider de la Tabla, así que este hijo sobra.
                    Transform legacySurface = boardGo.transform.Find("SolidSurface");
                    if (legacySurface != null) Undo.DestroyObjectImmediate(legacySurface.gameObject);

                    // Solo se coloca al crearlo. Si ya existe se respeta dónde esté, para no
                    // pisar un ajuste hecho a mano en el Inspector.
                    Transform snap = boardGo.transform.Find("SnapPoint");
                    if (snap == null)
                    {
                        var snapGo = new GameObject("SnapPoint");
                        snapGo.transform.SetParent(boardGo.transform, false);
                        snap = snapGo.transform;

                        // Los ingredientes tienen un SphereCollider de ~6 cm de radio y la
                        // tabla 2 cm de grosor, así que el centro va 7 cm sobre el centro de
                        // la tabla para que se apoyen encima en vez de quedar incrustados.
                        snap.localPosition = AMundo(new Vector3(0f, 0.07f, 0f));
                        snap.localRotation = Quaternion.identity;
                    }

                    var board = boardGo.GetComponent<CuttingBoard>();
                    if (board == null) board = boardGo.AddComponent<CuttingBoard>();
                    SetPrivateField(board, "snapPoint", snap);
                    SetPrivateField(board, "defaultCutPrefab", tomatePicadoPrefab);
                    SetPrivateField(board, "requiredHits", 3);
                    // Zona de detección en metros de mundo: 30 cm de alto para que un
                    // ingrediente que cae no la atraviese entre frames.
                    SetPrivateField(board, "zonaDeteccion", new Vector3(0.50f, 0.30f, 0.40f));
                    SetPrivateField(board, "zonaAltura", 0.15f);

                    Debug.Log($"[KitchenMechanicsSetup] Configured CuttingBoard on {boardGo.name} in {stationName}");
                }
            }

            // E. Clean up old primitive knives and place interactive Knife_Tool
            // Antes esto borraba todo objeto llamado "Cuchillo"/"KnifeGRP"/"Knife_Tool" para
            // migrar de los cuchillos primitivos viejos. Esa migración ya está hecha, y
            // mantenerla significaba destruir cualquier cuchillo puesto a mano. Fuera.

            // Cuchillo: solo se coloca si NO hay ninguno. Si ya está, se respeta dónde lo dejaste.
            if (knifePrefab != null && GameObject.Find("Knife_Tool") == null)
            {
                GameObject cuttingStation = GameObject.Find("CuttingStation_01");
                Vector3 knifePos = new Vector3(-0.65f, 0.98f, 2.35f);
                if (cuttingStation != null)
                {
                    knifePos = cuttingStation.transform.position + new Vector3(0.35f, 0.48f, 0f);
                }

                GameObject newKnife = (GameObject)PrefabUtility.InstantiatePrefab(knifePrefab);
                newKnife.name = "Knife_Tool";
                newKnife.transform.position = knifePos;
                newKnife.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Debug.Log($"[KitchenMechanicsSetup] Placed interactive Knife_Tool at {knifePos}");
            }

            // Sitio fijo del cuchillo, junto a la tabla, para que no se pierda al soltarlo.
            // Va aparte del bloque de arriba para que también alcance a un cuchillo que ya
            // estuviera puesto a mano.
            GameObject knife = GameObject.Find("Knife_Tool");
            GameObject station01 = GameObject.Find("CuttingStation_01");
            if (knife != null && station01 != null)
            {
                Transform sitio = station01.transform.Find("KnifeHome");
                if (sitio == null)
                {
                    var sitioGo = new GameObject("KnifeHome");
                    sitioGo.transform.SetParent(station01.transform, true);
                    // Al lado de la tabla, sobre el mostrador. Solo se coloca al crearlo:
                    // si lo mueves en el Inspector, el cuchillo volverá a donde tú digas.
                    sitioGo.transform.position = station01.transform.position + new Vector3(0.35f, 0.48f, 0f);
                    sitioGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    sitio = sitioGo.transform;
                }

                var home = knife.GetComponent<ToolHome>();
                if (home == null) home = knife.AddComponent<ToolHome>();

                var soHome = new SerializedObject(home);
                soHome.FindProperty("sitio").objectReferenceValue = sitio;
                soHome.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log("[KitchenMechanicsSetup] Cuchillo con sitio fijo en 'KnifeHome' junto a la tabla.");
            }

            // F. Un solo plato en la mesa de armado.
            // Antes se colocaba uno en CADA corrida y se acumulaban duplicados apilados,
            // peleandose por el mismo rayo del control. Se deja el primero y se borra el resto.
            var platos = new System.Collections.Generic.List<GameObject>();
            foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name == "Plate_Item") platos.Add(go);
            }
            for (int i = 1; i < platos.Count; i++)
            {
                Undo.DestroyObjectImmediate(platos[i]);
            }
            if (platos.Count > 1)
            {
                Debug.Log($"[KitchenMechanicsSetup] Habia {platos.Count} Plate_Item apilados; " +
                          "se dejo uno y se borraron los demas.");
            }

            if (platePrefab != null && platos.Count == 0)
            {
                GameObject assemblyStation = GameObject.Find("AssemblyStation");
                Vector3 platePos = new Vector3(-0.6f, 0.98f, 0f);
                if (assemblyStation != null)
                {
                    platePos = assemblyStation.transform.position + new Vector3(0.2f, 0.48f, 0f);
                }

                GameObject initialPlate = (GameObject)PrefabUtility.InstantiatePrefab(platePrefab);
                initialPlate.name = "Plate_Item";
                initialPlate.transform.position = platePos;
                initialPlate.transform.rotation = Quaternion.identity;
                Debug.Log($"[KitchenMechanicsSetup] Placed ready Plate_Item at {platePos}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[KitchenMechanicsSetup] Saved {ScenePath} successfully. Scene is now active and ready!");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null)
            {
                fi.SetValue(target, value);
            }
        }
    }
}
