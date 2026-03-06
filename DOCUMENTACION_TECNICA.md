# Documentación Técnica — VisualNovel2DRV

**Módulo:** PMDM · Grado Superior de Programación  
**Motor:** Unity 2022.3 LTS · **Lenguaje:** C#  

---

## 1. GDD — Game Design Document

### Resumen del juego

**VisualNovel2DRV** es una novela visual 2D ambientada en un futuro cyberpunk distópico. El jugador encarna a **Logan**, un humano sin implantes tecnológicos que debe obtener información de una facción clandestina para saldar una deuda. Las decisiones del jugador afectan a la relación con los personajes y al desarrollo narrativo.

> **Género:** Novela visual / Narrativa interactiva  
> **Plataforma:** PC (Windows)  
> **Duración estimada:** 15–20 minutos (demo)

### Mecánicas principales

**Avance de diálogo**  
El jugador progresa por la historia pulsando la tecla de acción o haciendo clic. El texto aparece con efecto typewriter. En momentos narrativos clave, el sistema puede requerir una confirmación explícita antes de continuar (`WAIT=CLICK`).

**Sistema de elecciones (CHOICE)**  
En determinados momentos el jugador elige entre dos o más opciones. Cada elección puede ramificar el diálogo y/o modificar la afinidad con un personaje. Las ramas convergen después del bloque CHOICE.

**Sistema de afinidad**  
Cada personaje tiene una puntuación de afinidad interna. Algunas respuestas la aumentan o disminuyen. La afinidad puede condicionar qué contenido narrativo se desbloquea. Se representa visualmente con un popup emergente.

**Minijuego de interceptación**  
Durante la escena `Scene_Game_Intercept`, el jugador participa en una secuencia de reconstrucción de datos. Es un minijuego de tipo puzle narrativo integrado directamente en el flujo de la VN.

**Minijuego de cartas (preparado)**  
Al final del arco narrativo principal, el antagonista propone una partida de cartas. El sistema de batalla está implementado completo (reglas, IA, UI), listo para conectar al flujo principal cuando el contenido narrativo lo requiera.

### Dinámica general

```
Menú → Vídeo Intro → Escena 1 → [Elección] → Escenas intermedias
→ Interceptación → Terraza (vídeo) → Escena clímax → [Cartas] → Créditos finales
```

Cada acto está separado por transiciones cinemáticas con texto sobre fondo negro, música y fundidos. No hay estado de "game over": la historia siempre avanza.

### Diseño de personajes

| Personaje | Rol | Notas |
|---|---|---|
| Logan | Protagonista (jugador) | Sin implantes, sarcástico, buscando salida |
| True-Fella | Antagonista / aliado potencial | Líder de la facción ANTIROBOTS |
| ANTIROBOTS | Facción | Guardianes del refugio subterráneo |

---

## 2. Diagrama de Clases (UML)

### Jerarquía del sistema de diálogo

```
MonoBehaviour
└── VNDialogue  (clase partial, dividida en 9 archivos)
    ├── VNDialogue.Commands.cs    → Parsing de comandos CSV
    ├── VNDialogue.Jump.cs        → Saltos entre Unity Scenes
    ├── VNDialogue.SaveLoad.cs    → Guardado y carga de partida
    ├── VNDialogue.Choice.cs      → Sistema de elecciones
    ├── VNDialogue.Intercept.cs   → Minijuego de interceptación
    ├── VNDialogue.Fades.cs       → Fundidos del panel de diálogo
    ├── VNDialogue.Typewriter.cs  → Efecto typewriter
    └── VNDialogue.SpeakerStyle.cs → Estilos visuales por personaje
```

### Sistema de cartas

```
ScriptableObject
└── CardData              → Datos estáticos de cada carta (nombre, stats, arte)

MonoBehaviour
├── CardBattleManager     → [Singleton] Orquesta el flujo de batalla
│   ├── usa → BattlePlayer (×2: jugador y IA)
│   ├── usa → BattleField  → Gestiona los slots visuales del campo
│   └── usa → CardGameAI   → Lógica de turno automático del oponente
├── BattleSceneController → Inicializa la escena y conecta eventos
├── CardBattleStarter     → [Static] API para lanzar/abandonar la batalla
└── CardUI                → Representación visual de una carta (drag&drop)

[no MonoBehaviour]
├── BattlePlayer          → Estado de un jugador (mano, campo, maná, vida)
├── Card                  → Instancia de juego de una CardData
├── CardDatabase          → [Singleton lazy] Carga y registra todos los CardData
└── CardAttackRules       → [Static] Reglas de color Rojo→Verde→Azul→Rojo
```

### Sistema de transiciones

```
MonoBehaviour
├── CinematicTransition   → Texto sobre negro + fade audio + carga escena
├── VNTransition          → Fundido de entrada/salida en escenas VN
├── VNTransitionFlags     → [Static] Comunicación de flags entre escenas
└── IntroVideoController  → Gestión del vídeo introductorio
```

### Gestión de audio

```
MonoBehaviour
├── MusicTailFader        → Fade-out suave de música al cambiar de escena
└── TextBlipController    → Sonido de mecanografía sincronizado con el texto
```

### Comunicación entre sistemas

- **VNDialogue → CardBattleStarter**: llamada estática vía `ACT=CARDGAME_START`
- **BattlePlayer → BattleSceneController**: eventos C# (`OnCardDrawn`, `OnCardPlayed`, `OnHealthChanged`...)
- **CardBattleManager → BattleUI**: eventos C# (`OnTurnStarted`, `OnBattleWon`, `OnBattleLost`)
- **Escenas → escenas**: `PlayerPrefs` como canal de comunicación entre Unity Scenes independientes (`JUMP_ACTIVE`, `VN_SAVE_UNITY_SCENE`, etc.)

---

## 3. Memoria de Arquitectura

### Patrón Singleton

Usado en dos sistemas con estado global compartido:

**`CardDatabase`** — Registro central de todas las cartas del juego. Se instancia de forma lazy (la primera vez que alguien accede a `CardDatabase.Instance`) y persiste durante la batalla. Garantiza que todos los sistemas manejen las mismas referencias de `CardData`.

**`CardBattleManager`** — Punto de acceso único al estado de la batalla en curso. La `CardGameAI` y la `CardUI` acceden a él sin necesidad de referencias directas en el Inspector, lo que simplifica el wiring de la escena.

### ScriptableObjects

Las cartas del juego se definen como `CardData : ScriptableObject`. Esto permite:
- Crear, modificar y previsualizar cartas directamente desde el Editor de Unity sin compilar
- Separar los datos estáticos (stats, arte, descripción) de la lógica de juego (`Card`, que es la instancia en partida)
- Referenciarlas desde `CardDatabase` vía `Resources.LoadAll<CardData>()`

Es el patrón recomendado por Unity para datos de configuración reutilizables.

### Clases Partial

`VNDialogue` es el sistema más complejo del proyecto. Para hacerlo mantenible, se divide en **9 archivos `partial`** que comparten el mismo tipo en C#. Cada archivo tiene una responsabilidad única (guardado, saltos, elecciones, typewriter...). Esto evita un fichero monolítico de miles de líneas y permite que varias personas trabajen sobre aspectos distintos sin conflictos de git.

### Patrón Comando (tabla CSV)

Los archivos `.csv` de diálogo actúan como una secuencia de comandos que `VNDialogue` interpreta línea a línea. El campo `cmd` de cada línea puede contener instrucciones como `BG=fondo`, `L=LOGAN:pose`, `JUMP_UNITY_SCENE=Scene_X`, `ACT=CARDGAME_START`... Esto desacopla completamente el **contenido narrativo** de la **lógica de programación**: un guionista puede editar los CSV sin tocar nunca el código.

### Sistema de eventos (Observer)

Los sistemas de UI del minijuego de cartas se suscriben a eventos C# estándar (`Action<T>`) expuestos por `BattlePlayer` y `CardBattleManager`. Por ejemplo, cuando una carta es destruida se dispara `OnCardDestroyed`, y `BattleUI` y `BattleSceneController` actualizan su parte visual de forma independiente. Esto elimina llamadas directas entre sistemas y facilita la extensión futura.

### Comunicación entre escenas con PlayerPrefs

Unity destruye todos los objetos al cambiar de escena. Para pasar estado entre escenas independientes (escena de VN → escena de batalla → vuelta a VN), el proyecto usa `PlayerPrefs` como canal ligero:
- `JUMP_ACTIVE`, `JUMP_SCENE_INDEX`, `JUMP_TARGET_LINE` — para los saltos narrativos
- `VN_SAVE_UNITY_SCENE`, `VN_SAVE_SCENE`, `VN_SAVE_LINE` — para el sistema de guardado

Es una solución pragmática apropiada para la escala del proyecto.

---

## 4. Decisiones de diseño relevantes

**¿Por qué no se usó herencia profunda en los scripts de VN?**  
Los sistemas de la VN son muy específicos de contexto (cada escena tiene condiciones distintas). La herencia habría creado jerarquías frágiles. Se prefirió la composición mediante la división en archivos `partial` y el uso de componentes Unity independientes.

**¿Por qué los diálogos en CSV y no en ScriptableObjects?**  
Los CSV son editables con cualquier herramienta (Excel, Sheets, un editor de texto). Permiten iterar sobre el guion sin abrir Unity. Para una VN donde el contenido cambia continuamente durante el desarrollo, esta flexibilidad vale más que la seguridad de tipos de un ScriptableObject.

**¿Por qué no se usó Object Pooling?**  
La escala del proyecto no lo justifica. Las cartas se instancian al inicio de la batalla (máximo ~14 objetos) y se destruyen al terminar. El coste de instanciación es despreciable en ese contexto.

---

> *Documentación generada a partir del código fuente del proyecto VisualNovel2DRV.*
