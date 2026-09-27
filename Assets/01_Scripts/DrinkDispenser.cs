using UnityEngine;
using CocinaBoliviana.Data;

namespace CocinaBoliviana
{
    /// <summary>
    /// Dispensador de refrescos. Pones un vaso vacío debajo, cae el líquido, y al llenarse
    /// el vaso se sustituye por el refresco servido.
    ///
    /// Qué refresco sirve NO está fijado aquí: sale del departamento del nivel. Cochabamba y
    /// La Paz dan mocochinchi, Santa Cruz da somo, sin tocar la escena ni el código: basta
    /// con poner el refresco en 'refrescos' del DepartmentData de ese nivel.
    /// </summary>
    public class DrinkDispenser : MonoBehaviour
    {
        [Header("Zona del vaso")]
        [Tooltip("Caja (en METROS de mundo) bajo el grifo donde se detecta el vaso.")]
        [SerializeField] private Vector3 zonaDeteccion = new Vector3(0.25f, 0.30f, 0.25f);

        [Tooltip("Altura del centro de esa caja sobre el dispensador, en metros. Solo se usa " +
                 "si no hay 'Punto Llenado': con él puesto, la zona se centra ahí.")]
        [SerializeField] private float zonaAltura = 0.12f;

        [Header("Servido")]
        [Tooltip("Segundos en llenar un vaso.")]
        [SerializeField] private float segundosEnLlenar = 3f;

        [Tooltip("Refresco por defecto si el departamento del nivel no define ninguno.")]
        [SerializeField] private DishData refrescoPorDefecto;

        [Header("Chorro")]
        [Tooltip("Las partículas del líquido cayendo. Solo se ven mientras sirve.")]
        [SerializeField] private ParticleSystem chorro;

        [Tooltip("Sonido del agua cayendo. Suena en bucle mientras se sirve y se corta al " +
                 "acabar. Déjalo en Assets/06_SFX y el setup lo engancha solo.")]
        [SerializeField] private AudioClip sonidoServir;

        [Header("Reposición de vasos")]
        [Tooltip("Prefab del vaso vacío. Aparece uno nuevo en el porta-vasos cada vez que " +
                 "se sirve, para no tener que ir a buscarlo.")]
        [SerializeField] private GameObject vasoVacioPrefab;

        [Header("Llenado")]
        [Tooltip("Justo bajo el grifo. Ahí nace el vaso limpio y ahí se acopla el que traigas, " +
                 "para no tener que afinar la posición con la mano.")]
        [SerializeField] private Transform puntoLlenado;

        [Tooltip("Menú flotante con el botón de servir. Sin él, el llenado arranca solo.")]
        [SerializeField] private IngredientSelectorMenu menu;

        [Tooltip("Dónde queda el refresco ya servido, al lado del dispensador. Si se deja " +
                 "vacío, aparece en el sitio del vaso y estorba al siguiente.")]
        [SerializeField] private Transform puntoServido;

        private DrinkCup vasoActual;
        private float progreso;
        private bool sirviendo;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            // Se fuerzan aunque el AudioSource ya existiera: el chorro tiene que sonar en
            // bucle mientras sirve, en 3D y sin arrancar solo al cargar la escena.
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.loop = true;

            PararChorro();
            ReponerVaso();
        }

        /// <summary>Deja un vaso limpio en el porta-vasos.</summary>
        /// <summary>
        /// Deja un vaso limpio bajo el grifo, listo para servir sin ir a buscarlo.
        /// </summary>
        private void ReponerVaso()
        {
            if (vasoVacioPrefab == null || puntoLlenado == null) return;

            // Si ya hay un vaso ahi se respeta: sin esto, uno colocado a mano en la escena
            // se duplicaba nada mas empezar la partida.
            Collider[] cerca = Physics.OverlapSphere(puntoLlenado.position, 0.12f, ~0,
                                                     QueryTriggerInteraction.Ignore);
            foreach (var col in cerca)
            {
                if (col.GetComponentInParent<DrinkCup>() != null) return;
            }

            var nuevo = Instantiate(vasoVacioPrefab, puntoLlenado.position, puntoLlenado.rotation);
            nuevo.name = "VasoPlastico";
        }

        /// <summary>
        /// Centro de la zona de deteccion. Si hay punto de llenado manda ese: asi, al moverlo
        /// para alinearlo con el grifo del modelo, la zona lo sigue. Antes quedaba anclada al
        /// origen de la estacion y el vaso caia fuera sin que nada lo dijera.
        /// </summary>
        private Vector3 CentroZona =>
            (puntoLlenado != null) ? puntoLlenado.position : transform.position + Vector3.up * zonaAltura;

        private void FixedUpdate()
        {
            BuscarVaso();
            Servir();
        }

        /// <summary>
        /// Cada cuanto se sondea la fisica. A 50/s (cada FixedUpdate) esto alocaba un array
        /// nuevo 50 veces por segundo POR componente, y con varias estaciones a la vez se
        /// notaba en los FPS. 10/s es imperceptible para dejar un objeto encima.
        /// </summary>
        private const float IntervaloSondeo = 0.1f;

        private float proximoSondeo;

        private void BuscarVaso()
        {
            if (Time.time < proximoSondeo) return;
            proximoSondeo = Time.time + IntervaloSondeo;

            if (vasoActual != null) return;

            Collider[] dentro = Physics.OverlapBox(CentroZona, zonaDeteccion * 0.5f, transform.rotation,
                                                   ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in dentro)
            {
                var vaso = col.GetComponentInParent<DrinkCup>();
                if (vaso == null || vaso.Lleno) continue;

                vasoActual = vaso;
                progreso = vaso.Llenado;
                sirviendo = false;

                DishData refresco = RefrescoDelNivel();
                if (refresco != null)
                {
                    Color color = ColorDe(refresco);
                    vaso.SetColor(color);
                    TenirChorro(color);
                }

                Acoplar(vaso);
                PedirConfirmacion();
                return;
            }
        }

        /// <summary>
        /// Coloca el vaso bajo el grifo en cuanto entra en la zona, para que el jugador no
        /// tenga que afinar la posicion con la mano.
        /// </summary>
        private void Acoplar(DrinkCup vaso)
        {
            if (puntoLlenado == null) return;
            vaso.transform.SetPositionAndRotation(puntoLlenado.position, puntoLlenado.rotation);
        }

        /// <summary>
        /// Abre el menu con el boton de servir. El llenado no arranca solo: asi el jugador
        /// puede dejar el vaso puesto y servir cuando le venga bien.
        /// </summary>
        private void PedirConfirmacion()
        {
            if (menu == null)
            {
                // Sin menu no hay forma de confirmar, asi que se sirve directamente.
                sirviendo = true;
                return;
            }

            menu.Show(new[] { "Servir" }, opcion => opcion, _ => sirviendo = true);
        }

        private void Servir()
        {
            if (vasoActual == null)
            {
                PararChorro();
                return;
            }

            // Si el jugador se lleva el vaso a medias, se queda como esté y se corta el chorro.
            if (!SigueDebajo(vasoActual))
            {
                vasoActual = null;
                sirviendo = false;
                if (menu != null && menu.IsOpen) menu.Hide();
                PararChorro();
                return;
            }

            DishData refresco = RefrescoDelNivel();
            if (refresco == null)
            {
                Debug.LogWarning("[DrinkDispenser] Ningún refresco configurado para este nivel; " +
                                 "añade uno a 'refrescos' del DepartmentData o asigna el de por defecto.");
                enabled = false;
                return;
            }

            if (!sirviendo)
            {
                PararChorro();
                return;
            }

            ArrancarChorro();

            progreso += Time.fixedDeltaTime / Mathf.Max(segundosEnLlenar, 0.01f);
            vasoActual.SetLlenado(progreso);

            if (progreso < 1f) return;

            Entregar(vasoActual, refresco);
            vasoActual = null;
            progreso = 0f;
            sirviendo = false;
            if (menu != null && menu.IsOpen) menu.Hide();
            PararChorro();
        }

        private bool SigueDebajo(DrinkCup vaso)
        {
            Bounds caja = new Bounds(CentroZona, zonaDeteccion);
            return caja.Contains(vaso.transform.position);
        }

        /// <summary>
        /// El vaso lleno se cambia por el modelo del refresco, que ya trae su propio vaso.
        /// Igual que el plato de emplatado con el plato servido.
        /// </summary>
        private void Entregar(DrinkCup vaso, DishData refresco)
        {
            if (refresco.platoPrefab == null)
            {
                Debug.LogWarning($"[DrinkDispenser] '{refresco.nombre}' no tiene 'platoPrefab'; " +
                                 "el vaso se queda lleno pero sin cambiar de modelo.");
                return;
            }

            // Al lado, no en el sitio del vaso: asi se coge rapido y deja libre el grifo
            // para el siguiente.
            Vector3 donde = (puntoServido != null) ? puntoServido.position : vaso.transform.position;
            Quaternion giro = (puntoServido != null) ? puntoServido.rotation : vaso.transform.rotation;

            GameObject servido = Instantiate(refresco.platoPrefab, donde, giro);
            servido.name = refresco.nombre;

            var marca = servido.GetComponent<ServedDish>();
            if (marca == null) marca = servido.AddComponent<ServedDish>();
            marca.SetPlato(refresco);

            // Desactivar antes de destruir: Destroy tarda hasta el final del frame y el
            // dispensador volveria a detectar el vaso viejo como si siguiera ahi.
            vaso.gameObject.SetActive(false);
            Destroy(vaso.gameObject);

            ReponerVaso();

            Debug.Log($"[DrinkDispenser] {refresco.nombre} servido.");
        }

        /// <summary>
        /// El primer refresco del departamento en curso. Asi el mismo dispensador sirve
        /// mocochinchi o somo segun el nivel, sin duplicar estaciones.
        /// </summary>
        private DishData RefrescoDelNivel()
        {
            var manager = OrderManager.Instancia;
            DepartmentData depto = (manager != null) ? manager.Departamento : null;

            if (depto != null && depto.refrescos != null)
            {
                foreach (var r in depto.refrescos)
                {
                    if (r != null) return r;
                }
            }
            return refrescoPorDefecto;
        }

        /// <summary>
        /// Color del liquido de esta bebida. Lo trae el propio DishData, asi que el
        /// mocochinchi y el somo caen de su color sin tocar nada aqui.
        /// </summary>
        private static Color ColorDe(DishData refresco)
        {
            return refresco.colorLiquido;
        }

        /// <summary>Tine el chorro con el color de la bebida en curso.</summary>
        private void TenirChorro(Color color)
        {
            if (chorro == null) return;

            var main = chorro.main;
            // Se conserva el alfa que trae el sistema: el chorro va semitransparente.
            float alfa = main.startColor.color.a;
            main.startColor = new Color(color.r, color.g, color.b, alfa);
        }

        private void ArrancarChorro()
        {
            if (chorro != null && !chorro.isPlaying) chorro.Play(true);
            if (audioSource != null && sonidoServir != null && !audioSource.isPlaying)
            {
                audioSource.clip = sonidoServir;
                audioSource.Play();
            }
        }

        private void PararChorro()
        {
            if (chorro != null && chorro.isPlaying)
            {
                chorro.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(CentroZona, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, zonaDeteccion);
        }
    }
}
