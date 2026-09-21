using System.Collections.Generic;
using UnityEngine;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Olla o sartén. Acepta los ingredientes que se le echen, los cocina juntos con un solo
    /// temporizador y los quema si nadie los saca a tiempo.
    ///
    /// Detecta por OverlapBox, igual que <see cref="CuttingBoard"/>: un trigger lo bastante
    /// grande para detectar acaba envolviendo a los ingredientes y les roba el rayo del
    /// control, dejándolos imposibles de agarrar.
    /// </summary>
    public class CookingVessel : MonoBehaviour
    {
        [Header("Tipo de Recipiente")]
        [Tooltip("La olla hierve, el sartén fríe. Solo acepta ingredientes que admitan este método.")]
        [SerializeField] private MetodoCoccion metodo = MetodoCoccion.Hervir;

        [Header("Zona de Detección")]
        [Tooltip("Caja (en METROS de mundo) sobre el recipiente donde se detecta un ingrediente.")]
        [SerializeField] private Vector3 zonaDeteccion = new Vector3(0.22f, 0.22f, 0.22f);

        [Tooltip("Altura del centro de esa caja sobre el recipiente, en metros.")]
        [SerializeField] private float zonaAltura = 0.10f;

        [Header("Contenido")]
        [SerializeField] private int capacidad = 4;

        [Tooltip("Dónde se apilan los ingredientes dentro del recipiente.")]
        [SerializeField] private Transform puntoContenido;

        [Header("Referencias")]
        [SerializeField] private CookingCounter contador;

        [Tooltip("Fuego de la hornalla. Se enciende solo mientras haya algo cocinándose.")]
        [SerializeField] private BurnerFlame fuego;

        private readonly List<IngredientItem> contenido = new List<IngredientItem>();
        private float tiempoAcumulado;

        public MetodoCoccion Metodo => metodo;
        public int Cantidad => contenido.Count;
        public bool EstaLleno => contenido.Count >= capacidad;

        private void Awake()
        {
            if (puntoContenido == null) puntoContenido = transform;
            if (fuego != null) fuego.SetEncendido(false);
            if (contador != null) contador.Ocultar();
        }

        private void FixedUpdate()
        {
            RecogerIngredientes();
            Cocinar();
        }

        private void RecogerIngredientes()
        {
            if (EstaLleno) return;

            Vector3 centro = transform.position + Vector3.up * zonaAltura;
            Collider[] dentro = Physics.OverlapBox(centro, zonaDeteccion * 0.5f, transform.rotation,
                                                   ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in dentro)
            {
                var item = col.GetComponentInParent<IngredientItem>();
                if (item == null || contenido.Contains(item)) continue;

                // Mientras el jugador lo sostenga no se le quita de la mano.
                if (item.GrabInteractable != null && item.GrabInteractable.isSelected) continue;

                if (item.Data == null || !item.Data.AdmiteCoccion(metodo))
                {
                    // Sin este filtro cualquier cosa caería dentro y se "cocinaría".
                    continue;
                }

                Agregar(item);
                if (EstaLleno) return;
            }
        }

        private void Agregar(IngredientItem item)
        {
            contenido.Add(item);
            item.SetMetodoCoccion(metodo);

            // Se apilan hacia arriba para que se vean varios y no uno solo tapando al resto.
            Vector3 pos = puntoContenido.position + Vector3.up * (0.03f * (contenido.Count - 1));
            item.SnapToCookingVessel(this, pos, puntoContenido.rotation);

            Debug.Log($"[CookingVessel] {name}: entra {item.IngredientName} ({contenido.Count}/{capacidad}).");
        }

        /// <summary>La saca el ingrediente al ser agarrado, igual que hace la tabla de cortar.</summary>
        public void ReleaseIngredient(IngredientItem item)
        {
            if (!contenido.Remove(item)) return;

            Debug.Log($"[CookingVessel] {name}: sale {item.IngredientName} ({contenido.Count}/{capacidad}).");

            if (contenido.Count == 0)
            {
                tiempoAcumulado = 0f;
                if (contador != null) contador.Ocultar();
                if (fuego != null) fuego.SetEncendido(false);
            }
        }

        private void Cocinar()
        {
            // Limpia lo que haya sido destruido por fuera (por ejemplo al tirarlo a la basura).
            contenido.RemoveAll(i => i == null);

            if (contenido.Count == 0)
            {
                if (fuego != null && fuego.Encendido) fuego.SetEncendido(false);
                return;
            }

            if (fuego != null && !fuego.Encendido) fuego.SetEncendido(true);

            // Un solo temporizador para toda la olla, como en Overcooked. El objetivo es el
            // ingrediente MÁS lento: así nada sale crudo por acompañar a algo rápido.
            float objetivo = 0f;
            float margen = 0f;
            foreach (var item in contenido)
            {
                if (item.Data == null) continue;
                objetivo = Mathf.Max(objetivo, item.Data.tiempoCoccion);
                margen = Mathf.Max(margen, item.Data.margenAntesDeQuemarse);
            }
            if (objetivo <= 0f) return;

            tiempoAcumulado += Time.fixedDeltaTime;

            // 0..1 cocinándose, 1..2 camino al carbón.
            float progreso = tiempoAcumulado <= objetivo
                ? tiempoAcumulado / objetivo
                : 1f + Mathf.Clamp01((tiempoAcumulado - objetivo) / Mathf.Max(margen, 0.01f));

            foreach (var item in contenido)
            {
                item.SetProgresoCoccion(progreso);
            }

            if (contador != null) contador.Mostrar(progreso, contenido.Count);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * zonaAltura, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, zonaDeteccion);
        }
    }
}
