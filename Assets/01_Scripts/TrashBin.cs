using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CocinaBoliviana
{
    /// <summary>
    /// Basurero de la cocina. Cuando cualquier comida (ingrediente crudo, cortado, cocido,
    /// quemado o plato terminado) entra en contacto con el basurero, se desecha y destruye.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrashBin : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Sonido opcional que se reproduce al tirar comida a la basura.")]
        [SerializeField] private AudioClip trashSound;

        [Header("Configuración")]
        [Tooltip("Si es true, destruye el objeto incluso si el jugador lo sostiene al tocar el basurero. Si es false, solo se destruye al soltarlo.")]
        [SerializeField] private bool destruirAunSostenido = true;

        private AudioSource audioSource;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcesarDesecho(other);
        }

        private void OnTriggerStay(Collider other)
        {
            ProcesarDesecho(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcesarDesecho(collision.collider);
        }

        private void ProcesarDesecho(Collider other)
        {
            if (other == null) return;

            // 1. Ingrediente (crudo, cortado, cocido, quemado)
            var ingredient = other.GetComponentInParent<IngredientItem>();
            if (ingredient != null)
            {
                DesecharIngrediente(ingredient);
                return;
            }

            // 2. Plato servido terminado
            var servedDish = other.GetComponentInParent<ServedDish>();
            if (servedDish != null)
            {
                DesecharPlatoServido(servedDish);
                return;
            }
        }

        private void DesecharIngrediente(IngredientItem ingredient)
        {
            if (ingredient == null || !ingredient.gameObject.activeInHierarchy) return;

            var grab = ingredient.GrabInteractable;
            if (grab != null && grab.isSelected)
            {
                if (!destruirAunSostenido) return;
                DesconectarDeInteractors(grab);
            }

            // Desvincular de estaciones si estaba registrado en alguna
            if (ingredient.CurrentBoard != null)
            {
                ingredient.CurrentBoard.ReleaseIngredient(ingredient);
            }
            if (ingredient.CurrentPlate != null)
            {
                ingredient.CurrentPlate.ReleaseIngredient(ingredient);
            }
            if (ingredient.CurrentVessel != null)
            {
                ingredient.CurrentVessel.ReleaseIngredient(ingredient);
            }

            ReproducirSonido();
            Debug.Log($"[TrashBin] Ingrediente '{ingredient.IngredientName}' ({ingredient.EstadoCoccion}, corte: {ingredient.CorteActual}) desechado en el basurero.");

            // Desactivar antes de destruir para silenciar física y colliders en este mismo frame
            ingredient.gameObject.SetActive(false);
            Destroy(ingredient.gameObject);
        }

        private void DesecharPlatoServido(ServedDish servedDish)
        {
            if (servedDish == null || !servedDish.gameObject.activeInHierarchy) return;

            var grab = servedDish.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                if (!destruirAunSostenido) return;
                DesconectarDeInteractors(grab);
            }

            ReproducirSonido();
            Debug.Log($"[TrashBin] Plato servido '{servedDish.Plato?.nombre ?? servedDish.name}' desechado en el basurero.");

            servedDish.gameObject.SetActive(false);
            Destroy(servedDish.gameObject);
        }

        private void DesconectarDeInteractors(XRGrabInteractable grab)
        {
            if (grab == null || !grab.isSelected) return;

            var mgr = grab.interactionManager;
            if (mgr == null) mgr = Object.FindAnyObjectByType<XRInteractionManager>();

            if (mgr != null)
            {
                var interactors = new List<IXRSelectInteractor>(grab.interactorsSelecting);
                foreach (var interactor in interactors)
                {
                    if (interactor != null)
                    {
                        mgr.SelectExit(interactor, grab);
                    }
                }
            }
        }

        private void ReproducirSonido()
        {
            if (audioSource != null && trashSound != null)
            {
                audioSource.PlayOneShot(trashSound, 0.85f);
            }
        }
    }
}
