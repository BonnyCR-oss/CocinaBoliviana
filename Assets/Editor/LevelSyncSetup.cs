using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Herramientas para mantener iguales las tres escenas de nivel:
    ///  - copiar las zonas de detección de la olla y el sartén de Cochabamba a La Paz y Santa Cruz,
    ///  - añadir un segundo plato de emplatado en cada nivel.
    ///
    /// Cochabamba es la escena "patrón": ajusta ahí y luego sincroniza.
    /// </summary>
    public static class LevelSyncSetup
    {
        private const string Patron = "Assets/00_Scenes/Nivel 1 - Cochabamba.unity";

        private static readonly string[] Destinos =
        {
            "Assets/00_Scenes/Nivel 2 - La Paz.unity",
            "Assets/00_Scenes/Nivel 3 - Santa Cruz.unity",
        };

        private static readonly string[] TodosLosNiveles =
        {
            Patron,
            "Assets/00_Scenes/Nivel 2 - La Paz.unity",
            "Assets/00_Scenes/Nivel 3 - Santa Cruz.unity",
        };

        /// <summary>Solo la zona: lo que se ajusta a mano para que el recipiente detecte bien.</summary>
        private struct Zona
        {
            public Vector3 tamano;
            public float altura;
            public Vector3 desplazamiento;
        }

        // ------------------------------------------------------------- zonas de cocción

        [MenuItem("Kitchen/Niveles/Copiar zonas de olla y sartén (Cochabamba → La Paz y Santa Cruz)")]
        public static void CopiarZonas()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Dictionary<MetodoCoccion, Zona> zonas = LeerZonas(Patron);
            if (zonas.Count == 0)
            {
                Debug.LogError("[LevelSyncSetup] No encontré olla ni sartén en Cochabamba.");
                return;
            }

            foreach (string ruta in Destinos)
            {
                Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                int cambiados = 0;

                foreach (var vessel in Object.FindObjectsByType<CookingVessel>(FindObjectsInactive.Include))
                {
                    // La parrilla tiene su propia zona, calculada por GrillSetup: no se toca.
                    if (vessel.Metodo == MetodoCoccion.Asar) continue;
                    if (!zonas.TryGetValue(vessel.Metodo, out Zona z)) continue;

                    var so = new SerializedObject(vessel);
                    so.FindProperty("zonaDeteccion").vector3Value = z.tamano;
                    so.FindProperty("zonaAltura").floatValue = z.altura;
                    so.FindProperty("zonaDesplazamiento").vector3Value = z.desplazamiento;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    cambiados++;

                    Debug.Log($"[LevelSyncSetup] {escena.name}: '{vessel.name}' ({vessel.Metodo}) ← zona de Cochabamba.");
                }

                if (cambiados > 0)
                {
                    EditorSceneManager.MarkSceneDirty(escena);
                    EditorSceneManager.SaveScene(escena);
                }
            }

            EditorSceneManager.OpenScene(Patron, OpenSceneMode.Single);
            Debug.Log("[LevelSyncSetup] Zonas de olla y sartén copiadas a La Paz y Santa Cruz.");
        }

        private static Dictionary<MetodoCoccion, Zona> LeerZonas(string ruta)
        {
            var zonas = new Dictionary<MetodoCoccion, Zona>();
            EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);

            foreach (var vessel in Object.FindObjectsByType<CookingVessel>(FindObjectsInactive.Include))
            {
                if (vessel.Metodo == MetodoCoccion.Asar || zonas.ContainsKey(vessel.Metodo)) continue;

                var so = new SerializedObject(vessel);
                zonas[vessel.Metodo] = new Zona
                {
                    tamano = so.FindProperty("zonaDeteccion").vector3Value,
                    altura = so.FindProperty("zonaAltura").floatValue,
                    desplazamiento = so.FindProperty("zonaDesplazamiento").vector3Value,
                };
            }
            return zonas;
        }

        // ------------------------------------------------------------- segundo plato

        private const string PlatePrefabPath = "Assets/02_Prefabs/Plate_Item.prefab";
        private const string MesaSegundoPlato = "Counter_Island_02";
        private const string NombreSegundoPlato = "Plate_Item_2";

        [MenuItem("Kitchen/Niveles/Añadir segundo plato de emplatado (3 niveles)")]
        public static void AnadirSegundoPlato()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LevelSyncSetup] No existe {PlatePrefabPath}.");
                return;
            }

            foreach (string ruta in TodosLosNiveles)
            {
                Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                var platos = Object.FindObjectsByType<PlateItem>(FindObjectsInactive.Include);

                if (platos.Length >= 2 || GameObject.Find(NombreSegundoPlato) != null)
                {
                    Debug.Log($"[LevelSyncSetup] {escena.name}: ya tiene {platos.Length} platos; se deja como está.");
                    continue;
                }

                GameObject mesa = GameObject.Find(MesaSegundoPlato);
                if (mesa == null || !TryGetBounds(mesa, out Bounds b))
                {
                    Debug.LogWarning($"[LevelSyncSetup] {escena.name}: no encontré '{MesaSegundoPlato}'; " +
                                     "coloca el segundo plato a mano duplicando Plate_Item.");
                    continue;
                }

                // Misma altura que el plato que ya hay: las mesas del set son todas iguales, y
                // así queda apoyado igual que el original.
                float y = (platos.Length > 0) ? platos[0].transform.position.y : b.max.y + 0.01f;
                Quaternion giro = (platos.Length > 0) ? platos[0].transform.rotation : Quaternion.identity;

                var nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
                nuevo.name = NombreSegundoPlato;
                nuevo.transform.SetPositionAndRotation(new Vector3(b.center.x, y, b.center.z), giro);

                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                Debug.Log($"[LevelSyncSetup] {escena.name}: segundo plato sobre '{MesaSegundoPlato}'. " +
                          "Muévelo en la Hierarchy si lo quieres en otro sitio.");
            }

            EditorSceneManager.OpenScene(Patron, OpenSceneMode.Single);
        }

        // ------------------------------------------------------- sonido de nuevo pedido

        private const string SonidoNuevoPedidoPath = "Assets/06_SFX/SonidoNuevoPedido.mp3";

        /// <summary>
        /// En el editor el OrderManager lo carga solo, pero en la build del Quest tiene que
        /// estar asignado en cada escena.
        /// </summary>
        [MenuItem("Kitchen/Niveles/Asignar sonido de nuevo pedido (3 niveles)")]
        public static void AsignarSonidoNuevoPedido()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SonidoNuevoPedidoPath);
            if (clip == null)
            {
                Debug.LogError($"[LevelSyncSetup] No existe {SonidoNuevoPedidoPath}.");
                return;
            }

            foreach (string ruta in TodosLosNiveles)
            {
                Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                var manager = Object.FindAnyObjectByType<OrderManager>();
                if (manager == null)
                {
                    Debug.LogWarning($"[LevelSyncSetup] {escena.name}: no tiene OrderManager.");
                    continue;
                }

                var so = new SerializedObject(manager);
                var prop = so.FindProperty("sonidoNuevoPedido");
                if (prop.objectReferenceValue != null)
                {
                    Debug.Log($"[LevelSyncSetup] {escena.name}: ya tiene sonido de nuevo pedido; se respeta.");
                    continue;
                }

                prop.objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
                Debug.Log($"[LevelSyncSetup] {escena.name}: sonido de nuevo pedido asignado.");
            }

            EditorSceneManager.OpenScene(Patron, OpenSceneMode.Single);
        }

        private static bool TryGetBounds(GameObject go, out Bounds b)
        {
            b = default;
            bool alguno = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!alguno) { b = r.bounds; alguno = true; }
                else b.Encapsulate(r.bounds);
            }
            return alguno;
        }
    }
}
