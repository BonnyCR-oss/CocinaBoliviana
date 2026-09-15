using System.Collections.Generic;
using UnityEngine;

namespace CocinaBoliviana.Data
{
    [CreateAssetMenu(fileName = "NewOrder", menuName = "Cocina Boliviana/Pedido")]
    public class OrderData : ScriptableObject
    {
        [Header("Contenido del Pedido")]
        public List<DishData> platosSolicitados = new List<DishData>();

        [Header("Reglas")]
        public float tiempoLimite;
        public int recompensa;
    }
}
