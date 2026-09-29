using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CocinaBoliviana
{
    /// <summary>
    /// Cartel flotante sobre un plato: qué receta va saliendo, qué le falta y un botón
    /// para retirar todo lo emplatado.
    /// No decide nada; solo muestra lo que le pasa <see cref="PlateItem"/>.
    /// </summary>
    public class PlateCounter : MonoBehaviour
    {
        [SerializeField] private GameObject raiz;
        [SerializeField] private Text titulo;
        [SerializeField] private Text faltantes;

        [SerializeField] private Color colorEnProgreso = new Color(1f, 0.85f, 0.35f);
        [SerializeField] private Color colorCompleto = new Color(0.35f, 0.9f, 0.4f);
        [SerializeField] private Color colorSinReceta = new Color(0.95f, 0.45f, 0.35f);

        [Header("Botón Retirar todo")]
        [Tooltip("Vacío = se crea solo al arrancar, debajo de los textos del cartel.")]
        [SerializeField] private Button botonRetirar;

        [Tooltip("Alto extra que se le da al cartel para que quepa el botón, en píxeles del canvas.")]
        [SerializeField] private float altoBoton = 64f;

        private Camera camaraJugador;
        private Action accionRetirar;

        private void Awake()
        {
            if (botonRetirar == null) CrearBotonRetirar();
            Ocultar();
        }

        public void Ocultar()
        {
            if (raiz != null) raiz.SetActive(false);
        }

        /// <summary>Qué hacer al pulsar 'Retirar todo'. Lo engancha el plato.</summary>
        public void SetAccionRetirar(Action accion) => accionRetirar = accion;

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

        /// <summary>
        /// Hay comida en el plato pero no se parece a ninguna receta (un sonso, algo crudo
        /// que debía ir frito...). Se muestra igual para que se vea el botón de retirar.
        /// </summary>
        public void MostrarSinReceta(int cantidad)
        {
            if (raiz == null) return;
            raiz.SetActive(true);

            if (titulo != null)
            {
                titulo.text = "Sin receta";
                titulo.color = colorSinReceta;
            }

            if (faltantes != null)
            {
                faltantes.text = (cantidad == 1)
                    ? "Ese ingrediente no forma ningún plato"
                    : $"Estos {cantidad} ingredientes no forman ningún plato";
            }

            MirarAlJugador();
        }

        private void AlPulsarRetirar()
        {
            accionRetirar?.Invoke();
        }

        /// <summary>
        /// Se construye por código para no tener que rehacer el prefab del plato: el cartel
        /// crece por abajo y los dos textos se reparten la parte de arriba.
        /// </summary>
        private void CrearBotonRetirar()
        {
            if (raiz == null) return;

            // El canvas necesita el raycaster de XR para que el rayo del mando pulse el botón.
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }

            var canvasRt = (canvas != null) ? canvas.GetComponent<RectTransform>() : null;
            float altoOriginal = (canvasRt != null) ? canvasRt.sizeDelta.y : 130f;
            if (canvasRt != null) canvasRt.sizeDelta = new Vector2(canvasRt.sizeDelta.x, altoOriginal + altoBoton);

            // Fracción del alto total que ocupa el botón, para recolocar los textos encima.
            float total = altoOriginal + altoBoton;
            float corte = altoBoton / total;
            if (titulo != null) Reanclar(titulo.rectTransform, corte + (1f - corte) * 0.5f, 1f);
            if (faltantes != null) Reanclar(faltantes.rectTransform, corte, corte + (1f - corte) * 0.5f);

            var go = new GameObject("Btn_RetirarTodo", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(raiz.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.18f, 0f);
            rt.anchorMax = new Vector2(0.82f, corte);
            rt.offsetMin = new Vector2(0f, 8f);
            rt.offsetMax = new Vector2(0f, -4f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.72f, 0.2f, 0.16f, 0.95f);

            botonRetirar = go.GetComponent<Button>();
            var colores = botonRetirar.colors;
            colores.highlightedColor = new Color(1f, 0.75f, 0.7f);
            colores.pressedColor = new Color(0.6f, 0.6f, 0.6f);
            botonRetirar.colors = colores;
            botonRetirar.targetGraphic = img;

            var textoGo = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            textoGo.transform.SetParent(go.transform, false);
            var trt = textoGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var t = textoGo.GetComponent<Text>();
            t.text = "Retirar todo  (-pts)";
            t.font = (titulo != null && titulo.font != null) ? titulo.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;

            botonRetirar.onClick.AddListener(AlPulsarRetirar);
        }

        private static void Reanclar(RectTransform rt, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(rt.anchorMin.x, yMin);
            rt.anchorMax = new Vector2(rt.anchorMax.x, yMax);
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
