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
        [Header("Niveles")]
        [Tooltip("Los niveles jugables, en orden. Todos usan la MISMA escena: lo único que " +
                 "cambia es el departamento y sus reglas.")]
        [SerializeField] private LevelData[] niveles;

        [Tooltip("Cuál se carga con 'Nueva Partida'. 0 = el primero de la lista.")]
        [SerializeField] private int nivelPorDefecto;

        public void NuevaPartida()
        {
            GameProgressManager.ReiniciarProgreso();

            if (niveles != null && niveles.Length > 0 && niveles[0] != null)
            {
                LevelSelection.Elegido = niveles[0];
            }

            string escena = GameProgressManager.ObtenerNombreEscenaNivel(1);
            Debug.Log($"[MainMenu] Nueva Partida seleccionada -> Cargando escena: {escena}");
            SceneManager.LoadScene(escena);
        }

        public void ContinuarPartida()
        {
            int nivelGuardado = GameProgressManager.ObtenerNivelGuardado();

            int indiceNivel = Mathf.Clamp(nivelGuardado - 1, 0, (niveles != null && niveles.Length > 0) ? niveles.Length - 1 : 0);
            if (niveles != null && indiceNivel < niveles.Length && niveles[indiceNivel] != null)
            {
                LevelSelection.Elegido = niveles[indiceNivel];
            }

            string escena = GameProgressManager.ObtenerNombreEscenaNivel(nivelGuardado);
            Debug.Log($"[MainMenu] Continuar Partida -> Nivel guardado {nivelGuardado}. Cargando escena: {escena}");
            SceneManager.LoadScene(escena);
        }

        /// <summary>
        /// Elige el nivel y entra. Se engancha a un boton por nivel en el menu: el indice es
        /// la posicion en la lista 'niveles' (0=Cbba, 1=La Paz, 2=Santa Cruz).
        /// </summary>
        public void JugarNivel(int indice)
        {
            int numeroNivel = indice + 1;
            GameProgressManager.GuardarNivel(numeroNivel);

            if (niveles != null && indice >= 0 && indice < niveles.Length && niveles[indice] != null)
            {
                LevelSelection.Elegido = niveles[indice];
            }

            string escena = GameProgressManager.ObtenerNombreEscenaNivel(numeroNivel);
            Debug.Log($"[MainMenuController] Nivel elegido: {numeroNivel} -> Cargando: {escena}");
            SceneManager.LoadScene(escena);
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
