using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CocinaBoliviana
{
    /// <summary>
    /// Cartel flotante sobre un plato: qué receta va saliendo y qué le falta.
    /// No decide nada; solo muestra lo que le pasa <see cref="PlateItem"/>.
    /// </summary>
    public class PlateCounter : MonoBehaviour
    {
        [SerializeField] private GameObject raiz;
        [SerializeField] private Text titulo;
        [SerializeField] private Text faltantes;

        [SerializeField] private Color colorEnProgreso = new Color(1f, 0.85f, 0.35f);
        [SerializeField] private Color colorCompleto = new Color(0.35f, 0.9f, 0.4f);

        private Camera camaraJugador;

        private void Awake() => Ocultar();

        public void Ocultar()
        {
            if (raiz != null) raiz.SetActive(false);
        }

        public void Mostrar(string nombrePlato, int puestos, int total, List<string> faltan)
        {
            if (raiz == null) return;
            raiz.SetActive(true);

            bool completo = faltan == null || faltan.Count == 0;

            if (titulo != null)
            {
                titulo.text = $"{nombrePlato}   {puestos}/{total}";
                titulo.color = completo ? colorCompleto : colorEnProgreso;
            }

            if (faltantes != null)
            {
                faltantes.text = completo ? "¡Completo!" : "Falta: " + string.Join(", ", faltan);
            }

            MirarAlJugador();
        }

        private void MirarAlJugador()
        {
            if (camaraJugador == null) camaraJugador = Camera.main;
            if (camaraJugador == null) return;

            // Solo gira en horizontal: el plato se lleva en la mano y se mueve mucho, y si
            // el cartel siguiera también la altura quedaría cabeceando todo el rato.
            Vector3 dir = raiz.transform.position - camaraJugador.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                raiz.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}
