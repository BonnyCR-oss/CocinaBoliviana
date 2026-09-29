using System.Collections.Generic;
using UnityEngine;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Olla, sartén o parrilla. Acepta los ingredientes que se le echen, los cocina y los
    /// quema si nadie los saca a tiempo.
    ///
    /// La olla y el sartén cocinan todo junto con un solo temporizador. La parrilla lleva
    /// uno por ingrediente, para que un sonso retirado a medias siga desde donde iba.
    ///
    /// Detecta por OverlapBox, igual que <see cref="CuttingBoard"/>: un trigger lo bastante
    /// grande para detectar acaba envolviendo a los ingredientes y les roba el rayo del
    /// control, dejándolos imposibles de agarrar.
    /// </summary>
    public class CookingVessel : MonoBehaviour
    {
        [Header("Tipo de Recipiente")]
        [Tooltip("La olla hierve, el sartén fríe, la parrilla asa. Solo acepta ingredientes que admitan este método.")]
        [SerializeField] private MetodoCoccion metodo = MetodoCoccion.Hervir;

        [Tooltip("Apagado: un solo temporizador para todo (olla, sartén). Encendido: cada " +
                 "ingrediente se cocina a su ritmo (parrilla).")]
        [SerializeField] private bool temporizadorPorIngrediente;

        [Header("Zona de Detección")]
        [Tooltip("Caja (en METROS de mundo) sobre el recipiente donde se detecta un ingrediente.")]
        [SerializeField] private Vector3 zonaDeteccion = new Vector3(0.22f, 0.22f, 0.22f);

        [Tooltip("Altura del centro de esa caja sobre el recipiente, en metros.")]
        [SerializeField] private float zonaAltura = 0.10f;

        [Tooltip("Desplazamiento horizontal de esa caja, en metros de mundo. Hace falta cuando " +
                 "el pivote del modelo no está en el centro (la parrilla).")]
        [SerializeField] private Vector3 zonaDesplazamiento;

        [Header("Contenido")]
        [SerializeField] private int capacidad = 4;

        [Tooltip("Dónde se colocan los ingredientes dentro del recipiente.")]
        [SerializeField] private Transform puntoContenido;

        [Tooltip("Giro extra al colocar cada ingrediente, sobre el de 'Punto Contenido'. " +
                 "En la parrilla tumba el sonso, que viene de pie en su palito.")]
        [SerializeField] private Vector3 rotacionAlColocar;

        [Tooltip("Apoya el ingrediente POR SU MALLA sobre el punto: centrado y con la base " +
                 "tocándolo. Hace falta cuando el pivote del modelo está en una punta.")]
        [SerializeField] private bool apoyarSobrePunto;

        [Header("Referencias")]
        [SerializeField] private CookingCounter contador;

        [Tooltip("Fuego de la hornalla. Se enciende solo mientras haya algo cocinándose.")]
        [SerializeField] private BurnerFlame fuego;

        [Tooltip("Humo, brasas y chisporroteo de la parrilla. Opcional.")]
        [SerializeField] private GrillEffects efectos;

        private readonly List<IngredientItem> contenido = new List<IngredientItem>();
        private float tiempoAcumulado;

        /// <summary>Solo con temporizador por ingrediente: segundos que lleva cada uno.</summary>
        private readonly Dictionary<IngredientItem, float> tiempos = new Dictionary<IngredientItem, float>();

        private Vector3 CentroZona => transform.position + zonaDesplazamiento + Vector3.up * zonaAltura;

        public MetodoCoccion Metodo => metodo;
        public int Cantidad => contenido.Count;
        public bool EstaLleno => contenido.Count >= capacidad;

        private void Awake()
        {
            if (puntoContenido == null) puntoContenido = transform;
            if (fuego != null) fuego.SetEncendido(false);
            if (efectos != null) efectos.SetCocinando(false, 0f);
            if (contador != null) contador.Ocultar();
        }

        private void FixedUpdate()
        {
            RecogerIngredientes();
            Cocinar();
        }

        /// <summary>
        /// Cada cuanto se sondea la fisica. A 50/s (cada FixedUpdate) esto alocaba un array
        /// nuevo 50 veces por segundo POR componente, y con varias estaciones a la vez se
        /// notaba en los FPS. 10/s es imperceptible para dejar un objeto encima.
        /// </summary>
        private const float IntervaloSondeo = 0.1f;

        private float proximoSondeo;

        private void RecogerIngredientes()
        {
            if (Time.time < proximoSondeo) return;
            proximoSondeo = Time.time + IntervaloSondeo;

            if (EstaLleno) return;

            Vector3 centro = CentroZona;
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
            item = TransformarAlEntrar(item);

            contenido.Add(item);
            item.SetMetodoCoccion(metodo);

            if (temporizadorPorIngrediente)
            {
                // Sigue desde donde iba: sacar un sonso a mirarlo no debe reiniciarlo.
                tiempos[item] = SegundosDesdeProgreso(item.Data, item.ProgresoCoccion);
            }

            // Se apilan hacia arriba para que se vean varios y no uno solo tapando al resto.
            Vector3 pos = puntoContenido.position + Vector3.up * (0.03f * (contenido.Count - 1));
            item.SnapToCookingVessel(this, pos, puntoContenido.rotation * Quaternion.Euler(rotacionAlColocar));
            if (apoyarSobrePunto) Apoyar(item, pos);

            Debug.Log($"[CookingVessel] {name}: entra {item.IngredientName} ({contenido.Count}/{capacidad}).");
        }

        /// <summary>
        /// Si el ingrediente tiene otra forma para este método (huevo con cáscara → huevo
        /// estrellado en el sartén), lo cambia por ese prefab. El nuevo hereda el dato y el
        /// corte, así que para las recetas sigue siendo "Huevo" y se valida igual que antes.
        /// </summary>
        private IngredientItem TransformarAlEntrar(IngredientItem item)
        {
            if (item.Data == null || item.YaTransformado) return item;

            GameObject prefab = item.Data.ObtenerPrefabParaCoccion(metodo);
            if (prefab == null) return item;

            GameObject go = Instantiate(prefab, item.transform.position, item.transform.rotation);
            go.name = prefab.name;

            var nuevo = go.GetComponent<IngredientItem>();
            if (nuevo == null)
            {
                Debug.LogWarning($"[CookingVessel] '{prefab.name}' no tiene IngredientItem; " +
                                 $"{item.IngredientName} entra sin transformarse. Corre 'Setup Ingredient Prefabs'.");
                Destroy(go);
                return item;
            }

            nuevo.SetData(item.Data);
            nuevo.SetCorteActual(item.CorteActual);
            nuevo.MarcarTransformado();

            // Desactivar antes de destruir: el siguiente sondeo lo vería aún y lo metería
            // otra vez.
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);

            Debug.Log($"[CookingVessel] {name}: {prefab.name} sustituye a {item.name} al {metodo}.");
            return nuevo;
        }

        /// <summary>
        /// Mueve el ingrediente para que el centro de su malla quede sobre <paramref name="punto"/>
        /// y su parte más baja justo a esa altura. Con el pivote del sonso en la punta del
        /// palito, colocarlo por el pivote lo dejaba medio fuera de la rejilla.
        /// </summary>
        private static void Apoyar(IngredientItem item, Vector3 punto)
        {
            var renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            item.transform.position += new Vector3(punto.x - b.center.x, punto.y - b.min.y, punto.z - b.center.z);
        }

        /// <summary>La saca el ingrediente al ser agarrado, igual que hace la tabla de cortar.</summary>
        public void ReleaseIngredient(IngredientItem item)
        {
            if (!contenido.Remove(item)) return;

            tiempos.Remove(item);

            Debug.Log($"[CookingVessel] {name}: sale {item.IngredientName} ({contenido.Count}/{capacidad}).");

            if (contenido.Count == 0)
            {
                tiempoAcumulado = 0f;
                if (contador != null) contador.Ocultar();
                if (fuego != null) fuego.SetEncendido(false);
                if (efectos != null) efectos.SetCocinando(false, 0f);
            }
        }

        private void Cocinar()
        {
            // Limpia lo que haya sido destruido por fuera (por ejemplo al tirarlo a la basura).
            if (contenido.RemoveAll(i => i == null) > 0) LimpiarTiemposHuerfanos();

            if (contenido.Count == 0)
            {
                if (fuego != null && fuego.Encendido) fuego.SetEncendido(false);
                if (efectos != null && efectos.Cocinando) efectos.SetCocinando(false, 0f);
                return;
            }

            if (fuego != null && !fuego.Encendido) fuego.SetEncendido(true);

            float progresoMostrado = temporizadorPorIngrediente ? CocinarPorSeparado() : CocinarJuntos();
            if (progresoMostrado < 0f) return;

            if (efectos != null) efectos.SetCocinando(true, progresoMostrado);
            if (contador != null) contador.Mostrar(progresoMostrado, contenido.Count);
        }

        /// <summary>
        /// Un solo temporizador para toda la olla, como en Overcooked. El objetivo es el
        /// ingrediente MÁS lento: así nada sale crudo por acompañar a algo rápido.
        /// </summary>
        private float CocinarJuntos()
        {
            float objetivo = 0f;
            float margen = 0f;
            foreach (var item in contenido)
            {
                if (item.Data == null) continue;
                objetivo = Mathf.Max(objetivo, item.Data.tiempoCoccion);
                margen = Mathf.Max(margen, item.Data.margenAntesDeQuemarse);
            }
            if (objetivo <= 0f) return -1f;

            tiempoAcumulado += Time.fixedDeltaTime;
            float progreso = Progreso(tiempoAcumulado, objetivo, margen);

            foreach (var item in contenido)
            {
                item.SetProgresoCoccion(progreso);
            }
            return progreso;
        }

        /// <summary>
        /// Cada ingrediente con su reloj. El cartel muestra el MÁS avanzado, que es el que
        /// está más cerca de quemarse y por tanto el que el jugador tiene que vigilar.
        /// </summary>
        private float CocinarPorSeparado()
        {
            float masAvanzado = -1f;

            foreach (var item in contenido)
            {
                if (item.Data == null || item.Data.tiempoCoccion <= 0f) continue;

                tiempos.TryGetValue(item, out float t);
                t += Time.fixedDeltaTime;
                tiempos[item] = t;

                float progreso = Progreso(t, item.Data.tiempoCoccion, item.Data.margenAntesDeQuemarse);
                item.SetProgresoCoccion(progreso);
                masAvanzado = Mathf.Max(masAvanzado, progreso);
            }

            return masAvanzado;
        }

        /// <summary>0..1 cocinándose, 1..2 camino al carbón.</summary>
        private static float Progreso(float segundos, float objetivo, float margen)
        {
            return segundos <= objetivo
                ? segundos / objetivo
                : 1f + Mathf.Clamp01((segundos - objetivo) / Mathf.Max(margen, 0.01f));
        }

        /// <summary>La inversa de <see cref="Progreso"/>, para retomar una cocción a medias.</summary>
        private static float SegundosDesdeProgreso(IngredientData data, float progreso)
        {
            if (data == null || progreso <= 0f) return 0f;
            if (progreso <= 1f) return progreso * data.tiempoCoccion;
            return data.tiempoCoccion + (progreso - 1f) * data.margenAntesDeQuemarse;
        }

        private readonly List<IngredientItem> huerfanos = new List<IngredientItem>();

        private void LimpiarTiemposHuerfanos()
        {
            huerfanos.Clear();
            foreach (var clave in tiempos.Keys)
            {
                if (clave == null) huerfanos.Add(clave);
            }
            foreach (var h in huerfanos) tiempos.Remove(h);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(CentroZona, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, zonaDeteccion);
        }
    }
}
