using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CocinaBoliviana
{
    /// <summary>
    /// Componente de mejora estética y game feeling en tiempo de ejecución.
    /// Se ejecuta automáticamente al cargar cualquier escena (Niveles 1, 2, 3, First Scene y Main Menu):
    /// 1. Convierte los cajones de ingredientes en auténticos huacales rústicos de madera con listones, herrajes, placas de pizarra y producto 3D en su interior.
    /// 2. Transforma la mesa central en una isla de chef profesional con patas tubulares, estante inferior con bandejas, tabla de picar encastrada y toallero.
    /// 3. Detalla los extractores con ductos con bridas y pernos, panel de control comercial con manómetro analógico y luz halógena cálida.
    /// 4. Embellece la zona de entrega con riel de comandas, tickets de pedidos, peana de caoba bajo la campana y franjas hazard.
    /// 5. En el Main Menu, añade mostrador de recepción de restaurante, lámparas colgantes y atrezzo culinario.
    /// </summary>
    public class KitchenVisualsEnhancer : MonoBehaviour
    {
        private static bool cargandoMateriales = false;

        private static Material matWood;
        private static Material matDarkMetal;
        private static Material matSteelTable;
        private static Material matFilter;
        private static Material matHood;
        private static Material matBell;
        private static Material matCutting;
        private static Material matHazard;
        private static Material matDeliverySign;
        private static Material matLabelPapas;
        private static Material matLabelCarne;
        private static Material matLabelVerduras;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlCargarEscena()
        {
            Scene scene = SceneManager.GetActiveScene();
            CargarMateriales();

            if (scene.name.Contains("Menu"))
            {
                MejorarMainMenu();
            }
            else
            {
                MejorarCocina();
            }
        }

        private static void CargarMateriales()
        {
            if (cargandoMateriales) return;
            cargandoMateriales = true;

#if UNITY_EDITOR
            matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Pantry.mat")
                   ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Menu_Wood.mat");
            matDarkMetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Dark_Metal.mat");
            matSteelTable = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Kitchen_Island_Steel.mat")
                         ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_CounterTop.mat");
            matFilter = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Extractor_Filter.mat");
            matHood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Metal_Hood.mat");
            matBell = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Brass_Bell.mat");
            matCutting = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Cutting_Board.mat")
                      ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Board.mat");
            matHazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Hazard_Stripe.mat");
            matDeliverySign = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Delivery_Sign.mat");
            matLabelPapas = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Label_Papas.mat");
            matLabelCarne = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Label_Carne.mat");
            matLabelVerduras = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Label_Verduras.mat");
#endif

            // Fallback con URP Lit si no están en editor o no cargan
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (matWood == null) { matWood = new Material(lit); matWood.color = new Color(0.48f, 0.31f, 0.18f); }
            if (matDarkMetal == null) { matDarkMetal = new Material(lit); matDarkMetal.color = new Color(0.18f, 0.19f, 0.21f); matDarkMetal.SetFloat("_Metallic", 0.88f); }
            if (matSteelTable == null) { matSteelTable = new Material(lit); matSteelTable.color = new Color(0.90f, 0.92f, 0.94f); matSteelTable.SetFloat("_Metallic", 0.95f); matSteelTable.SetFloat("_Smoothness", 0.85f); }
            if (matBell == null) { matBell = new Material(lit); matBell.color = new Color(0.98f, 0.80f, 0.28f); matBell.SetFloat("_Metallic", 0.98f); matBell.SetFloat("_Smoothness", 0.95f); }
            if (matCutting == null) { matCutting = new Material(lit); matCutting.color = new Color(0.72f, 0.52f, 0.32f); }
        }

        // ==============================================================
        // MEJORAS EN LA ESCENA DE COCINA (NIVELES 1, 2, 3 Y FIRST SCENE)
        // ==============================================================
        public static void MejorarCocina()
        {
            // 1. CAJONES DE INGREDIENTES HUACALES
            MejorarCajon("Cajon_Papas", matLabelPapas, 0);
            MejorarCajon("Cajon_Carne", matLabelCarne, 1);
            MejorarCajon("Cajon_Verduras", matLabelVerduras, 2);

            // 2. MESA CENTRAL DE CHEF
            MejorarMesaCentral();

            // 3. EXTRACTORES
            MejorarExtractores();

            // 4. ZONA DE ENTREGA
            MejorarZonaEntrega();
        }

        private static void MejorarCajon(string crateName, Material matLabel, int tipoContenido)
        {
            GameObject crate = GameObject.Find(crateName);
            if (crate == null) return;

            // Ocultar cubo monolítico plano original
            var rend = crate.GetComponent<MeshRenderer>();
            if (rend != null) rend.enabled = false;

            // Ocultar o destruir Produce anterior simple
            Transform oldProduce = crate.transform.Find("Produce");
            if (oldProduce != null) oldProduce.gameObject.SetActive(false);

            // Eliminar cualquier Contenido_Interior previo si existiera
            foreach (Transform t in crate.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == "Contenido_Interior")
                {
                    DestroyImmediate(t.gameObject);
                }
            }

            // Si ya tiene el visual mejorado, no duplicar
            if (crate.transform.Find("Crate_Visual") != null || crate.transform.Find("Caja_Visual") != null) return;

            Transform visual = new GameObject("Crate_Visual").transform;
            visual.SetParent(crate.transform, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;

            // 1. Base / Fondo del huacal
            CrearPieza(visual, PrimitiveType.Cube, "Fondo_Madera",
                new Vector3(0f, -0.42f, 0f), new Vector3(0.92f, 0.08f, 0.92f), matWood);

            // 2. Cuatro postes esquineros
            float cx = 0.44f;
            float cz = 0.44f;
            CrearPieza(visual, PrimitiveType.Cube, "Poste_NW", new Vector3(-cx, 0f, cz), new Vector3(0.12f, 0.88f, 0.12f), matWood);
            CrearPieza(visual, PrimitiveType.Cube, "Poste_NE", new Vector3(cx, 0f, cz), new Vector3(0.12f, 0.88f, 0.12f), matWood);
            CrearPieza(visual, PrimitiveType.Cube, "Poste_SW", new Vector3(-cx, 0f, -cz), new Vector3(0.12f, 0.88f, 0.12f), matWood);
            CrearPieza(visual, PrimitiveType.Cube, "Poste_SE", new Vector3(cx, 0f, -cz), new Vector3(0.12f, 0.88f, 0.12f), matWood);

            // 3. Tablones horizontales separados (slats de huacal)
            float[] alturasSlats = new[] { -0.28f, 0.02f, 0.32f };
            for (int i = 0; i < alturasSlats.Length; i++)
            {
                float y = alturasSlats[i];
                CrearPieza(visual, PrimitiveType.Cube, $"Slat_Front_{i}", new Vector3(0f, y, -cz), new Vector3(0.96f, 0.22f, 0.04f), matWood);
                CrearPieza(visual, PrimitiveType.Cube, $"Slat_Back_{i}", new Vector3(0f, y, cz), new Vector3(0.96f, 0.22f, 0.04f), matWood);
                CrearPieza(visual, PrimitiveType.Cube, $"Slat_Left_{i}", new Vector3(-cx, y, 0f), new Vector3(0.04f, 0.22f, 0.96f), matWood);
                CrearPieza(visual, PrimitiveType.Cube, $"Slat_Right_{i}", new Vector3(cx, y, 0f), new Vector3(0.04f, 0.22f, 0.96f), matWood);
            }

            // 4. Herrajes de hierro oscuro en esquinas
            float[] alturasH = new[] { -0.38f, 0.38f };
            foreach (float y in alturasH)
            {
                CrearPieza(visual, PrimitiveType.Cube, "Herraje_NW", new Vector3(-cx, y, cz), new Vector3(0.14f, 0.14f, 0.14f), matDarkMetal);
                CrearPieza(visual, PrimitiveType.Cube, "Herraje_NE", new Vector3(cx, y, cz), new Vector3(0.14f, 0.14f, 0.14f), matDarkMetal);
                CrearPieza(visual, PrimitiveType.Cube, "Herraje_SW", new Vector3(-cx, y, -cz), new Vector3(0.14f, 0.14f, -cz), matDarkMetal);
                CrearPieza(visual, PrimitiveType.Cube, "Herraje_SE", new Vector3(cx, y, -cz), new Vector3(0.14f, 0.14f, -cz), matDarkMetal);
            }

            // 5. Placa frontal identificadora (Pizarra oscura)
            CrearPieza(visual, PrimitiveType.Cube, "Placa_Marco", new Vector3(0f, 0.02f, -cz - 0.025f), new Vector3(0.72f, 0.36f, 0.03f), matWood);
            CrearPieza(visual, PrimitiveType.Cube, "Placa_Pizarra", new Vector3(0f, 0.02f, -cz - 0.045f), new Vector3(0.66f, 0.30f, 0.02f), matLabel ?? matWood);
            // El interior se mantiene despejado y limpio sin elementos falsos
        }

        private static void MejorarMesaCentral()
        {
            string[] estaciones = new[] { "AssemblyStation", "Counter_Island_01", "Counter_Island_02" };
            foreach (var estNombre in estaciones)
            {
                GameObject estacion = GameObject.Find(estNombre);
                if (estacion == null) continue;

                // Asignar acero inoxidable cepillado a la superficie
                Transform mesa = estacion.transform.Find("Mesa");
                if (mesa != null && matSteelTable != null)
                {
                    var r = mesa.GetComponent<MeshRenderer>();
                    if (r != null) r.sharedMaterial = matSteelTable;
                }

                Transform topTrim = estacion.transform.Find("TopTrim");
                if (topTrim != null && matSteelTable != null)
                {
                    var r = topTrim.GetComponent<MeshRenderer>();
                    if (r != null) r.sharedMaterial = matSteelTable;
                }

                if (estacion.transform.Find("Island_Details") != null) continue;

                Transform detalles = new GameObject("Island_Details").transform;
                detalles.SetParent(estacion.transform, false);
                detalles.localPosition = Vector3.zero;
                detalles.localRotation = Quaternion.identity;

                Vector3 mesaScale = mesa != null ? mesa.localScale : new Vector3(1f, 0.9f, 0.8f);
                float hx = mesaScale.x * 0.46f;
                float hz = mesaScale.z * 0.46f;

                // 4 patas tubulares cilíndricas con regatón nivelador en la base
                CrearPata(detalles, "Pata_NW", new Vector3(-hx, -0.05f, hz), 0.045f, 0.88f, matDarkMetal);
                CrearPata(detalles, "Pata_NE", new Vector3(hx, -0.05f, hz), 0.045f, 0.88f, matDarkMetal);
                CrearPata(detalles, "Pata_SW", new Vector3(-hx, -0.05f, -hz), 0.045f, 0.88f, matDarkMetal);
                CrearPata(detalles, "Pata_SE", new Vector3(hx, -0.05f, -hz), 0.045f, 0.88f, matDarkMetal);

                // Estante inferior abierto
                CrearPieza(detalles, PrimitiveType.Cube, "Estante_Inferior",
                    new Vector3(0f, -0.32f, 0f), new Vector3(mesaScale.x * 0.92f, 0.03f, mesaScale.z * 0.88f), matDarkMetal);

                // Bandeja gastronorm en estante inferior
                CrearPieza(detalles, PrimitiveType.Cube, "Bandeja_Gastro",
                    new Vector3(0f, -0.28f, 0f), new Vector3(mesaScale.x * 0.55f, 0.04f, mesaScale.z * 0.50f), matSteelTable);

                // Bisel frontal redondeado
                CrearPieza(detalles, PrimitiveType.Cube, "Bisel_Frontal",
                    new Vector3(0f, 0.47f, -hz - 0.02f), new Vector3(mesaScale.x * 1.02f, 0.04f, 0.04f), matSteelTable);

                if (estNombre == "Counter_Island_01")
                {
                    // Tabla de cortar encastrada
                    CrearPieza(detalles, PrimitiveType.Cube, "Tabla_Incrustada",
                        new Vector3(0f, 0.485f, 0f), new Vector3(0.58f, 0.035f, 0.52f), matCutting);
                }
                else if (estNombre == "Counter_Island_02")
                {
                    // Toallero lateral y paño
                    var barra = CrearPieza(detalles, PrimitiveType.Cylinder, "Toallero",
                        new Vector3(hx + 0.06f, 0.38f, 0f), new Vector3(0.02f, 0.32f, 0.02f), matSteelTable);
                    barra.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    Material mPano = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.92f, 0.92f, 0.95f) };
                    CrearPieza(detalles, PrimitiveType.Cube, "Pano",
                        new Vector3(hx + 0.07f, 0.26f, 0f), new Vector3(0.02f, 0.22f, 0.24f), mPano);
                }
            }
        }

        private static void MejorarExtractores()
        {
            var todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var t in todos)
            {
                if (t.name != "Extractor_Cocina" && t.name != "Extractor_Entrega") continue;

                // Asignar materiales PBR
                foreach (Transform child in t)
                {
                    if (child.name.StartsWith("Campana_") && matHood != null)
                    {
                        var r = child.GetComponent<MeshRenderer>();
                        if (r != null) r.sharedMaterial = matHood;
                    }
                    else if (child.name.StartsWith("Filtro_") && matFilter != null)
                    {
                        var r = child.GetComponent<MeshRenderer>();
                        if (r != null) r.sharedMaterial = matFilter;
                    }
                }

                if (t.Find("Detalles_Campana") != null) continue;

                Transform detalles = new GameObject("Detalles_Campana").transform;
                detalles.SetParent(t, false);
                detalles.localPosition = Vector3.zero;
                detalles.localRotation = Quaternion.identity;

                // Brida del ducto al techo
                CrearPieza(detalles, PrimitiveType.Cylinder, "Brida_Techo",
                    new Vector3(0f, 0.85f, 0f), new Vector3(0.42f, 0.04f, 0.42f), matDarkMetal);

                // Anillo intermedio
                CrearPieza(detalles, PrimitiveType.Cylinder, "Brida_Medio",
                    new Vector3(0f, 0.60f, 0f), new Vector3(0.38f, 0.03f, 0.38f), matDarkMetal);

                // Caja de control comercial frontal
                bool esCocina = t.name == "Extractor_Cocina";
                float frontZ = esCocina ? -0.44f : -0.96f;
                CrearPieza(detalles, PrimitiveType.Cube, "Panel_Control",
                    new Vector3(0f, -0.08f, frontZ), new Vector3(0.32f, 0.12f, 0.05f), matDarkMetal);

                // Interruptor ON/OFF con LED
                Material mSw = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mSw.color = new Color(0.15f, 0.85f, 0.25f);
                mSw.SetColor("_EmissionColor", new Color(0.15f, 0.85f, 0.25f) * 2.5f);
                mSw.EnableKeyword("_EMISSION");
                CrearPieza(detalles, PrimitiveType.Cube, "Switch",
                    new Vector3(-0.09f, -0.08f, frontZ - 0.035f), new Vector3(0.04f, 0.05f, 0.03f), mSw);

                // Manómetro analógico
                Material mGauge = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.white };
                var dial = CrearPieza(detalles, PrimitiveType.Cylinder, "Manometro",
                    new Vector3(0.08f, -0.08f, frontZ - 0.03f), new Vector3(0.07f, 0.015f, 0.07f), mGauge);
                dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private static void MejorarZonaEntrega()
        {
            GameObject delivery = GameObject.Find("DeliveryStation");
            if (delivery == null) return;

            // Franjas de seguridad en el riel
            Transform rail = delivery.transform.Find("Guide_Rail_Outer");
            if (rail != null && matHazard != null)
            {
                var r = rail.GetComponent<MeshRenderer>();
                if (r != null) r.sharedMaterial = matHazard;
            }

            // Peana de madera bajo la campana de servicio
            Transform campana = delivery.transform.Find("Campana_Servicio");
            if (campana != null)
            {
                if (campana.Find("Peana_Madera") == null)
                {
                    var p = CrearPieza(campana, PrimitiveType.Cylinder, "Peana_Madera",
                        new Vector3(0f, -0.45f, 0f), new Vector3(1.35f, 0.25f, 1.35f), matWood);
                }
            }

            // Riel de comandas suspendido con tickets de pedidos
            if (delivery.transform.Find("Riel_Comandas") == null)
            {
                Transform riel = new GameObject("Riel_Comandas").transform;
                riel.SetParent(delivery.transform, false);
                riel.localPosition = new Vector3(-0.35f, 1.38f, 0f);

                CrearPieza(riel, PrimitiveType.Cube, "Barra_Riel", Vector3.zero, new Vector3(0.04f, 0.03f, 1.25f), matDarkMetal);

                if (matDeliverySign != null)
                {
                    CrearPieza(riel, PrimitiveType.Cube, "Letrero_Entrega",
                        new Vector3(0f, 0.16f, 0f), new Vector3(0.04f, 0.24f, 0.95f), matDeliverySign);
                }

                Material mTicket = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.98f, 0.96f, 0.88f) };
                float[] zTickets = new[] { -0.35f, 0.0f, 0.35f };
                for (int i = 0; i < zTickets.Length; i++)
                {
                    CrearPieza(riel, PrimitiveType.Cube, $"Clip_{i}", new Vector3(0.02f, 0f, zTickets[i]), new Vector3(0.025f, 0.04f, 0.04f), matDarkMetal);
                    var t = CrearPieza(riel, PrimitiveType.Cube, $"Ticket_{i}", new Vector3(0.02f, -0.15f, zTickets[i]), new Vector3(0.01f, 0.26f, 0.16f), mTicket);
                    t.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 3f);
                }
            }
        }

        // ==============================================================
        // MEJORAS EN LA ESCENA DE MAIN MENU
        // ==============================================================
        public static void MejorarMainMenu()
        {
            // Mantener el Main Menu diáfano, espacioso y limpio según diseño original
            var desk = GameObject.Find("Reception_Desk");
            if (desk != null) DestroyImmediate(desk);

            var lamps = GameObject.Find("Menu_Pendant_Lamps");
            if (lamps != null) DestroyImmediate(lamps);

            var extraEnv = GameObject.Find("Environment");
            if (extraEnv != null && extraEnv.transform.childCount == 0) DestroyImmediate(extraEnv);
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private static GameObject CrearPieza(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            if (mat != null)
            {
                var rend = go.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }
            return go;
        }

        private static void CrearPata(Transform parent, string name, Vector3 pos, float radius, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            var pie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pie.name = "Regaton";
            pie.transform.SetParent(go.transform, false);
            pie.transform.localPosition = new Vector3(0f, -0.98f, 0f);
            pie.transform.localScale = new Vector3(1.4f, 0.12f, 1.4f);
            var colPie = pie.GetComponent<Collider>();
            if (colPie != null) Destroy(colPie);
            var rPie = pie.GetComponent<MeshRenderer>();
            if (rPie != null && mat != null) rPie.sharedMaterial = mat;
        }
    }
}
