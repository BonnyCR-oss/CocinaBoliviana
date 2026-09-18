using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Collider))]
    public class ItemDispenser : XRSimpleInteractable
    {
        [Header("Dispenser Configuration")]
        [SerializeField] private GameObject itemPrefab;
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
        }

        protected override void OnDestroy()
        {
            selectEntered.RemoveListener(OnGripDispense);
            activated.RemoveListener(OnTriggerDispense);
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
            DispenseToInteractor(args.interactorObject);
        }

        private void OnTriggerDispense(ActivateEventArgs args)
        {
            IXRSelectInteractor selectInteractor = args.interactorObject as IXRSelectInteractor;
            if (selectInteractor == null && args.interactorObject is Component comp)
            {
                selectInteractor = comp.GetComponent<IXRSelectInteractor>() ?? comp.GetComponentInParent<IXRSelectInteractor>();
            }

            DispenseToInteractor(selectInteractor, args.interactorObject is Component c ? c.transform : null);
        }

        public void DispenseToInteractor(IXRSelectInteractor interactor, Transform fallbackTransform = null)
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning($"[ItemDispenser] {gameObject.name} does not have an itemPrefab assigned!");
                return;
            }

            if (Time.time - lastDispenseTime < cooldownTime) return;
            lastDispenseTime = Time.time;

            if (interactor == null)
            {
                interactor = GetActiveOrHoveringInteractor();
            }

            // Determine target hand transform
            Transform handTransform = null;
            if (interactor is Component comp)
            {
                handTransform = comp.transform;
            }
            else if (fallbackTransform != null)
            {
                handTransform = fallbackTransform;
            }
            else
            {
                var camera = Camera.main;
                handTransform = (camera != null) ? camera.transform : transform;
            }

            Vector3 spawnPos = (spawnPoint != null) ? spawnPoint.position : handTransform.position + handTransform.forward * 0.12f;
            Quaternion spawnRot = (spawnPoint != null) ? spawnPoint.rotation : handTransform.rotation;

            GameObject spawnedItem = Instantiate(itemPrefab, spawnPos, spawnRot);
            spawnedItem.name = itemPrefab.name;

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
