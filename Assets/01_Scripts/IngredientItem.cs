using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class IngredientItem : MonoBehaviour
    {
        [Header("Ingredient Info")]
        [SerializeField] private string ingredientName = "Tomate";
        [SerializeField] private bool isCut = false;
        [SerializeField] private GameObject cutPrefab;

        [Header("Dato de Ingrediente (ScriptableObject)")]
        [SerializeField] private IngredientData data;

        private TipoCorte corteActual = TipoCorte.Ninguno;
        private EstadoCoccion estadoCoccion = EstadoCoccion.Crudo;
        private MetodoCoccion metodoCoccionUsado = MetodoCoccion.Hervir;
        private Vector3 escalaDeMundoOriginal;
        private Renderer[] renderers;
        private Color[] coloresOriginales;
        private int[] propiedadDeColor;
        private MaterialPropertyBlock bloqueDeColor;
        private Rigidbody rb;
        private XRGrabInteractable grabInteractable;
        private CuttingBoard currentBoard;
        private PlateItem currentPlate;
        private CookingVessel currentVessel;

        public bool IsCut => isCut;
        public string IngredientName => ingredientName;
        public GameObject CutPrefab => cutPrefab;
        public IngredientData Data => data;
        public TipoCorte CorteActual => corteActual;
        public EstadoCoccion EstadoCoccion => estadoCoccion;
        public bool EstaQuemado => estadoCoccion == EstadoCoccion.Quemado;
        public MetodoCoccion MetodoCoccionUsado => metodoCoccionUsado;
        public XRGrabInteractable GrabInteractable => grabInteractable;
        public CuttingBoard CurrentBoard => currentBoard;
        public PlateItem CurrentPlate => currentPlate;
        public CookingVessel CurrentVessel => currentVessel;

        public void SetData(IngredientData nuevoData)
        {
            data = nuevoData;
        }

        private static readonly Color ColorCocido = new Color(0.72f, 0.48f, 0.22f);
        private static readonly Color ColorQuemado = new Color(0.13f, 0.11f, 0.10f);

        /// <summary>
        /// Cada shader llama distinto a su color base. Los modelos .glb vienen por glTFast,
        /// que usa "baseColorFactor"; los materiales hechos en Unity usan "_BaseColor" (URP)
        /// o "_Color" (built-in). Se prueba en ese orden y se usa el primero que exista.
        /// </summary>
        private static readonly int[] PosiblesColorIds =
        {
            Shader.PropertyToID("baseColorFactor"),
            Shader.PropertyToID("_BaseColor"),
            Shader.PropertyToID("_Color"),
        };

        /// <summary>Lo llama el recipiente al meterlo: hace falta para distinguir
        /// una papa frita de una hervida a la hora de validar la receta.</summary>
        public void SetMetodoCoccion(MetodoCoccion metodo)
        {
            metodoCoccionUsado = metodo;
        }

        public void SetCorteActual(TipoCorte corte)
        {
            corteActual = corte;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            // La que trae el prefab. Sin padre, localScale == escala de mundo.
            escalaDeMundoOriginal = transform.localScale;

            CachearRenderers();

            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
                grabInteractable.selectExited.RemoveListener(OnSelectExited);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Detach from cutting board if grabbed
            if (currentBoard != null)
            {
                currentBoard.ReleaseIngredient(this);
                currentBoard = null;
            }

            // Detach from plate if grabbed
            if (currentPlate != null)
            {
                currentPlate.ReleaseIngredient(this);
                currentPlate = null;
            }

            // Y de la olla o el sartén
            if (currentVessel != null)
            {
                currentVessel.ReleaseIngredient(this);
                currentVessel = null;
            }

            // Restore physics
            transform.SetParent(null, true);
            RestaurarEscala();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        public void SnapToCuttingBoard(CuttingBoard board, Vector3 position, Quaternion rotation)
        {
            if (currentVessel != null)
            {
                currentVessel.ReleaseIngredient(this);
                currentVessel = null;
            }

            if (currentPlate != null)
            {
                currentPlate.ReleaseIngredient(this);
                currentPlate = null;
            }

            currentBoard = board;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            transform.SetParent(board.transform, true);
            transform.position = position;
            transform.rotation = rotation;
            RestaurarEscala();
        }

        /// <summary>
        /// Devuelve la fisica al soltar. Hace falta porque XRGrabInteractable apunta
        /// isKinematic al agarrar y lo RESTAURA al soltar: como dentro de la olla o de la
        /// tabla el ingrediente esta fijo, XRI anotaba "era kinematico" y se lo devolvia,
        /// dejandolo flotando en el aire.
        ///
        /// Va en 'selectExited' a proposito: XRI restaura el Rigidbody dentro de Drop(),
        /// que corre en OnSelectExiting, es decir ANTES de que se dispare este evento.
        /// Aqui ya se le puede pisar el valor.
        /// </summary>
        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        /// <summary>
        /// Lo deja quieto donde esta, sin emparentarlo a nada. Al agarrarlo, OnSelectExited
        /// le devuelve la fisica, asi que vuelve a caer con normalidad.
        /// </summary>
        public void FijarEnSitio()
        {
            if (rb == null) return;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        public void SnapToCookingVessel(CookingVessel vessel, Vector3 position, Quaternion rotation)
        {
            if (currentBoard != null)
            {
                currentBoard.ReleaseIngredient(this);
                currentBoard = null;
            }
            if (currentPlate != null)
            {
                currentPlate.ReleaseIngredient(this);
                currentPlate = null;
            }

            currentVessel = vessel;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            transform.SetParent(vessel.transform, true);
            transform.position = position;
            transform.rotation = rotation;
            RestaurarEscala();
        }

        public void SnapToPlate(PlateItem plate, Vector3 position, Quaternion rotation)
        {
            if (currentVessel != null)
            {
                currentVessel.ReleaseIngredient(this);
                currentVessel = null;
            }

            if (currentBoard != null)
            {
                currentBoard.ReleaseIngredient(this);
                currentBoard = null;
            }

            currentPlate = plate;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            transform.SetParent(plate.transform, true);
            transform.position = position;
            transform.rotation = rotation;
            RestaurarEscala();
        }

        /// <summary>
        /// Avance de la cocción, lo llama el recipiente cada frame.
        /// 0..1 = crudo a listo. 1..2 = listo a carbón.
        /// </summary>
        public void SetProgresoCoccion(float progreso)
        {
            estadoCoccion = progreso >= 2f ? EstadoCoccion.Quemado
                          : progreso >= 1f ? EstadoCoccion.Cocido
                          : EstadoCoccion.Crudo;

            AplicarTinte(progreso);
        }

        /// <summary>
        /// La localScale que le toca para verse de su tamaño real bajo su padre actual.
        /// </summary>
        public Vector3 EscalaLocalObjetivo
        {
            get
            {
                Vector3 padre = (transform.parent != null) ? transform.parent.lossyScale : Vector3.one;
                return new Vector3(
                    escalaDeMundoOriginal.x / Mathf.Max(Mathf.Abs(padre.x), 0.0001f),
                    escalaDeMundoOriginal.y / Mathf.Max(Mathf.Abs(padre.y), 0.0001f),
                    escalaDeMundoOriginal.z / Mathf.Max(Mathf.Abs(padre.z), 0.0001f));
            }
        }

        /// <summary>
        /// Recalcula la escala a partir del tamaño original del prefab, nunca del valor
        /// actual. SetParent con worldPositionStays no puede conservar la escala bajo un
        /// padre NO uniforme (la Tabla es 0.45 / 0.02 / 0.35): la aproxima, y el error se
        /// acumulaba en cada meter-y-sacar hasta deformar el ingrediente.
        /// </summary>
        private void RestaurarEscala()
        {
            transform.localScale = EscalaLocalObjetivo;
        }

        /// <summary>
        /// Guarda, por renderer, qué propiedad de color entiende su shader y con qué color
        /// venía de fábrica. Hace falta lo segundo para multiplicar el tinte encima en vez
        /// de reemplazarlo, que dejaría todos los ingredientes blancos estando crudos.
        /// </summary>
        private void CachearRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            coloresOriginales = new Color[renderers.Length];
            propiedadDeColor = new int[renderers.Length];
            bloqueDeColor = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Length; i++)
            {
                propiedadDeColor[i] = -1;
                coloresOriginales[i] = Color.white;

                Material mat = (renderers[i] != null) ? renderers[i].sharedMaterial : null;
                if (mat == null) continue;

                foreach (int id in PosiblesColorIds)
                {
                    if (!mat.HasProperty(id)) continue;
                    propiedadDeColor[i] = id;
                    coloresOriginales[i] = mat.GetColor(id);
                    break;
                }

                if (propiedadDeColor[i] < 0)
                {
                    Debug.LogWarning($"[IngredientItem] {name}: el shader de '{mat.shader.name}' no expone " +
                                     "ningun color base conocido; ese renderer no se tenira al cocinar.");
                }
            }
        }

        /// <summary>
        /// Crudo -> dorado -> negro, multiplicado sobre el color propio del modelo.
        /// Se usa un MaterialPropertyBlock en vez de tocar 'material': eso último clona el
        /// material en cada instancia y dejaría el asset compartido teñido para siempre.
        /// </summary>
        private void AplicarTinte(float progreso)
        {
            if (renderers == null) return;

            Color tinte = progreso <= 1f
                ? Color.Lerp(Color.white, ColorCocido, Mathf.Clamp01(progreso))
                : Color.Lerp(ColorCocido, ColorQuemado, Mathf.Clamp01(progreso - 1f));

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || propiedadDeColor[i] < 0) continue;

                renderers[i].GetPropertyBlock(bloqueDeColor);
                bloqueDeColor.SetColor(propiedadDeColor[i], coloresOriginales[i] * tinte);
                renderers[i].SetPropertyBlock(bloqueDeColor);
            }
        }

        public void ClearBoardReference()
        {
            currentBoard = null;
        }

        public void ClearPlateReference()
        {
            currentPlate = null;
        }
    }
}
