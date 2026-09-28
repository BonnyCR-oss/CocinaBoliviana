using UnityEngine;
using UnityEngine.UI;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Bandera colgada en una pared del ambiente. Muestra la del departamento del nivel en
    /// curso, así que la misma escena sirve para Cochabamba, La Paz o Santa Cruz sin tocar
    /// nada: basta con cambiar el departamento del OrderManager.
    ///
    /// El sprite vive en el DepartmentData, no aquí: así cada departamento lleva el suyo y
    /// añadir un cuarto no obliga a editar ningún componente de la escena.
    /// </summary>
    public class WallFlag : MonoBehaviour
    {
        [SerializeField] private Image imagen;

        [Tooltip("Solo para previsualizar en el editor cuando no hay OrderManager todavía.")]
        [SerializeField] private DepartmentData departamentoDePrueba;

        [Tooltip("Cada cuánto se comprueba si cambió el departamento, en segundos.")]
        [SerializeField] private float intervaloComprobacion = 1f;

        private DepartmentData ultimoPintado;
        private float proximaComprobacion;

        private void Start() => Refrescar();

        private void Update()
        {
            // El nivel no cambia a media partida, asi que comprobar una vez por segundo
            // sobra y evita hacer trabajo por frame para nada.
            if (Time.time < proximaComprobacion) return;
            proximaComprobacion = Time.time + intervaloComprobacion;

            Refrescar();
        }

        private void Refrescar()
        {
            DepartmentData depto = DepartamentoActual();
            if (depto == ultimoPintado) return;

            ultimoPintado = depto;
            if (imagen == null) return;

            Sprite bandera = (depto != null) ? depto.bandera : null;
            imagen.sprite = bandera;

            // Sin bandera se oculta entera: mejor una pared limpia que un cuadro blanco.
            imagen.enabled = bandera != null;

            if (depto != null && bandera == null)
            {
                Debug.LogWarning($"[WallFlag] '{depto.nombre}' no tiene bandera asignada; " +
                                 "ponle el sprite en su DepartmentData.");
            }
        }

        private DepartmentData DepartamentoActual()
        {
            var manager = OrderManager.Instancia;
            if (manager != null && manager.Departamento != null) return manager.Departamento;
            return departamentoDePrueba;
        }
    }
}
