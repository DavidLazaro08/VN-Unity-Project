using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Escena de transición cinematográfica reutilizable.
/// Muestra líneas de texto secuenciales con fade in/out sobre fondo negro,
/// opcionalmente con micro-glitch, y salta a la siguiente escena.
/// 
/// Persiste los AudioSources activos para que la música no se interrumpa
/// entre escenas encadenadas.
/// </summary>
public class CinematicTransition : MonoBehaviour
{
    // =========================================================
    //  DATOS CONFIGURABLES
    // =========================================================

    [Serializable]
    public class TransitionLine
    {
        [TextArea(1, 3)]
        public string text = "";

        [Header("Tiempos (segundos)")]
        [Range(0.1f, 3f)] public float fadeInTime  = 0.8f;
        [Range(0.1f, 5f)] public float holdTime    = 1.5f;
        [Range(0.1f, 3f)] public float fadeOutTime  = 0.8f;

        [Header("Efectos")]
        public bool enableGlitch = false;
    }

    [Header("Líneas de texto")]
    [Tooltip("Cada entrada es una línea que aparece y desaparece secuencialmente.")]
    public TransitionLine[] lines = new TransitionLine[]
    {
        new TransitionLine { text = "Horas después.",                                  fadeInTime = 0.8f, holdTime = 1.8f, fadeOutTime = 0.8f, enableGlitch = false },
        new TransitionLine { text = "La ciudad no duerme. Solo cambia de máscara.",    fadeInTime = 0.8f, holdTime = 2.0f, fadeOutTime = 0.8f, enableGlitch = true  },
    };

    [Header("Pausa entre líneas (s)")]
    [Range(0.1f, 3f)]
    public float pauseBetweenLines = 0.6f;

    [Header("Navegación")]
    [Tooltip("Escena Unity a cargar cuando terminen todas las líneas.")]
    public string nextSceneName = "Scene_Game_Intercept";

    [Header("Música")]
    [Tooltip("Si es true, los AudioSources activos se mantienen vivos al cambiar de escena.")]
    public bool persistMusic = true;

    [Header("Glitch")]
    [Tooltip("Intensidad máxima del micro-shake en píxeles.")]
    [Range(0.5f, 10f)]
    public float glitchIntensity = 2f;

    [Tooltip("Velocidad del glitch (ciclos por segundo).")]
    [Range(5f, 60f)]
    public float glitchSpeed = 30f;

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================

    private TextMeshProUGUI _displayText;
    private CanvasGroup _textGroup;
    private RectTransform _textRect;
    private bool _running = false;

    // =========================================================
    //  CICLO DE VIDA
    // =========================================================

    private void Start()
    {
        CreateUI();
        StartCoroutine(RunSequence());
    }

    // =========================================================
    //  UI RUNTIME
    // =========================================================

    private void CreateUI()
    {
        // Canvas Overlay (máxima prioridad)
        GameObject canvasGO = new GameObject("CinematicCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Fondo negro full-screen (por si la cámara no está en negro)
        GameObject bgGO = new GameObject("BlackBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        UnityEngine.UI.Image bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = false;

        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Texto centrado
        GameObject textGO = new GameObject("TransitionText");
        textGO.transform.SetParent(canvasGO.transform, false);

        _textRect = textGO.AddComponent<RectTransform>();
        _textRect.anchorMin = new Vector2(0.1f, 0.3f);
        _textRect.anchorMax = new Vector2(0.9f, 0.7f);
        _textRect.offsetMin = Vector2.zero;
        _textRect.offsetMax = Vector2.zero;

        _displayText = textGO.AddComponent<TextMeshProUGUI>();
        _displayText.text = "";
        _displayText.fontSize = 42;
        _displayText.fontStyle = FontStyles.Italic;
        _displayText.color = new Color(0.85f, 0.85f, 0.85f, 1f); // Blanco ligeramente cálido
        _displayText.alignment = TextAlignmentOptions.Center;
        _displayText.enableWordWrapping = true;

        // CanvasGroup para controlar alpha
        _textGroup = textGO.AddComponent<CanvasGroup>();
        _textGroup.alpha = 0f;
    }

    // =========================================================
    //  SECUENCIA PRINCIPAL
    // =========================================================

    private IEnumerator RunSequence()
    {
        _running = true;

        // Pequeña pausa inicial (dejar que la escena se asiente)
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < lines.Length; i++)
        {
            TransitionLine line = lines[i];
            _displayText.text = line.text;

            // --- FADE IN ---
            yield return StartCoroutine(FadeText(0f, 1f, line.fadeInTime));

            // --- HOLD (con glitch opcional) ---
            if (line.enableGlitch)
            {
                yield return StartCoroutine(HoldWithGlitch(line.holdTime));
            }
            else
            {
                yield return new WaitForSeconds(line.holdTime);
            }

            // --- FADE OUT ---
            yield return StartCoroutine(FadeText(1f, 0f, line.fadeOutTime));

            // Pausa entre líneas (excepto la última)
            if (i < lines.Length - 1)
            {
                yield return new WaitForSeconds(pauseBetweenLines);
            }
        }

        // Pausa final antes de saltar
        yield return new WaitForSeconds(0.5f);

        // Persistir música si está configurado
        if (persistMusic)
        {
            PersistAudioSources();
        }

        _running = false;

        // Cargar siguiente escena
        Debug.Log($"[CinematicTransition] Secuencia completa. Cargando: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    // =========================================================
    //  ANIMACIONES
    // =========================================================

    private IEnumerator FadeText(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            // Curva suave (ease in-out)
            float smooth = k * k * (3f - 2f * k);
            _textGroup.alpha = Mathf.Lerp(from, to, smooth);
            yield return null;
        }
        _textGroup.alpha = to;
    }

    /// <summary>
    /// Mantiene el texto visible aplicando un micro-shake sutil.
    /// El shake es aleatorio y de baja intensidad para dar efecto "glitch".
    /// </summary>
    private IEnumerator HoldWithGlitch(float duration)
    {
        Vector2 originalPos = _textRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Intensidad varía: más fuerte al principio, se calma
            float intensityFactor = Mathf.Lerp(1f, 0.3f, elapsed / duration);
            float currentIntensity = glitchIntensity * intensityFactor;

            // Offset aleatorio con frecuencia controlada
            float ox = Mathf.PerlinNoise(Time.time * glitchSpeed, 0f) * 2f - 1f;
            float oy = Mathf.PerlinNoise(0f, Time.time * glitchSpeed) * 2f - 1f;

            _textRect.anchoredPosition = originalPos + new Vector2(
                ox * currentIntensity,
                oy * currentIntensity * 0.5f  // Menos vertical
            );

            yield return null;
        }

        // Restaurar posición exacta
        _textRect.anchoredPosition = originalPos;
    }

    // =========================================================
    //  MÚSICA — PERSISTENCIA
    // =========================================================

    /// <summary>
    /// Marca todos los AudioSources activos como DontDestroyOnLoad
    /// para que la música continúe en la siguiente escena.
    /// </summary>
    private void PersistAudioSources()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource src in sources)
        {
            if (src.isPlaying)
            {
                // Evitar duplicados si ya está en DontDestroyOnLoad
                if (src.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    DontDestroyOnLoad(src.gameObject);
                    Debug.Log($"[CinematicTransition] AudioSource '{src.gameObject.name}' persistido.");
                }
            }
        }
    }
}
