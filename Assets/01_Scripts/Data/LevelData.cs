using UnityEngine;

namespace CocinaBoliviana.Data
{
    /// <summary>
    /// Un nivel: qué departamento se cocina y con qué reglas. Es lo único que cambia entre
    /// Cochabamba, La Paz y Santa Cruz, así que con esto la MISMA escena sirve para los tres.
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoNivel", menuName = "Cocina Boliviana/Nivel")]
    public class LevelData : ScriptableObject
    {
        [Header("Identidad")]
        public string nombreNivel = "Nivel 1 - Cochabamba";

        [Tooltip("Posición en la campaña: 1 = Cochabamba, 2 = La Paz, 3 = Santa Cruz. De aquí " +
                 "salen el guardado, los récords y qué escena viene después.")]
        public int numeroNivel = 1;

        [Tooltip("De aquí salen el menú de pedidos, el refresco del dispensador y la bandera " +
                 "de las paredes.")]
        public DepartmentData departamento;

        [Header("Reglas")]
        public float duracionNivel = 150f;

        [Header("Objetivos")]
        public int objetivoPuntos1Estrella = 80;
        public int objetivoPuntos2Estrellas = 150;
        public int objetivoPuntos3Estrellas = 220;
    }

    /// <summary>
    /// Qué nivel hay que cargar cuando arranque la escena de cocina.
    ///
    /// Es estático a propósito: la selección tiene que sobrevivir al cambio de escena desde
    /// el menú, y un campo en un objeto de la escena del menú se destruye al cargar la otra.
    /// Al ser un asset compartido, no hace falta serializar nada entre escenas.
    /// </summary>
    public static class LevelSelection
    {
        /// <summary>
        /// El nivel elegido en el menú. Si es null, LevelManager usa lo que tenga puesto en
        /// el Inspector, que es lo cómodo para probar dando a Play directamente.
        /// </summary>
        public static LevelData Elegido { get; set; }
    }
}
