using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CocinaBoliviana.Data;

namespace CocinaBoliviana.Editor
{
    /// <summary>
    /// Inspector personalizado para ItemDispenser: permite elegir primero una categoría
    /// (Verdura, Carne, etc.) y luego, filtrado por esa categoría, el IngredientData exacto,
    /// para agregarlo a la lista de opciones que reparte el dispensador.
    /// </summary>
    [CustomEditor(typeof(ItemDispenser))]
    public class ItemDispenserEditor : UnityEditor.Editor
    {
        private IngredientType categoriaFiltro = IngredientType.Verdura;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty ingredienteProp = serializedObject.FindProperty("ingredienteReferencia");

            var ingredienteActual = ingredienteProp.objectReferenceValue as IngredientData;
            if (ingredienteActual != null)
            {
                categoriaFiltro = ingredienteActual.tipo;
            }

            EditorGUILayout.LabelField("Selector de Ingrediente", EditorStyles.boldLabel);

            categoriaFiltro = (IngredientType)EditorGUILayout.EnumPopup("Categoría", categoriaFiltro);

            List<IngredientData> disponibles = AssetDatabase.FindAssets("t:IngredientData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<IngredientData>)
                .Where(d => d != null && d.tipo == categoriaFiltro)
                .OrderBy(d => d.nombre)
                .ToList();

            IngredientData seleccionActual = ingredienteActual;

            if (disponibles.Count == 0)
            {
                EditorGUILayout.HelpBox($"No hay IngredientData de tipo {categoriaFiltro} en el proyecto.", MessageType.Info);
            }
            else
            {
                string[] nombres = disponibles.Select(d => d.nombre).ToArray();
                int indiceActual = disponibles.FindIndex(d => d == ingredienteActual);

                int indiceElegido = EditorGUILayout.Popup("Ingrediente", Mathf.Max(indiceActual, 0), nombres);
                seleccionActual = disponibles[indiceElegido];

                if (indiceElegido != indiceActual)
                {
                    ingredienteProp.objectReferenceValue = seleccionActual;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Qué dispensa", EditorStyles.boldLabel);

            SerializedProperty opcionesProp = serializedObject.FindProperty("opcionesIngredientes");
            SerializedProperty menuProp = serializedObject.FindProperty("menu");

            EditorGUILayout.PropertyField(menuProp, new GUIContent("Menu (IngredientSelectorMenu)"));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = seleccionActual != null;
                if (GUILayout.Button($"+ Agregar '{(seleccionActual != null ? seleccionActual.nombre : "-")}' a la lista"))
                {
                    bool yaEsta = false;
                    for (int i = 0; i < opcionesProp.arraySize; i++)
                    {
                        if (opcionesProp.GetArrayElementAtIndex(i).objectReferenceValue == seleccionActual)
                        {
                            yaEsta = true;
                            break;
                        }
                    }
                    if (!yaEsta)
                    {
                        opcionesProp.arraySize++;
                        opcionesProp.GetArrayElementAtIndex(opcionesProp.arraySize - 1).objectReferenceValue = seleccionActual;
                    }
                }
                GUI.enabled = true;
            }

            for (int i = 0; i < opcionesProp.arraySize; i++)
            {
                SerializedProperty elemento = opcionesProp.GetArrayElementAtIndex(i);
                var data = elemento.objectReferenceValue as IngredientData;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(data != null ? data.nombre : "(vacío)");
                    if (GUILayout.Button("Quitar", GUILayout.Width(70)))
                    {
                        opcionesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            if (opcionesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("La lista está vacía: este dispensador no reparte nada. Agrega al menos un ingrediente.", MessageType.Warning);
            }
            else if (opcionesProp.arraySize == 1)
            {
                EditorGUILayout.HelpBox("Con una sola opción se entrega directo, sin abrir menú. 'Menu' puede quedar vacío.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Con dos o más opciones se abre el menú para elegir, así que 'Menu' es obligatorio.", MessageType.Info);
            }

            EditorGUILayout.Space();
            SerializedProperty spawnPointProp = serializedObject.FindProperty("spawnPoint");
            SerializedProperty cooldownProp = serializedObject.FindProperty("cooldownTime");
            SerializedProperty soundProp = serializedObject.FindProperty("dispenseSound");

            EditorGUILayout.PropertyField(spawnPointProp);
            EditorGUILayout.PropertyField(cooldownProp);
            EditorGUILayout.PropertyField(soundProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
