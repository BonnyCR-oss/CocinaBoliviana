using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CocinaBoliviana
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string gameSceneName = "First Scene";

        public void NuevaPartida()
        {
            Debug.Log("[MainMenu] Nueva Partida seleccionada -> Cargando escena: " + gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }

        public void ContinuarPartida()
        {
            Debug.Log("[MainMenu] Continuar Partida seleccionada -> Cargando escena: " + gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }

        public void Salir()
        {
            Debug.Log("[MainMenu] Salir del juego seleccionado.");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
