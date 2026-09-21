using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Hace que los objetos agarrados se peguen a la mano en vez de quedarse flotando
    /// en la punta del rayo.
    ///
    /// La causa era el default de XRI: NearFarInteractor.farAttachMode viene en 'Far', que
    /// según la propia documentación del paquete deja el objeto "distant at the far hit point".
    /// En 'Near' el objeto viaja a la mano al seleccionarlo.
    /// </summary>
    public static class GrabFeelSetup
    {
        private const string InteractorsFolder =
            "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/Interactors";

        private static readonly string[] InteractorPrefabs =
        {
            InteractorsFolder + "/Left_NearFarInteractor.prefab",
            InteractorsFolder + "/Right_NearFarInteractor.prefab",
        };

        /// <summary>Carpetas donde buscar objetos agarrables.</summary>
        private static readonly string[] GrabbableFolders = { "Assets/02_Prefabs" };

        [MenuItem("Kitchen/Setup Grab Feel (imán a la mano)")]
        public static void SetupGrabFeel()
        {
            Debug.Log("[GrabFeelSetup] Ajustando el agarre...");

            int interactores = 0;
            foreach (string path in InteractorPrefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[GrabFeelSetup] No existe {path}, se omite.");
                    continue;
                }

                using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    foreach (var nf in scope.prefabContentsRoot.GetComponentsInChildren<NearFarInteractor>(true))
                    {
                        nf.farAttachMode = InteractorFarAttachMode.Near;
                        interactores++;
                    }
                }
            }

            int agarrables = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", GrabbableFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || asset.GetComponentInChildren<XRGrabInteractable>(true) == null) continue;

                using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    foreach (var grab in scope.prefabContentsRoot.GetComponentsInChildren<XRGrabInteractable>(true))
                    {
                        // Sin ease el objeto aparece pegado al instante. Si se siente brusco,
                        // subir attachEaseInTime (el default de XRI es 0.15 s).
                        grab.attachEaseInTime = 0f;

                        // Con dynamic attach el objeto conserva el punto por donde lo agarraste
                        // y queda descentrado respecto a la mano.
                        grab.useDynamicAttach = false;

                        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                        grab.smoothPosition = false;
                        grab.smoothRotation = false;
                        agarrables++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GrabFeelSetup] Listo: {interactores} interactor(es) en modo 'Near' y {agarrables} objeto(s) agarrables ajustados.");
        }
    }
}
