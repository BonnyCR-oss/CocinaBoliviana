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
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Button buttonTemplate;

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();
        private Camera playerCamera;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>Atajo para el caso más común: un botón por ingrediente.</summary>
        public void Show(IReadOnlyList<IngredientData> opciones, Action<IngredientData> onSelected)
        {
            Show(opciones, ingrediente => ingrediente.nombre, onSelected);
        }

        /// <summary>
        /// Versión genérica: sirve para cualquier lista (ingredientes de un dispensador,
        /// tipos de corte de una tabla, etc.). 'etiqueta' decide qué texto lleva cada botón.
        /// </summary>
        public void Show<T>(IReadOnlyList<T> opciones, Func<T, string> etiqueta, Action<T> onSelected)
        {
            if (buttonTemplate == null || panelRoot == null || buttonContainer == null)
            {
                Debug.LogWarning("[IngredientSelectorMenu] Faltan referencias (panelRoot/buttonContainer/buttonTemplate).");
                return;
            }

            Clear();

            foreach (var opcion in opciones)
            {
                if (opcion is null) continue;

                GameObject buttonGo = Instantiate(buttonTemplate.gameObject, buttonContainer);
                buttonGo.SetActive(true);
                spawnedButtons.Add(buttonGo);

                var label = buttonGo.GetComponentInChildren<Text>();
                if (label != null) label.text = (etiqueta != null) ? etiqueta(opcion) : opcion.ToString();

                var button = buttonGo.GetComponent<Button>();
                T capturado = opcion;
                button.onClick.AddListener(() =>
                {
                    onSelected?.Invoke(capturado);
                    Hide();
                });
            }

            panelRoot.SetActive(true);
            FaceCamera();
        }

        public void Hide()
        {
            Clear();
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
    }
}
