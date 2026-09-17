using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CocinaBoliviana.Editor
{
    [InitializeOnLoad]
    public static class KitchenBuilder
    {
        private const string ScenePath = "Assets/Scenes/First Scene.unity";
        private const string MaterialsFolder = "Assets/Materials";

        static KitchenBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("KitchenBuilder_Executed_v3", false))
                {
                    SessionState.SetBool("KitchenBuilder_Executed_v3", true);
                    BuildKitchenScene();
                }
            };
        }

        [MenuItem("Kitchen/Build Kitchen Scene (First Scene)")]
        public static void BuildKitchenScene()
        {
            Debug.Log("[KitchenBuilder] Building Overcooked-style Bolivian VR Kitchen Scene (v3)...");

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Curated harmonious Overcooked-style materials
            Material matFloor = GetOrCreateMaterial("Mat_Floor", new Color(0.82f, 0.85f, 0.87f), 0.0f, 0.2f, urpShader);
            Material matWall = GetOrCreateMaterial("Mat_Wall", new Color(0.98f, 0.96f, 0.91f), 0.0f, 0.1f, urpShader);
            Material matCounter = GetOrCreateMaterial("Mat_Counter", new Color(0.87f, 0.63f, 0.37f), 0.0f, 0.3f, urpShader);
            Material matCounterTop = GetOrCreateMaterial("Mat_CounterTop", new Color(0.95f, 0.95f, 0.95f), 0.1f, 0.5f, urpShader);
            Material matMetal = GetOrCreateMaterial("Mat_Metal", new Color(0.60f, 0.66f, 0.70f), 0.85f, 0.65f, urpShader);
            Material matMetalHood = GetOrCreateMaterial("Mat_Metal_Hood", new Color(0.75f, 0.80f, 0.84f), 0.92f, 0.78f, urpShader);
            Material matBelt = GetOrCreateMaterial("Mat_Belt", new Color(0.12f, 0.13f, 0.14f), 0.05f, 0.2f, urpShader);
            Material matBeltStripe = GetOrCreateMaterial("Mat_Belt_Stripe", new Color(0.95f, 0.70f, 0.10f), 0.0f, 0.4f, urpShader);
            Material matTrash = GetOrCreateMaterial("Mat_Trash", new Color(0.18f, 0.42f, 0.31f), 0.0f, 0.25f, urpShader);
            Material matPantry = GetOrCreateMaterial("Mat_Pantry", new Color(0.52f, 0.36f, 0.26f), 0.0f, 0.2f, urpShader);
            Material matPlateClean = GetOrCreateMaterial("Mat_Plate_Clean", new Color(0.22f, 0.72f, 1.0f), 0.0f, 0.5f, urpShader);
            Material matPlateDirty = GetOrCreateMaterial("Mat_Plate_Dirty", new Color(0.91f, 0.44f, 0.32f), 0.0f, 0.4f, urpShader);
            Material matBoard = GetOrCreateMaterial("Mat_Board", new Color(0.96f, 0.64f, 0.38f), 0.0f, 0.35f, urpShader);
            Material matWater = GetOrCreateMaterial("Mat_Water", new Color(0.28f, 0.80f, 0.90f), 0.1f, 0.85f, urpShader);
            Material matLamp = GetOrCreateMaterial("Mat_Lamp", new Color(1.0f, 0.82f, 0.40f), 0.0f, 0.3f, urpShader);
            Material matProduceRed = GetOrCreateMaterial("Mat_Produce_Red", new Color(0.88f, 0.23f, 0.15f), 0.0f, 0.3f, urpShader);
            Material matProduceYellow = GetOrCreateMaterial("Mat_Produce_Yellow", new Color(0.96f, 0.73f, 0.22f), 0.0f, 0.3f, urpShader);
            Material matProduceGreen = GetOrCreateMaterial("Mat_Produce_Green", new Color(0.18f, 0.68f, 0.35f), 0.0f, 0.3f, urpShader);

            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[KitchenBuilder] Failed to open scene at {ScenePath}");
                return;
            }

            // Remove legacy Plane if any
            GameObject oldPlane = GameObject.Find("Plane");
            if (oldPlane != null)
            {
                Undo.DestroyObjectImmediate(oldPlane);
            }

            // Configure XR Origin (XR Rig)
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
                if (prefab != null)
                {
                    xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    xrOrigin.name = "XR Origin (XR Rig)";
                    Debug.Log("[KitchenBuilder] Restored missing XR Origin (XR Rig) with Camera from prefab.");
                }
            }

            if (xrOrigin != null)
            {
                // Put XR Origin on Layer 0 (Default) so CharacterController collides unconditionally with Default layer static colliders
                xrOrigin.layer = 0;
                xrOrigin.transform.position = new Vector3(0f, 0.05f, -1.2f);
                xrOrigin.transform.rotation = Quaternion.identity;

                var cc = xrOrigin.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.radius = 0.32f;
                    cc.height = 1.75f;
                    cc.center = new Vector3(0f, 0.875f, 0f);
                    cc.stepOffset = 0.10f; // Critical: low step offset prevents climbing onto 0.9m counters!
                    cc.skinWidth = 0.03f;
                    cc.slopeLimit = 45f;
                    cc.minMoveDistance = 0.0f;
                    EditorUtility.SetDirty(cc);
                }

                // Add or update PlayerVoidGuard to rescue player if falling below floor
                var voidGuard = xrOrigin.GetComponent<PlayerVoidGuard>();
                if (voidGuard == null)
                {
                    xrOrigin.AddComponent<PlayerVoidGuard>();
                }
                EditorUtility.SetDirty(xrOrigin);
            }

            // Clean previous kitchen environment
            GameObject oldKitchen = GameObject.Find("Kitchen_Environment");
            if (oldKitchen != null)
            {
                Undo.DestroyObjectImmediate(oldKitchen);
            }

            GameObject kitchenEnv = new GameObject("Kitchen_Environment");
            kitchenEnv.transform.position = Vector3.zero;
            kitchenEnv.transform.rotation = Quaternion.identity;
            kitchenEnv.transform.localScale = Vector3.one;

            // 1. FLOOR (10 x 0.1 x 6) with TeleportationArea
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(kitchenEnv.transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(10f, 0.1f, 6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = matFloor;

            Type teleportationAreaType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                teleportationAreaType = asm.GetType("UnityEngine.XR.Interaction.Toolkit.TeleportationArea") 
                                     ?? asm.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea");
                if (teleportationAreaType != null) break;
            }
            if (teleportationAreaType != null)
            {
                floor.AddComponent(teleportationAreaType);
            }

            // 2. CEILING (10 x 0.1 x 6)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(kitchenEnv.transform, false);
            ceiling.transform.localPosition = new Vector3(0f, 3f, 0f);
            ceiling.transform.localScale = new Vector3(10f, 0.1f, 6f);
            ceiling.GetComponent<MeshRenderer>().sharedMaterial = matFloor;

            // 3. VISIBLE WALLS (Static BoxColliders, NO Rigidbody)
            CreateWall(kitchenEnv.transform, "Wall_North", new Vector3(0f, 1.5f, 2.95f), new Vector3(10f, 2.9f, 0.1f), matWall);
            CreateWall(kitchenEnv.transform, "Wall_South", new Vector3(0f, 1.5f, -2.95f), new Vector3(10f, 2.9f, 0.1f), matWall);
            CreateWall(kitchenEnv.transform, "Wall_East", new Vector3(4.95f, 1.5f, 0f), new Vector3(0.1f, 2.9f, 5.8f), matWall);
            CreateWall(kitchenEnv.transform, "Wall_West", new Vector3(-4.95f, 1.5f, 0f), new Vector3(0.1f, 2.9f, 5.8f), matWall);

            // 4. SOLID PERIMETER BOUNDARY BLOCKERS (1.5m thick impenetrable static barriers)
            GameObject boundariesRoot = new GameObject("Solid_Boundaries");
            boundariesRoot.transform.SetParent(kitchenEnv.transform, false);
            CreateBoundaryBarrier(boundariesRoot.transform, "Boundary_North", new Vector3(0f, 1.5f, 3.75f), new Vector3(14f, 5f, 1.5f));
            CreateBoundaryBarrier(boundariesRoot.transform, "Boundary_South", new Vector3(0f, 1.5f, -3.75f), new Vector3(14f, 5f, 1.5f));
            CreateBoundaryBarrier(boundariesRoot.transform, "Boundary_East", new Vector3(5.75f, 1.5f, 0f), new Vector3(1.5f, 5f, 10f));
            CreateBoundaryBarrier(boundariesRoot.transform, "Boundary_West", new Vector3(-5.75f, 1.5f, 0f), new Vector3(1.5f, 5f, 10f));

            // 5. SAFETY SUB-FLOOR CATCHER (30m x 30m invisible platform at Y=-0.25m so player can never fall into void)
            CreateBoundaryBarrier(boundariesRoot.transform, "Safety_Catch_Floor", new Vector3(0f, -0.25f, 0f), new Vector3(30f, 0.3f, 30f));

            // 6. CEILING LAMPS with warm Point Lights
            GameObject lampsRoot = new GameObject("Ceiling_Lamps");
            lampsRoot.transform.SetParent(kitchenEnv.transform, false);
            AddCeilingLamp(lampsRoot.transform, "Lamp_NorthWest", new Vector3(-2.2f, 2.9f, 1.2f), matMetal, matLamp);
            AddCeilingLamp(lampsRoot.transform, "Lamp_NorthEast", new Vector3(2.2f, 2.9f, 1.2f), matMetal, matLamp);
            AddCeilingLamp(lampsRoot.transform, "Lamp_SouthWest", new Vector3(-2.2f, 2.9f, -1.2f), matMetal, matLamp);
            AddCeilingLamp(lampsRoot.transform, "Lamp_SouthEast", new Vector3(2.2f, 2.9f, -1.2f), matMetal, matLamp);

            // 7. STATIONS & MODULAR COUNTERS
            GameObject stationsRoot = new GameObject("Stations");
            stationsRoot.transform.SetParent(kitchenEnv.transform, false);

            // NORTH LINE (Z = 2.45m)
            CreateCounterModule(stationsRoot.transform, "Counter_North_01", new Vector3(-4.0f, 0.5f, 2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCounterModule(stationsRoot.transform, "Counter_North_02", new Vector3(-3.0f, 0.5f, 2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCounterModule(stationsRoot.transform, "Counter_North_03", new Vector3(-2.0f, 0.5f, 2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCuttingStation(stationsRoot.transform, "CuttingStation_01", new Vector3(-1.0f, 0.5f, 2.45f), matCounter, matCounterTop, matBoard, matMetal);
            CreateCounterModule(stationsRoot.transform, "Counter_North_04", new Vector3(0.0f, 0.5f, 2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCuttingStation(stationsRoot.transform, "CuttingStation_02", new Vector3(1.0f, 0.5f, 2.45f), matCounter, matCounterTop, matBoard, matMetal);
            CreateCounterModule(stationsRoot.transform, "Counter_North_05", new Vector3(2.0f, 0.5f, 2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCookingStationWithHood(stationsRoot.transform, "CookingStation_01", new Vector3(3.0f, 0.5f, 2.45f), matCounter, matCounterTop, matMetal, matMetalHood);
            CreateCounterModule(stationsRoot.transform, "Counter_North_06", new Vector3(4.0f, 0.5f, 2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);

            // EAST LINE (X = 4.45m)
            CreateCounterModule(stationsRoot.transform, "Counter_East_01", new Vector3(4.45f, 0.5f, 1.2f), new Vector3(0.8f, 0.9f, 1.0f), matCounter, matCounterTop);
            CreateWashingStation(stationsRoot.transform, "WashingStation", new Vector3(4.45f, 0.5f, 0.0f), matCounter, matCounterTop, matMetal, matWater);
            CreateCounterModule(stationsRoot.transform, "Counter_East_02", new Vector3(4.45f, 0.5f, -1.2f), new Vector3(0.8f, 0.9f, 1.0f), matCounter, matCounterTop);

            // WEST LINE (X = -4.45m) — REPLACES THE 5 BROWN CUBES WITH SPECTACULAR DELIVERY CONVEYOR + METALLIC EXTRACTOR HOOD
            CreateCounterModule(stationsRoot.transform, "Counter_West_01", new Vector3(-4.45f, 0.5f, 1.4f), new Vector3(0.8f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateDeliveryConveyorStation(stationsRoot.transform, "DeliveryStation", new Vector3(-4.45f, 0.5f, 0.0f), matCounter, matBelt, matBeltStripe, matMetal, matMetalHood, matLamp);
            CreateCounterModule(stationsRoot.transform, "Counter_West_02", new Vector3(-4.45f, 0.5f, -1.4f), new Vector3(0.8f, 0.9f, 0.8f), matCounter, matCounterTop);

            // SOUTH LINE (Z = -2.45m)
            CreateTrashBin(stationsRoot.transform, "TrashBin", new Vector3(-4.0f, 0.45f, -2.45f), matTrash);
            CreateCounterModule(stationsRoot.transform, "Counter_South_01", new Vector3(-3.0f, 0.5f, -2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCounterModule(stationsRoot.transform, "Counter_South_02", new Vector3(-1.8f, 0.5f, -2.45f), new Vector3(1.2f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateProducePantry(stationsRoot.transform, "Pantry", new Vector3(0.0f, 0.5f, -2.45f), matCounter, matCounterTop, matPantry, matProduceYellow, matProduceRed, matProduceGreen);
            CreateCounterModule(stationsRoot.transform, "Counter_South_03", new Vector3(1.8f, 0.5f, -2.45f), new Vector3(1.2f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCounterModule(stationsRoot.transform, "Counter_South_04", new Vector3(3.0f, 0.5f, -2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCounterModule(stationsRoot.transform, "Counter_South_05", new Vector3(4.0f, 0.5f, -2.45f), new Vector3(1.0f, 0.9f, 0.8f), matCounter, matCounterTop);

            // CENTRAL ISLAND (Z = 0.0m)
            CreateAssemblyStation(stationsRoot.transform, "AssemblyStation", new Vector3(-0.8f, 0.5f, 0.0f), matCounter, matCounterTop, matPlateClean, matPlateDirty);
            CreateCounterModule(stationsRoot.transform, "Counter_Island_01", new Vector3(0.3f, 0.5f, 0.0f), new Vector3(0.9f, 0.9f, 0.8f), matCounter, matCounterTop);
            CreateCounterModule(stationsRoot.transform, "Counter_Island_02", new Vector3(1.2f, 0.5f, 0.0f), new Vector3(0.9f, 0.9f, 0.8f), matCounter, matCounterTop);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[KitchenBuilder] Kitchen Scene built successfully with solid static obstacles, void guard, delivery conveyor belt, and industrial metallic hoods!");
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Pure static BoxCollider (no Rigidbody)
            var col = wall.GetComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = Vector3.one;
        }

        private static void CreateBoundaryBarrier(Transform parent, string name, Vector3 center, Vector3 size)
        {
            GameObject barrier = new GameObject(name);
            barrier.transform.SetParent(parent, false);
            barrier.transform.localPosition = center;

            var col = barrier.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = size;
            col.isTrigger = false;
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
            shade.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            shade.transform.localScale = new Vector3(0.35f, 0.06f, 0.35f);
            shade.GetComponent<MeshRenderer>().sharedMaterial = matLamp;
            UnityEngine.Object.DestroyImmediate(shade.GetComponent<Collider>());

            GameObject lightGo = new GameObject("PointLight");
            lightGo.transform.SetParent(lamp.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.90f, 0.75f);
            light.intensity = 2.4f;
            light.range = 6.5f;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateCounterModule(Transform parent, string name, Vector3 pos, Vector3 size, Material matCounter, Material matTop)
        {
            GameObject cnt = new GameObject(name);
            cnt.transform.SetParent(parent, false);
            cnt.transform.localPosition = pos;

            // Pure static BoxCollider on table (NO Rigidbody)
            GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa";
            mesa.transform.SetParent(cnt.transform, false);
            mesa.transform.localPosition = Vector3.zero;
            mesa.transform.localScale = size;
            mesa.GetComponent<MeshRenderer>().sharedMaterial = matCounter;

            // Countertop trim detail
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "TopTrim";
            trim.transform.SetParent(cnt.transform, false);
            trim.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.01f, 0f);
            trim.transform.localScale = new Vector3(size.x * 1.02f, 0.02f, size.z * 1.02f);
            trim.GetComponent<MeshRenderer>().sharedMaterial = matTop;
            UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());
        }

        private static void CreateCuttingStation(Transform parent, string name, Vector3 pos, Material matCounter, Material matTop, Material matBoard, Material matMetal)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = pos;

            // Root trigger for gameplay logic
            var trig = station.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.65f, 0f);
            trig.size = new Vector3(0.9f, 0.5f, 0.7f);

            // Table base with solid static BoxCollider
            GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa";
            mesa.transform.SetParent(station.transform, false);
            mesa.transform.localPosition = Vector3.zero;
            mesa.transform.localScale = new Vector3(1.0f, 0.9f, 0.8f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial = matCounter;

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "TopTrim";
            trim.transform.SetParent(station.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            trim.transform.localScale = new Vector3(1.02f, 0.02f, 0.82f);
            trim.GetComponent<MeshRenderer>().sharedMaterial = matTop;
            UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());

            GameObject tabla = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tabla.name = "Tabla";
            tabla.transform.SetParent(station.transform, false);
            tabla.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            tabla.transform.localScale = new Vector3(0.45f, 0.02f, 0.35f);
            tabla.GetComponent<MeshRenderer>().sharedMaterial = matBoard;
            UnityEngine.Object.DestroyImmediate(tabla.GetComponent<Collider>());

            GameObject cuchillo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cuchillo.name = "Cuchillo";
            cuchillo.transform.SetParent(station.transform, false);
            cuchillo.transform.localPosition = new Vector3(0.08f, 0.50f, 0f);
            cuchillo.transform.localScale = new Vector3(0.04f, 0.02f, 0.26f);
            cuchillo.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(cuchillo.GetComponent<Collider>());
        }

        private static void CreateCookingStationWithHood(Transform parent, string name, Vector3 pos, Material matCounter, Material matTop, Material matMetal, Material matMetalHood)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = pos;

            var trig = station.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.65f, 0f);
            trig.size = new Vector3(1.0f, 0.5f, 0.7f);

            GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa";
            mesa.transform.SetParent(station.transform, false);
            mesa.transform.localPosition = Vector3.zero;
            mesa.transform.localScale = new Vector3(1.0f, 0.9f, 0.8f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial = matCounter;

            GameObject topTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topTrim.name = "TopTrim";
            topTrim.transform.SetParent(station.transform, false);
            topTrim.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            topTrim.transform.localScale = new Vector3(1.02f, 0.02f, 0.82f);
            topTrim.GetComponent<MeshRenderer>().sharedMaterial = matTop;
            UnityEngine.Object.DestroyImmediate(topTrim.GetComponent<Collider>());

            // Hornallas
            GameObject h1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            h1.name = "Hornalla_01";
            h1.transform.SetParent(station.transform, false);
            h1.transform.localPosition = new Vector3(-0.25f, 0.48f, 0f);
            h1.transform.localScale = new Vector3(0.35f, 0.01f, 0.35f);
            h1.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(h1.GetComponent<Collider>());

            GameObject h2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            h2.name = "Hornalla_02";
            h2.transform.SetParent(station.transform, false);
            h2.transform.localPosition = new Vector3(0.25f, 0.48f, 0f);
            h2.transform.localScale = new Vector3(0.35f, 0.01f, 0.35f);
            h2.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(h2.GetComponent<Collider>());

            // 2 Identical Pots
            GameObject o1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            o1.name = "Olla_01";
            o1.transform.SetParent(station.transform, false);
            o1.transform.localPosition = new Vector3(-0.25f, 0.58f, 0f);
            o1.transform.localScale = new Vector3(0.32f, 0.10f, 0.32f);
            o1.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(o1.GetComponent<Collider>());

            GameObject o2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            o2.name = "Olla_02";
            o2.transform.SetParent(station.transform, false);
            o2.transform.localPosition = new Vector3(0.25f, 0.58f, 0f);
            o2.transform.localScale = new Vector3(0.32f, 0.10f, 0.32f);
            o2.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(o2.GetComponent<Collider>());

            // Extractor Hood above cooking stove
            AddMetalExtractorHood(station.transform, "Extractor_Cocina", new Vector3(0f, 1.70f, 0f), new Vector3(1.1f, 0.35f, 0.85f), matMetalHood, matMetal);
        }

        /// <summary>
        /// Creates the Delivery Station featuring an Overcooked conveyor belt and industrial metal extractor hood.
        /// Positioned on the West Wall where the 5 brown cubes were previously located.
        /// </summary>
        private static void CreateDeliveryConveyorStation(Transform parent, string name, Vector3 pos, Material matCounter, Material matBelt, Material matBeltStripe, Material matMetal, Material matMetalHood, Material matLamp)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = pos;

            // Root trigger for delivery logic
            var trig = station.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.65f, 0f);
            trig.size = new Vector3(0.85f, 0.5f, 1.8f);

            // Base Counter (Solid static BoxCollider)
            GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa_Base";
            mesa.transform.SetParent(station.transform, false);
            mesa.transform.localPosition = Vector3.zero;
            mesa.transform.localScale = new Vector3(0.8f, 0.9f, 1.8f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial = matCounter;

            // Stainless steel conveyor frame on top of counter
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Conveyor_Frame";
            frame.transform.SetParent(station.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            frame.transform.localScale = new Vector3(0.76f, 0.04f, 1.74f);
            frame.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(frame.GetComponent<Collider>());

            // Main rubber belt bed
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "Conveyor_Belt";
            belt.transform.SetParent(station.transform, false);
            belt.transform.localPosition = new Vector3(0f, 0.49f, 0f);
            belt.transform.localScale = new Vector3(0.56f, 0.02f, 1.60f);
            belt.GetComponent<MeshRenderer>().sharedMaterial = matBelt;
            UnityEngine.Object.DestroyImmediate(belt.GetComponent<Collider>());

            // High-visibility side accent stripes on belt
            GameObject stripeLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripeLeft.name = "Belt_Stripe_Left";
            stripeLeft.transform.SetParent(station.transform, false);
            stripeLeft.transform.localPosition = new Vector3(-0.27f, 0.495f, 0f);
            stripeLeft.transform.localScale = new Vector3(0.025f, 0.015f, 1.58f);
            stripeLeft.GetComponent<MeshRenderer>().sharedMaterial = matBeltStripe;
            UnityEngine.Object.DestroyImmediate(stripeLeft.GetComponent<Collider>());

            GameObject stripeRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripeRight.name = "Belt_Stripe_Right";
            stripeRight.transform.SetParent(station.transform, false);
            stripeRight.transform.localPosition = new Vector3(0.27f, 0.495f, 0f);
            stripeRight.transform.localScale = new Vector3(0.025f, 0.015f, 1.58f);
            stripeRight.GetComponent<MeshRenderer>().sharedMaterial = matBeltStripe;
            UnityEngine.Object.DestroyImmediate(stripeRight.GetComponent<Collider>());

            // Rollers at both ends (North & South)
            float[] rollerZ = new float[] { -0.82f, 0.82f };
            for (int r = 0; r < rollerZ.Length; r++)
            {
                GameObject roller = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                roller.name = $"Roller_{r + 1:00}";
                roller.transform.SetParent(station.transform, false);
                roller.transform.localPosition = new Vector3(0f, 0.49f, rollerZ[r]);
                roller.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                roller.transform.localScale = new Vector3(0.05f, 0.28f, 0.05f);
                roller.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
                UnityEngine.Object.DestroyImmediate(roller.GetComponent<Collider>());
            }

            // Chrome side guide rails
            GameObject railOuter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railOuter.name = "Guide_Rail_Outer";
            railOuter.transform.SetParent(station.transform, false);
            railOuter.transform.localPosition = new Vector3(0.35f, 0.54f, 0f);
            railOuter.transform.localScale = new Vector3(0.04f, 0.08f, 1.76f);
            railOuter.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(railOuter.GetComponent<Collider>());

            // Service Bell on side
            GameObject bellBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bellBase.name = "Campana_Servicio";
            bellBase.transform.SetParent(station.transform, false);
            bellBase.transform.localPosition = new Vector3(0.32f, 0.60f, 0.70f);
            bellBase.transform.localScale = new Vector3(0.10f, 0.04f, 0.10f);
            bellBase.GetComponent<MeshRenderer>().sharedMaterial = matLamp;
            UnityEngine.Object.DestroyImmediate(bellBase.GetComponent<Collider>());

            // Serving Pass-Through Hatch into the wall
            GameObject hatch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hatch.name = "Ventana_Entrega";
            hatch.transform.SetParent(station.transform, false);
            hatch.transform.localPosition = new Vector3(-0.48f, 0.65f, 0f);
            hatch.transform.localScale = new Vector3(0.16f, 0.55f, 1.40f);
            hatch.GetComponent<MeshRenderer>().sharedMaterial = matLamp; // Warmly backlit opening
            UnityEngine.Object.DestroyImmediate(hatch.GetComponent<Collider>());

            // Industrial Metallic Extractor Hood directly above the conveyor belt!
            AddMetalExtractorHood(station.transform, "Extractor_Entrega", new Vector3(0f, 1.70f, 0f), new Vector3(0.95f, 0.40f, 1.90f), matMetalHood, matMetal);
        }

        /// <summary>
        /// Builds a polished metallic extractor hood with tapered canopy, grease filters, and vertical ceiling duct pipe.
        /// </summary>
        private static void AddMetalExtractorHood(Transform parent, string name, Vector3 localPos, Vector3 size, Material matHood, Material matDuct)
        {
            GameObject hoodRoot = new GameObject(name);
            hoodRoot.transform.SetParent(parent, false);
            hoodRoot.transform.localPosition = localPos;

            // Lower flared rim
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "Campana_Borde";
            rim.transform.SetParent(hoodRoot.transform, false);
            rim.transform.localPosition = new Vector3(0f, -size.y * 0.45f, 0f);
            rim.transform.localScale = new Vector3(size.x * 1.05f, size.y * 0.12f, size.z * 1.05f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = matHood;
            UnityEngine.Object.DestroyImmediate(rim.GetComponent<Collider>());

            // Main tapered canopy body
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Campana_Cuerpo";
            canopy.transform.SetParent(hoodRoot.transform, false);
            canopy.transform.localPosition = Vector3.zero;
            canopy.transform.localScale = size;
            canopy.GetComponent<MeshRenderer>().sharedMaterial = matHood;
            UnityEngine.Object.DestroyImmediate(canopy.GetComponent<Collider>());

            // Slanted grease filter slats underneath
            GameObject filter1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            filter1.name = "Filtro_01";
            filter1.transform.SetParent(hoodRoot.transform, false);
            filter1.transform.localPosition = new Vector3(0f, -size.y * 0.48f, -size.z * 0.22f);
            filter1.transform.localScale = new Vector3(size.x * 0.85f, 0.02f, size.z * 0.35f);
            filter1.GetComponent<MeshRenderer>().sharedMaterial = matDuct;
            UnityEngine.Object.DestroyImmediate(filter1.GetComponent<Collider>());

            GameObject filter2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            filter2.name = "Filtro_02";
            filter2.transform.SetParent(hoodRoot.transform, false);
            filter2.transform.localPosition = new Vector3(0f, -size.y * 0.48f, size.z * 0.22f);
            filter2.transform.localScale = new Vector3(size.x * 0.85f, 0.02f, size.z * 0.35f);
            filter2.GetComponent<MeshRenderer>().sharedMaterial = matDuct;
            UnityEngine.Object.DestroyImmediate(filter2.GetComponent<Collider>());

            // Vertical exhaust chimney duct going into the ceiling
            GameObject duct = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            duct.name = "Tubo_Ventilacion";
            duct.transform.SetParent(hoodRoot.transform, false);
            duct.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.35f, 0f);
            duct.transform.localScale = new Vector3(0.32f, 0.35f, 0.32f);
            duct.GetComponent<MeshRenderer>().sharedMaterial = matDuct;
            UnityEngine.Object.DestroyImmediate(duct.GetComponent<Collider>());

            // Hood spot downlight
            GameObject hoodLightGo = new GameObject("Luz_Extractor");
            hoodLightGo.transform.SetParent(hoodRoot.transform, false);
            hoodLightGo.transform.localPosition = new Vector3(0f, -size.y * 0.52f, 0f);
            var hLight = hoodLightGo.AddComponent<Light>();
            hLight.type = LightType.Spot;
            hLight.color = new Color(1.0f, 0.95f, 0.85f);
            hLight.intensity = 2.0f;
            hLight.range = 2.5f;
            hLight.spotAngle = 65f;
            hLight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static void CreateWashingStation(Transform parent, string name, Vector3 pos, Material matCounter, Material matTop, Material matMetal, Material matWater)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = pos;

            var trig = station.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.65f, 0f);
            trig.size = new Vector3(0.7f, 0.5f, 1.0f);

            GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa";
            mesa.transform.SetParent(station.transform, false);
            mesa.transform.localPosition = Vector3.zero;
            mesa.transform.localScale = new Vector3(0.8f, 0.9f, 1.2f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial = matCounter;

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "TopTrim";
            trim.transform.SetParent(station.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            trim.transform.localScale = new Vector3(0.82f, 0.02f, 1.22f);
            trim.GetComponent<MeshRenderer>().sharedMaterial = matTop;
            UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());

            GameObject fregadero = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fregadero.name = "Fregadero";
            fregadero.transform.SetParent(station.transform, false);
            fregadero.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            fregadero.transform.localScale = new Vector3(0.55f, 0.04f, 0.8f);
            fregadero.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(fregadero.GetComponent<Collider>());

            GameObject agua = GameObject.CreatePrimitive(PrimitiveType.Cube);
            agua.name = "Agua";
            agua.transform.SetParent(station.transform, false);
            agua.transform.localPosition = new Vector3(0f, 0.49f, 0f);
            agua.transform.localScale = new Vector3(0.45f, 0.02f, 0.7f);
            agua.GetComponent<MeshRenderer>().sharedMaterial = matWater;
            UnityEngine.Object.DestroyImmediate(agua.GetComponent<Collider>());

            GameObject grifoBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grifoBase.name = "Grifo_Base";
            grifoBase.transform.SetParent(station.transform, false);
            grifoBase.transform.localPosition = new Vector3(0.2f, 0.60f, 0f);
            grifoBase.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            grifoBase.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(grifoBase.GetComponent<Collider>());

            GameObject grifoPico = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grifoPico.name = "Grifo_Pico";
            grifoPico.transform.SetParent(station.transform, false);
            grifoPico.transform.localPosition = new Vector3(0.12f, 0.70f, 0f);
            grifoPico.transform.localScale = new Vector3(0.14f, 0.03f, 0.03f);
            grifoPico.GetComponent<MeshRenderer>().sharedMaterial = matMetal;
            UnityEngine.Object.DestroyImmediate(grifoPico.GetComponent<Collider>());
        }

        private static void CreateTrashBin(Transform parent, string name, Vector3 pos, Material matTrash)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = pos;

            var trig = station.AddComponent<CapsuleCollider>();
            trig.isTrigger = true;
            trig.center = Vector3.zero;
            trig.radius = 0.35f;
            trig.height = 1.0f;

            // Physical solid cylinder (static collider)
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Cylinder";
            cylinder.transform.SetParent(station.transform, false);
            cylinder.transform.localPosition = Vector3.zero;
            cylinder.transform.localScale = new Vector3(0.5f, 0.45f, 0.5f);
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = matTrash;
        }

        /// <summary>
        /// Beautiful Overcooked-style Bolivian Produce Pantry with tiered wooden produce bins.
        /// Positioned on South Wall, unmistakably an ingredient dispensary.
        /// </summary>
        private static void CreateProducePantry(Transform parent, string name, Vector3 pos, Material matCounter, Material matTop, Material matWood, Material matPapas, Material matCarne, Material matVerduras)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = pos;

            var trig = station.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.65f, 0f);
            trig.size = new Vector3(1.4f, 0.5f, 0.7f);

            GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa";
            mesa.transform.SetParent(station.transform, false);
            mesa.transform.localPosition = Vector3.zero;
            mesa.transform.localScale = new Vector3(1.4f, 0.9f, 0.8f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial = matCounter;

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "TopTrim";
            trim.transform.SetParent(station.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            trim.transform.localScale = new Vector3(1.42f, 0.02f, 0.82f);
            trim.GetComponent<MeshRenderer>().sharedMaterial = matTop;
            UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());

            // 3 Market Crates with colorful ingredients (Papas, Carne, Verduras)
            float[] xCrates = new float[] { -0.42f, 0.0f, 0.42f };
            Material[] crateMats = new Material[] { matPapas, matCarne, matVerduras };
            string[] crateNames = new string[] { "Cajon_Papas", "Cajon_Carne", "Cajon_Verduras" };

            for (int i = 0; i < xCrates.Length; i++)
            {
                GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crate.name = crateNames[i];
                crate.transform.SetParent(station.transform, false);
                crate.transform.localPosition = new Vector3(xCrates[i], 0.55f, 0f);
                crate.transform.localScale = new Vector3(0.36f, 0.16f, 0.55f);
                crate.GetComponent<MeshRenderer>().sharedMaterial = matWood;
                UnityEngine.Object.DestroyImmediate(crate.GetComponent<Collider>());

                // Inside produce mockup
                GameObject produce = GameObject.CreatePrimitive(PrimitiveType.Cube);
                produce.name = "Produce";
                produce.transform.SetParent(crate.transform, false);
                produce.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                produce.transform.localScale = new Vector3(0.80f, 0.50f, 0.80f);
                produce.GetComponent<MeshRenderer>().sharedMaterial = crateMats[i];
                UnityEngine.Object.DestroyImmediate(produce.GetComponent<Collider>());
            }
        }

        private static void CreateAssemblyStation(Transform parent, string name, Vector3 pos, Material matCounter, Material matTop, Material matClean, Material matDirty)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = pos;

            var trig = station.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.65f, 0f);
            trig.size = new Vector3(1.2f, 0.5f, 0.7f);

            GameObject mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa";
            mesa.transform.SetParent(station.transform, false);
            mesa.transform.localPosition = Vector3.zero;
            mesa.transform.localScale = new Vector3(1.2f, 0.9f, 0.8f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial = matCounter;

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "TopTrim";
            trim.transform.SetParent(station.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            trim.transform.localScale = new Vector3(1.22f, 0.02f, 0.82f);
            trim.GetComponent<MeshRenderer>().sharedMaterial = matTop;
            UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());

            GameObject cleanPlates = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cleanPlates.name = "CleanPlates";
            cleanPlates.transform.SetParent(station.transform, false);
            cleanPlates.transform.localPosition = new Vector3(-0.3f, 0.48f, 0f);
            cleanPlates.transform.localScale = new Vector3(0.4f, 0.02f, 0.45f);
            cleanPlates.GetComponent<MeshRenderer>().sharedMaterial = matClean;
            UnityEngine.Object.DestroyImmediate(cleanPlates.GetComponent<Collider>());

            GameObject dirtyPlates = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirtyPlates.name = "DirtyPlates";
            dirtyPlates.transform.SetParent(station.transform, false);
            dirtyPlates.transform.localPosition = new Vector3(0.3f, 0.48f, 0f);
            dirtyPlates.transform.localScale = new Vector3(0.4f, 0.02f, 0.45f);
            dirtyPlates.GetComponent<MeshRenderer>().sharedMaterial = matDirty;
            UnityEngine.Object.DestroyImmediate(dirtyPlates.GetComponent<Collider>());
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
    }
}
