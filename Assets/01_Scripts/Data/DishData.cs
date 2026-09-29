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
            if (debeEstarCocido)
            {
                detalles.Add(metodo == MetodoCoccion.Freir ? "Frito"
                           : metodo == MetodoCoccion.Asar ? "Asado"
                           : "Hervido");
            }

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

        [Tooltip("Se entrega tal cual sale de la estación, sin pasar por el plato de emplatado. " +
                 "Es el sonso: el mismo que sale de la parrilla, ya asado, va directo al mostrador. " +
                 "La receta debe tener UN solo ingrediente, en el estado en que se entrega.")]
        public bool entregaDirecta;

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

        [Header("Refresco")]
        [Tooltip("Color del líquido al servirse: tiñe el chorro y lo que se ve dentro del " +
                 "vaso. Solo se usa en bebidas. DrinksSetup lo saca del material del modelo.")]
        public Color colorLiquido = new Color(0.78f, 0.45f, 0.12f, 1f);

        [Header("Reglas")]
        public float tiempoLimite;
        public int puntos;
    }
}
