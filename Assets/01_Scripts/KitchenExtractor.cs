using UnityEngine;
using UnityEngine.SceneManagement;

namespace CocinaBoliviana
{
    /// <summary>
    /// Controlador estético y visual para las campanas extractoras de la cocina y de la entrega.
    /// Garantiza en tiempo de ejecución:
    /// 1. Iluminación focalizada y cálida hacia la zona de cocción / entrega.
    /// 2. Barra LED de trabajo con material emisivo bloom bajo la campana.
    /// 3. Filtros metálicos con material deflector (baffle grease filters).
    /// 4. Efecto de vapor y succión aerodinámica en los extractores sobre los fuegos de la cocina.
    /// </summary>
    public class KitchenExtractor : MonoBehaviour
    {
        [Header("Tipo de Estación")]
        [SerializeField] private bool esExtractorCocina = true;

        [Header("Iluminación")]
        [SerializeField] private Color colorLuz = new Color(1f, 0.94f, 0.84f, 1f);
        [SerializeField] private float intensidadLuz = 2.6f;
        [SerializeField] private float anguloSpot = 75f;
        [SerializeField] private float rangoLuz = 2.8f;

        private static Material matFilterCache;
        private static Material matLedCache;
        private static Material matHumoCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInicializarEnEscena()
        {
            var todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var t in todos)
            {
                if (t.name == "Extractor_Cocina")
                {
                    if (t.GetComponent<KitchenExtractor>() == null)
                    {
                        var ext = t.gameObject.AddComponent<KitchenExtractor>();
                        ext.esExtractorCocina = true;
                    }
                }
                else if (t.name == "Extractor_Entrega")
                {
                    if (t.GetComponent<KitchenExtractor>() == null)
                    {
                        var ext = t.gameObject.AddComponent<KitchenExtractor>();
                        ext.esExtractorCocina = false;
                    }
                }
            }
        }

        private void Awake()
        {
            if (name.Contains("Entrega")) esExtractorCocina = false;

            CargarMaterialesSiNecesario();
            ConfigurarBarraLED();
            ConfigurarLuzTrabajo();
            ConfigurarFiltros();

            if (esExtractorCocina)
            {
                ConfigurarVaporSuccion();
            }
        }

        private static void CargarMaterialesSiNecesario()
        {
            if (matFilterCache == null)
            {
                var shaderFilter = Shader.Find("CocinaBoliviana/ExtractorFilterGrill") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (shaderFilter != null)
                {
                    matFilterCache = new Material(shaderFilter);
                    matFilterCache.name = "Mat_Extractor_Filter_Runtime";
                    matFilterCache.SetColor("_BaseColor", new Color(0.72f, 0.74f, 0.77f, 1f));
                    matFilterCache.SetColor("_SlatColor", new Color(0.18f, 0.20f, 0.23f, 1f));
                    matFilterCache.SetFloat("_Metallic", 0.95f);
                    matFilterCache.SetFloat("_Smoothness", 0.78f);
                    matFilterCache.SetFloat("_SlatFrequency", 28f);
                    matFilterCache.SetFloat("_SlatDepth", 0.55f);
                }
            }

            if (matLedCache == null)
            {
                var shaderLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shaderLit != null)
                {
                    matLedCache = new Material(shaderLit);
                    matLedCache.name = "Mat_LED_Strip_Runtime";
                    matLedCache.SetColor("_BaseColor", new Color(1f, 0.98f, 0.92f, 1f));
                    matLedCache.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f, 1f) * 2.8f);
                    matLedCache.EnableKeyword("_EMISSION");
                    matLedCache.SetFloat("_Metallic", 0.1f);
                    matLedCache.SetFloat("_Smoothness", 0.9f);
                }
            }

            if (matHumoCache == null)
            {
                var shaderParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                if (shaderParticle != null)
                {
                    matHumoCache = new Material(shaderParticle);
                    matHumoCache.name = "Mat_Humo_Runtime";
                    matHumoCache.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.15f));
                }
            }
        }

        private void ConfigurarBarraLED()
        {
            Transform led = transform.Find("Barra_LED");
            if (led == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Barra_LED";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, -0.21f, 0f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = new Vector3(0.65f, 0.02f, 0.06f);

                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                led = go.transform;
            }

            var renderer = led.GetComponent<MeshRenderer>();
            if (renderer != null && matLedCache != null)
            {
                renderer.sharedMaterial = matLedCache;
            }
        }

        private void ConfigurarLuzTrabajo()
        {
            Transform luzTransform = transform.Find("Luz_Extractor");
            if (luzTransform != null)
            {
                luzTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                luzTransform.localPosition = new Vector3(0f, -0.22f, 0f);

                var light = luzTransform.GetComponent<Light>();
                if (light != null)
                {
                    light.type = LightType.Spot;
                    light.color = colorLuz;
                    light.intensity = intensidadLuz;
                    light.range = rangoLuz;
                    light.spotAngle = anguloSpot;
                    light.innerSpotAngle = 35f;
                }
            }
        }

        private void ConfigurarFiltros()
        {
            if (matFilterCache == null) return;

            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Filtro_"))
                {
                    var renderer = child.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = matFilterCache;
                    }
                }
            }
        }

        private void ConfigurarVaporSuccion()
        {
            Transform vaporTransform = transform.Find("Vapor_Extractor");
            if (vaporTransform == null)
            {
                var go = new GameObject("Vapor_Extractor");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, -0.65f, 0f);
                go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // hacia arriba

                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = true;
                main.loop = true;
                main.duration = 2.0f;
                main.startLifetime = 1.4f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
                main.startColor = new Color(1f, 1f, 1f, 0.12f);

                var emission = ps.emission;
                emission.rateOverTime = 6f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(0.6f, 0.4f, 0.1f);

                var col = ps.colorOverLifetime;
                col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.12f, 0.25f), new GradientAlphaKey(0f, 1f) }
                );
                col.color = grad;

                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && matHumoCache != null)
                {
                    renderer.sharedMaterial = matHumoCache;
                }
            }
        }
    }
}
