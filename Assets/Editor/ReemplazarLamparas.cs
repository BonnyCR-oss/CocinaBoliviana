using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor script que reemplaza los GameObjects Lamp_* hijos de Ceiling_Lamps
/// por instancias del prefab Lamp_prime en todas las escenas de cocina.
/// </summary>
public class ReemplazarLamparas : EditorWindow
{
    [MenuItem("Tools/Cocina Boliviana/Reemplazar Lamparas con Lamp_prime")]
    public static void Ejecutar()
    {
        string[] escenas = new string[]
        {
            "Assets/00_Scenes/First Scene.unity",
            "Assets/00_Scenes/Nivel 1 - Cochabamba.unity",
            "Assets/00_Scenes/Nivel 2 - La Paz.unity",
            "Assets/00_Scenes/Nivel 3 - Santa Cruz.unity"
        };

        // Cargar el prefab Lamp_prime
        GameObject lampPrimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/02_Prefabs/Lamp_prime.prefab");
        if (lampPrimePrefab == null)
        {
            Debug.LogError("[ReemplazarLamparas] No se encontró el prefab Lamp_prime en Assets/02_Prefabs/Lamp_prime.prefab");
            return;
        }

        // Guardar escena activa para restaurarla después
        string escenaActiva = SceneManager.GetActiveScene().path;

        foreach (string rutaEscena in escenas)
        {
            Scene escena = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);

            bool modificado = ProcesarEscena(escena, lampPrimePrefab);

            if (modificado)
            {
                EditorSceneManager.SaveScene(escena);
                Debug.Log($"[ReemplazarLamparas] ✓ Escena guardada: {rutaEscena}");
            }
            else
            {
                Debug.LogWarning($"[ReemplazarLamparas] Sin cambios en: {rutaEscena}");
            }
        }

        // Restaurar escena original si es posible
        if (!string.IsNullOrEmpty(escenaActiva))
            EditorSceneManager.OpenScene(escenaActiva, OpenSceneMode.Single);

        Debug.Log("[ReemplazarLamparas] ¡Proceso completado en todas las escenas!");
    }

    private static bool ProcesarEscena(Scene escena, GameObject lampPrimePrefab)
    {
        bool modificado = false;

        // Buscar el GameObject "Ceiling_Lamps"
        GameObject[] roots = escena.GetRootGameObjects();
        GameObject ceilingLamps = BuscarRecursivo(roots, "Ceiling_Lamps");

        if (ceilingLamps == null)
        {
            Debug.LogWarning($"[ReemplazarLamparas] No se encontró Ceiling_Lamps en {escena.name}");
            return false;
        }

        Transform ceilingTransform = ceilingLamps.transform;

        // Recopilar hijos Lamp_* a eliminar
        var hijosAEliminar = new System.Collections.Generic.List<Transform>();
        var posiciones = new System.Collections.Generic.List<Vector3>();

        for (int i = 0; i < ceilingTransform.childCount; i++)
        {
            Transform hijo = ceilingTransform.GetChild(i);
            if (hijo.name.StartsWith("Lamp_"))
            {
                hijosAEliminar.Add(hijo);
                posiciones.Add(hijo.localPosition);
            }
        }

        if (hijosAEliminar.Count == 0)
        {
            Debug.LogWarning($"[ReemplazarLamparas] No se encontraron hijos Lamp_* en Ceiling_Lamps en {escena.name}");
            return false;
        }

        // Eliminar los hijos Lamp_* existentes
        string[] nombresOriginales = new string[hijosAEliminar.Count];
        for (int i = 0; i < hijosAEliminar.Count; i++)
        {
            nombresOriginales[i] = hijosAEliminar[i].name;
            Object.DestroyImmediate(hijosAEliminar[i].gameObject);
        }

        // Instanciar Lamp_prime en cada posición
        string[] nombres = new string[] { "Lamp_NorthWest", "Lamp_NorthEast", "Lamp_SouthWest", "Lamp_SouthEast" };
        Vector3[] posicionesStd = new Vector3[]
        {
            new Vector3(-2.2f, 2.9f, 1.2f),
            new Vector3(2.2f, 2.9f, 1.2f),
            new Vector3(-2.2f, 2.9f, -1.2f),
            new Vector3(2.2f, 2.9f, -1.2f)
        };

        // Usar posiciones originales si las tenemos, sino las estándar
        if (posiciones.Count == 4)
        {
            posicionesStd = posiciones.ToArray();
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(lampPrimePrefab, escena);
            instancia.name = (i < nombresOriginales.Length) ? nombresOriginales[i] : nombres[i];
            instancia.transform.SetParent(ceilingTransform, false);
            instancia.transform.localPosition = (i < posicionesStd.Length) ? posicionesStd[i] : posicionesStd[i % posicionesStd.Length];
            instancia.transform.localRotation = Quaternion.identity;
            instancia.transform.localScale = Vector3.one;
        }

        modificado = true;
        Debug.Log($"[ReemplazarLamparas] Reemplazadas {hijosAEliminar.Count} lámparas en {escena.name}");
        return modificado;
    }

    private static GameObject BuscarRecursivo(GameObject[] roots, string nombre)
    {
        foreach (GameObject root in roots)
        {
            GameObject resultado = BuscarEnJerarquia(root.transform, nombre);
            if (resultado != null) return resultado;
        }
        return null;
    }

    private static GameObject BuscarEnJerarquia(Transform padre, string nombre)
    {
        if (padre.name == nombre) return padre.gameObject;
        for (int i = 0; i < padre.childCount; i++)
        {
            GameObject resultado = BuscarEnJerarquia(padre.GetChild(i), nombre);
            if (resultado != null) return resultado;
        }
        return null;
    }
}
