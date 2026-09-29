using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Gestiona el guardado persistente del progreso de la campaña mediante PlayerPrefs.
    ///
    /// Guarda:
    ///  - el nivel desbloqueado más alto (1, 2 o 3),
    ///  - el récord de puntos y de estrellas de cada nivel.
    ///
    /// No guarda estado a mitad de partida: si sales, el nivel se repite entero.
    /// </summary>
    public static class GameProgressManager
    {
        private const string SaveKeyNivel = "CocinaBoliviana_NivelGuardado";
        private const string SaveKeyTienePartida = "CocinaBoliviana_TienePartida";
        private const string SaveKeyRecordPuntos = "CocinaBoliviana_RecordPuntos_";
        private const string SaveKeyRecordEstrellas = "CocinaBoliviana_RecordEstrellas_";
        private const string SaveKeyNivelEnCurso = "CocinaBoliviana_NivelEnCurso";

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

        /// <summary>Si el jugador ya puede entrar a ese nivel.</summary>
        public static bool EstaDesbloqueado(int nivel)
        {
            return nivel >= NivelMinimo && nivel <= ObtenerNivelGuardado();
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
        /// Reinicia la partida al Nivel 1 (para 'Nueva Partida'). Borra también los récords:
        /// una partida nueva empieza de cero.
        /// </summary>
        public static void ReiniciarProgreso()
        {
            PlayerPrefs.SetInt(SaveKeyNivel, NivelMinimo);
            PlayerPrefs.SetInt(SaveKeyTienePartida, 1);
            PlayerPrefs.SetInt(SaveKeyNivelEnCurso, NivelMinimo);
            for (int n = NivelMinimo; n <= NivelMaximo; n++)
            {
                PlayerPrefs.DeleteKey(SaveKeyRecordPuntos + n);
                PlayerPrefs.DeleteKey(SaveKeyRecordEstrellas + n);
            }
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

        // ------------------------------------------------------------ nivel en curso

        /// <summary>
        /// El nivel en el que se quedó el jugador: el que estaba jugando al salir al menú, o
        /// el siguiente si acaba de superar uno. Es a donde lleva 'Continuar'.
        /// </summary>
        public static void GuardarNivelEnCurso(int nivel)
        {
            if (nivel < NivelMinimo || nivel > NivelMaximo) return;
            PlayerPrefs.SetInt(SaveKeyNivelEnCurso, nivel);
            PlayerPrefs.SetInt(SaveKeyTienePartida, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// A qué nivel lleva 'Continuar'. Si el guardado en curso no está desbloqueado (datos
        /// viejos o tocados a mano), el más alto desbloqueado.
        /// </summary>
        public static int ObtenerNivelParaContinuar()
        {
            int enCurso = PlayerPrefs.GetInt(SaveKeyNivelEnCurso, 0);
            return EstaDesbloqueado(enCurso) ? enCurso : ObtenerNivelGuardado();
        }

        // ------------------------------------------------------------------ récords

        /// <summary>Mejor puntuación conseguida en ese nivel. 0 si nunca se jugó.</summary>
        public static int ObtenerRecordPuntos(int nivel) =>
            PlayerPrefs.GetInt(SaveKeyRecordPuntos + nivel, 0);

        /// <summary>Máximo de estrellas conseguidas en ese nivel (0 a 3).</summary>
        public static int ObtenerRecordEstrellas(int nivel) =>
            Mathf.Clamp(PlayerPrefs.GetInt(SaveKeyRecordEstrellas + nivel, 0), 0, 3);

        /// <summary>
        /// Anota el resultado de una partida. Puntos y estrellas se guardan por separado y
        /// solo si mejoran: no se pierde un récord por jugar peor después.
        /// </summary>
        /// <returns>true si los puntos superan el récord anterior.</returns>
        public static bool GuardarResultado(int nivel, int puntos, int estrellas)
        {
            if (nivel < NivelMinimo || nivel > NivelMaximo) return false;

            bool nuevoRecord = puntos > ObtenerRecordPuntos(nivel);
            if (nuevoRecord) PlayerPrefs.SetInt(SaveKeyRecordPuntos + nivel, puntos);

            if (estrellas > ObtenerRecordEstrellas(nivel))
            {
                PlayerPrefs.SetInt(SaveKeyRecordEstrellas + nivel, Mathf.Clamp(estrellas, 0, 3));
            }

            PlayerPrefs.Save();
            Debug.Log($"[GameProgressManager] Nivel {nivel}: {puntos} pts, {estrellas}★ " +
                      $"(récord {ObtenerRecordPuntos(nivel)} pts, {ObtenerRecordEstrellas(nivel)}★).");
            return nuevoRecord;
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
