using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Collider))]
    public class ItemDispenser : XRSimpleInteractable
    {
        [Header("Dispenser Configuration")]
        [Tooltip("Solo para el selector por categoría del Inspector. No se dispensa: lo que sale " +
                 "de este dispensador son las 'Opciones Ingredientes'.")]
        [SerializeField] private IngredientData ingredienteReferencia;

        [Header("Qué dispensa")]
        [Tooltip("Única fuente de lo que reparte este dispensador. Con un solo elemento lo entrega " +
                 "directo; con dos o más abre el menú para elegir (requiere 'Menu').")]
        [SerializeField] private List<IngredientData> opcionesIngredientes = new List<IngredientData>();
        [SerializeField] private IngredientSelectorMenu menu;

        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float cooldownTime = 0.35f;
        [SerializeField] private AudioClip dispenseSound;

        private float lastDispenseTime = -10f;
        private AudioSource audioSource;
        private readonly HashSet<IXRSelectInteractor> touchingInteractors = new HashSet<IXRSelectInteractor>();
        private readonly HashSet<XRInputButtonReader> activePressedReaders = new HashSet<XRInputButtonReader>();

        protected override void Awake()
        {
            base.Awake();

            // Enable trigger raycasting so rays hit trigger colliders as well as solid ones
            Physics.queriesHitTriggers = true;

            var cols = GetComponents<Collider>();
            foreach (var c in cols)
            {
                if (!colliders.Contains(c))
                {
                    colliders.Add(c);
                }
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }

            selectEntered.AddListener(OnGripDispense);
            activated.AddListener(OnTriggerDispense);
            hoverEntered.AddListener(OnHoverEnteredLog);
            hoverExited.AddListener(OnHoverExitedLog);
        }

        private void OnHoverEnteredLog(HoverEnterEventArgs args)
        {
            Debug.Log($"[ItemDispenser] {gameObject.name}: HOVER ENTER de {args.interactorObject}.");
        }

        private void OnHoverExitedLog(HoverExitEventArgs args)
        {
            Debug.Log($"[ItemDispenser] {gameObject.name}: hover exit.");
        }

        protected override void OnDestroy()
        {
            selectEntered.RemoveListener(OnGripDispense);
            activated.RemoveListener(OnTriggerDispense);
            hoverEntered.RemoveListener(OnHoverEnteredLog);
            hoverExited.RemoveListener(OnHoverExitedLog);
            base.OnDestroy();
        }

        private void Update()
        {
            // 1. Check for keyboard Trigger (T) or Grip (G) keys while hovering or touching
            if (isHovered || touchingInteractors.Count > 0)
            {
                bool tPressed = false;
                bool gPressed = false;

                // New Input System Keyboard
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.tKey.wasPressedThisFrame) tPressed = true;
                    if (kb.gKey.wasPressedThisFrame) gPressed = true;
                }

                // Legacy Input (for XR Device Simulator / Play mode)
                try
                {
                    if (Input.GetKeyDown(KeyCode.T)) tPressed = true;
                    if (Input.GetKeyDown(KeyCode.G)) gPressed = true;
                    if (Input.GetMouseButtonDown(0)) tPressed = true;
                }
                catch { }

                if (tPressed || gPressed)
                {
                    IXRSelectInteractor target = GetActiveOrHoveringInteractor();
                    DispenseToInteractor(target);
                    return;
                }

                // 2. Check VR Controller inputs on hovering interactors
                foreach (var interactor in interactorsHovering)
                {
                    if (interactor is XRBaseInputInteractor inputInteractor)
                    {
                        bool triggerPressed = CheckReaderPressedEdge(inputInteractor.activateInput);
                        if (!triggerPressed && inputInteractor is NearFarInteractor nearFar)
                        {
                            triggerPressed = CheckReaderPressedEdge(nearFar.uiPressInput);
                        }
                        bool gripPressed = CheckReaderPressedEdge(inputInteractor.selectInput);

                        if (triggerPressed || gripPressed)
                        {
                            DispenseToInteractor(interactor as IXRSelectInteractor, inputInteractor.transform);
                            return;
                        }
                    }
                }
            }

            // 3. Check VR Controller inputs on touching interactors
            foreach (var interactor in touchingInteractors)
            {
                if (interactor is XRBaseInputInteractor inputInteractor)
                {
                    bool triggerPressed = CheckReaderPressedEdge(inputInteractor.activateInput);
                    if (!triggerPressed && inputInteractor is NearFarInteractor nearFar)
                    {
                        triggerPressed = CheckReaderPressedEdge(nearFar.uiPressInput);
                    }
                    bool gripPressed = CheckReaderPressedEdge(inputInteractor.selectInput);

                    if (triggerPressed || gripPressed)
                    {
                        DispenseToInteractor(interactor, inputInteractor.transform);
                        return;
                    }
                }
            }
        }

        private bool CheckReaderPressedEdge(XRInputButtonReader reader)
        {
            if (reader == null) return false;

            bool isDown = reader.ReadWasPerformedThisFrame() || reader.ReadIsPerformed() || (reader.ReadValue() > 0.4f);
            bool wasDown = activePressedReaders.Contains(reader);

            if (isDown)
            {
                activePressedReaders.Add(reader);
                return !wasDown; // Trigger on leading edge
            }
            else
            {
                activePressedReaders.Remove(reader);
                return false;
            }
        }

        private IXRSelectInteractor GetActiveOrHoveringInteractor()
        {
            if (interactorsHovering.Count > 0)
            {
                foreach (var candidate in interactorsHovering)
                {
                    if (candidate is IXRSelectInteractor sel) return sel;
                }
            }

            foreach (var candidate in touchingInteractors)
            {
                if (candidate != null) return candidate;
            }

            // Fallback to finding an active interactor in the scene
            var allInteractors = Object.FindObjectsByType<XRBaseInputInteractor>();
            foreach (var candidate in allInteractors)
            {
                if (candidate.gameObject.activeInHierarchy && candidate is IXRSelectInteractor sel)
                {
                    return sel;
                }
            }

            return null;
        }

        private void OnTriggerEnter(Collider other)
        {
            var interactor = other.GetComponentInParent<IXRSelectInteractor>();
            if (interactor != null)
            {
                touchingInteractors.Add(interactor);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var interactor = other.GetComponentInParent<IXRSelectInteractor>();
            if (interactor != null)
            {
                touchingInteractors.Remove(interactor);
            }
        }

        private void OnGripDispense(SelectEnterEventArgs args)
        {
            Debug.Log($"[ItemDispenser] {gameObject.name}: evento selectEntered (Grip) recibido de {args.interactorObject}.");
            DispenseToInteractor(args.interactorObject);
        }

        private void OnTriggerDispense(ActivateEventArgs args)
        {
            Debug.Log($"[ItemDispenser] {gameObject.name}: evento activated (Trigger) recibido de {args.interactorObject}.");
            IXRSelectInteractor selectInteractor = args.interactorObject as IXRSelectInteractor;
            if (selectInteractor == null && args.interactorObject is Component comp)
            {
                selectInteractor = comp.GetComponent<IXRSelectInteractor>() ?? comp.GetComponentInParent<IXRSelectInteractor>();
            }

            DispenseToInteractor(selectInteractor, args.interactorObject is Component c ? c.transform : null);
        }

        public void DispenseToInteractor(IXRSelectInteractor interactor, Transform fallbackTransform = null)
        {
            int totalOpciones = (opcionesIngredientes != null) ? opcionesIngredientes.Count : 0;
            Debug.Log($"[ItemDispenser] {gameObject.name}: DispenseToInteractor llamado. opciones={totalOpciones}, menu={(menu != null ? menu.name : "null")}.");

            if (totalOpciones == 0)
            {
                Debug.LogWarning($"[ItemDispenser] {gameObject.name} no tiene ningún ingrediente en 'Opciones Ingredientes'.");
                return;
            }

            if (Time.time - lastDispenseTime < cooldownTime)
            {
                Debug.Log($"[ItemDispenser] {gameObject.name}: en cooldown, ignorado.");
                return;
            }
            if (menu != null && menu.IsOpen)
            {
                Debug.Log($"[ItemDispenser] {gameObject.name}: el menú ya está abierto, ignorado.");
                return;
            }

            if (interactor == null)
            {
                interactor = GetActiveOrHoveringInteractor();
            }

            Transform handTransform = ResolveHandTransform(interactor, fallbackTransform);

            // Con una sola opción no hay nada que elegir: se entrega directo y nos ahorramos
            // un menú de un solo botón (es el caso de CleanPlates).
            if (totalOpciones == 1)
            {
                lastDispenseTime = Time.time;
                IngredientData unico = opcionesIngredientes[0];
                SpawnItem(unico != null ? unico.prefab : null, interactor, handTransform);
                return;
            }

            if (menu == null)
            {
                Debug.LogWarning($"[ItemDispenser] {gameObject.name} tiene {totalOpciones} opciones pero no tiene un 'menu' (IngredientSelectorMenu) asignado.");
                return;
            }

            lastDispenseTime = Time.time;
            menu.Show(opcionesIngredientes, elegido => SpawnItem(elegido != null ? elegido.prefab : null, interactor, handTransform));
        }

        private Transform ResolveHandTransform(IXRSelectInteractor interactor, Transform fallbackTransform)
        {
            if (interactor is Component comp)
            {
                return comp.transform;
            }
            if (fallbackTransform != null)
            {
                return fallbackTransform;
            }
            var camera = Camera.main;
            return (camera != null) ? camera.transform : transform;
        }

        private void SpawnItem(GameObject prefabToSpawn, IXRSelectInteractor interactor, Transform handTransform)
        {
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[ItemDispenser] {gameObject.name}: no hay prefab para dispensar.");
                return;
            }

            Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : handTransform.position + handTransform.forward * 0.12f;
            Quaternion spawnRot = (spawnPoint != null) ? spawnPoint.rotation : handTransform.rotation;

            GameObject spawnedItem = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            spawnedItem.name = prefabToSpawn.name;

            var grabInteractable = spawnedItem.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null && interactor != null)
            {
                StartCoroutine(ForceGrabRoutine(interactor, grabInteractable));
            }

            PlaySound();
            Debug.Log($"[ItemDispenser] Dispensed {spawnedItem.name} to {(handTransform != null ? handTransform.name : "player")}");
        }

        private IEnumerator ForceGrabRoutine(IXRSelectInteractor interactor, XRGrabInteractable grabInteractable)
        {
            var mgr = (interactionManager != null) ? interactionManager : Object.FindAnyObjectByType<XRInteractionManager>();
            if (mgr == null || interactor == null || grabInteractable == null) yield break;

            // If interactor was selecting this dispenser, release it
            if (interactor.hasSelection)
            {
                var currentSelection = interactor.interactablesSelected[0];
                if (currentSelection != (IXRSelectInteractable)grabInteractable)
                {
                    mgr.SelectExit(interactor, currentSelection);
                }
            }

            // Wait one frame for release to fully clear
            yield return null;

            if (mgr != null && interactor != null && grabInteractable != null && !interactor.hasSelection)
            {
                mgr.SelectEnter(interactor, grabInteractable);
            }
        }

        private void PlaySound()
        {
            if (audioSource != null && dispenseSound != null)
            {
                audioSource.PlayOneShot(dispenseSound, 0.7f);
            }
        }
    }
}
