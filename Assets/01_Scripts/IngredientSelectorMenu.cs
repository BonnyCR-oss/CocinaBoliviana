using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Menú flotante (World Space Canvas) que muestra un botón por cada opción recibida
    /// y avisa cuál eligió el jugador. No sabe nada de dispensadores ni de tablas de cortar:
    /// solo muestra opciones y reporta la elección.
    /// </summary>
    public class IngredientSelectorMenu : MonoBehaviour
    {
        [Header("Referencias de UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip selectSound;

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();
        private Camera playerCamera;
        private Action currentCancelCallback;
        private AudioSource audioSource;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }
        }

        /// <summary>Atajo para el caso común: un botón por ingrediente.</summary>
        public void Show(IReadOnlyList<IngredientData> opciones, Action<IngredientData> onSelected, string titulo = "Ingredientes", Action onCancel = null)
        {
            Show(opciones, ingrediente => ingrediente.nombre, onSelected, titulo, onCancel);
        }

        /// <summary>
        /// Versión genérica: sirve para cualquier lista (ingredientes de un dispensador,
        /// tipos de corte de una tabla, etc.). 'etiqueta' decide qué texto lleva cada botón.
        /// </summary>
        public void Show<T>(IReadOnlyList<T> opciones, Func<T, string> etiqueta, Action<T> onSelected, string titulo = null, Action onCancel = null)
        {
            if (buttonTemplate == null || panelRoot == null || buttonContainer == null)
            {
                Debug.LogWarning("[IngredientSelectorMenu] Faltan referencias (panelRoot/buttonContainer/buttonTemplate).");
                return;
            }

            Clear();
            currentCancelCallback = onCancel;

            if (titleText != null)
            {
                if (!string.IsNullOrEmpty(titulo))
                {
                    titleText.gameObject.SetActive(true);
                    titleText.text = titulo.ToUpperInvariant();
                }
                else
                {
                    titleText.gameObject.SetActive(false);
                }
            }

            foreach (var opcion in opciones)
            {
                if (opcion is null) continue;

                GameObject buttonGo = Instantiate(buttonTemplate.gameObject, buttonContainer);
                buttonGo.SetActive(true);
                spawnedButtons.Add(buttonGo);

                var label = buttonGo.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = (etiqueta != null) ? etiqueta(opcion) : opcion.ToString();
                    label.raycastTarget = false;
                }

                var button = buttonGo.GetComponent<Button>();
                T capturado = opcion;
                button.onClick.AddListener(() =>
                {
                    PlaySound(selectSound);
                    currentCancelCallback = null;
                    onSelected?.Invoke(capturado);
                    HideInternal(false);
                });
            }

            panelRoot.SetActive(true);
            FaceCamera();
            PlaySound(openSound);
        }

        public void Hide()
        {
            HideInternal(true);
        }

        private void HideInternal(bool notifyCancel)
        {
            Clear();

            if (notifyCancel)
            {
                var cancel = currentCancelCallback;
                currentCancelCallback = null;
                cancel?.Invoke();
                PlaySound(closeSound);
            }
            else
            {
                currentCancelCallback = null;
            }

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Clear()
        {
            foreach (var go in spawnedButtons)
            {
                if (go != null) Destroy(go);
            }
            spawnedButtons.Clear();
        }

        private void FaceCamera()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return;

            Vector3 dir = panelRoot.transform.position - playerCamera.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                panelRoot.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, 0.8f);
            }
        }
    }
}
