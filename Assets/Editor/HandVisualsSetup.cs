using UnityEditor;
using UnityEngine;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Replaces the visible controller model with a static hand mesh, keeping controller
    /// button/grip input untouched (ControllerAnimator still drives the hidden controller
    /// transforms; only the rendered mesh changes).
    /// </summary>
    [InitializeOnLoad]
    public static class HandVisualsSetup
    {
        private const string LeftControllerPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab";
        private const string RightControllerPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab";
        private const string LeftHandModelPath = "Assets/Samples/XR Hands/1.8.1/HandVisualizer/Models/LeftHand.fbx";
        private const string RightHandModelPath = "Assets/Samples/XR Hands/1.8.1/HandVisualizer/Models/RightHand.fbx";

        // El modelo de mano viene orientado para tracking real, no para colgar del control.
        // Si tras correr el setup la mano mira hacia una dirección rara, ajusta este offset
        // y vuelve a correr "Kitchen > Setup Hand Visuals (Controllers)".
        private static readonly Vector3 HandRotationOffsetEuler = new Vector3(0f, 180f, 0f);

        static HandVisualsSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("HandVisualsSetup_Executed_v1", false))
                {
                    SessionState.SetBool("HandVisualsSetup_Executed_v1", true);
                    SetupHandVisuals();
                }
            };
        }

        [MenuItem("Kitchen/Setup Hand Visuals (Controllers)")]
        public static void SetupHandVisuals()
        {
            Debug.Log("[HandVisualsSetup] Reemplazando modelo de control por mano estática...");
            ApplyHandVisual(LeftControllerPrefabPath, LeftHandModelPath, "HandVisual_Left");
            ApplyHandVisual(RightControllerPrefabPath, RightHandModelPath, "HandVisual_Right");
        }

        private static void ApplyHandVisual(string controllerPrefabPath, string handModelPath, string handObjectName)
        {
            GameObject handModel = AssetDatabase.LoadAssetAtPath<GameObject>(handModelPath);
            if (handModel == null)
            {
                Debug.LogError($"[HandVisualsSetup] No se encontró el modelo de mano en {handModelPath}");
                return;
            }

            GameObject controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(controllerPrefabPath);
            if (controllerPrefab == null)
            {
                Debug.LogError($"[HandVisualsSetup] No se encontró el prefab de control en {controllerPrefabPath}");
                return;
            }

            using (var editScope = new PrefabUtility.EditPrefabContentsScope(controllerPrefabPath))
            {
                GameObject root = editScope.prefabContentsRoot;

                Transform controllerVisual = root.transform.Find("UniversalController");
                if (controllerVisual != null)
                {
                    controllerVisual.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"[HandVisualsSetup] No se encontró 'UniversalController' en {controllerPrefabPath}");
                }

                // Idempotent: quita una mano previa si el comando se corre de nuevo
                Transform existingHand = root.transform.Find(handObjectName);
                if (existingHand != null)
                {
                    Object.DestroyImmediate(existingHand.gameObject);
                }

                GameObject handInstance = (GameObject)PrefabUtility.InstantiatePrefab(handModel, root.transform);
                handInstance.name = handObjectName;
                handInstance.transform.localPosition = Vector3.zero;
                handInstance.transform.localRotation = Quaternion.Euler(HandRotationOffsetEuler);
                handInstance.transform.localScale = Vector3.one;
            }

            Debug.Log($"[HandVisualsSetup] Mano estática aplicada en {controllerPrefabPath}. Ajusta la posición/rotación de '{handObjectName}' en el prefab si no queda alineada con el agarre.");
        }
    }
}
