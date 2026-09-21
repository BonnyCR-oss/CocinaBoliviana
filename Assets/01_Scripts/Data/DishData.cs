using System.Collections.Generic;
using UnityEngine;

namespace CocinaBoliviana.Data
{
    /// <summary>
    /// Un ingrediente de una receta, con el estado exacto en el que hace falta.
    /// No es lo mismo una papa cruda que una papa en rodajas y frita.
    /// </summary>
    [System.Serializable]
    public class IngredienteRequerido
    {
        public IngredientData ingrediente;

        [Tooltip("Ninguno = entero, sin cortar.")]
        public TipoCorte corte = TipoCorte.Ninguno;

        public bool debeEstarCocido;

        [Tooltip("Solo cuenta si 'Debe Estar Cocido' está marcado.")]
        public MetodoCoccion metodo = MetodoCoccion.Hervir;

        /// <summary>Texto corto para el cartel del plato: "Papa (Rodajas, Frito)".</summary>
        public string Describir()
        {
            string nombre = (ingrediente != null) ? ingrediente.nombre : "?";

            var detalles = new List<string>();
            if (corte != TipoCorte.Ninguno) detalles.Add(corte.ToString());
            if (debeEstarCocido) detalles.Add(metodo == MetodoCoccion.Freir ? "Frito" : "Hervido");

            return (detalles.Count == 0) ? nombre : $"{nombre} ({string.Join(", ", detalles)})";
        }

        /// <summary>
        /// Si este ingrediente concreto cumple el requisito. Lo quemado nunca vale.
        /// </summary>
        public bool LoCumple(IngredientData data, TipoCorte corteActual, EstadoCoccion estado, MetodoCoccion metodoUsado)
        {
            if (ingrediente == null || data != ingrediente) return false;
            if (corteActual != corte) return false;
            if (estado == EstadoCoccion.Quemado) return false;

            if (debeEstarCocido)
            {
                return estado == EstadoCoccion.Cocido && metodoUsado == metodo;
            }
            return estado == EstadoCoccion.Crudo;
        }
    }

    [CreateAssetMenu(fileName = "NewDish", menuName = "Cocina Boliviana/Plato")]
    public class DishData : ScriptableObject
    {
        [Header("Informacion General")]
        public string nombre;
        public Sprite icono;
        public GameObject platoPrefab;

        [Header("Receta")]
        public List<IngredienteRequerido> receta = new List<IngredienteRequerido>();

        [HideInInspector]
        [Tooltip("Legado. Solo sirve para migrar a 'receta'; no lo edites.")]
        public List<IngredientData> ingredientesRequeridos = new List<IngredientData>();

        /// <summary>
        /// Pasa la receta vieja (solo ingredientes) a la nueva (ingrediente + estado), con
        /// todo crudo y entero, que es como se comportaba antes. Devuelve true si migró.
        /// </summary>
        public bool MigrarRecetaLegada()
        {
            if (receta.Count > 0 || ingredientesRequeridos.Count == 0) return false;

            foreach (var ingrediente in ingredientesRequeridos)
            {
                if (ingrediente == null) continue;
                receta.Add(new IngredienteRequerido { ingrediente = ingrediente });
            }
            return true;
        }

        [Header("Reglas")]
        public float tiempoLimite;
        public int puntos;
    }
}
