using UnityEngine;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Un plato terminado, listo para llevar a entregar. Su modelo ya trae el plato incluido,
    /// así que es un objeto independiente: no va encima del plato de emplatado, lo sustituye.
    /// </summary>
    public class ServedDish : MonoBehaviour
    {
        [SerializeField] private DishData plato;

        /// <summary>Qué plato es. Lo leerá la entrega para saber si es el pedido correcto.</summary>
        public DishData Plato => plato;

        public void SetPlato(DishData valor) => plato = valor;
    }
}
