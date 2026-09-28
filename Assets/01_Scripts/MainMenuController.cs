using UnityEngine;
using CocinaBoliviana.Data;
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

        [Header("Niveles")]
        [Tooltip("Los niveles jugables, en orden. Todos usan la MISMA escena: lo único que " +
                 "cambia es el departamento y sus reglas.")]
        [SerializeField] private LevelData[] niveles;

        [Tooltip("Cuál se carga con 'Nueva Partida'. 0 = el primero de la lista.")]
        [SerializeField] private int nivelPorDefecto;

        public void NuevaPartida()
        {
            if (niveles != null && nivelPorDefecto >= 0 && nivelPorDefecto < niveles.Length)
            {
                LevelSelection.Elegido = niveles[nivelPorDefecto];
            }

            Debug.Log("[MainMenu] Nueva Partida seleccionada -> Cargando escena: " + gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }

        public void ContinuarPartida()
        {
            Debug.Log("[MainMenu] Continuar Partida seleccionada -> Cargando escena: " + gameSceneName);
            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// Elige el nivel y entra. Se engancha a un boton por nivel en el menu: el indice es
        /// la posicion en la lista 'niveles'.
        /// </summary>
        public void JugarNivel(int indice)
        {
            if (niveles == null || indice < 0 || indice >= niveles.Length || niveles[indice] == null)
            {
                Debug.LogWarning($"[MainMenuController] No hay nivel en el indice {indice}; " +
                                 "rellena la lista 'Niveles'.");
                return;
            }

            LevelSelection.Elegido = niveles[indice];
            Debug.Log($"[MainMenuController] Nivel elegido: {niveles[indice].nombreNivel}");
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
