using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CocinaBoliviana.Editor
{
    [InitializeOnLoad]
    public static class KitchenMechanicsSetup
    {
        static KitchenMechanicsSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("KitchenMechanics_AutoConfig_Run_v1", false))
                {
                    SessionState.SetBool("KitchenMechanics_AutoConfig_Run_v1", true);
                    SetupFirstSceneMechanics();
                }
            };
        }
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
            GameObject root = new GameObject("Tomato_Item");

            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.08f;
            col.center = Vector3.zero;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.35f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
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
            GameObject root = new GameObject("TomatePicado_Item");

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.16f, 0.08f, 0.16f);
            col.center = new Vector3(0f, 0.04f, 0f);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
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
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;

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
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
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

            // B. Cajon_Tomate Setup (was Cajon_Carne)
            GameObject pantry = GameObject.Find("Pantry");
            if (pantry != null)
            {
                var pCol = pantry.GetComponent<Collider>();
                if (pCol != null) pCol.enabled = false;
            }

            GameObject cajonTomate = GameObject.Find("Cajon_Carne");
            if (cajonTomate != null)
            {
                cajonTomate.name = "Cajon_Tomate";
            }
            else
            {
                cajonTomate = GameObject.Find("Cajon_Tomate");
            }

            if (cajonTomate != null)
            {
                var col = cajonTomate.GetComponent<BoxCollider>();
                if (col == null) col = cajonTomate.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 1.2f, 1.5f);
                col.center = new Vector3(0f, 0f, 0.25f);
                col.isTrigger = false; // Solid collider so rays hit it reliably!

                var dispenser = cajonTomate.GetComponent<ItemDispenser>();
                if (dispenser == null) dispenser = cajonTomate.AddComponent<ItemDispenser>();
                SetPrivateField(dispenser, "itemPrefab", tomatoPrefab);
                SetPrivateField(dispenser, "cooldownTime", 0.35f);

                if (!dispenser.colliders.Contains(col))
                {
                    dispenser.colliders.Add(col);
                }

                Debug.Log("[KitchenMechanicsSetup] Configured Cajon_Tomate dispenser with solid ray-interactive collider.");
            }
            else
            {
                Debug.LogWarning("[KitchenMechanicsSetup] Cajon_Tomate not found in scene!");
            }

            // C. CleanPlates Setup
            GameObject cleanPlates = GameObject.Find("CleanPlates");
            if (cleanPlates != null)
            {
                var col = cleanPlates.GetComponent<BoxCollider>();
                if (col == null) col = cleanPlates.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 15f, 1.2f);
                col.center = new Vector3(0f, 7.5f, 0f);
                col.isTrigger = false; // Solid collider so rays hit it reliably!

                var dispenser = cleanPlates.GetComponent<ItemDispenser>();
                if (dispenser == null) dispenser = cleanPlates.AddComponent<ItemDispenser>();
                SetPrivateField(dispenser, "itemPrefab", platePrefab);
                SetPrivateField(dispenser, "cooldownTime", 0.35f);

                if (!dispenser.colliders.Contains(col))
                {
                    dispenser.colliders.Add(col);
                }

                Debug.Log("[KitchenMechanicsSetup] Configured CleanPlates dispenser with solid ray-interactive collider.");
            }
            else
            {
                Debug.LogWarning("[KitchenMechanicsSetup] CleanPlates not found in scene!");
            }

            // D. Cutting Stations & Cutting Boards
            string[] cuttingStationNames = { "CuttingStation_01", "CuttingStation_02" };
            foreach (var stationName in cuttingStationNames)
            {
                GameObject station = GameObject.Find(stationName);
                if (station != null)
                {
                    Transform tabla = station.transform.Find("Tabla");
                    GameObject boardGo = (tabla != null) ? tabla.gameObject : station;

                    var col = boardGo.GetComponent<BoxCollider>();
                    if (col == null) col = boardGo.AddComponent<BoxCollider>();
                    col.size = new Vector3(1.2f, 0.4f, 1.2f);
                    col.center = new Vector3(0f, 0.15f, 0f);
                    col.isTrigger = true; // Trigger for detecting ingredients dropped on board

                    Transform snap = boardGo.transform.Find("SnapPoint");
                    if (snap == null)
                    {
                        var snapGo = new GameObject("SnapPoint");
                        snapGo.transform.SetParent(boardGo.transform, false);
                        snapGo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                        snap = snapGo.transform;
                    }

                    var board = boardGo.GetComponent<CuttingBoard>();
                    if (board == null) board = boardGo.AddComponent<CuttingBoard>();
                    SetPrivateField(board, "snapPoint", snap);
                    SetPrivateField(board, "defaultCutPrefab", tomatePicadoPrefab);
                    SetPrivateField(board, "requiredHits", 3);

                    Debug.Log($"[KitchenMechanicsSetup] Configured CuttingBoard on {boardGo.name} in {stationName}");
                }
            }

            // E. Clean up old primitive knives and place interactive Knife_Tool
            var oldKnives = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in oldKnives)
            {
                if (go.name == "Cuchillo" || go.name == "KnifeGRP" || go.name == "Knife_Tool")
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            // Place Knife_Tool right on the cutting station table next to cutting board
            if (knifePrefab != null)
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

            // F. Place a ready clean plate on the assembly counter
            if (platePrefab != null)
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
