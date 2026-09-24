using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Deja el proyecto listo para probar en el Meta Quest, ya sea con un APK (Android)
    /// o con Meta Horizon Link desde Windows (Standalone). Usa la API de XR Management/OpenXR
    /// en vez de editar los .asset a mano, para que Unity no pise los cambios.
    /// </summary>
    public static class QuestTestingSetup
    {
        private const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string SimulatorSettingsPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";

        [MenuItem("Kitchen/XR/Preparar para probar en el Quest")]
        public static void PrepararParaQuest()
        {
            ConfigurarTarget(BuildTargetGroup.Android);
            ConfigurarTarget(BuildTargetGroup.Standalone);
            SetSimuladorAutomatico(false);
            AssetDatabase.SaveAssets();
            Debug.Log("[QuestTestingSetup] Listo para el Quest: OpenXR inicia al arrancar, perfiles de control Touch activos y simulador desactivado. " +
                      "Usa 'Kitchen > XR > Volver al simulador' para probar otra vez sin visor.");
        }

        [MenuItem("Kitchen/XR/Volver al simulador")]
        public static void VolverAlSimulador()
        {
            SetSimuladorAutomatico(true);
            AssetDatabase.SaveAssets();
            Debug.Log("[QuestTestingSetup] Simulador de XRI activado otra vez para Play mode.");
        }

        private static void ConfigurarTarget(BuildTargetGroup group)
        {
            XRGeneralSettings general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            if (general == null || general.Manager == null)
            {
                Debug.LogError($"[QuestTestingSetup] No hay XR General Settings para {group}. Abre Project Settings > XR Plug-in Management una vez y vuelve a correr el comando.");
                return;
            }

            // Sin esto, al darle Play con el visor conectado por Link no se inicia XR (en PC estaba apagado)
            general.InitManagerOnStart = true;
            EditorUtility.SetDirty(general);

            if (!XRPackageMetadataStore.IsLoaderAssigned(OpenXRLoaderType, group))
            {
                XRPackageMetadataStore.AssignLoader(general.Manager, OpenXRLoaderType, group);
            }

            OpenXRSettings openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (openXR == null)
            {
                Debug.LogError($"[QuestTestingSetup] No se encontraron OpenXR Settings para {group}.");
                return;
            }

            // Sin un perfil de interacción OpenXR no entrega pose ni botones de los controles:
            // la mano queda congelada y no se puede agarrar nada.
            ActivarFeature<OculusTouchControllerProfile>(openXR, group);
            ActivarFeature<MetaQuestTouchPlusControllerProfile>(openXR, group);
            EditorUtility.SetDirty(openXR);
        }

        private static void ActivarFeature<T>(OpenXRSettings openXR, BuildTargetGroup group) where T : OpenXRFeature
        {
            T feature = openXR.GetFeature<T>();
            if (feature == null)
            {
                Debug.LogWarning($"[QuestTestingSetup] {typeof(T).Name} no está disponible para {group}.");
                return;
            }

            if (!feature.enabled)
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
                Debug.Log($"[QuestTestingSetup] {typeof(T).Name} activado en {group}.");
            }
        }

        private static void SetSimuladorAutomatico(bool activo)
        {
            // XRDeviceSimulatorSettings es internal en XRI, por eso se edita con SerializedObject
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SimulatorSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"[QuestTestingSetup] No se encontró {SimulatorSettingsPath}; cambia el simulador desde Project Settings > XR Plug-in Management > XR Interaction Toolkit.");
                return;
            }

            var so = new SerializedObject(settings);
            SerializedProperty prop = so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab");
            if (prop == null)
            {
                Debug.LogWarning("[QuestTestingSetup] No se encontró la opción del simulador en XRDeviceSimulatorSettings.");
                return;
            }

            prop.boolValue = activo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
