using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Vaso de plástico vacío. Se llena bajo el dispensador y, al terminar, se sustituye por
    /// el modelo del refresco servido, que ya viene con su propio vaso.
    ///
    /// El llenado se simula con un cilindro de líquido dentro del vaso que crece en Y. No
    /// hace falta modelar nada: se genera por código y se le pone el color de la bebida.
    /// </summary>
    public class DrinkCup : MonoBehaviour
    {
        [Tooltip("El líquido que crece dentro. Lo crea DrinksSetup.")]
        [SerializeField] private Transform liquido;

        [Tooltip("Altura del líquido lleno, en metros.")]
        [SerializeField] private float alturaLlena = 0.09f;

        [Tooltip("Radio del líquido, en metros. Un pelín menor que el interior del vaso.")]
        [SerializeField] private float radio = 0.03f;

        [Tooltip("Altura del fondo del vaso sobre su pivote, en metros. Súbelo si el líquido " +
                 "asoma por debajo del modelo.")]
        [SerializeField] private float alturaBase = 0.01f;

        private Renderer liquidoRenderer;
        private MaterialPropertyBlock bloque;
        private Rigidbody rb;
        private XRGrabInteractable grab;

        /// <summary>0 = vacío, 1 = lleno.</summary>
        public float Llenado { get; private set; }
        public bool Lleno => Llenado >= 1f;

        private void Awake()
        {
            if (liquido != null)
            {
                liquidoRenderer = liquido.GetComponent<Renderer>();
                bloque = new MaterialPropertyBlock();
            }
            SetLlenado(0f);

            rb = GetComponent<Rigidbody>();
            grab = GetComponent<XRGrabInteractable>();
            if (grab != null) grab.selectExited.AddListener(OnSoltado);

            // Se queda quieto donde lo dejes. Siendo dinamico rodaba por la mesa y se caia
            // del dispensador al menor roce.
            Fijar();
        }

        private void OnDestroy()
        {
            if (grab != null) grab.selectExited.RemoveListener(OnSoltado);
        }

        private void OnSoltado(SelectExitEventArgs args) => Fijar();

        private void Fijar()
        {
            if (rb == null) return;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        /// <summary>Le pone el color de la bebida que se está sirviendo.</summary>
        public void SetColor(Color color)
        {
            if (liquidoRenderer == null) return;

            liquidoRenderer.GetPropertyBlock(bloque);
            // Los tres nombres posibles segun el shader; ver IngredientItem para el porque.
            bloque.SetColor(BaseColorFactorId, color);
            bloque.SetColor(BaseColorId, color);
            bloque.SetColor(ColorId, color);
            liquidoRenderer.SetPropertyBlock(bloque);
        }

        public void SetLlenado(float valor)
        {
            Llenado = Mathf.Clamp01(valor);
            if (liquido == null) return;

            // Las medidas son METROS DE MUNDO, no locales: el vaso puede venir escalado
            // (el modelo actual esta a 0.1) y un radio local de 0.03 serian 3 mm.
            Vector3 lossy = transform.lossyScale;
            float ex = Mathf.Max(Mathf.Abs(lossy.x), 0.0001f);
            float ey = Mathf.Max(Mathf.Abs(lossy.y), 0.0001f);
            float ez = Mathf.Max(Mathf.Abs(lossy.z), 0.0001f);

            // El cilindro de Unity mide 2 unidades de alto, de ahi el medio.
            float alto = alturaLlena * Llenado;
            liquido.localScale = new Vector3(radio * 2f / ex, alto * 0.5f / ey, radio * 2f / ez);

            // Sube desde el fondo del vaso conforme se llena, en vez de crecer hacia los lados.
            liquido.localPosition = new Vector3(0f, (alturaBase + alto * 0.5f) / ey, 0f);
            liquido.gameObject.SetActive(Llenado > 0.01f);
        }

        private static readonly int BaseColorFactorId = Shader.PropertyToID("baseColorFactor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
    }
}
