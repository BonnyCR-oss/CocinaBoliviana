using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Deja la parrilla lista para asar sonsos:
    ///  - el ingrediente "Sonso crudo" (SonsoCrudo.asset + SonsoPrefab) en el cajón de las papas,
    ///  - la receta del plato Sonso (sonso crudo asado) y su prefab servido,
    ///  - en la escena, la parrilla para un sonso a la vez, con humo, chispas, brasas y
    ///    chisporroteo.
    ///
    /// No pisa lo colocado a mano: todo lo que ya exista y no esté en su valor por defecto
    /// se respeta.
    /// </summary>
    public static class GrillSetup
    {
        private const string ScenePath = "Assets/00_Scenes/First Scene.unity";
        private const string GrillName = "ParrillaModel";

        private const string SonsoModelPath = "Assets/04_Models/PlatosModels/sonsomodel.glb";
        private const string SonsoCrudoDataPath = "Assets/03_SO/Ingredientes/SonsoCrudo.asset";
        private const string SonsoCrudoPrefabPath = "Assets/02_Prefabs/IngredientePrefabs/SonsoPrefab.prefab";
        private const string SonsoDishPath = "Assets/03_SO/Platos/Sonso.asset";
        private const string ServidoPrefabPath = "Assets/02_Prefabs/PlatosPrefabs/Sonso_Servido.prefab";
        private const string CounterPrefabPath = "Assets/02_Prefabs/UI/CookingCounter.prefab";
        private const string SmokeMaterialPath = "Assets/Materials/Mat_Humo.mat";

        /// <summary>Largo del sonso servido, en metros. El .glb viene a casi un metro.</summary>
        private const float LargoSonso = 0.20f;

        /// <summary>Un sonso a la vez: es el ritmo del nivel de Santa Cruz.</summary>
        private const int CapacidadParrilla = 1;

        [MenuItem("Kitchen/Setup Grill (parrilla y sonsos)")]
        public static void SetupGrill()
        {
            Debug.Log("[GrillSetup] Configurando parrilla...");

            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(SonsoModelPath);
            if (modelo == null)
            {
                Debug.LogError($"[GrillSetup] No existe {SonsoModelPath}.");
                return;
            }

            IngredientData sonso = EnsureSonsoCrudoData();
            GameObject sonsoPrefab = EnsureSonsoCrudoPrefab(sonso);
            if (sonsoPrefab == null) return;
            if (sonso.prefab == null)
            {
                sonso.prefab = sonsoPrefab;
                EditorUtility.SetDirty(sonso);
            }

            EnsureSonsoDish(modelo, sonso);
            AssetDatabase.SaveAssets();

            ConfigurarEscena(sonso);
        }

        // ---------------------------------------------------------------- datos

        /// <summary>
        /// Completa TU SonsoCrudo.asset. Solo se rellenan los campos vacíos o con el valor por
        /// defecto: si ya le pusiste nombre o tiempos, se respetan.
        /// </summary>
        private static IngredientData EnsureSonsoCrudoData()
        {
            var data = AssetDatabase.LoadAssetAtPath<IngredientData>(SonsoCrudoDataPath);
            if (data == null)
            {
                // Sonso cruceño: masa de yuca con queso, en un palito, a la parrilla.
                data = ScriptableObject.CreateInstance<IngredientData>();
                AssetDatabase.CreateAsset(data, SonsoCrudoDataPath);
                Debug.Log($"[GrillSetup] Ingrediente creado en {SonsoCrudoDataPath}.");
            }

            if (string.IsNullOrEmpty(data.nombre)) data.nombre = "Sonso crudo";
            data.tipo = IngredientType.Otro;
            data.sePuedeCocinar = true;
            if (!data.metodosCoccion.Contains(MetodoCoccion.Asar)) data.metodosCoccion.Add(MetodoCoccion.Asar);

            // 8 y 6 son los valores por defecto de IngredientData: nadie los ajustó.
            if (Mathf.Approximately(data.tiempoCoccion, 8f)) data.tiempoCoccion = 12f;
            if (Mathf.Approximately(data.margenAntesDeQuemarse, 6f)) data.margenAntesDeQuemarse = 8f;

            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>
        /// Tu SonsoPrefab ya trae Rigidbody, agarre e IngredientItem, pero el IngredientItem
        /// decía "Tomate". Esto lo deja como al resto de ingredientes: nombre y dato bien
        /// puestos, collider ajustado a la malla y física que no rueda ni rebota.
        /// </summary>
        private static GameObject EnsureSonsoCrudoPrefab(IngredientData sonso)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SonsoCrudoPrefabPath) == null)
            {
                Debug.LogError($"[GrillSetup] No existe {SonsoCrudoPrefabPath}.");
                return null;
            }
            return IngredientPrefabSetup.EnsureIngredientPrefab(SonsoCrudoPrefabPath, sonso, isCut: false);
        }

        private static void EnsureSonsoDish(GameObject modelo, IngredientData sonso)
        {
            var plato = AssetDatabase.LoadAssetAtPath<DishData>(SonsoDishPath);
            if (plato == null)
            {
                Debug.LogWarning($"[GrillSetup] No existe {SonsoDishPath}; no se toca la receta.");
                return;
            }

            if (plato.receta.Count == 0)
            {
                plato.receta.Add(new IngredienteRequerido
                {
                    ingrediente = sonso,
                    corte = TipoCorte.Ninguno,
                    debeEstarCocido = true,
                    metodo = MetodoCoccion.Asar,
                });
                Debug.Log("[GrillSetup] Receta del Sonso: sonso crudo asado.");
            }

            if (plato.platoPrefab == null)
            {
                // Prefab propio a escala: PlatingSetup lo completa EN SU SITIO (collider,
                // agarre, ServedDish) en vez de duplicar el .glb gigante.
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ServidoPrefabPath) == null)
                {
                    GuardarPrefabEscalado(modelo, "Sonso", ServidoPrefabPath);
                }
                plato.platoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ServidoPrefabPath);
            }

            // El sonso asado se lleva tal cual al mostrador, sin emplatar.
            plato.entregaDirecta = true;

            if (plato.tiempoLimite <= 0f) plato.tiempoLimite = 60f;
            if (plato.puntos <= 0) plato.puntos = 30;

            EditorUtility.SetDirty(plato);
        }

        /// <summary>
        /// Raíz vacía con el modelo dentro, tumbado y a escala. La raíz queda a escala 1 y
        /// centrada en la malla: IngredientItem toma la escala de la raíz como su tamaño
        /// real, y la parrilla coloca cada sonso por su centro.
        /// </summary>
        private static void GuardarPrefabEscalado(GameObject modelo, string nombre, string ruta)
        {
            var raiz = new GameObject(nombre);
            var hijo = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            hijo.transform.SetParent(raiz.transform, false);

            // Tumbado: el palito queda a lo largo de Z. Sobre la rejilla así se ve asándose,
            // y no clavado de pie.
            hijo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            if (TryGetBounds(hijo, out Bounds b) && b.size.magnitude > 0f)
            {
                float largo = Mathf.Max(b.size.x, b.size.y, b.size.z);
                hijo.transform.localScale = Vector3.one * (LargoSonso / largo);

                TryGetBounds(hijo, out b);
                hijo.transform.localPosition = -b.center;
            }

            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            Object.DestroyImmediate(raiz);
        }

        // --------------------------------------------------------------- escena

        private static void ConfigurarEscena(IngredientData sonso)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[GrillSetup] No se pudo abrir {ScenePath}");
                return;
            }

            GameObject parrilla = GameObject.Find(GrillName);
            if (parrilla == null)
            {
                Debug.LogWarning($"[GrillSetup] No se encontró '{GrillName}' en la escena.");
                return;
            }

            if (!TryGetBounds(parrilla, out Bounds b))
            {
                Debug.LogWarning($"[GrillSetup] '{GrillName}' no tiene ningún Renderer.");
                return;
            }

            AsegurarCollider(parrilla);

            var vessel = parrilla.GetComponent<CookingVessel>();
            if (vessel == null) vessel = parrilla.AddComponent<CookingVessel>();

            Transform punto = AsegurarPuntoContenido(parrilla, b);
            CookingCounter contador = AsegurarContador(parrilla, b);
            GrillEffects efectos = AsegurarEfectos(parrilla, b);

            var so = new SerializedObject(vessel);
            so.FindProperty("metodo").enumValueIndex = (int)MetodoCoccion.Asar;
            so.FindProperty("temporizadorPorIngrediente").boolValue = true;
            so.FindProperty("capacidad").intValue = CapacidadParrilla;
            // Tu sonso viene de pie en su palito: se tumba a lo largo de la parrilla (su X
            // local) y se apoya por la malla, porque el pivote está en la punta del palito.
            so.FindProperty("rotacionAlColocar").vector3Value = new Vector3(0f, 0f, 90f);
            so.FindProperty("apoyarSobrePunto").boolValue = true;
            so.FindProperty("puntoContenido").objectReferenceValue = punto;
            so.FindProperty("efectos").objectReferenceValue = efectos;
            if (contador != null) so.FindProperty("contador").objectReferenceValue = contador;

            // La zona solo se recalcula si sigue con los valores por defecto del componente.
            var zona = so.FindProperty("zonaDeteccion");
            if (Vector3.Distance(zona.vector3Value, new Vector3(0.22f, 0.22f, 0.22f)) < 0.001f)
            {
                // En los ejes del objeto, que es como la usa CookingVessel.
                Vector3 enLocal = Quaternion.Inverse(parrilla.transform.rotation) * b.size;
                zona.vector3Value = new Vector3(Mathf.Abs(enLocal.x), 0.25f, Mathf.Abs(enLocal.z));

                Vector3 pos = parrilla.transform.position;
                so.FindProperty("zonaAltura").floatValue = (b.max.y - pos.y) + 0.08f;
                so.FindProperty("zonaDesplazamiento").vector3Value = new Vector3(b.center.x - pos.x, 0f, b.center.z - pos.z);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AnadirAlCajon(sonso);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GrillSetup] Parrilla lista: un sonso a la vez, con humo y brasas.");
        }

        /// <summary>Sin collider el sonso atraviesa la rejilla y cae al mesón.</summary>
        private static void AsegurarCollider(GameObject parrilla)
        {
            if (parrilla.GetComponentInChildren<Collider>() != null) return;

            var filtro = parrilla.GetComponentInChildren<MeshFilter>();
            GameObject destino = (filtro != null) ? filtro.gameObject : parrilla;

            // Al añadirse sobre un MeshFilter, BoxCollider se ajusta solo a la malla.
            destino.AddComponent<BoxCollider>();
            Debug.Log($"[GrillSetup] BoxCollider añadido a '{destino.name}'.");
        }

        private static Transform AsegurarPuntoContenido(GameObject parrilla, Bounds b)
        {
            Transform punto = parrilla.transform.Find("PuntoContenido");
            bool nuevo = punto == null;
            if (nuevo)
            {
                punto = new GameObject("PuntoContenido").transform;
                punto.SetParent(parrilla.transform, false);
            }

            // (0, 0.05, 0) es lo que deja CookingSetup: eso no es colocación a mano, y en la
            // parrilla quedaría dentro del carbón.
            bool porDefecto = Vector3.Distance(punto.localPosition, new Vector3(0f, 0.05f, 0f)) < 0.001f;
            if (nuevo || porDefecto)
            {
                punto.position = new Vector3(b.center.x, b.max.y + 0.005f, b.center.z);
                punto.localRotation = Quaternion.identity;
            }
            return punto;
        }

        private static CookingCounter AsegurarContador(GameObject parrilla, Bounds b)
        {
            Transform cartel = parrilla.transform.Find("CookingCounter");
            if (cartel == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CounterPrefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning("[GrillSetup] No hay cartel de cocción; corre antes 'Setup Cooking'.");
                    return null;
                }
                cartel = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, parrilla.transform)).transform;
                cartel.name = "CookingCounter";
                cartel.localPosition = new Vector3(0f, 0.30f, 0f);
            }

            // Si está por debajo de la rejilla queda escondido dentro del modelo (el
            // (0, 0.30, 0) de CookingSetup con la parrilla a escala 0.7, o un (0, 0, 0)).
            // Se sube por encima del humo. Si ya lo pusiste arriba, se respeta.
            if (cartel.position.y < b.max.y + 0.05f)
            {
                cartel.position = new Vector3(b.center.x, b.max.y + 0.35f, b.center.z);
            }
            return cartel.GetComponent<CookingCounter>();
        }

        // ------------------------------------------------------------- efectos

        private static GrillEffects AsegurarEfectos(GameObject parrilla, Bounds b)
        {
            Transform existente = parrilla.transform.Find("EfectosParrilla");
            if (existente != null && existente.GetComponent<GrillEffects>() != null)
            {
                Debug.Log("[GrillSetup] 'EfectosParrilla' ya existe; se deja como está.");
                return existente.GetComponent<GrillEffects>();
            }

            Texture2D textura = FlameSetup.EnsureFlameTexture();
            Material chispasMat = FlameSetup.EnsureFlameMaterial(textura);
            Material humoMat = EnsureSmokeMaterial(textura);

            var root = new GameObject("EfectosParrilla");
            root.transform.SetParent(parrilla.transform, false);
            root.transform.position = new Vector3(b.center.x, b.max.y - 0.02f, b.center.z);
            root.transform.rotation = parrilla.transform.rotation;

            // Escala de mundo 1: la parrilla está a 0.7 y encogería todo el efecto.
            Vector3 lossy = parrilla.transform.lossyScale;
            root.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);

            Vector3 area = Quaternion.Inverse(parrilla.transform.rotation) * b.size;
            area = new Vector3(Mathf.Abs(area.x) * 0.8f, 0.01f, Mathf.Abs(area.z) * 0.8f);

            ParticleSystem humo = CrearHumo(root.transform, humoMat, area);
            ParticleSystem chispas = CrearChispas(root.transform, chispasMat, area);

            var luzGo = new GameObject("LuzBrasas");
            luzGo.transform.SetParent(root.transform, false);
            luzGo.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            var luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.38f, 0.12f);
            luz.range = 0.9f;
            luz.intensity = 0.6f;
            luz.shadows = LightShadows.None;

            var efectos = root.AddComponent<GrillEffects>();
            var so = new SerializedObject(efectos);
            so.FindProperty("humo").objectReferenceValue = humo;
            so.FindProperty("chispas").objectReferenceValue = chispas;
            so.FindProperty("luzBrasas").objectReferenceValue = luz;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[GrillSetup] Efectos de parrilla creados (humo, chispas, brasas, sonido).");
            return efectos;
        }

        private static ParticleSystem CrearHumo(Transform padre, Material mat, Vector3 area)
        {
            var go = new GameObject("Humo");
            go.transform.SetParent(padre, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.92f, 0.92f, 0.90f, 0.35f);
            main.gravityModifier = -0.02f;
            // Mundo: si alguien empuja la parrilla, el humo ya soltado no la sigue.
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;

            var emision = ps.emission;
            emision.rateOverTime = 10f;

            AreaHaciaArriba(ps, area);

            // Sube recto y se deshilacha arriba.
            var ruido = ps.noise;
            ruido.enabled = true;
            ruido.strength = 0.12f;
            ruido.frequency = 0.6f;
            ruido.scrollSpeed = 0.3f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.6f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = new ParticleSystem.MinMaxGradient(g);

            var tam = ps.sizeOverLifetime;
            tam.enabled = true;
            tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(1f, 2.2f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            return ps;
        }

        private static ParticleSystem CrearChispas(Transform padre, Material mat, Vector3 area)
        {
            var go = new GameObject("Chispas");
            go.transform.SetParent(padre, false);
            go.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
            main.gravityModifier = -0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emision = ps.emission;
            emision.rateOverTime = 3f;

            AreaHaciaArriba(ps, area);

            var ruido = ps.noise;
            ruido.enabled = true;
            ruido.strength = 0.25f;
            ruido.frequency = 1.5f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                    new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(g);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            return ps;
        }

        /// <summary>
        /// Caja plana del tamaño de la rejilla que emite hacia arriba. La forma Box dispara
        /// por su eje Z, así que se tumba -90° en X; con eso su Y queda en horizontal y es
        /// ahí donde va el fondo de la parrilla.
        /// </summary>
        private static void AreaHaciaArriba(ParticleSystem ps, Vector3 area)
        {
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            shape.scale = new Vector3(area.x, area.z, 0.01f);
        }

        /// <summary>
        /// Como el de las llamas pero con mezcla alfa: el humo tapa lo de detrás en vez de
        /// sumarle luz. Si fuera aditivo, el humo negro de un sonso quemado sería invisible.
        /// </summary>
        private static Material EnsureSmokeMaterial(Texture2D textura)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                             ?? Shader.Find("Particles/Standard Unlit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, SmokeMaterialPath);
            }

            mat.SetTexture("_BaseMap", textura);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.renderQueue = (int)RenderQueue.Transparent;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // ------------------------------------------------------------- cajón

        /// <summary>
        /// El sonso crudo sale del cajón de las papas (es de yuca). Solo se AÑADE a lo que ya
        /// tenga, para no pisar la lista si la tocaste a mano.
        /// </summary>
        private static void AnadirAlCajon(IngredientData sonso)
        {
            GameObject cajon = GameObject.Find("Cajon_Papas");
            var dispenser = (cajon != null) ? cajon.GetComponent<ItemDispenser>() : null;
            if (dispenser == null)
            {
                Debug.LogWarning("[GrillSetup] No hay ItemDispenser en 'Cajon_Papas'; añade el sonso crudo a mano a algún cajón.");
                return;
            }

            var so = new SerializedObject(dispenser);
            var lista = so.FindProperty("opcionesIngredientes");
            for (int i = 0; i < lista.arraySize; i++)
            {
                if (lista.GetArrayElementAtIndex(i).objectReferenceValue == sonso) return;
            }

            lista.InsertArrayElementAtIndex(lista.arraySize);
            lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = sonso;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[GrillSetup] 'Sonso crudo' añadido al cajón de las papas.");
        }

        // ------------------------------------------------------------ utilidades

        private static bool TryGetBounds(GameObject go, out Bounds b)
        {
            b = default;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            bool alguno = false;
            foreach (var r in renderers)
            {
                // Las partículas y el cartel no cuentan como tamaño de la parrilla.
                if (r is ParticleSystemRenderer || r.GetComponentInParent<Canvas>() != null) continue;
                if (!alguno) { b = r.bounds; alguno = true; }
                else b.Encapsulate(r.bounds);
            }
            return alguno;
        }

    }
}
