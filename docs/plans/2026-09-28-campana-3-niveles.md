# Plan de Implementación: Campaña Completa de 3 Niveles y Sistema de Progresión

> **Objetivo:** Completar el flujo integral del juego con 3 niveles basados en la gastronomía boliviana (Cochabamba, La Paz, Santa Cruz), sistema de guardado persistente de progreso, recetas auténticas ampliadas, flujo de menús ("Nueva Partida" / "Continuar") y ambientación con banderas y bebidas típicas.

---

## 1. Alcance y Arquitectura General

```mermaid
flowchart TD
    Menu["Main Menu.unity\n- Nueva Partida (Nivel 1)\n- Continuar (Nivel Guardado)"] 
    Save[(PlayerPrefs:\nNivel Desbloqueado)]
    
    Menu -->|Guarda Nivel 1| Nivel1["Nivel 1 - Cochabamba.unity\n- Platos: Silpancho, Pique\n- Refresco: Mocochinchi\n- Bandera: Cochabamba"]
    Nivel1 -->|Completado (>=1★)| Save
    Save --> Nivel2["Nivel 2 - La Paz.unity\n- Platos: Plato Paceño\n- Refresco: Mocochinchi\n- Bandera: La Paz"]
    Nivel2 -->|Completado (>=1★)| Save
    Save --> Nivel3["Nivel 3 - Santa Cruz.unity\n- Platos: Majadito, Sonso\n- Refresco: Somó\n- Bandera: Santa Cruz"]
    Nivel3 -->|Fin Campaña| Victoria["Pantalla de Victoria Final\nRegreso al Menú"]
```

---

## 2. Componentes a Desarrollar

### A. Sistema de Guardado Persistente (`GameProgressManager.cs`)
- **Almacenamiento**: `PlayerPrefs` clave `CocinaBoliviana_NivelGuardado`.
- **Valores**: `1` (Cochabamba), `2` (La Paz), `3` (Santa Cruz).
- **Métodos**:
  - `int ObtenerNivelGuardado()`: Devuelve el nivel actual (por defecto 1).
  - `void GuardarNivel(int nivel)`: Actualiza si el nivel alcanzado es mayor o si se avanza.
  - `void ReiniciarProgreso()`: Reinicia a 1 al pulsar "Nueva Partida".
  - `bool TienePartidaGuardada()`: Determina si existe progreso previo (para habilitar botón "Continuar").

### B. Flujo del Menú Principal (`MainMenuController.cs`)
- **"Nueva Partida"**:
  - Llama a `GameProgressManager.ReiniciarProgreso()`.
  - Carga la escena `Nivel 1 - Cochabamba`.
- **"Continuar Partida"**:
  - Consulta `GameProgressManager.ObtenerNivelGuardado()`.
  - Carga directamente la escena del nivel correspondiente (1, 2 o 3).
  - Si no hay guardado, arranca en Nivel 1.
- **Estado Visual**: Si no hay partida previa iniciada, el botón "Continuar" se muestra deshabilitado o redirige a Nivel 1.

### C. Transición y Fin de Nivel (`LevelManager.cs` y `LevelScreenHUD.cs`)
- En `LevelManager`:
  - Al ganar el nivel con $\ge 1$ estrella (`NivelSuperado`):
    - Guarda automáticamente el avance al siguiente nivel (`nivelActual + 1`) mediante `GameProgressManager`.
- En `LevelScreenHUD`:
  - Si es victoria y estamos en Nivel 1 o 2: Mostrar botón **"Siguiente Nivel"** que carga la siguiente escena.
  - Si es victoria en Nivel 3 (Santa Cruz): Mostrar mensaje especial: **"¡MAESTRO DE LA COCINA BOLIVIANA! Has completado todos los departamentos"** y botón para volver al menú.

---

## 3. Especificación de Platos, Recetas e Ingredientes

Actualmente los platos tenían sólo 1 o 2 ingredientes para pruebas rápidas. Se actualizarán a recetas ricas y auténticas utilizando los prefabs y mecánicas ya implementadas (corte, cocción por hervor/fritura/asado y crudos):

| Nivel / Departamento | Plato | Ingredientes y Estados de Preparación | Puntos | Tiempo |
| :--- | :--- | :--- | :---: | :---: |
| **Nivel 1: Cochabamba** | **Silpancho** | 1. **Carne** (Cubitos, Frita)<br>2. **Arroz** (Entero, Hervido)<br>3. **Papa** (Rodajas, Frita)<br>4. **Huevo** (Entero, Frito)<br>5. **Tomate** (Cubitos, Crudo)<br>6. **Cebolla** (Cubitos, Crudo) | 60 pts | 90s |
| | **Pique Macho** | 1. **Carne** (Cubitos, Frita)<br>2. **Chorizo** (Rodajas, Frito)<br>3. **Papa** (Rodajas/Bastones, Frita)<br>4. **Huevo** (Entero, Hervido)<br>5. **Tomate** (Rodajas, Crudo)<br>6. **Cebolla** (Rodajas, Crudo) | 65 pts | 95s |
| **Nivel 2: La Paz** | **Plato Paceño** | 1. **Papa** (Entera/Rodajas, Hervida)<br>2. **Haba** (Entera, Hervida)<br>3. **Queso** (Entero, Frito)<br>4. **Carne** (Cubitos, Frita) *(opcional tradicional carne asada/frita)* | 50 pts | 80s |
| **Nivel 3: Santa Cruz** | **Majadito** | 1. **Arroz** (Entero, Hervido)<br>2. **Carne** (Cubitos, Frita)<br>3. **Huevo** (Entero, Frito)<br>4. **Tomate** (Cubitos, Crudo)<br>5. **Cebolla** (Cubitos, Crudo) | 55 pts | 85s |
| | **Sonso** | 1. **SonsoCrudo** (Entero, Asado a la Parrilla)<br>2. **Queso** (Entero, Frito) | 35 pts | 50s |

### Bebidas por Nivel (Dispensador)
- **Nivel 1 (Cochabamba)**: Mocochinchi (`Moccochinchi.asset`).
- **Nivel 2 (La Paz)**: Mocochinchi (`Moccochinchi.asset`).
- **Nivel 3 (Santa Cruz)**: Somó (`Zomo.asset`).

---

## 4. Configuración de Escenas e Identidad Departamental

A partir de `First Scene.unity`, se crearán las 3 escenas en `Assets/00_Scenes/`:
1. `Assets/00_Scenes/Nivel 1 - Cochabamba.unity`:
   - `LevelManager`: Vinculado a `Nivel1_Cochabamba.asset`.
   - Banderas de pared (`WallFlag`): Muestran `BanderaCbba.jpg`.
   - Dispensadores: Configurados con Papa, Arroz, Carne, Huevo, Tomate, Cebolla, Chorizo.
   - Refresco: Mocochinchi.
2. `Assets/00_Scenes/Nivel 2 - La Paz.unity`:
   - `LevelManager`: Vinculado a `Nivel2_LaPaz.asset`.
   - Banderas de pared: Muestran `BanderaLapaz.jpg`.
   - Dispensadores: Configurados con Papa, Haba, Queso, Carne.
   - Refresco: Mocochinchi.
3. `Assets/00_Scenes/Nivel 3 - Santa Cruz.unity`:
   - `LevelManager`: Vinculado a `Nivel3_SantaCruz.asset`.
   - Banderas de pared: Muestran `banderaSantaCruz.jpg`.
   - Dispensadores: Configurados con Arroz, Carne, Huevo, Tomate, Cebolla, SonsoCrudo, Queso.
   - Refresco: Somó (`Zomo.asset`).
   - Parrilla activa para asar el Sonso.

### Configuración en Build Settings
Las 4 escenas se registrarán formalmente en `EditorBuildSettings.asset`:
- `0`: `Assets/00_Scenes/Main Menu.unity`
- `1`: `Assets/00_Scenes/Nivel 1 - Cochabamba.unity`
- `2`: `Assets/00_Scenes/Nivel 2 - La Paz.unity`
- `3`: `Assets/00_Scenes/Nivel 3 - Santa Cruz.unity`

---

## 5. Entregables Concretos

1. **Nuevo Script de Guardado**: [`GameProgressManager.cs`](file:///c:/Universidad/6to%20semestre/game/CocinaBoliviana/Assets/01_Scripts/GameProgressManager.cs) con almacenamiento seguro en `PlayerPrefs`.
2. **Actualización de Scripts de Flujo**:
   - [`MainMenuController.cs`](file:///c:/Universidad/6to%20semestre/game/CocinaBoliviana/Assets/01_Scripts/MainMenuController.cs): Lógica completa para "Nueva Partida" y "Continuar".
   - [`LevelManager.cs`](file:///c:/Universidad/6to%20semestre/game/CocinaBoliviana/Assets/01_Scripts/LevelManager.cs): Guardado automático de progreso al superar el nivel y método para cargar siguiente nivel.
   - [`LevelScreenHUD.cs`](file:///c:/Universidad/6to%20semestre/game/CocinaBoliviana/Assets/01_Scripts/LevelScreenHUD.cs): Botón interactivo "Siguiente Nivel" y pantalla final de campaña.
3. **ScriptableObjects Actualizados en `Assets/03_SO/`**:
   - `Platos/Silpancho.asset`, `Pique.asset`, `PlatoPaceño.asset`, `Majadito.asset`, `Sonso.asset` con sus recetas completas.
   - `Departamentos/Cochabamba.asset`, `La paz.asset`, `SantaCruz.asset` con sus menús y banderas oficiales.
   - `Niveles/Nivel1_Cochabamba.asset`, `Nivel2_LaPaz.asset`, `Nivel3_SantaCruz.asset` con tiempos y objetivos de estrellas calibrados.
4. **3 Escenas Operativas en `Assets/00_Scenes/`**:
   - `Nivel 1 - Cochabamba.unity`
   - `Nivel 2 - La Paz.unity`
   - `Nivel 3 - Santa Cruz.unity`
5. **Herramienta de Automatización en `Assets/Editor/`**:
   - Script de Editor para duplicar y cablear las 3 escenas de forma automática e idempotente sin errores manuales.
6. **Actualización de `EditorBuildSettings.asset`**: Las 4 escenas indexadas en el build del juego.
