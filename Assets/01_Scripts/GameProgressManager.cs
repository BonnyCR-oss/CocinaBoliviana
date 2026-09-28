using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Gestiona el guardado persistente del progreso de la campaña mediante PlayerPrefs.
    /// Solo almacena el nivel desbloqueado actual (1, 2 o 3), sin guardar estado en mitad de partida.
    /// </summary>
    public static class GameProgressManager
    {
        private const string SaveKeyNivel = "CocinaBoliviana_NivelGuardado";
        private const string SaveKeyTienePartida = "CocinaBoliviana_TienePartida";

        public const int NivelMinimo = 1;
        public const int NivelMaximo = 3;

        public const string EscenaMenuPrincipal = "Main Menu";
        public const string EscenaNivel1 = "Nivel 1 - Cochabamba";
        public const string EscenaNivel2 = "Nivel 2 - La Paz";
        public const string EscenaNivel3 = "Nivel 3 - Santa Cruz";

        /// <summary>
        /// Devuelve el nivel guardado actualmente (1, 2 o 3). Por defecto 1.
        /// </summary>
        public static int ObtenerNivelGuardado()
        {
            int nivel = PlayerPrefs.GetInt(SaveKeyNivel, NivelMinimo);
            return Mathf.Clamp(nivel, NivelMinimo, NivelMaximo);
        }

        /// <summary>
        /// Guarda el nivel alcanzado (solo actualiza si el nuevo nivel es mayor o igual).
        /// </summary>
        public static void GuardarNivel(int nivel)
        {
            int nivelActual = ObtenerNivelGuardado();
            int nivelAGuardar = Mathf.Clamp(Mathf.Max(nivelActual, nivel), NivelMinimo, NivelMaximo);

            PlayerPrefs.SetInt(SaveKeyNivel, nivelAGuardar);
            PlayerPrefs.SetInt(SaveKeyTienePartida, 1);
            PlayerPrefs.Save();

            Debug.Log($"[GameProgressManager] Progreso guardado: Nivel {nivelAGuardar}");
        }

        /// <summary>
        /// Reinicia el progreso al Nivel 1 (para 'Nueva Partida').
        /// </summary>
        public static void ReiniciarProgreso()
        {
            PlayerPrefs.SetInt(SaveKeyNivel, NivelMinimo);
            PlayerPrefs.SetInt(SaveKeyTienePartida, 1);
            PlayerPrefs.Save();

            Debug.Log("[GameProgressManager] Progreso reiniciado a Nivel 1.");
        }

        /// <summary>
        /// Indica si el usuario ya ha iniciado una partida previamente.
        /// </summary>
        public static bool TieneProgresoGuardado()
        {
            return PlayerPrefs.GetInt(SaveKeyTienePartida, 0) == 1;
        }

        /// <summary>
        /// Mapea un número de nivel (1, 2 o 3) a su nombre de escena correspondiente.
        /// </summary>
        public static string ObtenerNombreEscenaNivel(int nivel)
        {
            return nivel switch
            {
                1 => EscenaNivel1,
                2 => EscenaNivel2,
                3 => EscenaNivel3,
                _ => EscenaNivel1
            };
        }
    }
}
