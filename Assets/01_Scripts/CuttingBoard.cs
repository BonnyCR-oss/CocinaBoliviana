using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Collider))]
    public class CuttingBoard : MonoBehaviour
    {
        [Header("Cutting Board Settings")]
        [SerializeField] private Transform snapPoint;
        [SerializeField] private GameObject defaultCutPrefab;
        [SerializeField] private int requiredHits = 3;
        [SerializeField] private AudioClip chopSound;
        [SerializeField] private AudioClip cutCompleteSound;

        private IngredientItem currentIngredient;
        private int hitsCount = 0;
        private AudioSource audioSource;
        private Coroutine punchRoutine;

        public IngredientItem CurrentIngredient => currentIngredient;
        public bool HasIngredient => currentIngredient != null;
        public int HitsCount => hitsCount;
        public int RequiredHits => requiredHits;

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

            if (snapPoint == null)
            {
                Transform existingSnap = transform.Find("SnapPoint");
                if (existingSnap != null)
                {
                    snapPoint = existingSnap;
                }
                else
                {
                    GameObject snapGo = new GameObject("SnapPoint");
                    snapGo.transform.SetParent(transform, false);
                    snapGo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                    snapPoint = snapGo.transform;
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // If already holding an ingredient, don't snap another
            if (currentIngredient != null) return;

            IngredientItem ingredient = other.GetComponentInParent<IngredientItem>();
            if (ingredient != null && !ingredient.IsCut)
            {
                // Only snap if the player is NOT actively holding it with Grip
                if (ingredient.GrabInteractable == null || !ingredient.GrabInteractable.isSelected)
                {
                    SnapIngredient(ingredient);
                }
            }
        }

        public void SnapIngredient(IngredientItem ingredient)
        {
            if (ingredient == null || currentIngredient != null) return;

            currentIngredient = ingredient;
            hitsCount = 0;

            Vector3 snapPos = (snapPoint != null) ? snapPoint.position : transform.position + Vector3.up * 0.04f;
            Quaternion snapRot = (snapPoint != null) ? snapPoint.rotation : Quaternion.identity;

            currentIngredient.SnapToCuttingBoard(this, snapPos, snapRot);
            Debug.Log($"[CuttingBoard] Ingrediente {ingredient.IngredientName} acoplado firmemente a la tabla.");
        }

        public void ReleaseIngredient(IngredientItem ingredient)
        {
            if (currentIngredient == ingredient)
            {
                currentIngredient = null;
                hitsCount = 0;
                Debug.Log("[CuttingBoard] Ingrediente retirado de la tabla con Grip.");
            }
        }

        public void RegisterKnifeHit()
        {
            if (currentIngredient == null || currentIngredient.IsCut) return;

            hitsCount++;
            Debug.Log($"[CuttingBoard] Golpe de cuchillo registrado: {hitsCount}/{requiredHits}");

            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(DoPunchAnimation());

            if (hitsCount < requiredHits)
            {
                if (audioSource != null && chopSound != null)
                {
                    audioSource.PlayOneShot(chopSound, 0.8f);
                }
            }
            else
            {
                FinishCutting();
            }
        }

        private void FinishCutting()
        {
            if (currentIngredient == null) return;

            GameObject cutPrefabToSpawn = currentIngredient.CutPrefab != null ? currentIngredient.CutPrefab : defaultCutPrefab;
            Vector3 pos = currentIngredient.transform.position;
            Quaternion rot = currentIngredient.transform.rotation;

            // Remove uncut tomato
            Destroy(currentIngredient.gameObject);
            currentIngredient = null;
            hitsCount = 0;

            if (cutPrefabToSpawn != null)
            {
                GameObject cutGo = Instantiate(cutPrefabToSpawn, pos, rot);
                cutGo.name = cutPrefabToSpawn.name;

                IngredientItem cutItem = cutGo.GetComponent<IngredientItem>();
                if (cutItem != null)
                {
                    currentIngredient = cutItem;
                    cutItem.SnapToCuttingBoard(this, pos, rot);
                }
            }

            if (audioSource != null && cutCompleteSound != null)
            {
                audioSource.PlayOneShot(cutCompleteSound, 1.0f);
            }

            Debug.Log("[CuttingBoard] ¡3 golpes completados! Tomate Picado generado y fijado a la tabla.");
        }

        private IEnumerator DoPunchAnimation()
        {
            if (currentIngredient == null) yield break;
            Transform target = currentIngredient.transform;
            Vector3 originalScale = target.localScale;
            Vector3 squishedScale = new Vector3(originalScale.x * 1.18f, originalScale.y * 0.72f, originalScale.z * 1.18f);

            float elapsed = 0f;
            float duration = 0.07f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(originalScale, squishedScale, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            duration = 0.11f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(squishedScale, originalScale, elapsed / duration);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = originalScale;
            }
        }
    }
}
