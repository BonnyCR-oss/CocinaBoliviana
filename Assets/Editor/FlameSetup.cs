using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Construye el efecto de fuego de las hornallas y lo coloca en CookingStation_01.
    ///
    /// Todo se genera por código (textura incluida) porque el proyecto no trae ningún asset
    /// de fuego ni de partículas.
    /// </summary>
    public static class FlameSetup
    {
        private const string ScenePath = "Assets/Scenes/First Scene.unity";
        private const string TexturesFolder = "Assets/Materials/Textures";
        private const string FlameTexturePath = TexturesFolder + "/Flame_Particle.png";
        private const string FlameMaterialPath = "Assets/Materials/Mat_Flame.mat";
        private const string FlamePrefabPath = "Assets/02_Prefabs/Fuego_Hornalla.prefab";

        private const string StationName = "CookingStation_01";

        /// <summary>
        /// Hornallas donde va el fuego, con su posición local dentro de la estación.
        /// Y = 0.49 es justo la cara superior de la hornalla (centro 0.48, alto 0.02).
        /// </summary>
        private static readonly (string burner, Vector3 localPos)[] Hornallas =
        {
            ("Hornalla_01", new Vector3(-0.25f, 0.49f, 0f)),
            ("Hornalla_02", new Vector3(0.25f, 0.49f, 0f)),
        };

        /// <summary>
        /// Radio del anillo emisor. La hornalla mide 0.175 de radio y la olla 0.16, así que
        /// emitir en 0.175 hace que las llamas suban por FUERA de la olla y se vean. Emitir
        /// más adentro las dejaría completamente tapadas: la olla se apoya directamente sobre
        /// la hornalla y no hay hueco debajo.
        /// </summary>
        private const float RadioEmision = 0.175f;

        [MenuItem("Kitchen/Setup Burner Flames (fuego en hornallas)")]
        public static void SetupFlames()
        {
            Debug.Log("[FlameSetup] Generando fuego de hornallas...");

            Texture2D textura = EnsureFlameTexture();
            Material material = EnsureFlameMaterial(textura);
            GameObject prefab = BuildFlamePrefab(material);
            if (prefab == null) return;

            PlaceInScene(prefab);
        }

        /// <summary>
        /// Degradado radial suave, blanco con alfa decreciente. El color lo pone el
        /// Color over Lifetime del sistema de partículas, no la textura.
        /// </summary>
        private static Texture2D EnsureFlameTexture()
        {
            Texture2D existente = AssetDatabase.LoadAssetAtPath<Texture2D>(FlameTexturePath);
            if (existente != null) return existente;

            if (!AssetDatabase.IsValidFolder(TexturesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Textures");
            }

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float centro = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro)) / centro;
                    // Cuadrático: borde bien difuso, sin el anillo duro que deja una rampa lineal.
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            File.WriteAllBytes(FlameTexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(FlameTexturePath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(FlameTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            Debug.Log($"[FlameSetup] Textura de llama creada en {FlameTexturePath}");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(FlameTexturePath);
        }

        private static Material EnsureFlameMaterial(Texture2D textura)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                             ?? Shader.Find("Particles/Standard Unlit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, FlameMaterialPath);
            }

            // Aditivo: las llamas se suman a lo que tienen detrás en vez de taparlo, que es
            // lo que hace que se vean como luz y no como calcomanías.
            mat.SetTexture("_BaseMap", textura);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 2f);   // Additive
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.renderQueue = (int)RenderQueue.Transparent;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static GameObject BuildFlamePrefab(Material material)
        {
            var root = new GameObject("Fuego_Hornalla");
            var ps = root.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.05f; // empuje hacia arriba, como el aire caliente
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 120;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 55f;

            // Anillo en el borde de la hornalla, emitiendo hacia arriba.
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = RadioEmision;
            shape.radiusThickness = 0f; // solo el borde, no el disco entero
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.00f, 0.95f, 0.55f), 0.00f), // amarillo en la base
                    new GradientColorKey(new Color(1.00f, 0.62f, 0.15f), 0.35f), // naranja
                    new GradientColorKey(new Color(0.95f, 0.25f, 0.05f), 0.75f), // rojo
                    new GradientColorKey(new Color(0.35f, 0.08f, 0.02f), 1.00f), // se apaga
                },
                new[]
                {
                    new GradientAlphaKey(0.0f, 0.00f),
                    new GradientAlphaKey(1.0f, 0.15f),
                    new GradientAlphaKey(0.7f, 0.60f),
                    new GradientAlphaKey(0.0f, 1.00f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Crece al salir y se afina al subir: la silueta típica de una llama.
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0.00f, 0.35f),
                new Keyframe(0.30f, 1.00f),
                new Keyframe(1.00f, 0.15f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortingFudge = -5f; // dibujar por delante de la olla
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Luz de apoyo. Sin ella el fuego no ilumina nada y se nota que es una calcomanía.
            var lightGo = new GameObject("FlameLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.25f);
            light.range = 1.1f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;

            var flame = root.AddComponent<BurnerFlame>();
            var so = new SerializedObject(flame);
            so.FindProperty("flameLight").objectReferenceValue = light;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FlamePrefabPath);
            Object.DestroyImmediate(root);

            if (prefab == null)
            {
                Debug.LogError($"[FlameSetup] No se pudo guardar {FlamePrefabPath}");
                return null;
            }

            Debug.Log($"[FlameSetup] Prefab de fuego creado en {FlamePrefabPath}");
            return prefab;
        }

        private static void PlaceInScene(GameObject prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[FlameSetup] No se pudo abrir {ScenePath}");
                return;
            }

            GameObject station = GameObject.Find(StationName);
            if (station == null)
            {
                Debug.LogWarning($"[FlameSetup] No se encontró '{StationName}' en la escena.");
                return;
            }

            foreach (var (burner, localPos) in Hornallas)
            {
                string flameName = "Fuego_" + burner;

                // Si ya está, se respeta dónde lo dejaste: recolocarlo en cada corrida borraba
                // los ajustes hechos a mano en la escena.
                if (station.transform.Find(flameName) != null)
                {
                    Debug.Log($"[FlameSetup] '{flameName}' ya existe; se deja como está.");
                    continue;
                }

                // Va colgado de la ESTACIÓN, no de la hornalla: la hornalla tiene escala
                // (0.35, 0.01, 0.35) y aplastaría las partículas a 1/100 de su altura.
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, station.transform);
                instancia.name = flameName;
                instancia.transform.localPosition = localPos;
                instancia.transform.localRotation = Quaternion.identity;
                instancia.transform.localScale = Vector3.one;

                Debug.Log($"[FlameSetup] Fuego colocado en '{burner}'.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
