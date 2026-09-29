# Cocina Boliviana VR - Manual Técnico y Guía de Defensa del Proyecto

Este documento reúne toda la información técnica, arquitectónica y de diseño del videojuego **Cocina Boliviana VR**. Está estructurado específicamente para responder con precisión y solvencia técnica a las preguntas de evaluadores, docentes y tribunales durante la presentación y defensa del proyecto.

---

## 1. Ficha Técnica y Resumen Ejecutivo

* **Motor de Juego:** Unity 6000.5.6f1
* **Pipeline de Renderizado:** Universal Render Pipeline (URP)
* **Framework de Realidad Virtual:** Unity XR Interaction Toolkit (v3.5.1), OpenXR, Meta XR Core SDK, XR Device Simulator (para pruebas en editor sin visor físico).
* **Paradigma de Arquitectura:** Arquitectura basada en datos (*Data-Driven Architecture*) mediante `ScriptableObjects`, desacoplando completamente la lógica de ejecución respecto a la definición de recetas, ingredientes y reglas de nivel.
* **Sistema de Entrada (Input):** Unity New Input System con controladores Action-Based y simulador de escritorio con ratón y teclado (Point-and-Click para manos virtuales).
* **Persistencia:** Guardado progresivo de campaña mediante `PlayerPrefs` (`GameProgressManager`), almacenando el desbloqueo del nivel más alto superado.

### Resumen del Juego
Cocina Boliviana es un simulador de cocina en realidad virtual estructurado en una campaña de 3 niveles regionales consecutivos:
1. **Nivel 1 - Cochabamba:** Especialidad en Silpancho y Pique Macho, con bebida tradicional Mocochinchi.
2. **Nivel 2 - La Paz:** Especialidad en Plato Paceño (choclo, habas, queso frito, papa) y Silpancho/Pique, con Mocochinchi.
3. **Nivel 3 - Santa Cruz:** Especialidad en Majadito Cruceño y Sonso, con bebida típica Somó.

El jugador opera en una estación gastronómica interactiva donde procesa ingredientes (corte en tabla, cocción en hornalla/sartén), monta las preparaciones en platos, sirve bebidas artesanales mediante dispensadores y entrega los pedidos antes de que caduque el temporizador, evaluando su rendimiento mediante un sistema de 1 a 3 estrellas.

---

## 2. Mapa de Carpetas y Organización de Assets

La raíz del proyecto sigue un estándar limpio de nomenclatura numérica para mantener el orden de compilación y búsqueda:

```
Assets/
├── 00_Scenes/             -> Escenas del juego (Menú y niveles de campaña)
├── 01_Scripts/            -> Código fuente en C# del gameplay y subsistemas
│   └── Data/              -> Definición de ScriptableObjects y enumeraciones
├── 02_Prefabs/            -> Elementos interactivos, platos, lámparas y utensilios
├── 03_SO/                 -> Instancias .asset de ScriptableObjects
│   ├── Departamentos/     -> Cochabamba, La Paz, Santa Cruz
│   ├── Ingredientes/      -> Papa, Carne, Cebolla, Tomate, Huevo, etc.
│   ├── Niveles/           -> Configuración de dificultad, duración y platos
│   ├── Platos/            -> Silpancho, Pique, Majadito, Plato Paceño, Sonso
│   └── Refresco/          -> Mocochinchi y Somó (líquidos, colores y datos)
├── Materials/             -> Materiales URP (metales, maderas, cerámicas, líquidos)
├── Samples/               -> Ejemplos del XR Interaction Toolkit (Device Simulator)
└── Editor/                -> Herramientas y scripts de automatización en el editor
```

---

## 3. Arquitectura Basada en Datos (ScriptableObjects)

Una de las decisiones arquitectónicas centrales fue evitar "hardcodear" (escribir en código duro) las recetas, tiempos o reglas de nivel en clases monolíticas. Toda la lógica lee datos desde instancias de `ScriptableObject`.

### ¿Por qué se hizo así?
1. **Escalabilidad:** Añadir un nuevo plato o ingrediente no requiere modificar ni una sola línea de C#. Solo se crea un nuevo asset en Unity (`Assets/03_SO/...`).
2. **Reutilización:** El mismo sistema de cocción (`CookingVessel`) procesa carne, papa o cebolla porque consulta las propiedades del `IngredientData` del objeto colocado.
3. **Mantenimiento limpio:** Si se quiere ajustar el tiempo de cocción de la papa de 8 a 6 segundos, se modifica en el inspector del archivo `Papa.asset` sin necesidad de recompilar el proyecto.

### Definición de Clases de Datos (`Assets/01_Scripts/Data/`)

#### A. `IngredientData.cs`
* **Ubicación:** `Assets/01_Scripts/Data/IngredientData.cs`
* **Propósito:** Define las características físicas y gastronómicas de un ingrediente base.
* **Propiedades principales:**
  * `nombre` (`string`): Nombre identificador (ej. "Papa").
  * `tipoCorteRequerido` (`TipoCorte`): Define si requiere corte y de qué tipo (`Entero`, `Rodajas`, `Picado`, `Tiras`, etc.).
  * `metodoCoccionRequerido` (`MetodoCoccion`): Método necesario (`Ninguno`, `Hervir`, `Freir`, `Asar`).
  * `tiempoCoccion` (`float`): Segundos requeridos sobre la fuente de calor para alcanzar el punto cocido.
  * `tiempoHastaQuemarse` (`float`): Segundos adicionales antes de que el ingrediente pase a estado quemado.
  * `icono` (`Sprite`): Representación visual para la interfaz de comandas.
  * `prefabCrudo`, `prefabProcesado`, `prefabCocido`, `prefabQuemado`: Modelos 3D intercambiables según el estado culinario.

#### B. `DishData.cs`
* **Ubicación:** `Assets/01_Scripts/Data/DishData.cs`
* **Propósito:** Define la composición de una receta completa.
* **Propiedades principales:**
  * `nombre` (`string`): Nombre del plato (ej. "Silpancho").
  * `icono` (`Sprite`): Imagen del plato para la tarjeta de pedido.
  * `ingredientesRequeridos` (`List<IngredientData>`): Lista de ingredientes que deben estar presentes y procesados.
  * `tiempoLimite` (`float`): Tiempo base otorgado al jugador para entregar la orden.
  * `puntosBase` (`int`): Recompensa otorgada al completar el plato con éxito.
  * `platoPrefab` (`GameObject`): Prefab final que se genera sobre la barra de servicio cuando la receta se completa.
  * `colorLiquido` (`Color`): En caso de usarse para bebidas, almacena la tonalidad del fluido.

#### C. `DepartmentData.cs`
* **Ubicación:** `Assets/01_Scripts/Data/DepartmentData.cs`
* **Propósito:** Agrupa la identidad regional de un departamento boliviano.
* **Propiedades principales:**
  * `nombreDepartamento` (`string`): Ej. "Cochabamba".
  * `banderaSprite` (`Sprite`): Textura de la bandera departamental que se proyecta en el cuadro de la pared (`WallFlag.cs`).
  * `platosTipicos` (`List<DishData>`): Platos característicos de la zona.
  * `refrescoTipico` (`DishData`): Bebida asociada (Mocochinchi para Cochabamba y La Paz, Somó para Santa Cruz).

#### D. `LevelData.cs`
* **Ubicación:** `Assets/01_Scripts/Data/LevelData.cs`
* **Propósito:** Modela las condiciones y metas de juego de un nivel específico de la campaña.
* **Propiedades principales:**
  * `nombreNivel` (`string`): Ej. "Nivel 1 - Cochabamba".
  * `departamento` (`DepartmentData`): Referencia al departamento vinculado.
  * `duracionNivel` (`float`): Duración en segundos de la ronda (ej. 150s = 2:30 min).
  * `puntos1Estrella`, `puntos2Estrellas`, `puntos3Estrellas` (`int`): Umbrales de puntuación requeridos.
  * `platosDisponibles` (`DishData[]`): Catálogo de platos que el generador de pedidos puede solicitar aleatoriamente.
  * `maximoOrdenesSimultaneas` (`int`): Límite de comandas visibles a la vez en la pantalla.

#### E. Enumeraciones (`TipoCorte.cs` y `MetodoCoccion.cs`)
* **`TipoCorte`**: `Entero`, `PicadoFino`, `Rodajas`, `Cubos`, `Tiras`.
* **`MetodoCoccion`**: `Crudo`, `Freir`, `Hervir`, `Plancha`, `Hornear`.

---

## 4. Catálogo Completo de Scripts del Proyecto

A continuación se detalla cada script del proyecto, agrupado por su subsistema funcional.

### 4.1. Núcleo de Campaña, Menú y Persistencia

#### `GameProgressManager.cs`
* **Ruta:** `Assets/01_Scripts/GameProgressManager.cs`
* **Qué hace:** Administrador estático responsable del almacenamiento persistente del juego mediante `PlayerPrefs`.
* **Detalle técnico:**
  * Almacena únicamente el índice del nivel más alto desbloqueado (`1`, `2` o `3`) bajo la clave `"CocinaBoliviana_NivelGuardado"`.
  * No guarda el estado volátil a mitad de nivel (como ingredientes sueltos en mesas), respetando el flujo de juego ágil tipo arcade/Overcooked.
  * Provee métodos utilitarios: `ObtenerNivelGuardado()`, `GuardarNivel(int nivel)`, `ReiniciarProgreso()`, `TienePartidaGuardada()` y `ObtenerNombreEscenaNivel(int nivel)`.

#### `MainMenuController.cs`
* **Ruta:** `Assets/01_Scripts/MainMenuController.cs`
* **Qué hace:** Gestiona la navegación y botones en la escena `Main Menu`.
* **Detalle técnico:**
  * `NuevaPartida()`: Llama a `GameProgressManager.ReiniciarProgreso()` (fija nivel 1) y carga mediante `SceneManager.LoadScene` la escena `"Nivel 1 - Cochabamba"`.
  * `ContinuarPartida()`: Consulta `GameProgressManager.ObtenerNivelGuardado()` y carga directamente la escena del nivel guardado (1, 2 o 3).
  * `JugarNivel(int indice)`: Permite saltar a un nivel específico desde los selectores del mapa.
  * En su inicio (`Start()`), evalúa si existe partida previa para activar o atenuar el botón interactivo de "Continuar".

#### `LevelManager.cs`
* **Ruta:** `Assets/01_Scripts/LevelManager.cs`
* **Qué hace:** Es el controlador maestro (*Game Loop Controller*) de la partida activa dentro de la cocina.
* **Detalle técnico:**
  * Maneja una máquina de estados: `Starting` (cuenta atrás 3.. 2.. 1..), `Playing` (reloj corriendo, pedidos activos) y `Finished` (resumen y cálculo de estrellas).
  * Controla el sistema de puntuación Overcooked: suma puntos por plato entregado a tiempo, suma propina por racha de aciertos consecutivos (`bonusPorNivelRacha`), y aplica penalizaciones sin permitir números negativos.
  * Al expirar el tiempo, calcula si se obtuvo al menos 1 estrella. Si el nivel es superado, invoca automáticamente `GameProgressManager.GuardarNivel(numeroNivel + 1)`, desbloqueando el siguiente escalón de la campaña.
  * Dispara eventos que sincronizan la interfaz de usuario (`LevelScreenHUD`).

#### `LevelScreenHUD.cs`
* **Ruta:** `Assets/01_Scripts/LevelScreenHUD.cs`
* **Qué hace:** Controla la pantalla física colocada sobre la pared de la cocina (World-Space Canvas).
* **Detalle técnico:**
  * Muestra el temporizador regresivo formateado en minutos y segundos (`mm:ss`).
  * Muestra la puntuación actual, la barra de progreso hacia las estrellas y el multiplicador de combo.
  * Al finalizar la partida, despliega el panel de resultados: estrellas conseguidas, platos entregados, botón para reintentar el nivel y botón de "Siguiente Nivel" (que carga la escena correspondiente mediante `GameProgressManager.ObtenerNombreEscenaNivel`). Si se concluye el Nivel 3, muestra la felicitación final de "¡Maestro de la Cocina Boliviana!".

#### `OrderManager.cs` y `OrderBoard.cs`
* **Ruta:** `Assets/01_Scripts/OrderManager.cs` y `Assets/01_Scripts/OrderBoard.cs`
* **Qué hace:** Generador y monitor de comandas de clientes.
* **Detalle técnico:**
  * Genera instancias de `OrderData` a intervalos configurables, seleccionando platos al azar entre los permitidos en el `LevelData` activo.
  * Cada orden tiene un temporizador individual (`tiempoRestante`). Si el temporizador llega a cero, la orden caduca, se reproduce un sonido de fallo, se rompe la racha de combo y se penaliza la puntuación general.
  * `OrderBoard` instancia tarjetas visuales sobre la barra con los iconos de los ingredientes que componen cada pedido solicitado.

---

### 4.2. Infraestructura y Robustez en Realidad Virtual (XR)

#### `XRSceneRelinker.cs`
* **Ruta:** `Assets/01_Scripts/XRSceneRelinker.cs`
* **Qué hace:** Resuelve un problema de compatibilidad crítico entre el XR Interaction Toolkit 3.5.1 y el cambio de escenas en tiempo de ejecución.
* **¿Qué problema resuelve y cómo?**
  * *El problema:* En Unity XRI 3.5.1, el simulador de manos (`XRInteractionSimulator`) se instancia como un objeto `DontDestroyOnLoad`. Al cambiar de escena (por ejemplo, del Menú Principal a Nivel 1), el `XR Origin`, la cámara y los mandos de la escena vieja se destruyen, pero el simulador mantenía referencias nulas a los transforms antiguos. Esto causaba que la mano virtual quedara totalmente paralizada e inmóvil al entrar al nivel.
  * *La solución:* `XRSceneRelinker` se inicializa automáticamente con `[RuntimeInitializeOnLoadMethod]` y se suscribe al evento `SceneManager.sceneLoaded`. Al detectar una nueva escena, busca el nuevo `XR Origin`, localiza los nuevos componentes de entrada (`XRInputModalityManager`, `ActionBasedController`), reasigna las referencias internas del simulador mediante reflexión y reactiva el modo Point-and-Click del ratón para la mano derecha.

#### `ToolHome.cs`
* **Ruta:** `Assets/01_Scripts/ToolHome.cs`
* **Qué hace:** Sistema de retorno seguro de herramientas.
* **Detalle técnico:**
  * Si el jugador suelta el cuchillo en el suelo o este cae por un error de física, `ToolHome` detecta la deselección (`selectExited`) y, tras un breve temporizador o al cruzar un límite de altura, devuelve el utensilio suavemente a su soporte original sobre la mesa de trabajo con rotación y velocidad neutralizadas (`Rigidbody.linearVelocity = Vector3.zero`).

#### `PlayerVoidGuard.cs` y `HeadCollisionGuard.cs`
* **Ruta:** `Assets/01_Scripts/PlayerVoidGuard.cs` y `Assets/01_Scripts/HeadCollisionGuard.cs`
* **Qué hace:** Seguridad de navegación en VR.
* **Detalle técnico:**
  * `PlayerVoidGuard` comprueba la posición vertical del jugador en cada frame; si la coordenada $Y < -0.15$ m (indicando que el jugador atravesó el suelo por pérdida de tracking o calibración), reubica inmediatamente el `XR Origin` en el punto de inicio de la cocina.
  * `HeadCollisionGuard` oscurece la pantalla suavemente o empuja la cámara si el visor físico intenta atravesar una pared sólida o campana de cocina.

---

### 4.3. Estaciones de Preparación y Mecánicas Culinarias

#### `ItemDispenser.cs` e `IngredientSelectorMenu.cs`
* **Ruta:** `Assets/01_Scripts/ItemDispenser.cs` y `Assets/01_Scripts/IngredientSelectorMenu.cs`
* **Qué hace:** Dispensación infinita de materias primas desde las cajas de ingredientes.
* **Detalle técnico:**
  * Al interactuar con una caja de verduras, carnes o abarrotes, `ItemDispenser` instancia una copia del prefab de ingrediente en la mano del jugador o sobre la boca del cajón.
  * `IngredientSelectorMenu` provee una interfaz gráfica diegética en 3D para alternar entre diferentes variedades de ingredientes en una misma estación (por ejemplo, elegir entre carne de res o chorizo).
  * **Manejo de escala:** Aplica normalización de escala para que los objetos hijos no hereden deformaciones si la caja contenedora tiene escalas no uniformes en sus ejes.

#### `IngredientItem.cs`
* **Ruta:** `Assets/01_Scripts/IngredientItem.cs`
* **Qué hace:** Representa el estado físico y culinario de un ingrediente individual en la escena.
* **Detalle técnico:**
  * Contiene la referencia a su `IngredientData`.
  * Registra las variables de estado: `estadoCorteActual` y `estadoCoccionActual`.
  * Métodos como `AplicarCorte()` y `Cocinar()` evalúan si la acción coincide con los requerimientos del `ScriptableObject`. Al cambiar de estado, oculta la malla actual y activa la correspondiente (crudo $\rightarrow$ picado $\rightarrow$ cocido $\rightarrow$ quemado).

#### `CuttingBoard.cs` y `KnifeChopper.cs`
* **Ruta:** `Assets/01_Scripts/CuttingBoard.cs` y `Assets/01_Scripts/KnifeChopper.cs`
* **Qué hace:** Sistema de fijación y corte físico de alimentos.
* **Detalle técnico:**
  * La tabla de picar (`CuttingBoard`) detecta ingredientes colocados sobre ella mediante consultas espaciales no asignativas (`Physics.OverlapBoxNonAlloc`) throttled a 10 Hz para optimizar rendimiento VR.
  * Cuando el cuchillo (`KnifeChopper`) desciende con velocidad adecuada y colisiona con el ingrediente montado, emite retroalimentación acústica de corte, genera partículas y avanza el progreso de rebanado hasta transformar el ingrediente a su versión procesada.

#### `CookingCounter.cs`, `CookingVessel.cs` y `BurnerFlame.cs`
* **Ruta:** `Assets/01_Scripts/CookingCounter.cs`, `Assets/01_Scripts/CookingVessel.cs` y `Assets/01_Scripts/BurnerFlame.cs`
* **Qué hace:** Simulación térmica de hornallas, sartenes y ollas.
* **Detalle técnico:**
  * `CookingCounter` representa la cocina de gas; administra el encendido de hornallas mediante perillas interactivas.
  * `BurnerFlame` controla visualmente el fuego de la hornalla (luz puntual y sistema de partículas de llama azul/naranja).
  * `CookingVessel` (asociado al sartén o la olla) comprueba si está posicionado sobre una hornalla activa. Al calentarse, transfiere temperatura a los ingredientes depositados en su interior, incrementando su tiempo de cocción, reproduciendo el sonido de fritura/ebullición (`AudioSource`) y emitiendo partículas de vapor/humo. Si el jugador se descuida, el ingrediente sobrepasa su tiempo límite y pasa al estado quemado.

#### `PlateCounter.cs`, `PlateItem.cs`, `DishPickupPoint.cs` y `ServedDish.cs`
* **Ruta:** `Assets/01_Scripts/PlateCounter.cs`, `Assets/01_Scripts/PlateItem.cs`, `Assets/01_Scripts/DishPickupPoint.cs` y `Assets/01_Scripts/ServedDish.cs`
* **Qué hace:** Ensamble del plato, validación de recetas y preparación para entrega.
* **Detalle técnico:**
  * `PlateItem` es el plato receptor. Al acercar ingredientes cocidos o procesados, estos se acoplan magnéticamente en posiciones predeterminadas (ej. cama de arroz abajo, carne en el centro, huevo encima, sarza arriba).
  * El plato compara continuamente su lista de ingredientes adheridos contra las recetas registradas en los `DishData`.
  * Cuando se reúnen todos los ingredientes correctos de un plato (por ejemplo, los de un Silpancho), `PlateItem` se auto-reemplaza por una instancia de `ServedDish`, un prefab consolidado optimizado con el plato terminado listo para servicio.

#### `DrinkDispenser.cs` y `DrinkCup.cs`
* **Ruta:** `Assets/01_Scripts/DrinkDispenser.cs` y `Assets/01_Scripts/DrinkCup.cs`
* **Qué hace:** Dispensador de refrescos tradicionales bolivianos.
* **Detalle técnico:**
  * Detecta la presencia de un vaso vacío (`DrinkCup`) bajo la boquilla.
  * Al accionar la palanca o grifo, emite un chorro continuo mediante un `ParticleSystem` con simulación de líquido teñido según la región:
    * **Cochabamba y La Paz:** Tono caramelo/café claro característico del **Mocochinchi** (`Color(0.58f, 0.35f, 0.16f)`).
    * **Santa Cruz:** Tono amarillo cremoso suave característico del **Somó** (`Color(0.96f, 0.90f, 0.58f)`).
  * Llena progresivamente el vaso, activando el shader de volumen de líquido en su interior hasta declararlo listo para consumo.

#### `DeliveryCounter.cs` y `ServiceBell.cs`
* **Ruta:** `Assets/01_Scripts/DeliveryCounter.cs` y `Assets/01_Scripts/ServiceBell.cs`
* **Qué hace:** Zona de despacho y validación final de comandas.
* **Detalle técnico:**
  * El jugador sitúa el plato servido (`ServedDish`) o el vaso sobre la barra de entrega.
  * Al presionar la campana metálica de servicio (`ServiceBell`), esta ejecuta una pequeña animación elástica, emite el sonido de timbre clásico y notifica a `DeliveryCounter`.
  * `DeliveryCounter` compara el plato depositado contra las órdenes pendientes en `OrderManager`. Si coincide, suma el puntaje, despacha la comanda con éxito y destruye el plato entregado; si no coincide o no hay pedidos activos de ese tipo, emite retroalimentación de error.

#### `TrashBin.cs`
* **Ruta:** `Assets/01_Scripts/TrashBin.cs`
* **Qué hace:** Basurero interactivo. Destruye de forma segura y con efecto sonoro cualquier ingrediente quemado, residuo o preparación fallida que caiga dentro de su volumen de colisión.

---

### 4.4. Ambientación, Audio y Efectos Visuales

#### `KitchenExtractor.cs`
* **Ruta:** `Assets/01_Scripts/KitchenExtractor.cs`
* **Qué hace:** Control de campanas extractoras sobre la cocina y la zona de entrega.
* **Detalle técnico:**
  * Incluye interruptores funcionales con luz indicadora LED que encienden los extractores, activando el giro de los ventiladores y disipando el humo excesivo de los sartenes para evitar saturación visual en el visor.

#### `KitchenVisualsEnhancer.cs`
* **Ruta:** `Assets/01_Scripts/KitchenVisualsEnhancer.cs`
* **Qué hace:** Aplica en tiempo de ejecución mejoras de materiales, acabados de madera rústica y superficies de acero inoxidable sobre la isla central y cajones, manteniendo la textura original de ladrillo/estuco en las paredes norte.

#### `BackgroundMusicManager.cs`
* **Ruta:** `Assets/01_Scripts/BackgroundMusicManager.cs`
* **Qué hace:** Reproductor continuo de música de fondo tradicional instrumental, manteniendo persistencia auditiva y volumen controlado durante la sesión.

#### `WallFlag.cs`
* **Ruta:** `Assets/01_Scripts/WallFlag.cs`
* **Qué hace:** Script adjunto al cuadro sobre la pared de la cocina. En `Awake()` consulta el `LevelData` cargado y asigna dinámicamente la textura del escudo y bandera del departamento correspondiente (Cochabamba, La Paz o Santa Cruz).

---

### 4.5. Scripts de Automatización en Editor (`Assets/Editor/`)

Se desarrollaron herramientas para el Editor de Unity que permitieron configurar de manera reproducible las escenas sin tener que rehacer pasos manuales propensos a error:
* **`CampaignSetup.cs`:** Automatiza la creación y vinculación de las escenas de los 3 niveles a partir de `First Scene.unity`, inyectando las dependencias de `LevelData`, `OrderManager`, flags y `Build Settings`.
* **`ReemplazarLamparas.cs`:** Herramienta que localizó los 4 GameObjects de lámparas antiguas (`Lamp_NorthWest`, `Lamp_NorthEast`, etc.) en todas las escenas y los sustituyó por instancias del nuevo prefab estandarizado `Lamp_prime.prefab` respetando sus coordenadas precisas.
* **`KitchenVisualsSetup.cs`:** Asegura que los materiales URP Lit estén creados con los GUIDs correctos y asignados a las campanas, barras y estaciones.

---

## 5. Decisiones Técnicas Clave y Resolución de Problemas

Esta sección resume las preguntas más complejas que suelen plantear los evaluadores sobre la arquitectura del software.

### Pregunta 1: ¿Por qué y cómo optimizaron la física para Realidad Virtual?
* **Respuesta:** En VR, mantener una tasa de cuadros estable (mínimo 72 o 90 FPS) es imperativo para evitar mareo cinetósico (*motion sickness*). Detectar objetos en estaciones usando triggers continuos (`OnTriggerStay`) o escaneos en `FixedUpdate` (50 veces por segundo por estación) generaba picos de recolección de basura (*Garbage Collection*) y consumo innecesario de CPU.
* **Solución aplicada:** Se migraron todas las estaciones a escaneos espaciales `Physics.OverlapBoxNonAlloc` con arrays de colisionadores pre-asignados (cero asignación en heap) y se limitó la frecuencia de escaneo a intervalos temporizados de 0.1 segundos (10 Hz). Además, se evitaron colliders trigger de gran volumen sobre las mesas porque interferían e interceptaban el raycast de los mandos del jugador al intentar tomar objetos lejanos.

### Pregunta 2: ¿Cómo solucionaron el congelamiento de las manos al cambiar de escena en VR?
* **Respuesta:** En el paquete `XR Interaction Toolkit 3.5.1`, el componente `XRInteractionSimulator` se marca como `DontDestroyOnLoad`. Cuando se ejecuta `SceneManager.LoadScene` (modo Single) para pasar del Menú Principal a Cochabamba, la jerarquía de la escena anterior (incluyendo el `XR Origin` y los controladores) se destruye. Sin embargo, el simulador mantenía en caché las variables privadas de los transforms destruidos, haciendo que el método de apuntado `AimDeviceAtWorldPoint` fallara silenciosamente y dejara las manos congeladas en el origen.
* **Solución aplicada:** Se implementó `XRSceneRelinker.cs`. Mediante reflexión y suscripción a `SceneManager.sceneLoaded`, localiza dinámicamente los nuevos controladores de la escena entrante, reinyecta sus referencias en el simulador activo y recalibra la entrada del ratón sin requerir reiniciar la aplicación.

### Pregunta 3: ¿Cómo funciona el guardado y por qué no se guarda el estado a mitad de receta?
* **Respuesta:** El guardado se gestiona mediante `GameProgressManager` utilizando `PlayerPrefs`. Guarda el índice entero del nivel alcanzado (`1`, `2` o `3`).
* **Justificación técnica:** Guardar el estado a mitad de nivel en un juego de ritmo rápido tipo arcade requeriría serializar decenas de transforms dinámicos, grados de cocción y velocidades físicas de verduras rebanadas, lo cual añade fragilidad al juego con nulo valor de experiencia. Guardar el nivel desbloqueado garantiza estabilidad absoluta, permitiendo al jugador reanudar su avance de campaña directamente desde el Menú Principal mediante el botón "Continuar".

### Pregunta 4: ¿Cómo evitaron deformaciones de malla al emparentar ingredientes a platos o cajas?
* **Respuesta:** Si una caja o estación tiene una escala no uniforme (por ejemplo, `Scale = (1.5, 0.8, 1.2)`), emparentar directamente un tomate como hijo de esa caja provocaría que el tomate se distorsione y aplaste.
* **Solución aplicada:** En `IngredientItem.cs`, se almacena la escala original de mundo (`lossyScale`) del prefab. Al emparentar el objeto a un contenedor, se recalcula su `localScale` dividiendo la escala de mundo deseada entre la escala acumulada del padre, manteniendo proporciones geométricas exactas.

### Pregunta 5: ¿Por qué se veían objetos en color magenta y cómo se solucionó?
* **Respuesta:** Ocurría cuando materiales del proyecto hacían referencia a shaders del pipeline 2D (`Mesh2D-Lit-Default`) o shaders eliminados cuyos identificadores universales (GUIDs) estaban rotos en los archivos `.mat`.
* **Solución aplicada:** Se estandarizó el uso del shader universal 3D de URP (`Universal Render Pipeline/Lit`, GUID: `933532a4fcc9baf4fa0491de14d08ed7`). Se regeneraron los metadatos corruptos de los materiales y se actualizaron las referencias en las 4 escenas para garantizar compatibilidad con luces en tiempo real e iluminación horneada.

---

## 6. Guía Rápida para Demostración en Vivo

Si durante la defensa te piden realizar una demostración práctica de las mecánicas, sigue este guion paso a paso:

### Demostración 1: Preparación de Silpancho (Nivel 1 - Cochabamba)
1. **Inicio:** Presiona "Nueva Partida" en el Menú Principal. Observa la transición fluida a la cocina de Cochabamba con la bandera departamental en la pared y la música de fondo.
2. **Corte:** Toma una cebolla o tomate de la caja de verduras. Colócalo sobre la tabla de picar (`CuttingBoard`). Toma el cuchillo y corta hacia abajo dos o tres veces hasta que se transforme en sarza picada.
3. **Cocción:** Toma un filete de carne cruda y papas. Enciende la perilla de la cocina de gas para ver la llama azul. Coloca el sartén sobre la hornalla y deposita la carne. Observa el sonido de fritura y el humo de cocción. Retíralo antes de que se queme.
4. **Emplatado:** Toma un plato blanco (`PlateItem`). Coloca el arroz, luego añade las papas fritas, la carne cocida y finalmente la sarza picada.
5. **Servicio y Entrega:** Una vez ensamblado, el plato se consolida automáticamente como un `ServedDish`. Llévalo a la barra de despacho (`DeliveryCounter`) y presiona la campana metálica (`ServiceBell`). Observa la suma de puntos en la pantalla HUD superior.

### Demostración 2: Servicio de Bebida Típica
1. Toma un vaso vacío (`DrinkCup`) del soporte de cristalería.
2. Colócalo en la bandeja del dispensador artesanal (`DrinkDispenser`).
3. Gira la palanca del grifo: observa el chorro de partículas teñido de color café ámbar (Mocochinchi) y el llenado gradual del líquido en el vaso.
4. Retira el vaso lleno y colócalo junto al plato para despachar un pedido combinado.

### Demostración 3: Flujo de Campaña y Guardado
1. Al acumular los puntos mínimos (1 estrella) antes de agotar el tiempo, observa la pantalla de victoria del Nivel 1.
2. Presiona el botón "Siguiente Nivel": el juego transiciona a **Nivel 2 - La Paz**.
3. Detén la ejecución en Unity. Vuelve a dar Play y presiona **"Continuar Partida"** en el Menú Principal: el sistema cargará directamente el Nivel 2, demostrando la persistencia del progreso del jugador.
