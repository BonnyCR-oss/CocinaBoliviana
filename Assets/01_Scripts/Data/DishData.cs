using System.Collections.Generic;
using UnityEngine;

namespace CocinaBoliviana.Data
{
    [CreateAssetMenu(fileName = "NewDish", menuName = "Cocina Boliviana/Plato")]
    public class DishData : ScriptableObject
    {
        [Header("Informacion General")]
        public string nombre;
        public Sprite icono;
        public GameObject platoPrefab;

        [Header("Receta")]
        public List<IngredientData> ingredientesRequeridos = new List<IngredientData>();

        [Header("Reglas")]
        public float tiempoLimite;
        public int puntos;
    }
}
