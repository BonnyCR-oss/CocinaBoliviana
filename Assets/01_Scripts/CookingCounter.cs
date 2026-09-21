using UnityEngine;
using UnityEngine.UI;

namespace CocinaBoliviana
{
    /// <summary>
    /// Cartel flotante sobre una olla o un sartén: barra de progreso y estado.
    /// No sabe cocinar; solo muestra lo que le pasa <see cref="CookingVessel"/>.
    /// </summary>
    public class CookingCounter : MonoBehaviour
    {
        [SerializeField] private GameObject raiz;
        [SerializeField] private Image barra;
        [SerializeField] private Text etiqueta;

        [Header("Colores de la barra")]
        [SerializeField] private Color colorCocinando = new Color(1f, 0.75f, 0.2f);
        [SerializeField] private Color colorListo = new Color(0.3f, 0.85f, 0.35f);
        [SerializeField] private Color colorQuemado = new Color(0.85f, 0.2f, 0.15f);

        private Camera camaraJugador;

        private void Awake()
        {
            Ocultar();
        }

        public void Ocultar()
        {
            if (raiz != null) raiz.SetActive(false);
        }

        /// <summary>
        /// <paramref name="progreso"/> va de 0 a 2: hasta 1 se está cocinando, de 1 a 2 se
        /// acerca a quemarse. Por eso la barra se rellena dos veces con colores distintos,
        /// en vez de quedarse llena y no decir nada mientras se arruina la comida.
        /// </summary>
        public void Mostrar(float progreso, int cantidadIngredientes)
        {
            if (raiz == null) return;
            raiz.SetActive(true);

            if (progreso >= 2f)
            {
                SetBarra(1f, colorQuemado);
                SetTexto("SE QUEMÓ");
            }
            else if (progreso >= 1f)
            {
                // Segunda vuelta: la barra baja mientras se acerca al carbón.
                SetBarra(1f - (progreso - 1f), colorListo);
                SetTexto("LISTO");
            }
            else
            {
                SetBarra(progreso, colorCocinando);
                SetTexto($"Cocinando  {Mathf.RoundToInt(progreso * 100f)}%   ({cantidadIngredientes})");
            }

            MirarAlJugador();
        }

        private void SetBarra(float valor, Color color)
        {
            if (barra == null) return;
            barra.fillAmount = Mathf.Clamp01(valor);
            barra.color = color;
        }

        private void SetTexto(string texto)
        {
            if (etiqueta != null) etiqueta.text = texto;
        }

        private void MirarAlJugador()
        {
            if (camaraJugador == null) camaraJugador = Camera.main;
            if (camaraJugador == null) return;

            // Solo gira en horizontal: si siguiera también la altura, el cartel se
            // inclinaría hacia atrás cada vez que el jugador se agacha sobre la olla.
            Vector3 dir = raiz.transform.position - camaraJugador.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                raiz.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}
