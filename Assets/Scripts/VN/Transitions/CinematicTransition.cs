using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla una transición “cinemática” sencilla: muestra una secuencia de textos sobre fondo negro
/// con fundidos (y un glitch opcional) y, al terminar, carga otra escena.
/// 
/// Opcionalmente puede mantener vivos los AudioSources que estén sonando para que la música
/// no se corte entre escenas encadenadas.
/// </summary>
/// 
public class CinematicTransition : MonoBehaviour
{
    // =========================================================
    //  CONFIGURACIÓN
    // =========================================================

    [Serializable]
    public class TransitionLine
    {
        [TextArea(1, 3)]
        public string text = "";

        [Header("Tiempos (segundos)")]
        [Range(0.1f, 3f)] public float fadeInTime = 0.8f;
        [Range(0.1f, 5f)] public float holdTime = 1.5f;
        [Range(0.1f, 3f)] public float fadeOutTime = 0.8f;

        [Header("Efectos")]
        public bool enableGlitch = false;
    }

    [Header("Líneas de texto")]
    [Tooltip("Cada entrada es una línea que aparece y desaparece secuencialmente.")]
    public TransitionLine[] lines = new TransitionLine[]
    {
        new TransitionLine { text = "Horas después.",                               fadeInTime = 0.8f, holdTime = 1.8f, fadeOutTime = 0.8f, enableGlitch = false },
        new TransitionLine { text = "La ciudad no duerme. Solo cambia de máscara.", fadeInTime = 0.8f, holdTime = 2.0f, fadeOutTime = 0.8f, enableGlitch = true  },
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

    [Tooltip("Duración del fade-out de música antes de cargar la siguiente escena (0 = sin fade).")]
    [Range(0f, 15f)]
    public float musicFadeOutDuration = 0f;

    [Header("Transición de entrada")]
    [Tooltip("Tiempo de fundido desde negro al inicio de la escena (hace el paso de la escena anterior más suave).")]
    [Range(0f, 4f)]
    public float initialFadeInDuration = 2f;

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
        // Limpiamos cualquier canvas de fade que haya quedado de un salto anterior (ej. VNDialogue.Jump)
        GameObject oldJumpCanvas = GameObject.Find("JumpFadeCanvas");
        if (oldJumpCanvas != null)
        {
            Debug.Log("[CinematicTransition] Destruyendo JumpFadeCanvas residual de la escena anterior.");
            Destroy(oldJumpCanvas);
        }

        CreateUI();
        StartCoroutine(RunSequence());
    }

    // =========================================================
    //  UI RUNTIME
    // =========================================================

    private void CreateUI()
    {
        // Canvas Overlay (prioridad alta para que esté por encima del resto)
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

        // CanvasGroup para controlar alpha del texto
        _textGroup = textGO.AddComponent<CanvasGroup>();
        _textGroup.alpha = 0f;
    }

    // =========================================================
    //  SECUENCIA PRINCIPAL
    // =========================================================

    private IEnumerator RunSequence()
    {
        _running = true;

        // Fundido de entrada desde negro (suaviza el paso de la escena anterior)
        if (initialFadeInDuration > 0f)
        {
            // Usamos un panel negro transparente que se desvanece
            yield return StartCoroutine(FadeInFromBlack(initialFadeInDuration));
        }
        else
        {
            // Pequeña pausa inicial (dejar que la escena se asiente)
            yield return new WaitForSeconds(0.3f);
        }

        for (int i = 0; i < lines.Length; i++)
        {
            TransitionLine line = lines[i];
            _displayText.text = line.text;

            // Fade In
            yield return StartCoroutine(FadeText(0f, 1f, line.fadeInTime));

            // Hold (con glitch opcional)
            if (line.enableGlitch)
                yield return StartCoroutine(HoldWithGlitch(line.holdTime));
            else
                yield return new WaitForSeconds(line.holdTime);

            // Fade Out
            yield return StartCoroutine(FadeText(1f, 0f, line.fadeOutTime));

            // Pausa entre líneas (excepto la última)
            if (i < lines.Length - 1)
                yield return new WaitForSeconds(pauseBetweenLines);
        }

        // Pausa final antes de saltar
        yield return new WaitForSeconds(0.5f);

        // Fade-out de música y carga de la siguiente escena
        if (musicFadeOutDuration > 0f)
            yield return StartCoroutine(FadeOutMusicAndLoad());
        else
        {
            if (persistMusic)
                PersistAudioSources();

            _running = false;
            Debug.Log($"[CinematicTransition] Secuencia completa. Cargando: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
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
    /// Mantiene el texto visible aplicando un micro-shake sutil (glitch).
    /// </summary>
    private IEnumerator HoldWithGlitch(float duration)
    {
        Vector2 originalPos = _textRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Intensidad más fuerte al principio, más suave al final
            float intensityFactor = Mathf.Lerp(1f, 0.3f, elapsed / duration);
            float currentIntensity = glitchIntensity * intensityFactor;

            // Offset pseudo-aleatorio con frecuencia controlada
            float ox = Mathf.PerlinNoise(Time.time * glitchSpeed, 0f) * 2f - 1f;
            float oy = Mathf.PerlinNoise(0f, Time.time * glitchSpeed) * 2f - 1f;

            _textRect.anchoredPosition = originalPos + new Vector2(
                ox * currentIntensity,
                oy * currentIntensity * 0.5f // Menos vertical
            );

            yield return null;
        }

        _textRect.anchoredPosition = originalPos;
    }

    // =========================================================
    //  MÚSICA — FADE OUT Y PERSISTENCIA
    // =========================================================

    /// <summary>
    /// Hace fade-out de todos los AudioSources durante musicFadeOutDuration segundos
    /// y luego carga la siguiente escena.
    /// </summary>
    private IEnumerator FadeOutMusicAndLoad()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        float[] startVolumes = new float[sources.Length];

        for (int i = 0; i < sources.Length; i++)
            startVolumes[i] = sources[i].volume;

        float elapsed = 0f;
        while (elapsed < musicFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / musicFadeOutDuration);
            // Curva ease-in: el volumen cae más rápido al final
            float smooth = t * t;

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                    sources[i].volume = startVolumes[i] * smooth;
            }

            yield return null;
        }

        // Silenciar completamente
        foreach (AudioSource src in sources)
            if (src != null) src.Stop();

        _running = false;
        Debug.Log($"[CinematicTransition] Fade-out de música completo. Cargando: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private void PersistAudioSources()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource src in sources)
        {
            if (!src.isPlaying) continue;

            if (src.gameObject.scene.name == "DontDestroyOnLoad") continue;

            DontDestroyOnLoad(src.gameObject);
            Debug.Log($"[CinematicTransition] AudioSource '{src.gameObject.name}' persistido.");
        }
    }

    // =========================================================
    //  FUNDIDO DE ENTRADA DESDE NEGRO
    // =========================================================

    /// <summary>
    /// Crea un panel negro encima de todo y lo desvanece suavemente,
    /// fusionando la llegada a esta escena con el negro de la escena anterior.
    /// </summary>
    private IEnumerator FadeInFromBlack(float duration)
    {
        GameObject fadeGO = new GameObject("_FadeInOverlay");
        Canvas fadeCanvas = fadeGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 200;
        fadeGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        GameObject panelGO = new GameObject("FadePanel");
        panelGO.transform.SetParent(fadeGO.transform, false);
        var img = panelGO.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        var rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        var cg = panelGO.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // Disolver el negro suavemente
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t); // smoothstep
            cg.alpha = 1f - smooth;
            yield return null;
        }

        cg.alpha = 0f;
        Destroy(fadeGO);
    }
}
