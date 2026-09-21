using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PlateItem : MonoBehaviour
    {
        [Header("Plate Settings")]
        [SerializeField] private Transform foodSnapPoint;
        [SerializeField] private AudioClip plateSound;

        [Header("Recetas")]
        [Tooltip("Platos que este plato puede llegar a ser. Los rellena PlatingSetup con " +
                 "todos los DishData del proyecto.")]
        [SerializeField] private List<DishData> recetasConocidas = new List<DishData>();
        [SerializeField] private PlateCounter contador;

        [Header("Disposición")]
        [Tooltip("Radio máximo, en metros, del círculo donde se reparte la comida.")]
        [SerializeField] private float radioMaximo = 0.10f;

        private readonly List<IngredientItem> platedIngredients = new List<IngredientItem>();
        private AudioSource audioSource;
        private XRGrabInteractable grabInteractable;
        private Rigidbody plateRb;

        private DishData platoCompletado;

        public IReadOnlyList<IngredientItem> PlatedIngredients => platedIngredients;
        public bool HasFood => platedIngredients.Count > 0;

        /// <summary>El plato terminado, o null si aun se esta armando. Lo mirara la entrega.</summary>
        public DishData PlatoCompletado => platoCompletado;
        public bool EstaCompleto => platoCompletado != null;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            plateRb = GetComponent<Rigidbody>();

            // El plato se queda donde lo dejes. Siendo dinamico, cualquier roce al pasar
            // lo empujaba por el mostrador mientras intentabas emplatar.
            Fijar();
            if (grabInteractable != null) grabInteractable.selectExited.AddListener(OnSoltado);
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            if (foodSnapPoint == null)
            {
                Transform existingSnap = transform.Find("FoodSnapPoint");
                if (existingSnap != null)
                {
                    foodSnapPoint = existingSnap;
                }
                else
                {
                    GameObject snapGo = new GameObject("FoodSnapPoint");
                    snapGo.transform.SetParent(transform, false);
                    snapGo.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                    foodSnapPoint = snapGo.transform;
                }
            }
        }

        private void OnDestroy()
        {
            if (grabInteractable != null) grabInteractable.selectExited.RemoveListener(OnSoltado);
        }

        private void OnSoltado(SelectExitEventArgs args) => Fijar();

        private void Fijar()
        {
            if (plateRb == null) return;
            plateRb.linearVelocity = Vector3.zero;
            plateRb.angularVelocity = Vector3.zero;
            plateRb.isKinematic = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (EstaCompleto) return; // ya es un plato servido, no un plato en construccion

            IngredientItem ingredient = other.GetComponentInParent<IngredientItem>();
            if (ingredient != null && !platedIngredients.Contains(ingredient))
            {
                // Only snap if the player has let go of the ingredient (not selected)
                if (ingredient.GrabInteractable == null || !ingredient.GrabInteractable.isSelected)
                {
                    AddIngredient(ingredient);
                }
            }
        }

        public void AddIngredient(IngredientItem ingredient)
        {
            if (ingredient == null || platedIngredients.Contains(ingredient) || EstaCompleto) return;

            platedIngredients.Add(ingredient);

            Quaternion snapRot = (foodSnapPoint != null) ? foodSnapPoint.rotation : Quaternion.identity;
            ingredient.SnapToPlate(this, PosicionEnPlato(platedIngredients.Count - 1, platedIngredients.Count), snapRot);
            Reacomodar();
            EvaluarReceta();

            if (audioSource != null && plateSound != null)
            {
                audioSource.PlayOneShot(plateSound, 0.7f);
            }

            Debug.Log($"[PlateItem] Ingrediente {ingredient.IngredientName} emplatado exitosamente. Total en plato: {platedIngredients.Count}");
        }

        /// <summary>
        /// Reparte la comida en círculo sobre el plato. Antes se apilaba en vertical, lo que
        /// con 6 ingredientes daba una torre de 12 cm en vez de algo servido.
        /// </summary>
        private Vector3 PosicionEnPlato(int indice, int total)
        {
            Vector3 centro = (foodSnapPoint != null)
                ? foodSnapPoint.position
                : transform.position + Vector3.up * 0.035f;

            if (total <= 1) return centro;

            // El círculo crece con la cantidad: con dos cosas quedan juntas, con seis se
            // abren para no montarse del todo.
            float radio = Mathf.Min(radioMaximo, 0.02f + total * 0.012f);
            float angulo = (indice / (float)total) * Mathf.PI * 2f;

            Vector3 desplazamiento = transform.right * Mathf.Cos(angulo) * radio
                                   + transform.forward * Mathf.Sin(angulo) * radio;
            return centro + desplazamiento;
        }

        private void Reacomodar()
        {
            int total = platedIngredients.Count;
            Quaternion rot = (foodSnapPoint != null) ? foodSnapPoint.rotation : Quaternion.identity;

            for (int i = 0; i < total; i++)
            {
                var item = platedIngredients[i];
                if (item == null) continue;
                item.transform.SetPositionAndRotation(PosicionEnPlato(i, total), rot);
            }
        }

        /// <summary>
        /// Deduce qué receta está saliendo: la que más ingredientes coincidentes tenga. No
        /// hace falta el sistema de pedidos para esto, el plato se da cuenta solo.
        /// </summary>
        private void EvaluarReceta()
        {
            platedIngredients.RemoveAll(i => i == null);

            if (contador == null) return;

            if (platedIngredients.Count == 0 || recetasConocidas == null || recetasConocidas.Count == 0)
            {
                contador.Ocultar();
                return;
            }

            DishData mejor = null;
            List<string> mejorFaltan = null;
            int mejorAciertos = -1;

            foreach (var plato in recetasConocidas)
            {
                if (plato == null || plato.receta == null || plato.receta.Count == 0) continue;

                // Copia para ir tachando: así una receta que pide dos papas necesita dos.
                var pendientes = new List<IngredienteRequerido>(plato.receta);
                int aciertos = 0;

                foreach (var item in platedIngredients)
                {
                    if (item.Data == null) continue;

                    // Se busca el requisito que cumpla ESTE ingrediente con su estado exacto:
                    // corte, cocción y método. Una papa cruda no tacha "papa frita".
                    int idx = pendientes.FindIndex(r => r != null && r.LoCumple(
                        item.Data, item.CorteActual, item.EstadoCoccion, item.MetodoCoccionUsado));

                    if (idx < 0) continue;
                    pendientes.RemoveAt(idx);
                    aciertos++;
                }

                // A igualdad de aciertos gana la receta más corta: es la más alcanzable.
                bool mejora = aciertos > mejorAciertos
                           || (aciertos == mejorAciertos && mejor != null
                               && plato.receta.Count < mejor.receta.Count);

                if (!mejora) continue;

                mejorAciertos = aciertos;
                mejor = plato;
                mejorFaltan = new List<string>();
                foreach (var falta in pendientes)
                {
                    if (falta != null) mejorFaltan.Add(falta.Describir());
                }
            }

            if (mejor == null || mejorAciertos <= 0)
            {
                contador.Ocultar();
                return;
            }

            contador.Mostrar(mejor.nombre, mejorAciertos, mejor.receta.Count, mejorFaltan);

            if (mejorFaltan.Count == 0) Completar(mejor);
        }

        /// <summary>
        /// Receta lista: el plato de emplatado SE SUSTITUYE por el modelo del plato
        /// terminado. Ese modelo ya trae su propio plato, así que ponerlo encima dejaría
        /// dos platos, uno dentro del otro.
        /// </summary>
        private void Completar(DishData plato)
        {
            if (plato.platoPrefab == null)
            {
                // Sin modelo no se destruye nada: peor que no cambiar es quedarse sin comida.
                Debug.LogWarning($"[PlateItem] '{plato.nombre}' no tiene 'platoPrefab' asignado; " +
                                 "se dejan los ingredientes sueltos.");
                return;
            }

            platoCompletado = plato;

            foreach (var item in platedIngredients)
            {
                if (item != null) Destroy(item.gameObject);
            }
            platedIngredients.Clear();

            // Aparece en la mesa de al lado, no donde estaba el plato de emplatado: así no
            // se confunde con los platos blancos vacíos que haya por el mostrador.
            var punto = DishPickupPoint.Encontrar();
            Vector3 donde = (punto != null) ? punto.SiguienteSitio() : transform.position;
            Quaternion giro = (punto != null) ? punto.transform.rotation : transform.rotation;

            if (punto == null)
            {
                Debug.LogWarning("[PlateItem] No hay ningún DishPickupPoint en la escena; " +
                                 "el plato aparece en el sitio del de emplatado. " +
                                 "Corre 'Kitchen > Setup Plating'.");
            }

            GameObject servido = Instantiate(plato.platoPrefab, donde, giro);
            servido.name = plato.nombre;

            var marca = servido.GetComponent<ServedDish>();
            if (marca == null) marca = servido.AddComponent<ServedDish>();
            marca.SetPlato(plato);

            if (servido.GetComponent<XRGrabInteractable>() == null)
            {
                Debug.LogWarning($"[PlateItem] El prefab de '{plato.nombre}' no tiene XRGrabInteractable; " +
                                 "no se podrá levantar. Corre 'Kitchen > Setup Plating'.");
            }

            if (audioSource != null && plateSound != null)
            {
                AudioSource.PlayClipAtPoint(plateSound, transform.position, 1f);
            }

            Debug.Log($"[PlateItem] ¡{plato.nombre} terminado! Te espera en el punto de recogida.");

            // El plato de emplatado ya cumplió: ahora el plato servido ocupa su lugar.
            Destroy(gameObject);
        }

        public void ReleaseIngredient(IngredientItem ingredient)
        {
            if (platedIngredients.Remove(ingredient))
            {
                Reacomodar();
                EvaluarReceta();
                Debug.Log($"[PlateItem] Ingrediente {ingredient.IngredientName} retirado del plato con Grip.");
            }
        }
    }
}
