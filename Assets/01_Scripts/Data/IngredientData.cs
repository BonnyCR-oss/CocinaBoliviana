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
