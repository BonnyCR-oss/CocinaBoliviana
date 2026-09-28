using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Cuelga dos banderas en las paredes largas del ambiente. Muestran la del departamento
    /// del nivel en curso, así que la misma escena vale para los tres.
    /// </summary>
    public static class FlagsSetup
    {
        private const string ScenePath = "Assets/00_Scenes/First Scene.unity";

        /// <summary>
        /// Las dos paredes largas (10 m en X, a z = ±2.95). Se eligen esas y no la oeste
        /// porque ahí está el tablero de pedidos, ni la este, donde está el fregadero.
        /// El signo dice hacia dónde mira la bandera: al centro de la cocina.
        /// </summary>
        private static readonly (string pared, float z, float giroY)[] Paredes =
        {
            ("Wall_North", 2.88f, 180f),
            ("Wall_South", -2.88f, 0f),
        };

        private const float AnchoBandera = 1.6f;   // metros
        private const float AltoBandera = 1.05f;
        private const float AlturaSuelo = 2.0f;

        [MenuItem("Kitchen/Setup Flags (banderas del departamento)")]
        public static void SetupFlags()
        {
            Debug.Log("[FlagsSetup] Colocando banderas...");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[FlagsSetup] No se pudo abrir {ScenePath}");
                return;
            }

            GameObject raiz = GameObject.Find("Kitchen_Environment") ?? GameObject.Find("Stations");
            int puestas = 0;

            foreach (var (pared, z, giroY) in Paredes)
            {
                string nombre = "Bandera_" + pared.Replace("Wall_", "");

                if (GameObject.Find(nombre) != null)
                {
                    Debug.Log($"[FlagsSetup] '{nombre}' ya existe; se deja donde está.");
                    continue;
                }

                Construir(nombre, z, giroY, raiz);
                puestas++;
            }

            AvisarDeBanderasQueFaltan();

            if (puestas > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[FlagsSetup] Listo. {puestas} bandera(s) nueva(s). " +
                      "Muévelas en la Hierarchy si quieres otro sitio en la pared.");
        }

        private static void Construir(string nombre, float z, float giroY, GameObject raiz)
        {
            var canvasGo = new GameObject(nombre,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (raiz != null) canvasGo.transform.SetParent(raiz.transform, true);

            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            // El canvas se dimensiona en pixeles y se escala a metros: 1000 px = 1 m.
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(AnchoBandera * 1000f, AltoBandera * 1000f);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            canvasGo.transform.position = new Vector3(0f, AlturaSuelo, z);
            canvasGo.transform.rotation = Quaternion.Euler(0f, giroY, 0f);

            // Marco oscuro detrás: despega la bandera de la pared y le da un borde limpio
            // aunque el sprite venga sin márgenes.
            var marco = new GameObject("Marco", typeof(RectTransform), typeof(Image));
            marco.transform.SetParent(canvasGo.transform, false);
            var mRt = marco.GetComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero;
            mRt.anchorMax = Vector2.one;
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            var mImg = marco.GetComponent<Image>();
            mImg.color = new Color(0.10f, 0.10f, 0.12f, 1f);
            mImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            mImg.type = Image.Type.Sliced;

            var telaGo = new GameObject("Bandera", typeof(RectTransform), typeof(Image));
            telaGo.transform.SetParent(marco.transform, false);
            var tRt = telaGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(26f, 26f);
            tRt.offsetMax = new Vector2(-26f, -26f);
            var tela = telaGo.GetComponent<Image>();
            tela.preserveAspect = true;
            tela.raycastTarget = false;

            var flag = canvasGo.AddComponent<WallFlag>();
            var so = new SerializedObject(flag);
            so.FindProperty("imagen").objectReferenceValue = tela;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[FlagsSetup] '{nombre}' colocada en z={z}, mirando al centro.");
        }

        /// <summary>Avisa de los departamentos a los que aún no les pusiste bandera.</summary>
        private static void AvisarDeBanderasQueFaltan()
        {
            const string carpeta = "Assets/03_SO/Departamentos";
            if (!AssetDatabase.IsValidFolder(carpeta)) return;

            foreach (string guid in AssetDatabase.FindAssets("t:DepartmentData", new[] { carpeta }))
            {
                var dep = AssetDatabase.LoadAssetAtPath<DepartmentData>(AssetDatabase.GUIDToAssetPath(guid));
                if (dep == null) continue;

                if (dep.bandera == null)
                {
                    Debug.LogWarning($"[FlagsSetup] '{dep.nombre}' no tiene bandera. Arrastra su " +
                                     "sprite al campo 'Bandera' de su DepartmentData.");
                }
                else
                {
                    Debug.Log($"[FlagsSetup] '{dep.nombre}': bandera '{dep.bandera.name}'.");
                }
            }
        }
    }
}
