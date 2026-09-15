using UnityEngine;

namespace CocinaBoliviana.Data
{
    public enum IngredientType
    {
        Verdura,
        Carne,
        Condimento,
        Lacteo,
        Grano,
        Fruta,
        Otro
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
    }
}
