using UnityEngine;

namespace CocinaBoliviana
{
    /// <summary>
    /// Sitio donde aparecen los platos terminados, listos para recoger y llevar a entregar.
    ///
    /// Se pone en la mesa de al lado de la de emplatado: así el plato servido nunca se
    /// confunde con los platos blancos vacíos que andan por el mostrador.
    /// </summary>
    public class DishPickupPoint : MonoBehaviour
    {
        [Tooltip("Separación entre platos si se terminan varios seguidos, en metros.")]
        [SerializeField] private float separacion = 0.28f;

        [Tooltip("Cuántos caben en fila antes de empezar a amontonarse encima.")]
        [SerializeField] private int porFila = 3;

        private int entregados;

        /// <summary>
        /// Busca el punto en la escena. Se cachea porque <see cref="PlateItem"/> lo pide
        /// justo al destruirse y no conviene barrer la escena en ese momento.
        /// </summary>
        private static DishPickupPoint cache;

        public static DishPickupPoint Encontrar()
        {
            if (cache == null) cache = FindAnyObjectByType<DishPickupPoint>();
            return cache;
        }

        /// <summary>
        /// Dónde dejar el siguiente plato. Se van colocando en fila para que no salgan
        /// todos dentro del mismo hueco si preparas varios.
        /// </summary>
        public Vector3 SiguienteSitio()
        {
            int i = entregados++;
            int columna = i % Mathf.Max(porFila, 1);
            int fila = i / Mathf.Max(porFila, 1);

            return transform.position
                 + transform.right * (columna * separacion)
                 + transform.forward * (fila * separacion);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.4f, 0.6f);
            for (int i = 0; i < porFila; i++)
            {
                Gizmos.DrawWireSphere(transform.position + transform.right * (i * separacion), 0.06f);
            }
        }
    }
}
