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

        [Header("Cocción")]
        [Tooltip("En qué recipientes se puede cocinar. Vacío = no se cocina en ninguno, " +
                 "aunque 'Se Puede Cocinar' esté marcado.")]
        public List<MetodoCoccion> metodosCoccion = new List<MetodoCoccion>();

        [Tooltip("Segundos hasta quedar en su punto.")]
        public float tiempoCoccion = 8f;

        [Tooltip("Segundos EXTRA, ya estando listo, antes de quemarse.")]
        public float margenAntesDeQuemarse = 6f;

        /// <summary>
        /// Si este ingrediente admite ese recipiente. Comprueba las dos cosas, porque
        /// 'sePuedeCocinar' es el interruptor general y la lista dice en qué exactamente.
        /// </summary>
        public bool AdmiteCoccion(MetodoCoccion metodo)
        {
            return sePuedeCocinar && metodosCoccion != null && metodosCoccion.Contains(metodo);
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
