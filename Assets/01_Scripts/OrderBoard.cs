using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Tablero de comandas en la pared, sobre la estación de entrega.
    ///
    /// Va en el mundo y no pegado a la cámara a propósito: un HUD fijo en VR cansa la vista
    /// —el ojo no puede enfocar algo a distancia constante mientras el resto tiene profundidad
    /// real— y no se puede apartar la mirada de él. En la pared es además diegético, y está
    /// justo donde el jugador va a entregar.
    /// </summary>
    public class OrderBoard : MonoBehaviour
    {
        [SerializeField] private RectTransform contenedor;
        [SerializeField] private GameObject plantillaTicket;
        [SerializeField] private Text textoPuntos;
        [SerializeField] private Text textoVacio;

        [Header("Colores del tiempo")]
        [SerializeField] private Color colorTranquilo = new Color(0.35f, 0.85f, 0.4f);
        [SerializeField] private Color colorApurado = new Color(1f, 0.8f, 0.25f);
        [SerializeField] private Color colorCritico = new Color(0.9f, 0.25f, 0.2f);

        /// <summary>Filas fijas por ticket. Coincide con el maximo de elementos por pedido.</summary>
        public const int FilasPorTicket = 2;

        private readonly List<GameObject> tickets = new List<GameObject>();

        private void Awake()
        {
            if (plantillaTicket != null) plantillaTicket.SetActive(false);
        }

        public void Refrescar(IReadOnlyList<PedidoActivo> pedidos, int puntos)
        {
            if (contenedor == null || plantillaTicket == null) return;

            if (textoPuntos != null) textoPuntos.text = $"Puntos: {puntos}";
            if (textoVacio != null) textoVacio.gameObject.SetActive(pedidos.Count == 0);

            // Solo se reconstruye cuando cambia el número de pedidos. Rehacerlo cada frame
            // haría basura sin parar; el resto del tiempo basta con mover las barras.
            if (tickets.Count != pedidos.Count) Reconstruir(pedidos.Count);

            for (int i = 0; i < pedidos.Count && i < tickets.Count; i++)
            {
                Pintar(tickets[i], pedidos[i]);
            }
        }

        private void Reconstruir(int cuantos)
        {
            foreach (var t in tickets)
            {
                if (t != null) Destroy(t);
            }
            tickets.Clear();

            for (int i = 0; i < cuantos; i++)
            {
                GameObject ticket = Instantiate(plantillaTicket, contenedor);
                ticket.SetActive(true);
                tickets.Add(ticket);
            }
        }

        private static int Contar(List<DishData> lista, DishData plato)
        {
            int n = 0;
            foreach (var d in lista)
            {
                if (d == plato) n++;
            }
            return n;
        }

        private void Pintar(GameObject ticket, PedidoActivo pedido)
        {
            if (ticket == null) return;

            // Los pedidos son de 1 o 2 elementos, asi que la plantilla trae dos filas fijas
            // y se oculta la que sobre. Mas simple y barato que instanciarlas cada vez.
            var distintos = new List<DishData>();
            foreach (var plato in pedido.Todos)
            {
                if (plato != null && !distintos.Contains(plato)) distintos.Add(plato);
            }

            for (int fila = 0; fila < FilasPorTicket; fila++)
            {
                Transform t = ticket.transform.Find("Fila" + fila);
                if (t == null) continue;

                if (fila >= distintos.Count)
                {
                    t.gameObject.SetActive(false);
                    continue;
                }
                t.gameObject.SetActive(true);

                DishData plato = distintos[fila];
                int total = Contar(pedido.Todos, plato);
                int faltan = Contar(pedido.Pendientes, plato);
                bool hecho = faltan == 0;

                var icono = t.Find("Icono")?.GetComponent<Image>();
                if (icono != null)
                {
                    icono.sprite = plato.icono;
                    icono.enabled = plato.icono != null;
                    // Lo ya entregado se apaga para que se vea de un vistazo que esta hecho.
                    icono.color = hecho ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                }

                var etiqueta = t.Find("Texto")?.GetComponent<Text>();
                if (etiqueta != null)
                {
                    string marca = hecho ? "✓  " : "";
                    etiqueta.text = (total > 1)
                        ? $"{marca}{plato.nombre}   {total - faltan}/{total}"
                        : marca + plato.nombre;
                    etiqueta.color = hecho ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.12f, 0.12f, 0.12f);
                }
            }

            var barra = ticket.transform.Find("Barra/Relleno")?.GetComponent<Image>();
            if (barra != null)
            {
                float p = pedido.Progreso;
                barra.fillAmount = p;
                barra.color = (p > 0.5f) ? colorTranquilo : (p > 0.2f) ? colorApurado : colorCritico;
            }
        }
    }
}
