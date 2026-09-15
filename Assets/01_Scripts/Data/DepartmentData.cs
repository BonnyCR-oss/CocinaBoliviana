using System.Collections.Generic;
using UnityEngine;

namespace CocinaBoliviana.Data
{
    [CreateAssetMenu(fileName = "NewDepartment", menuName = "Cocina Boliviana/Departamento")]
    public class DepartmentData : ScriptableObject
    {
        [Header("Informacion General")]
        public string nombre;

        [Header("Menu Tipico")]
        public List<DishData> comidas = new List<DishData>();
        public List<DishData> refrescos = new List<DishData>();

        [Header("Pedidos")]
        public List<OrderData> pedidos = new List<OrderData>();
    }
}
