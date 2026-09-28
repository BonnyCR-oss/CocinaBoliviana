using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace CocinaBoliviana
{
    /// <summary>
    /// Re-vincula el simulador de XR (XRInteractionSimulator) y el sistema de input
    /// tras un cambio de escena en tiempo de ejecución.
    ///
    /// Problema resuelto:
    /// En XRI 3.5.1, el XRInteractionSimulator se instancia una sola vez y permanece en DontDestroyOnLoad.
    /// Al cambiar de escena (ej. de Main Menu a Nivel 1), los objetos de la escena previa (XR Origin, 
    /// XRInputModalityManager, Cámara, Controles) se destruyen. El simulador NO actualiza automáticamente
    /// las referencias cacheadas m_RightControllerTransform ni m_LeftControllerTransform en su Update(), 
    /// provocando que AimDeviceAtWorldPoint se salte por completo y la mano quede congelada.
    ///
    /// Este script detecta la carga de la nueva escena, reasigna los transforms activos, reactiva el 
    /// apuntado por Point-and-Click del mouse en la mano derecha, actualiza la física y el caché de
    /// ComponentLocatorUtility, y asegura que NUNCA se desincronice ni se corrompa el m_DeviceLifecycleManager.
    /// </summary>
    public class XRSceneRelinker : MonoBehaviour
    {
        private static XRSceneRelinker instancia;
        private float proximaComprobacionWatchdog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inicializar()
        {
            if (instancia != null) return;

            var go = new GameObject("[XRSceneRelinker]");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<XRSceneRelinker>();
        }

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            instancia = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instancia == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                instancia = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RelinkNow();
            StartCoroutine(RelinkNextFrames());
        }

        private IEnumerator RelinkNextFrames()
        {
            yield return null;
            RelinkNow();
            yield return null;
            RelinkNow();
        }

        private void Update()
        {
            if (Time.time < proximaComprobacionWatchdog) return;
            proximaComprobacionWatchdog = Time.time + 0.25f;

            var sim = XRInteractionSimulator.instance != null 
                ? XRInteractionSimulator.instance 
                : UnityEngine.Object.FindAnyObjectByType<XRInteractionSimulator>();

            if (sim != null && sim.isActiveAndEnabled)
            {
                // En Unity, si un Transform fue destruido, sim.rightControllerTransform == null evalúa a true
                if (sim.cameraTransform == null || sim.rightControllerTransform == null || !sim.pointAndClickActive)
                {
                    RelinkNow();
                }
            }
        }

        public static void RelinkNow()
        {
            try
            {
                // 1. Identificar cámara principal de la escena actual
                var mainCam = Camera.main;
                Transform camTransform = (mainCam != null) ? mainCam.transform : null;

                // 2. Identificar XRInputModalityManager en la nueva escena
                var modality = UnityEngine.Object.FindAnyObjectByType<XRInputModalityManager>();
                Transform leftT = null;
                Transform rightT = null;

                if (modality != null)
                {
                    if (modality.leftController != null)
                    {
                        if (!modality.leftController.activeSelf)
                            modality.leftController.SetActive(true);

                        var tpd = modality.leftController.GetComponentInChildren<TrackedPoseDriver>();
                        leftT = (tpd != null) ? tpd.transform : modality.leftController.transform;
                    }

                    if (modality.rightController != null)
                    {
                        if (!modality.rightController.activeSelf)
                            modality.rightController.SetActive(true);

                        var tpd = modality.rightController.GetComponentInChildren<TrackedPoseDriver>();
                        rightT = (tpd != null) ? tpd.transform : modality.rightController.transform;
                    }

                    // Actualizar el caché estático interno de ComponentLocatorUtility<XRInputModalityManager>
                    try
                    {
                        var locatorType = typeof(XRInteractionSimulator).Assembly.GetType("UnityEngine.XR.Interaction.Toolkit.Utilities.ComponentLocatorUtility`1")
                            ?.MakeGenericType(typeof(XRInputModalityManager));
                        var setCacheMethod = locatorType?.GetMethod("SetComponentCache", BindingFlags.Static | BindingFlags.NonPublic);
                        setCacheMethod?.Invoke(null, new object[] { modality });
                    }
                    catch
                    {
                        // Fallback silencioso si no se encuentra
                    }
                }

                // Fallbacks si no se encontró modalidad
                if (leftT == null) leftT = camTransform;
                if (rightT == null) rightT = camTransform;

                // 3. Re-vincular XRInteractionSimulator (XRI 3.x)
                var simulators = UnityEngine.Object.FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Include);
                foreach (var sim in simulators)
                {
                    if (sim == null) continue;

                    var simType = typeof(XRInteractionSimulator);

                    // Restaurar DeviceLifecycleManager si por algún motivo fue nulificado
                    var dlmField = simType.GetField("m_DeviceLifecycleManager", BindingFlags.Instance | BindingFlags.NonPublic);
                    var dlm = dlmField?.GetValue(sim) as SimulatedDeviceLifecycleManager;
                    if (dlm == null)
                    {
                        dlm = sim.GetComponent<SimulatedDeviceLifecycleManager>()
                            ?? sim.GetComponentInChildren<SimulatedDeviceLifecycleManager>()
                            ?? UnityEngine.Object.FindAnyObjectByType<SimulatedDeviceLifecycleManager>();

                        if (dlm == null)
                        {
                            dlm = sim.gameObject.AddComponent<SimulatedDeviceLifecycleManager>();
                        }
                        dlmField?.SetValue(sim, dlm);
                    }

                    // Asignar transforms públicos directamente (SIN deshabilitar el simulador para no desfasar la posición)
                    if (camTransform != null) sim.cameraTransform = camTransform;
                    if (leftT != null) sim.leftControllerTransform = leftT;
                    if (rightT != null) sim.rightControllerTransform = rightT;
                    sim.leftHandAimTransform = leftT;
                    sim.rightHandAimTransform = rightT;

                    // Modo de entrada por defecto: FPS para rotación de cámara con click derecho, y RightDevice para mover mano con mouse
                    sim.targetedDeviceInput = TargetedDevices.FPS | TargetedDevices.RightDevice;

                    // Ajustar campos internos de Point-and-Click y física de la escena actual
                    try
                    {
                        if (mainCam != null)
                        {
                            simType.GetField("m_CachedCamera", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, (camTransform, mainCam));
                        }

                        simType.GetField("m_CanUsePointAndClickControllers", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, true);
                        simType.GetField("m_CanUsePointAndClickHands", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, true);
                        simType.GetField("m_PointAndClickActive", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, true);
                        simType.GetField("m_UsePointAndClick", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, true);
                        simType.GetField("m_PreviousRaycastHitDistance", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, 0f);

                        var activeScene = SceneManager.GetActiveScene();
                        if (activeScene.IsValid())
                        {
                            simType.GetField("m_LocalPhysicsScene", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, activeScene.GetPhysicsScene());
                        }

                        // Resetear UIInputModule para que lo busque limpiamente en el nuevo EventSystem
                        simType.GetField("m_UIInputModule", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(sim, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[XRSceneRelinker] Advertencia al ajustar campos del simulador: {ex.Message}");
                    }
                }

                // 4. Re-vincular XRDeviceSimulator clásico (si estuviera presente)
                var classicSims = UnityEngine.Object.FindObjectsByType<XRDeviceSimulator>(FindObjectsInactive.Include);
                foreach (var sim in classicSims)
                {
                    if (sim == null) continue;
                    if (camTransform != null) sim.cameraTransform = camTransform;
                }

                // 5. Refrescar InputActionManager en la nueva escena
                var actionManagers = UnityEngine.Object.FindObjectsByType<InputActionManager>(FindObjectsInactive.Include);
                foreach (var iam in actionManagers)
                {
                    if (iam == null) continue;
                    iam.DisableInput();
                    iam.EnableInput();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XRSceneRelinker] Error durante RelinkNow: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
