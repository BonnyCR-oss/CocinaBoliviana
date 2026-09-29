# Guía Detallada de Scripts - Cocina Boliviana VR

Este documento detalla la totalidad de los **39 scripts** que componen la lógica de juego en `Assets/01_Scripts/`. Para cada script se describe qué hace, su rol dentro de la arquitectura del juego y sus métodos o bloques de código principales.

---

## Índice General

1. [Capa de Datos y ScriptableObjects (`Assets/01_Scripts/Data/`)](#1-capa-de-datos-y-scriptableobjects) (7 scripts)
2. [Control de Campaña, Progresión y Menú Principal](#2-control-de-campaña-progresión-y-menú-principal) (4 scripts)
3. [Gestión de Nivel, Comandas y Puntuación](#3-gestión-de-nivel-comandas-y-puntuación) (4 scripts)
4. [Estaciones y Mecánicas de Cocina (Corte, Cocción, Emplatado, Entrega)](#4-estaciones-y-mecánicas-de-cocina) (13 scripts)
5. [Infraestructura de Entrada, Simulación y Seguridad XR](#5-infraestructura-de-entrada-simulación-y-seguridad-xr) (5 scripts)
6. [Atmósfera, Audio y Efectos Visuales](#6-atmósfera-audio-y-efectos-visuales) (6 scripts)

---

## 1. Capa de Datos y ScriptableObjects

Ubicación: `Assets/01_Scripts/Data/`

### 1.1 `DepartmentData.cs`
* **Ruta:** `Assets/01_Scripts/Data/DepartmentData.cs`
* **Qué hace:** Modela la identidad cultural y gastronómica de cada región de Bolivia (Cochabamba, La Paz, Santa Cruz).
* **Función principal:** Es el punto central de consulta para los platos que pueden pedirse, la bandera decorativa de la pared y la bebida típica asignada a la cocina.
* **Código y campos clave:**
```csharp
[CreateAssetMenu(fileName = "NuevoDepartamento", menuMenuName = "Cocina/Departamento")]
public class DepartmentData : ScriptableObject
{
    public string nombreDepartamento;
    public string prefijo;             // Ej: "cbba", "lpz", "scz"
    public Sprite banderaSprite;        // Se inyecta en WallFlag
    public List<DishData> platosTipicos;
    public DishData refrescoTipico;     // Mocochinchi o Somó
}
```

---

### 1.2 `DishData.cs`
* **Ruta:** `Assets/01_Scripts/Data/DishData.cs`
* **Qué hace:** Modela una receta culinaria completa. Define qué ingredientes deben colocarse, en qué estado de corte y cocción, y qué prefab servido representa el plato terminado.
* **Función principal:** Evalúa si la combinación de ingredientes colocada en un plato (`PlateItem`) satisface los requisitos para transformarse en un plato servido (`ServedDish`).
* **Código y métodos clave:**
```csharp
[CreateAssetMenu(fileName = "NuevoPlato", menuName = "Cocina/Plato")]
public class DishData : ScriptableObject
{
    public string nombre;
    public Sprite icono;
    public List<IngredienteRequerido> ingredientes;
    public float tiempoLimite = 60f;
    public int puntosBase = 100;
    public GameObject platoPrefab;
    public Color colorLiquido; // Usado para bebidas

    // Evalúa si un ingrediente colocado cumple con la receta
    public bool LoCumple(IngredientData data, TipoCorte corteActual, EstadoCoccion estado, MetodoCoccion metodoUsado)
    {
        foreach (var req in ingredientes)
        {
            if (req.ingrediente == data && req.corteRequerido == corteActual && req.metodoRequerido == metodoUsado)
                return true;
        }
        return false;
    }
}
```

---

### 1.3 `IngredientData.cs`
* **Ruta:** `Assets/01_Scripts/Data/IngredientData.cs`
* **Qué hace:** Define las características físicas y gastronómicas de una materia prima (papa, carne, cebolla, etc.).
* **Función principal:** Establece qué transformaciones admite el ingrediente (si se puede picar, qué tipo de cocción tolera, y tiempos antes de cocerse o quemarse).
* **Código y métodos clave:**
```csharp
[CreateAssetMenu(fileName = "NuevoIngrediente", menuName = "Cocina/Ingrediente")]
public class IngredientData : ScriptableObject
{
    public string nombre;
    public Sprite icono;
    public TipoCorte tipoCorteRequerido;
    public MetodoCoccion metodoCoccionRequerido;
    public float tiempoCoccion = 5f;
    public float tiempoHastaQuemarse = 10f;
    public GameObject prefabCrudo;
    public GameObject prefabProcesado;
    public GameObject prefabCocido;
    public GameObject prefabQuemado;

    public bool AdmiteCoccion(MetodoCoccion metodo) => (metodoCoccionRequerido & metodo) != 0;
}
```

---

### 1.4 `LevelData.cs`
* **Ruta:** `Assets/01_Scripts/Data/LevelData.cs`
* **Qué hace:** Define la configuración de dificultad y reglas de un nivel de campaña.
* **Función principal:** Configura la duración de la partida, los platos que se pedirán en ese departamento y las metas de puntos para ganar estrellas.
* **Código y campos clave:**
```csharp
[CreateAssetMenu(fileName = "NuevoNivel", menuName = "Cocina/Nivel")]
public class LevelData : ScriptableObject
{
    public string nombreNivel;
    public DepartmentData departamento;
    public float duracionNivel = 150f;
    public int puntos1Estrella = 80;
    public int puntos2Estrellas = 150;
    public int puntos3Estrellas = 220;
    public DishData[] platosDisponibles;
    public int maximoOrdenesSimultaneas = 3;
}
```

---

### 1.5 `OrderData.cs`
* **Ruta:** `Assets/01_Scripts/Data/OrderData.cs`
* **Qué hace:** Representa la comanda en tiempo real generada para un cliente.
* **Función principal:** Rastrea el tiempo individual restante de un pedido activo y calcula la bonificación por rapidez.
* **Código y campos clave:**
```csharp
public class OrderData
{
    public DishData plato;
    public float tiempoTotal;
    public float tiempoRestante;
    public bool EstaVencida => tiempoRestante <= 0f;
    public float PorcentajeTiempo => Mathf.Clamp01(tiempoRestante / tiempoTotal);
}
```

---

### 1.6 `MetodoCoccion.cs`
* **Ruta:** `Assets/01_Scripts/Data/MetodoCoccion.cs`
* **Qué hace:** Enumeración con flags de bits que clasifica los métodos de transmisión de calor culinario.
* **Código:**
```csharp
[System.Flags]
public enum MetodoCoccion
{
    Ninguno = 0,
    Hervir  = 1 << 0,
    Freir   = 1 << 1,
    Asar    = 1 << 2,
    Hornear = 1 << 3
}
```

---

### 1.7 `TipoCorte.cs`
* **Ruta:** `Assets/01_Scripts/Data/TipoCorte.cs`
* **Qué hace:** Enumeración que define el estado mecánico del ingrediente.
* **Código:**
```csharp
public enum TipoCorte
{
    Entero,
    PicadoFino,
    Rodajas,
    Cubos,
    Tiras
}
```

---

## 2. Control de Campaña, Progresión y Menú Principal

### 2.1 `GameProgressManager.cs`
* **Ruta:** `Assets/01_Scripts/GameProgressManager.cs`
* **Qué hace:** Clase estática que administra el guardado persistente del progreso mediante `PlayerPrefs`.
* **Función principal:** Guarda el nivel más alto alcanzado por el jugador (Nivel 1, 2 o 3) y provee nombres de escena unificados para transiciones limpias.
* **Código y métodos clave:**
```csharp
public static class GameProgressManager
{
    private const string SaveKeyNivel = "CocinaBoliviana_NivelGuardado";
    public const int NivelMinimo = 1, NivelMaximo = 3;

    public static int ObtenerNivelGuardado() => Mathf.Clamp(PlayerPrefs.GetInt(SaveKeyNivel, NivelMinimo), NivelMinimo, NivelMaximo);

    public static void GuardarNivel(int nivel)
    {
        int actual = ObtenerNivelGuardado();
        int aGuardar = Mathf.Clamp(Mathf.Max(actual, nivel), NivelMinimo, NivelMaximo);
        PlayerPrefs.SetInt(SaveKeyNivel, aGuardar);
        PlayerPrefs.Save();
    }

    public static void ReiniciarProgreso()
    {
        PlayerPrefs.SetInt(SaveKeyNivel, NivelMinimo);
        PlayerPrefs.Save();
    }
}
```

---

### 2.2 `MainMenuController.cs`
* **Ruta:** `Assets/01_Scripts/MainMenuController.cs`
* **Qué hace:** Gestiona los botones interactivos del menú principal en espacio 3D.
* **Función principal:** Permite al jugador comenzar una campaña desde cero ("Nueva Partida") o retomar desde el último departamento desbloqueado ("Continuar Partida").
* **Código y métodos clave:**
```csharp
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private LevelData[] niveles;

    public void NuevaPartida()
    {
        GameProgressManager.ReiniciarProgreso();
        SceneManager.LoadScene("Nivel 1 - Cochabamba");
    }

    public void ContinuarPartida()
    {
        int nivelGuardado = GameProgressManager.ObtenerNivelGuardado();
        string escena = GameProgressManager.ObtenerNombreEscenaNivel(nivelGuardado);
        SceneManager.LoadScene(escena);
    }
}
```

---

### 2.3 `BackgroundMusicManager.cs`
* **Ruta:** `Assets/01_Scripts/BackgroundMusicManager.cs`
* **Qué hace:** Administrador de audio que reproduce las pistas musicales folclóricas de fondo.
* **Función principal:** Mantiene continuidad auditiva y control de volumen entre transiciones.
* **Código clave:** Implementa métodos `Play()`, `PauseMusic()` y modulación suave de volumen en el componente `AudioSource`.

---

### 2.4 `WallFlag.cs`
* **Ruta:** `Assets/01_Scripts/WallFlag.cs`
* **Qué hace:** Aplica automáticamente la textura de la bandera departamental en el cuadro decorativo de la cocina.
* **Función principal:** Lee el departamento del `LevelData` activo y asigna el `banderaSprite` al `MeshRenderer` de la pared.
* **Código y métodos clave:**
```csharp
public class WallFlag : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    private void Start() => Refrescar();

    public void Refrescar()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.NivelActivo != null)
        {
            var depto = LevelManager.Instance.NivelActivo.departamento;
            if (depto != null && depto.banderaSprite != null)
                spriteRenderer.sprite = depto.banderaSprite;
        }
    }
}
```

---

## 3. Gestión de Nivel, Comandas y Puntuación

### 3.1 `LevelManager.cs`
* **Ruta:** `Assets/01_Scripts/LevelManager.cs`
* **Qué hace:** Controlador del ciclo de vida de la partida (*Game Loop*) dentro de la cocina.
* **Función principal:** Gestiona el reloj de ronda, calcula el puntaje, otorga bonos de racha (combo), evalúa estrellas al terminar y desbloquea el siguiente nivel.
* **Código y métodos clave:**
```csharp
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public LevelState EstadoActual { get; private set; }

    private IEnumerator LevelLoop()
    {
        // 1. Cuenta atrás inicial (3, 2, 1, ¡A Cocinar!)
        EstadoActual = LevelState.Starting;
        yield return new WaitForSeconds(segundosCuentaAtras);

        // 2. Partida activa con temporizador regresivo
        EstadoActual = LevelState.Playing;
        while (tiempoRestante > 0f)
        {
            tiempoRestante -= Time.deltaTime;
            yield return null;
        }

        // 3. Fin de partida y evaluación de estrellas
        EstadoActual = LevelState.Finished;
        EvaluarResultados();
    }

    private void EvaluarResultados()
    {
        int estrellas = CalcularEstrellas(puntuacionActual);
        if (estrellas >= 1)
        {
            // Guarda automáticamente el avance al siguiente nivel
            GameProgressManager.GuardarNivel(numeroNivel + 1);
        }
    }
}
```

---

### 3.2 `LevelScreenHUD.cs`
* **Ruta:** `Assets/01_Scripts/LevelScreenHUD.cs`
* **Qué hace:** Controla la pantalla física colocada sobre la pared de la cocina (Canvas en World Space).
* **Función principal:** Actualiza en tiempo real el reloj (`mm:ss`), la barra de estrellas y despliega la ventana de victoria con el botón para cargar el siguiente nivel.
* **Código y métodos clave:**
```csharp
public class LevelScreenHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoTiempo;
    [SerializeField] private TextMeshProUGUI textoPuntos;
    [SerializeField] private Button botonSiguienteNivel;

    public void OnClickSiguienteNivel()
    {
        int nivelActual = LevelManager.Instance.NumeroNivel;
        string siguienteEscena = GameProgressManager.ObtenerNombreEscenaNivel(nivelActual + 1);
        SceneManager.LoadScene(siguienteEscena);
    }
}
```

---

### 3.3 `OrderManager.cs`
* **Ruta:** `Assets/01_Scripts/OrderManager.cs`
* **Qué hace:** Generador y evaluador de pedidos de clientes en curso.
* **Función principal:** Instancia nuevas órdenes según el intervalo de spawn, decrementa los temporizadores de comanda y penaliza pedidos expirados.
* **Código y métodos clave:**
```csharp
public class OrderManager : MonoBehaviour
{
    public List<PedidoActivo> PedidosActivos = new List<PedidoActivo>();

    public bool Tachar(DishData platoEntregado)
    {
        // Busca si el plato entregado coincide con alguna orden pendiente
        for (int i = 0; i < PedidosActivos.Count; i++)
        {
            if (PedidosActivos[i].plato == platoEntregado)
            {
                int recompensa = PedidosActivos[i].CalcularPuntos();
                LevelManager.Instance.SumarPuntos(recompensa);
                PedidosActivos.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}
```

---

### 3.4 `OrderBoard.cs`
* **Ruta:** `Assets/01_Scripts/OrderBoard.cs`
* **Qué hace:** Representación visual de las comandas sobre la barra.
* **Función principal:** Dibuja los tickets de pedidos con la barra de tiempo regresiva y los iconos de ingredientes requeridos.

---

## 4. Estaciones y Mecánicas de Cocina

### 4.1 `IngredientItem.cs`
* **Ruta:** `Assets/01_Scripts/IngredientItem.cs`
* **Qué hace:** Representa la instancia física e interactuable de un alimento individual en el mundo 3D.
* **Función principal:** Registra el estado de corte (`TipoCorte`) y cocción (`EstadoCoccion`), gestiona las partículas de vapor y permuta las mallas visuales (crudo $\rightarrow$ picado $\rightarrow$ cocido $\rightarrow$ quemado).
* **Código y métodos clave:**
```csharp
public class IngredientItem : MonoBehaviour
{
    public IngredientData Data { get; private set; }
    public TipoCorte CorteActual { get; private set; }
    public EstadoCoccion CoccionActual { get; private set; }

    public void AplicarCorte(TipoCorte nuevoCorte)
    {
        CorteActual = nuevoCorte;
        ActualizarVisuales();
    }

    public void AvanzarCoccion(float deltaTiempo, MetodoCoccion metodo)
    {
        tiempoCocinado += deltaTiempo;
        if (tiempoCocinado >= Data.tiempoCoccion && CoccionActual == EstadoCoccion.Crudo)
        {
            CoccionActual = EstadoCoccion.Cocido;
            ActualizarVisuales();
        }
        else if (tiempoCocinado >= Data.tiempoCoccion + Data.tiempoHastaQuemarse)
        {
            CoccionActual = EstadoCoccion.Quemado;
            ActualizarVisuales();
        }
    }
}
```

---

### 4.2 `ItemDispenser.cs`
* **Ruta:** `Assets/01_Scripts/ItemDispenser.cs`
* **Qué hace:** Mecanismo de las cajas/cajones de despensa para generar ingredientes ilimitados.
* **Función principal:** Al detectar el agarre o la interacción del jugador, instancia un nuevo `IngredientItem` en la mano o sobre la boca del cajón.
* **Código clave:** Emplea `XRBaseInteractable` o triggers con detección de botones para hacer `Instantiate(prefabIngrediente, spawnPoint.position, Quaternion.identity)`.

---

### 4.3 `IngredientSelectorMenu.cs`
* **Ruta:** `Assets/01_Scripts/IngredientSelectorMenu.cs`
* **Qué hace:** Menú flotante 3D que permite al jugador seleccionar qué ingrediente generar desde una caja multipropósito (ej. elegir entre carne de res, pollo o chorizo).
* **Código clave:** Método `Show(opciones, onSelected)` que construye botones dinámicos con iconos y callbacks al hacer click en VR.

---

### 4.4 `CuttingBoard.cs`
* **Ruta:** `Assets/01_Scripts/CuttingBoard.cs`
* **Qué hace:** Tabla de picar alimentos.
* **Función principal:** Acopla físicamente el ingrediente sobre su superficie y recibe los impactos del cuchillo.
* **Código y métodos clave:**
```csharp
public class CuttingBoard : MonoBehaviour
{
    private void FixedUpdate()
    {
        // Throttling a 10 Hz para optimizar rendimiento en VR
        if (Time.time < proximoCheck) return;
        proximoCheck = Time.time + 0.1f;

        // Escaneo espacial sin asignación de memoria heap
        int encontrados = Physics.OverlapBoxNonAlloc(centro, halfExtents, bufferColisionadores);
        // Acopla el ingrediente si no hay ninguno en la tabla
    }
}
```

---

### 4.5 `KnifeChopper.cs`
* **Ruta:** `Assets/01_Scripts/KnifeChopper.cs`
* **Qué hace:** Implementa la física del cuchillo de cocina.
* **Función principal:** Mide la velocidad lineal del cuchillo; al colisionar con un ingrediente montado en la tabla, aplica un golpe de corte, reproduce el sonido de picado y genera partículas.
* **Código clave:** `OnTriggerEnter(Collider other)` verifica que la velocidad del cuchillo supere el umbral mínimo (`velocidad > 0.4f`) para transformar el alimento.

---

### 4.6 `CookingCounter.cs`
* **Ruta:** `Assets/01_Scripts/CookingCounter.cs`
* **Qué hace:** Controla la cocina de gas con sus hornallas.
* **Función principal:** Administra las perillas interactivas, encendiendo o apagando la transmisión de calor y el fuego.

---

### 4.7 `CookingVessel.cs`
* **Ruta:** `Assets/01_Scripts/CookingVessel.cs`
* **Qué hace:** Controla los recipientes de cocción (sartén y olla).
* **Función principal:** Detecta si está sobre una hornalla activa; de ser así, calienta los ingredientes en su interior aumentando su cocción y emitiendo sonido de fritura y partículas de humo.
* **Código y métodos clave:**
```csharp
public class CookingVessel : MonoBehaviour
{
    [SerializeField] private MetodoCoccion metodo = MetodoCoccion.Freir;

    private void FixedUpdate()
    {
        if (estaSobreHornallaEncendida)
        {
            foreach (var ingrediente in ingredientesEnRecipiente)
            {
                ingrediente.AvanzarCoccion(Time.fixedDeltaTime, metodo);
            }
        }
    }
}
```

---

### 4.8 `BurnerFlame.cs`
* **Ruta:** `Assets/01_Scripts/BurnerFlame.cs`
* **Qué hace:** Controla visualmente el fuego de la hornalla.
* **Función principal:** Activa la luz puntual cálida y el sistema de partículas de llama cuando `CookingCounter` activa la hornalla.

---

### 4.9 `GrillEffects.cs`
* **Ruta:** `Assets/01_Scripts/GrillEffects.cs`
* **Qué hace:** Administra los efectos especiales de la plancha/parrilla (chispas, humo gradual y audio de fritura sostenido).

---

### 4.10 `PlateItem.cs`
* **Ruta:** `Assets/01_Scripts/PlateItem.cs`
* **Qué hace:** Plato vacío interactivo donde el jugador monta los ingredientes preparados.
* **Función principal:** Acopla magnéticamente los alimentos cocidos en capas radiales y evalúa continuamente contra los `DishData` para transformarse automáticamente en un `ServedDish`.
* **Código y métodos clave:**
```csharp
public class PlateItem : MonoBehaviour
{
    private List<IngredientItem> ingredientesPuestos = new List<IngredientItem>();

    public void IntentarAgregar(IngredientItem item)
    {
        ingredientesPuestos.Add(item);
        VerificarRecetaCompleta();
    }

    private void VerificarRecetaCompleta()
    {
        foreach (var receta in catalogoPlatos)
        {
            if (CumpleReceta(receta))
            {
                // Auto-reemplazo por el plato servido terminado
                var platoFinal = Instantiate(receta.platoPrefab, transform.position, transform.rotation);
                Destroy(gameObject);
                break;
            }
        }
    }
}
```

---

### 4.11 `PlateCounter.cs`
* **Ruta:** `Assets/01_Scripts/PlateCounter.cs`
* **Qué hace:** Muestra un HUD flotante sobre el plato indicando qué ingredientes tiene montados y cuáles faltan para completar la receta elegida.

---

### 4.12 `DishPickupPoint.cs` y `ServedDish.cs`
* **Ruta:** `Assets/01_Scripts/DishPickupPoint.cs` y `Assets/01_Scripts/ServedDish.cs`
* **Qué hace:**
  * `ServedDish`: Contenedor final del plato terminado con su referencia a `DishData`.
  * `DishPickupPoint`: Punto de anclaje que asegura que el plato servido quede firme sobre la barra antes de ser transportado.

---

### 4.13 `DrinkDispenser.cs` y `DrinkCup.cs`
* **Ruta:** `Assets/01_Scripts/DrinkDispenser.cs` y `Assets/01_Scripts/DrinkCup.cs`
* **Qué hace:** Dispensador de refrescos tradicionales (Mocochinchi y Somó).
* **Función principal:** Al colocar un vaso vacío (`DrinkCup`) y girar el grifo, activa un sistema de partículas con el color del refresco regional (ámbar para Cochabamba/La Paz, crema para Santa Cruz) y llena progresivamente el vaso.
* **Código y métodos clave:**
```csharp
public class DrinkDispenser : MonoBehaviour
{
    [SerializeField] private ParticleSystem chorroParticulas;

    public void TenirChorro(Color colorBebida)
    {
        var main = chorroParticulas.main;
        main.startColor = colorBebida;
    }

    public void Servir()
    {
        if (vasoPresente != null && !vasoPresente.EstaLleno)
        {
            vasoPresente.Llenar(colorActual);
        }
    }
}
```

---

### 4.14 `DeliveryCounter.cs` y `ServiceBell.cs`
* **Ruta:** `Assets/01_Scripts/DeliveryCounter.cs` y `Assets/01_Scripts/ServiceBell.cs`
* **Qué hace:** Barra de despacho y validación de comandas.
* **Función principal:** Al colocar un `ServedDish` o vaso en la barra y golpear la campana (`ServiceBell`), `DeliveryCounter` consulta a `OrderManager` para verificar si el plato coincide con alguna orden activa.
* **Código y métodos clave:**
```csharp
public class DeliveryCounter : MonoBehaviour
{
    public void ProcesarEntrega()
    {
        var plato = DetectarPlatoEnBarra();
        if (plato != null)
        {
            bool acierto = OrderManager.Instance.Tachar(plato.Data);
            if (acierto)
            {
                DestellarLuz(Color.green, 2f, 0.5f);
                Destroy(plato.gameObject);
            }
            else
            {
                DestellarLuz(Color.red, 2f, 0.5f);
            }
        }
    }
}
```

---

### 4.15 `TrashBin.cs`
* **Ruta:** `Assets/01_Scripts/TrashBin.cs`
* **Qué hace:** Basurero de cocina.
* **Función principal:** Detecta alimentos quemados o platos fallidos que entren en su colisionador y los destruye limpiamente con sonido y efecto de desecho.

---

## 5. Infraestructura de Entrada, Simulación y Seguridad XR

### 5.1 `XRSceneRelinker.cs`
* **Ruta:** `Assets/01_Scripts/XRSceneRelinker.cs`
* **Qué hace:** Corrige el error del `XRInteractionSimulator` en Unity XRI 3.5.1 donde las manos virtuales se congelaban al cambiar de escena.
* **Función principal:** Se suscribe a `SceneManager.sceneLoaded`, encuentra los nuevos controladores en la escena cargada y reasigna los transforms internos mediante reflexión.
* **Código y métodos clave:**
```csharp
public class XRSceneRelinker : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RelinkNextFrames());
    }

    private IEnumerator RelinkNextFrames()
    {
        yield return null; // Espera a que la nueva jerarquía despierte
        // Re-vincula m_RightControllerTransform y m_LeftControllerTransform en el simulador
    }
}
```

---

### 5.2 `ToolHome.cs`
* **Ruta:** `Assets/01_Scripts/ToolHome.cs`
* **Qué hace:** Garantiza que las herramientas esenciales (como el cuchillo) nunca se pierdan si el jugador las suelta accidentalmente en el suelo.
* **Función principal:** Al detectar el evento `selectExited` de XR, espera un tiempo y reubica la herramienta en su base magnética sobre la mesa.

---

### 5.3 `PlayerVoidGuard.cs`
* **Ruta:** `Assets/01_Scripts/PlayerVoidGuard.cs`
* **Qué hace:** Salvaguarda contra caídas por descalibración del visor en VR.
* **Función principal:** Si la coordenada $Y < -0.15$ m, reubica el `XR Origin` de vuelta en el centro de la cocina.

---

### 5.4 `HeadCollisionGuard.cs`
* **Ruta:** `Assets/01_Scripts/HeadCollisionGuard.cs`
* **Qué hace:** Evita que el jugador atraviese paredes o campanas de cocina con la cabeza en VR empujando el rig o fundiendo a negro si invade geometría sólida.

---

## 6. Atmósfera, Audio y Efectos Visuales

### 6.1 `KitchenExtractor.cs`
* **Ruta:** `Assets/01_Scripts/KitchenExtractor.cs`
* **Qué hace:** Controla los extractores y campanas de ventilación sobre las hornallas y la zona de entrega.
* **Función principal:** Activa la rotación de aspas, iluminación LED y disipación de humo de los sartenes para mantener despejada la vista del jugador.

---

### 6.2 `KitchenVisualsEnhancer.cs`
* **Ruta:** `Assets/01_Scripts/KitchenVisualsEnhancer.cs`
* **Qué hace:** Mejora estética en tiempo de ejecución.
* **Función principal:** Configura los materiales metálicos de la campana, las texturas de madera de los cajones y asegura que las paredes norte mantengan su acabado original.

---

## Resumen de Conexión entre Subsistemas

```
[Main Menu] 
   └── MainMenuController ──▶ GameProgressManager (PlayerPrefs)
                                    │
                                    ▼ (Carga de Escena)
[Escena de Nivel (1, 2 o 3)]
   ├── XRSceneRelinker (Re-vincula manos y simulador XR)
   ├── LevelManager (Game Loop, Temporizador y Estrellas)
   │     ├── LevelData (Configuración del nivel y platos)
   │     │     └── DepartmentData ──▶ WallFlag (Bandera de la pared)
   │     └── OrderManager (Comandas activas)
   │           └── OrderBoard (Tickets en barra)
   │
   ├── Estaciones Culinarias:
   │     ├── ItemDispenser ──▶ IngredientItem (Materia prima)
   │     ├── CuttingBoard + KnifeChopper (Picado)
   │     ├── CookingCounter + CookingVessel (Fritura / Cocción)
   │     ├── PlateItem (Ensamble de ingredientes) ──▶ ServedDish
   │     └── DrinkDispenser + DrinkCup (Refresco regional)
   │
   └── Entrega y Puntuación:
         └── DeliveryCounter + ServiceBell ──▶ Evalúa contra OrderManager
                                                      │
                                                      ▼
                                       LevelScreenHUD (Pantalla de resultados)
                                                      │
                                                      ▼
                                       Desbloqueo de siguiente nivel en GameProgressManager
```
