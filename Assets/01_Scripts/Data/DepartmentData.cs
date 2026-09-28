using System.Collections.Generic;
using UnityEngine;

namespace CocinaBoliviana.Data
{
    [CreateAssetMenu(fileName = "NewDepartment", menuName = "Cocina Boliviana/Departamento")]
    public class DepartmentData : ScriptableObject
    {
        [Header("Informacion General")]
        public string nombre;

        [Tooltip("Bandera del departamento. Se muestra en las paredes del ambiente cuando " +
                 "este es el nivel en curso.")]
        public Sprite bandera;

        [Header("Menu Tipico")]
        public List<DishData> comidas = new List<DishData>();
        public List<DishData> refrescos = new List<DishData>();

        [Header("Pedidos")]
        public List<OrderData> pedidos = new List<OrderData>();
    }
}
