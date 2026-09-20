using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PlateItem : MonoBehaviour
    {
        [Header("Plate Settings")]
        [SerializeField] private Transform foodSnapPoint;
        [SerializeField] private AudioClip plateSound;

        private readonly List<IngredientItem> platedIngredients = new List<IngredientItem>();
        private AudioSource audioSource;
        private XRGrabInteractable grabInteractable;

        public IReadOnlyList<IngredientItem> PlatedIngredients => platedIngredients;
        public bool HasFood => platedIngredients.Count > 0;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
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

        private void OnTriggerStay(Collider other)
        {
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
            if (ingredient == null || platedIngredients.Contains(ingredient)) return;

            platedIngredients.Add(ingredient);

            Vector3 basePos = (foodSnapPoint != null) ? foodSnapPoint.position : transform.position + Vector3.up * 0.035f;
            Vector3 snapPos = basePos + Vector3.up * (0.02f * (platedIngredients.Count - 1));
            Quaternion snapRot = (foodSnapPoint != null) ? foodSnapPoint.rotation : Quaternion.identity;

            ingredient.SnapToPlate(this, snapPos, snapRot);

            if (audioSource != null && plateSound != null)
            {
                audioSource.PlayOneShot(plateSound, 0.7f);
            }

            Debug.Log($"[PlateItem] Ingrediente {ingredient.IngredientName} emplatado exitosamente. Total en plato: {platedIngredients.Count}");
        }

        public void ReleaseIngredient(IngredientItem ingredient)
        {
            if (platedIngredients.Remove(ingredient))
            {
                Debug.Log($"[PlateItem] Ingrediente {ingredient.IngredientName} retirado del plato con Grip.");
            }
        }
    }
}
