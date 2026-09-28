using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Crea los tres niveles y los cablea, para que la MISMA escena de cocina sirva para
    /// Cochabamba, La Paz y Santa Cruz.
    /// </summary>
    public static class LevelsSetup
    {
        private const string NivelesFolder = "Assets/03_SO/Niveles";
        private const string DepartamentosFolder = "Assets/03_SO/Departamentos";
        private const string CocinaScene = "Assets/00_Scenes/First Scene.unity";
        private const string MenuScene = "Assets/00_Scenes/Main Menu.unity";

        /// <summary>
        /// Los tres niveles, en orden de juego. El nombre del departamento es el del asset,
        /// no el del campo 'nombre': así se localiza aunque el nombre visible cambie.
        /// </summary>
        private static readonly (string asset, string depto, string titulo, float duracion,
                                 int e1, int e2, int e3)[] Niveles =
        {
            ("Nivel1_Cochabamba", "Cochabamba", "Nivel 1 - Cochabamba", 150f,  80, 150, 220),
            ("Nivel2_LaPaz",      "La paz",     "Nivel 2 - La Paz",     150f, 100, 180, 260),
            ("Nivel3_SantaCruz",  "SantaCruz",  "Nivel 3 - Santa Cruz", 150f, 120, 210, 300),
        };

        [MenuItem("Kitchen/Setup Levels (3 niveles, una escena)")]
        public static void SetupLevels()
        {
            Debug.Log("[LevelsSetup] Creando niveles...");

            List<LevelData> creados = CrearNiveles();
            if (creados.Count == 0)
            {
                Debug.LogError("[LevelsSetup] No se pudo crear ningún nivel.");
                return;
            }

            CablearMenu(creados);
            CablearCocina(creados[0]);

            Debug.Log($"[LevelsSetup] Listo. {creados.Count} niveles usando la misma escena.");
        }

        private static List<LevelData> CrearNiveles()
        {
            if (!AssetDatabase.IsValidFolder(NivelesFolder))
            {
                AssetDatabase.CreateFolder("Assets/03_SO", "Niveles");
            }

            var lista = new List<LevelData>();

            foreach (var (asset, depto, titulo, duracion, e1, e2, e3) in Niveles)
            {
                string ruta = $"{NivelesFolder}/{asset}.asset";

                var nivel = AssetDatabase.LoadAssetAtPath<LevelData>(ruta);
                if (nivel == null)
                {
                    nivel = ScriptableObject.CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(nivel, ruta);
                    Debug.Log($"[LevelsSetup] Creado {ruta}");
                }

                nivel.nombreNivel = titulo;
                nivel.duracionNivel = duracion;
                nivel.objetivoPuntos1Estrella = e1;
                nivel.objetivoPuntos2Estrellas = e2;
                nivel.objetivoPuntos3Estrellas = e3;

                var departamento = AssetDatabase.LoadAssetAtPath<DepartmentData>(
                    $"{DepartamentosFolder}/{depto}.asset");

                if (departamento == null)
                {
                    Debug.LogWarning($"[LevelsSetup] No encontré '{depto}.asset'; " +
                                     $"'{titulo}' se queda sin departamento.");
                }
                else
                {
                    nivel.departamento = departamento;
                }

                EditorUtility.SetDirty(nivel);
                lista.Add(nivel);
            }

            AssetDatabase.SaveAssets();
            return lista;
        }

        /// <summary>Rellena la lista de niveles del menú principal.</summary>
        private static void CablearMenu(List<LevelData> niveles)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogWarning($"[LevelsSetup] No se pudo abrir {MenuScene}");
                return;
            }

            var menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null)
            {
                Debug.LogWarning("[LevelsSetup] No hay MainMenuController en el menú; " +
                                 "la lista de niveles habrá que rellenarla a mano.");
                return;
            }

            var so = new SerializedObject(menu);
            SerializedProperty lista = so.FindProperty("niveles");
            lista.ClearArray();
            for (int i = 0; i < niveles.Count; i++)
            {
                lista.InsertArrayElementAtIndex(i);
                lista.GetArrayElementAtIndex(i).objectReferenceValue = niveles[i];
            }
            so.FindProperty("nivelPorDefecto").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LevelsSetup] Menú: {niveles.Count} niveles en la lista. " +
                      "Engancha cada botón a MainMenuController.JugarNivel con su índice (0, 1, 2).");
        }

        /// <summary>
        /// Deja el nivel 1 como el de por defecto de la escena de cocina, para poder darle
        /// a Play ahí directamente sin pasar por el menú.
        /// </summary>
        private static void CablearCocina(LevelData primero)
        {
            Scene scene = EditorSceneManager.OpenScene(CocinaScene, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogWarning($"[LevelsSetup] No se pudo abrir {CocinaScene}");
                return;
            }

            var manager = Object.FindAnyObjectByType<LevelManager>();
            if (manager == null)
            {
                Debug.LogWarning("[LevelsSetup] No hay LevelManager en la cocina.");
                return;
            }

            var so = new SerializedObject(manager);
            so.FindProperty("nivelPorDefecto").objectReferenceValue = primero;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LevelsSetup] Cocina: nivel por defecto '{primero.nombreNivel}'. " +
                      "Cámbialo en el LevelManager para probar otro departamento con Play directo.");
        }
    }
}
