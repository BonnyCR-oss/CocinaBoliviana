using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class IngredientItem : MonoBehaviour
    {
        [Header("Ingredient Info")]
        [SerializeField] private string ingredientName = "Tomate";
        [SerializeField] private bool isCut = false;
        [SerializeField] private GameObject cutPrefab;

        private Rigidbody rb;
        private XRGrabInteractable grabInteractable;
        private CuttingBoard currentBoard;
        private PlateItem currentPlate;

        public bool IsCut => isCut;
        public string IngredientName => ingredientName;
        public GameObject CutPrefab => cutPrefab;
        public XRGrabInteractable GrabInteractable => grabInteractable;
        public CuttingBoard CurrentBoard => currentBoard;
        public PlateItem CurrentPlate => currentPlate;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();

            grabInteractable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Detach from cutting board if grabbed
            if (currentBoard != null)
            {
                currentBoard.ReleaseIngredient(this);
                currentBoard = null;
            }

            // Detach from plate if grabbed
            if (currentPlate != null)
            {
                currentPlate.ReleaseIngredient(this);
                currentPlate = null;
            }

            // Restore physics
            transform.SetParent(null, true);
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        public void SnapToCuttingBoard(CuttingBoard board, Vector3 position, Quaternion rotation)
        {
            if (currentPlate != null)
            {
                currentPlate.ReleaseIngredient(this);
                currentPlate = null;
            }

            currentBoard = board;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            transform.SetParent(board.transform, true);
            transform.position = position;
            transform.rotation = rotation;
        }

        public void SnapToPlate(PlateItem plate, Vector3 position, Quaternion rotation)
        {
            if (currentBoard != null)
            {
                currentBoard.ReleaseIngredient(this);
                currentBoard = null;
            }

            currentPlate = plate;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            transform.SetParent(plate.transform, true);
            transform.position = position;
            transform.rotation = rotation;
        }

        public void ClearBoardReference()
        {
            currentBoard = null;
        }

        public void ClearPlateReference()
        {
            currentPlate = null;
        }
    }
}
