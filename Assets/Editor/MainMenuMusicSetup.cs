using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Pone música de fondo en el menú principal con el mismo BackgroundMusicManager que la
    /// cocina. Usa las pistas 1 y 2, que la cocina no usa (allí suenan la 3, 4 y 5), para
    /// que al entrar a jugar se note el cambio.
    ///
    /// No es persistente entre escenas: al pulsar "Nueva Partida" se corta y la cocina
    /// arranca con su propia lista, sin sonar dos músicas a la vez.
    ///
    /// Si el objeto ya existe no se toca su lista: las canciones que le pongas a mano en el
    /// Inspector se respetan.
    /// </summary>
    public static class MainMenuMusicSetup
    {
        private const string ScenePath = "Assets/00_Scenes/Main Menu.unity";
        private const string ObjectName = "MusicaMenu";

        private static readonly string[] Pistas =
        {
            "Assets/06_SFX/musica de fondo 1.mp3",
            "Assets/06_SFX/musica de fondo 2.mp3",
        };

        [MenuItem("Kitchen/Setup Main Menu Music (música del menú)")]
        public static void SetupMusic()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[MainMenuMusicSetup] No se pudo abrir {ScenePath}");
                return;
            }

            var existente = Object.FindAnyObjectByType<BackgroundMusicManager>();
            if (existente != null)
            {
                Debug.Log($"[MainMenuMusicSetup] Ya hay música en '{existente.name}'; se deja como está. " +
                          "Cambia las canciones en su lista 'Playlist'.");
                Selection.activeGameObject = existente.gameObject;
                return;
            }

            var go = new GameObject(ObjectName, typeof(AudioSource));
            var musica = go.AddComponent<BackgroundMusicManager>();

            var so = new SerializedObject(musica);
            var lista = so.FindProperty("playlist");
            lista.ClearArray();
            foreach (string ruta in Pistas)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
                if (clip == null)
                {
                    Debug.LogWarning($"[MainMenuMusicSetup] No existe {ruta}, se omite.");
                    continue;
                }
                lista.InsertArrayElementAtIndex(lista.arraySize);
                lista.GetArrayElementAtIndex(lista.arraySize - 1).objectReferenceValue = clip;
            }

            // Algo más alta que en la cocina (0.22): aquí no hay efectos que tapar.
            so.FindProperty("musicVolume").floatValue = 0.35f;
            so.FindProperty("shuffle").boolValue = true;
            so.FindProperty("autoPlayOnStart").boolValue = true;
            so.FindProperty("persistBetweenScenes").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[MainMenuMusicSetup] Música del menú lista en '{ObjectName}' " +
                      $"con {lista.arraySize} canción(es).");
        }
    }
}
