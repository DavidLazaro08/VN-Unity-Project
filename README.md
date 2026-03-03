# VisualNovel2DRV — Novela Visual Cyberpunk

> Proyecto de curso · PMDM · Grado Superior de Programación  
> Motor: **Unity** | Lenguaje: **C#**

---

## 📖 Descripción

**VisualNovel2DRV** es una novela visual 2D ambientada en un universo *cyberpunk* distópico.  
El jugador encarna a **Logan**, un humano sin implantes que navega por un mundo donde la frontera entre lo orgánico y lo digital se ha borrado. Las decisiones del jugador moldean las relaciones con los personajes y determinan el rumbo de la historia.

El objetivo del proyecto era construir una **base técnica sólida y modular** sobre Unity: sistema de diálogos, gestión de escenas, toma de decisiones, sistema de afinidad y preparación para minijuegos integrados.

---

## 🎮 Características principales

| Característica | Descripción |
|---|---|
| **Sistema de diálogos CSV** | Los diálogos se leen desde archivos `.csv` en `Resources/Dialogue/`, sin necesidad de recompilar |
| **Branching narrativo** | Elecciones del jugador (CHOICE) que bifurcan la historia |
| **Sistema de afinidad** | Puntuación relacional por personaje que afecta al desarrollo narrativo |
| **Gestión de sprites L/R** | Personajes en posición izquierda y derecha con sistema de poses por nombre |
| **Transiciones cinemáticas** | Fundidos, texto sobre negro, glitch effect y paso suave entre escenas |
| **Fondos en vídeo y loop** | Fondos animados con `VideoPlayer` y transición cruzada entre clips |
| **Sistema de guardado** | Guarda escena Unity + índice CSV + línea actual con `PlayerPrefs` |
| **Minijuego de cartas** | Battle card game con sistema de colores Rojo/Verde/Azul (preparado) |
| **Minijuego de intercepción** | Secuencia de reconstrucción de datos integrada en el flujo narrativo |

---

## 🗂️ Estructura de escenas

```
Scene_Menu              → Menú principal animado
Scene_Intro             → Vídeo introductorio
Scene_Game              → Escena principal (acto 1)
Scene_Transition_Night  → Transición cinemática "Horas después"
Scene_Game0_5           → Escena de silueta / misterio
Scene_Game_Intercept    → Minijuego de interceptación de datos
Scene_Terraza           → Escena videográfica exterior
Scene_Game1_5           → Puente narrativo
Scene_Game2             → Desarrollo (acto 2)
Scene_Game3             → Clímax — encuentro con True-Fella
Scene_CardBattle        → Minijuego de cartas (integrado)
Scene_Transition_End    → Créditos finales con fade-out de música
```

---

## 🧠 Arquitectura técnica

### Sistema de diálogos (`VNDialogue`)
El corazón de la VN. Dividido en clases `partial`:

| Archivo | Responsabilidad |
|---|---|
| `VNDialogue.cs` | Núcleo: arranque, bucle de líneas, entrada del jugador |
| `VNDialogue.Commands.cs` | Parsing de comandos CSV (`ParseValue`, helpers de slots) |
| `VNDialogue.Jump.cs` | Gestión de saltos entre escenas Unity (`JUMP_UNITY_SCENE`) |
| `VNDialogue.SaveLoad.cs` | Guardado y carga de partida |
| `VNDialogue.Choice.cs` | Elecciones y ramificaciones |
| `VNDialogue.Intercept.cs` | Lógica del minijuego de interceptación |
| `VNDialogue.Fades.cs` | Fundidos de entrada/salida del panel de diálogo |
| `VNDialogue.Typewriter.cs` | Efecto typewriter letra a letra |
| `VNDialogue.SpeakerStyle.cs` | Estilos visuales por personaje |

### Comandos CSV disponibles

```
BG=nombre_bg          → Cambia el fondo (vídeo o imagen)
L=PERSONAJE:pose      → Personaje izquierda
R=PERSONAJE:pose      → Personaje derecha
C=PERSONAJE:pose      → Centro
WAIT=BEAT|CLICK       → Pausa con o sin input
JUMP_UNITY_SCENE=X    → Salta a otra escena Unity
ACT=ID                → Micro-acción del jugador
CHOICE                → Inicio de bloque de elección
AFFINITY=+1:NOMBRE    → Modifica afinidad
RAIN=ON|OFF           → Activa/desactiva lluvia
FADE=1                → Fundido al cambiar fondo
```

### Minijuego de cartas (`CardGame`)
Sistema completo de batalla por turnos:
- **Mecánica de colores**: Rojo → Verde → Azul → Rojo (como piedra-papel-tijera)
- **IA del oponente** con estrategia simple (menor coste / mayor daño)
- **12 cartas** con ilustraciones cyberpunk originales
- Integrado en el flujo narrativo vía `ACT=CARDGAME_START`

---

## 📁 Organización de assets

```
Assets/
├── Resources/
│   ├── Dialogue/       → CSVs de cada escena
│   ├── CardGame/
│   │   ├── Cards/      → ScriptableObjects de cartas (.asset)
│   │   ├── CardArt/    → Ilustraciones de cartas (PNG)
│   │   └── CardFrames/ → Marcos por color (Rojo/Verde/Azul)
│   └── ...
├── Scenes/             → Escenas Unity
├── Scripts/
│   ├── VN/             → Sistema de novela visual
│   ├── CardGame/       → Minijuego de cartas
│   ├── Menu/           → Lógica del menú
│   ├── Intro/          → Control de vídeo introductorio
│   └── GlobalFX/       → Efectos globales (lluvia, zoom, sombras)
└── Prefabs/
    └── CardGame/       → Prefab de carta UI
```

---

## 🚀 Cómo ejecutar

1. Abre el proyecto en **Unity 2022.3 LTS** o superior
2. Carga la escena `Assets/Scenes/Scene_Menu.unity`
3. Pulsa **Play** ▶️

> Para generar los assets de cartas desde el editor:  
> `Card Game → Generar todos los CardData` (menú superior de Unity)

---

## 👥 Equipo

Proyecto desarrollado en equipo como parte del módulo **PMDM** del Grado Superior de Programación.

---

## 📄 Licencia

Proyecto académico — uso interno y evaluación únicamente.