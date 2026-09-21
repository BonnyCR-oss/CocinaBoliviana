using UnityEditor;
using UnityEngine;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Aplica la mecánica de la cocina sobre la escena ya construida, en orden de dependencia.
    ///
    /// No genera geometría ni destruye nada: la escena y los prefabs son la fuente de verdad,
    /// se editan a mano y se versionan en git. Esto solo cablea componentes encima.
    ///
    /// No se ejecuta solo. Es siempre una acción deliberada desde el menú Kitchen.
    /// </summary>
    public static class KitchenBootstrap
    {
        [MenuItem("Kitchen/Apply Mechanics (seguro)", priority = 0)]
        public static void RunAll()
        {
            Debug.Log("[KitchenBootstrap] Aplicando mecánica en orden...");

            // Ojo: aquí NO va KitchenBuilder ni MainMenuBuilder. Esos GENERAN las escenas
            // desde cero y son andamiaje de arranque, no algo de uso diario. Viven en su
            // propio ítem de menú y piden confirmación antes de demoler nada.

            // 1. Mecánica sobre la geometría existente: colliders en los 3 cajones,
            //    ItemDispenser, tabla de cortar.
            Run("KitchenMechanicsSetup", KitchenMechanicsSetup.SetupFirstSceneMechanics);

            // 2. Prefabs de ingredientes y sus cortes. Va después porque limpia el
            //    defaultCutPrefab legado que el paso 1 deja puesto en la tabla.
            Run("IngredientPrefabSetup", IngredientPrefabSetup.SetupIngredientPrefabs);

            // 3. Menús flotantes: necesitan el ItemDispenser que agrega el paso 1.
            Run("DispenserMenuSetup", DispenserMenuSetup.SetupAll);

            // 4. Manos en los controles (prefabs, independiente).
            Run("HandVisualsSetup", HandVisualsSetup.SetupHandVisuals);

            // 5. Agarre tipo imán. Toca los prefabs agarrables de los pasos 1 y 2.
            Run("GrabFeelSetup", GrabFeelSetup.SetupGrabFeel);

            // 6. Fuego de las hornallas.
            Run("FlameSetup", FlameSetup.SetupFlames);

            Debug.Log("[KitchenBootstrap] Mecánica aplicada.");
        }

        /// <summary>
        /// Aísla cada paso: si uno falla, los siguientes igual corren y el log dice cuál rompió.
        /// </summary>
        private static void Run(string label, System.Action step)
        {
            try
            {
                step();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[KitchenBootstrap] '{label}' falló y se omitió: {e}");
            }
        }
    }
}
