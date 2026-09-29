using System.Collections.Generic;
using UnityEngine;

namespace CocinaBoliviana.Data
{
    public enum IngredientType
    {
        Verdura,
        Carne,
        Salsa,
        Grano,
        Fruta,
        Otro
    }

    [System.Serializable]
    public class ResultadoCorte
    {
        public TipoCorte tipo;
        public GameObject prefabResultado;
    }

    /// <summary>
    /// En qué se convierte un ingrediente al entrar a un recipiente. El huevo con cáscara
    /// pasa a estrellado en el sartén y a huevo duro pelado en la olla: la forma cambia de
    /// verdad, así que se cambia el prefab (como al cortar) en vez de solo teñirlo.
    /// </summary>
    [System.Serializable]
    public class ResultadoCoccion
    {
        public MetodoCoccion metodo;
        public GameObject prefabResultado;
    }

    [CreateAssetMenu(fileName = "NewIngredient", menuName = "Cocina Boliviana/Ingrediente")]
    public class IngredientData : ScriptableObject
    {
        [Header("Informacion General")]
        public string nombre;
        public IngredientType tipo;
        public GameObject prefab;

        [Header("Interacciones")]
        public bool sePuedeCortar;
        public bool sePuedeCocinar;

        [Header("Cortes Posibles")]
        public List<ResultadoCorte> cortesDisponibles = new List<ResultadoCorte>();

        [Tooltip("Solo se puede cortar una vez cocido. Es el huevo: con cáscara no se pica, " +
                 "hervido sí.")]
        public bool cortarSoloCocido;

        [Header("Cocción")]
        [Tooltip("En qué recipientes se puede cocinar. Vacío = no se cocina en ninguno, " +
                 "aunque 'Se Puede Cocinar' esté marcado.")]
        public List<MetodoCoccion> metodosCoccion = new List<MetodoCoccion>();

        [Tooltip("Segundos hasta quedar en su punto.")]
        public float tiempoCoccion = 8f;

        [Tooltip("Segundos EXTRA, ya estando listo, antes de quemarse.")]
        public float margenAntesDeQuemarse = 6f;

        [Tooltip("Opcional. Prefab por el que se cambia al ENTRAR en un recipiente de ese " +
                 "método (huevo con cáscara → estrellado en el sartén). Sin entrada para un " +
                 "método, el ingrediente se queda con su modelo y solo cambia de color.")]
        public List<ResultadoCoccion> resultadosCoccion = new List<ResultadoCoccion>();

        /// <summary>
        /// Si este ingrediente admite ese recipiente. Comprueba las dos cosas, porque
        /// 'sePuedeCocinar' es el interruptor general y la lista dice en qué exactamente.
        /// </summary>
        public bool AdmiteCoccion(MetodoCoccion metodo)
        {
            return sePuedeCocinar && metodosCoccion != null && metodosCoccion.Contains(metodo);
        }

        /// <summary>El prefab en que se convierte con ese método, o null si no cambia.</summary>
        public GameObject ObtenerPrefabParaCoccion(MetodoCoccion metodo)
        {
            if (resultadosCoccion == null) return null;
            foreach (var r in resultadosCoccion)
            {
                if (r != null && r.metodo == metodo) return r.prefabResultado;
            }
            return null;
        }

        /// <summary>
        /// Si la tabla puede cortar este ingrediente en el estado en que llega. Un huevo con
        /// cáscara no; el mismo huevo ya hervido, sí.
        /// </summary>
        public bool PuedeCortarseEn(EstadoCoccion estado)
        {
            if (!sePuedeCortar || cortesDisponibles == null || cortesDisponibles.Count == 0) return false;
            return !cortarSoloCocido || estado == EstadoCoccion.Cocido;
        }

        public GameObject ObtenerPrefabParaCorte(TipoCorte tipoDeCorte)
        {
            foreach (var corte in cortesDisponibles)
            {
                if (corte.tipo == tipoDeCorte)
                {
                    return corte.prefabResultado;
                }
            }
            return null;
        }
    }
}
