using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

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

        [Header("Tipo de Corte")]
        [Tooltip("Corte activo. Al acoplar un ingrediente se reemplaza por el que elija el jugador; " +
                 "solo se usa tal cual si el ingrediente tiene un único corte o si falta el menú.")]
        [SerializeField] private TipoCorte tipoDeCorte = TipoCorte.Cubitos;

        [Tooltip("Menú flotante para elegir el corte al acoplar el ingrediente. Si falta, la tabla " +
                 "se queda con el valor fijo de 'Tipo De Corte'.")]
        [SerializeField] private IngredientSelectorMenu corteMenu;

        [Header("Zona de Detección")]
        [Tooltip("Caja (en METROS de mundo) sobre la tabla donde se detecta un ingrediente soltado.")]
        [SerializeField] private Vector3 zonaDeteccion = new Vector3(0.50f, 0.30f, 0.40f);

        [Tooltip("Altura del centro de esa caja sobre la tabla, en metros.")]
        [SerializeField] private float zonaAltura = 0.15f;

        private IngredientItem currentIngredient;
        private int hitsCount = 0;
        private bool esperandoEleccionDeCorte;
        private AudioSource audioSource;
        private Coroutine punchRoutine;

        public IngredientItem CurrentIngredient => currentIngredient;
        public bool HasIngredient => currentIngredient != null;
        public int HitsCount => hitsCount;
        public int RequiredHits => requiredHits;

        private void Awake()
        {
            // Ojo: aquí NO se toca isTrigger. El collider de la Tabla es su superficie sólida,
            // lo que sostiene los ingredientes. La detección va por OverlapBox, no por trigger:
            // un trigger lo bastante alto para detectar acababa envolviendo al ingrediente y
            // robándole el rayo del control, dejándolo imposible de agarrar.

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

        private void FixedUpdate()
        {
            // Si ya hay algo en la tabla, no busques nada más.
            if (currentIngredient != null) return;

            Vector3 centro = transform.position + Vector3.up * zonaAltura;
            Collider[] dentro = Physics.OverlapBox(centro, zonaDeteccion * 0.5f, transform.rotation,
                                                   ~0, QueryTriggerInteraction.Ignore);

            foreach (var other in dentro)
            {
                IngredientItem ingredient = other.GetComponentInParent<IngredientItem>();
                if (ingredient == null || ingredient.IsCut) continue;

                // Solo acoplar si el jugador NO lo tiene agarrado con Grip
                if (ingredient.GrabInteractable != null && ingredient.GrabInteractable.isSelected) continue;

                SnapIngredient(ingredient);
                return;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * zonaAltura, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, zonaDeteccion);
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

            PedirTipoDeCorte(ingredient);
        }

        /// <summary>
        /// Ofrece solo los cortes que ESTE ingrediente sabe producir, así nunca se queda
        /// un corte imposible seleccionado (ej. Cubitos sobre una Papa, que solo hace Rodajas).
        /// </summary>
        private void PedirTipoDeCorte(IngredientItem ingredient)
        {
            esperandoEleccionDeCorte = false;

            IngredientData data = ingredient.Data;
            var cortes = (data != null) ? data.cortesDisponibles : null;

            if (cortes == null || cortes.Count == 0)
            {
                Debug.LogWarning($"[CuttingBoard] {ingredient.IngredientName} no tiene cortes definidos en su IngredientData; no se puede cortar.");
                return;
            }

            if (cortes.Count == 1)
            {
                tipoDeCorte = cortes[0].tipo;
                Debug.Log($"[CuttingBoard] {ingredient.IngredientName}: único corte disponible, {tipoDeCorte}.");
                return;
            }

            if (corteMenu == null)
            {
                Debug.LogWarning($"[CuttingBoard] {gameObject.name} no tiene 'corteMenu' asignado; se usa el corte fijo {tipoDeCorte}.");
                return;
            }

            esperandoEleccionDeCorte = true;
            corteMenu.Show(cortes, corte => corte.tipo.ToString(), elegido =>
            {
                tipoDeCorte = elegido.tipo;
                esperandoEleccionDeCorte = false;
                Debug.Log($"[CuttingBoard] Corte elegido para {ingredient.IngredientName}: {tipoDeCorte}.");
            });
        }

        public void ReleaseIngredient(IngredientItem ingredient)
        {
            if (currentIngredient == ingredient)
            {
                currentIngredient = null;
                hitsCount = 0;
                esperandoEleccionDeCorte = false;
                if (corteMenu != null && corteMenu.IsOpen) corteMenu.Hide();
                Debug.Log("[CuttingBoard] Ingrediente retirado de la tabla con Grip.");
            }
        }

        public void RegisterKnifeHit()
        {
            if (currentIngredient == null || currentIngredient.IsCut) return;

            if (esperandoEleccionDeCorte)
            {
                Debug.Log("[CuttingBoard] Elige primero un tipo de corte en el menú.");
                return;
            }

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

            IngredientData sourceData = currentIngredient.Data;
            GameObject cutPrefabToSpawn = null;

            // 1. Prioridad: el IngredientData sabe qué prefab corresponde a ESTE tipo de corte
            if (sourceData != null)
            {
                cutPrefabToSpawn = sourceData.ObtenerPrefabParaCorte(tipoDeCorte);
            }

            // 2. Fallback: prefabs configurados directamente en el IngredientItem/tabla (flujo legado)
            if (cutPrefabToSpawn == null)
            {
                cutPrefabToSpawn = currentIngredient.CutPrefab != null ? currentIngredient.CutPrefab : defaultCutPrefab;
            }

            // Si esta tabla no sabe producir este corte para este ingrediente, no destruyas nada.
            if (cutPrefabToSpawn == null)
            {
                Debug.LogWarning($"[CuttingBoard] {gameObject.name} no tiene un resultado configurado para {tipoDeCorte} con {currentIngredient.IngredientName}. Corte cancelado.");
                hitsCount = requiredHits - 1;
                return;
            }

            Vector3 pos = currentIngredient.transform.position;
            Quaternion rot = currentIngredient.transform.rotation;

            // Remove uncut ingredient
            Destroy(currentIngredient.gameObject);
            currentIngredient = null;
            hitsCount = 0;
            esperandoEleccionDeCorte = false;

            if (cutPrefabToSpawn != null)
            {
                GameObject cutGo = Instantiate(cutPrefabToSpawn, pos, rot);
                cutGo.name = cutPrefabToSpawn.name;

                IngredientItem cutItem = cutGo.GetComponent<IngredientItem>();
                if (cutItem != null)
                {
                    if (sourceData != null)
                    {
                        cutItem.SetData(sourceData);
                    }
                    cutItem.SetCorteActual(tipoDeCorte);
                }

                // El resultado queda suelto, apoyado en la superficie sólida de la tabla.
                // Antes se volvía a fijar (kinemático y emparentado), y así no había forma
                // de levantarlo; además dejaba la tabla ocupada para el siguiente ingrediente.
            }

            if (audioSource != null && cutCompleteSound != null)
            {
                audioSource.PlayOneShot(cutCompleteSound, 1.0f);
            }

            Debug.Log($"[CuttingBoard] Corte completado ({tipoDeCorte}). Resultado libre sobre la tabla.");
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
