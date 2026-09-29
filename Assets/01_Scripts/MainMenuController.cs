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
        [Tooltip("Los niveles jugables, en orden: 0 = Cochabamba, 1 = La Paz, 2 = Santa Cruz. " +
                 "Cada uno tiene su propia escena.")]
        [SerializeField] private LevelData[] niveles;

        [Tooltip("Cuál se carga con 'Nueva Partida'. 0 = el primero de la lista.")]
        [SerializeField] private int nivelPorDefecto;

        [Header("Botones")]
        [Tooltip("Se oculta si todavía no hay ninguna partida guardada. Vacío = se busca " +
                 "'Btn_ContinuarPartida' en la escena.")]
        [SerializeField] private GameObject botonContinuar;

        private const string NombreBotonContinuar = "Btn_ContinuarPartida";

        private void Start()
        {
            if (botonContinuar == null) botonContinuar = GameObject.Find(NombreBotonContinuar);

            // Sin partida guardada, 'Continuar' haría lo mismo que 'Nueva Partida': confunde.
            if (botonContinuar != null)
            {
                botonContinuar.SetActive(GameProgressManager.TieneProgresoGuardado());
            }
        }

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
            if (!GameProgressManager.TieneProgresoGuardado())
            {
                NuevaPartida();
                return;
            }

            // El nivel en el que te quedaste (se empieza de nuevo), no el más alto desbloqueado.
            int nivelGuardado = GameProgressManager.ObtenerNivelParaContinuar();

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
        /// Entra a un nivel concreto. Se engancha a un boton por nivel en el menu: el indice es
        /// la posicion en la lista 'niveles' (0=Cbba, 1=La Paz, 2=Santa Cruz).
        ///
        /// NO desbloquea nada: solo deja entrar a niveles ya ganados. Desbloquear es cosa de
        /// superar el nivel anterior (LevelManager).
        /// </summary>
        public void JugarNivel(int indice)
        {
            int numeroNivel = indice + 1;
            if (!GameProgressManager.EstaDesbloqueado(numeroNivel))
            {
                Debug.LogWarning($"[MainMenuController] El nivel {numeroNivel} está bloqueado: " +
                                 "supera antes el anterior.");
                return;
            }

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
