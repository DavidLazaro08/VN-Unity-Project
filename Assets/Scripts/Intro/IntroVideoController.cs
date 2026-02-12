using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador para la escena de introducción con video.
/// Reproduce el video de intro, permite saltarlo con cualquier tecla o click,
/// y transiciona automáticamente a Scene_Game al terminar con fade out a negro.
/// </summary>
public class IntroVideoController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Componente Video Player (se busca automáticamente si no se asigna)")]
    public VideoPlayer videoPlayer;

    [Header("Configuración")]
    [Tooltip("Nombre de la escena a cargar después del video")]
    public string nextSceneName = "Scene_Game";

    [Tooltip("Permitir saltar el video con cualquier tecla o click")]
    public bool allowSkip = true;

    [Tooltip("Tiempo mínimo antes de permitir saltar (segundos)")]
    [Range(0f, 5f)]
    public float minTimeBeforeSkip = 0.5f;

    [Header("Transición")]
    [Tooltip("Duración del fade out a negro (segundos)")]
    [Range(0.1f, 3f)]
    public float fadeOutDuration = 1f;

    [Tooltip("Fade out del audio del video junto con la imagen")]
    public bool fadeOutAudio = true;

    private bool _videoStarted = false;
    private bool _isTransitioning = false;
    private float _elapsedTime = 0f;

    // Sistema de fade
    private GameObject _fadeCanvasGO;
    private CanvasGroup _fadeGroup;

    // Temporizador de seguridad
    private double _videoDuration;

    void Start()
    {
        // ... (código anterior de búsqueda de videoPlayer) ...
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            
            if (videoPlayer == null)
            {
                videoPlayer = FindObjectOfType<VideoPlayer>();
            }

            if (videoPlayer == null)
            {
                Debug.LogError("[IntroVideoController] No se encontró VideoPlayer. Asigna uno en el Inspector.");
                SceneManager.LoadScene(nextSceneName);
                return;
            }
        }

        // Crear sistema de fade
        CreateFadeSystem();

        // Configurar eventos del video
        videoPlayer.loopPointReached += OnVideoFinished;

        // Iniciar reproducción
        videoPlayer.Play();
        _videoStarted = true;

        // Guardar duración para backup
        _videoDuration = videoPlayer.length;
        Debug.Log($"[IntroVideoController] Video iniciado. Duración: {_videoDuration}s. Presiona cualquier tecla o click para saltar.");
    }

    void Update()
    {
        if (_isTransitioning) return;

        _elapsedTime += Time.deltaTime;

        // Detectar input para saltar (teclado o mouse) - NEW INPUT SYSTEM
        if (allowSkip && _elapsedTime >= minTimeBeforeSkip)
        {
            bool skipInput = false;

            // Check Keyboard
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                skipInput = true;
            }

            // Check Mouse
            if (!skipInput && Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame || 
                    Mouse.current.rightButton.wasPressedThisFrame || 
                    Mouse.current.middleButton.wasPressedThisFrame)
                {
                    skipInput = true;
                }
            }

            // Optional: Check Gamepad
            if (!skipInput && Gamepad.current != null && Gamepad.current.allControls.Count > 0)
            {
                 // Checking all gamepad buttons is complex, usually just check specific ones
                 if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)
                 {
                     skipInput = true;
                 }
            }

            if (skipInput)
            {
                Debug.Log("[IntroVideoController] Video saltado por el usuario.");
                SkipIntro();
                return;
            }
        }

        // Verificar si el video terminó (múltiples métodos de detección + TEMPORIZADOR DE SEGURIDAD)
        if (_videoStarted && videoPlayer != null)
        {
            // Método 1: Video no está reproduciendo y ya pasó tiempo suficiente
            bool videoStopped = !videoPlayer.isPlaying && _elapsedTime > 1f;
            
            // Método 2: Frame actual está cerca del final
            bool nearEnd = videoPlayer.frameCount > 0 && 
                          videoPlayer.frame >= (long)(videoPlayer.frameCount - 5);
            
            // Método 3: Tiempo actual está cerca de la duración total
            bool timeNearEnd = videoPlayer.length > 0 && 
                              videoPlayer.time >= (videoPlayer.length - 0.1); // Margen más pequeño

            // Método 4: Temporizador de seguridad (videoDuration + 0.5s margen)
            bool hardTimeLimit = _videoDuration > 0 && _elapsedTime > (_videoDuration + 0.5f);

            if (videoStopped || nearEnd || timeNearEnd || hardTimeLimit)
            {
                Debug.Log($"[IntroVideoController] Video terminado. Trigger: Stopped={videoStopped}, NearEnd={nearEnd}, Time={timeNearEnd}, HardLimit={hardTimeLimit}");
                StartTransition();
            }
        }
    }

    /// <summary>
    /// Crea el sistema de fade out (canvas overlay con panel negro).
    /// </summary>
    private void CreateFadeSystem()
    {
        // Canvas global en Overlay con prioridad máxima
        _fadeCanvasGO = new GameObject("IntroFadeCanvas");
        
        Canvas canvas = _fadeCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // Máxima prioridad posible (Short.MaxValue)

        _fadeCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        _fadeCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Panel negro full-screen
        GameObject panelGO = new GameObject("FadePanel");
        panelGO.transform.SetParent(_fadeCanvasGO.transform, false);

        UnityEngine.UI.Image img = panelGO.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        img.raycastTarget = false; // No bloquear clicks durante el video

        RectTransform rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        _fadeGroup = panelGO.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f; // Transparente al inicio
        _fadeGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Callback cuando el video termina de reproducirse.
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[IntroVideoController] Video terminado (evento loopPointReached).");
        StartTransition();
    }

    /// <summary>
    /// Salta la introducción y comienza la transición inmediatamente.
    /// </summary>
    public void SkipIntro()
    {
        if (_isTransitioning) return;

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        StartTransition();
    }

    /// <summary>
    /// Inicia la transición con fade out a negro.
    /// </summary>
    private void StartTransition()
    {
        if (_isTransitioning) return;
        
        // Apagar lluvia inmediatamente
        DisableFXCamera();

        StartCoroutine(FadeOutAndLoad());
    }

    /// <summary>
    /// Busca y desactiva la cámara de FX (lluvia) para asegurar que no se vea sobre el negro.
    /// </summary>
    private void DisableFXCamera()
    {
        GameObject fxCam = GameObject.Find("FXCamera");
        if (fxCam != null)
        {
            fxCam.SetActive(false);
        }
    }

    /// <summary>
    /// Corrutina que hace fade out a negro y carga la siguiente escena.
    /// </summary>
    private IEnumerator FadeOutAndLoad()
    {
        _isTransitioning = true;

        // Bloquear interacción durante el fade
        if (_fadeGroup != null)
        {
            _fadeGroup.blocksRaycasts = true;
        }

        // Obtener volumen inicial del video
        float startVolume = 0f;
        if (fadeOutAudio && videoPlayer != null)
        {
            startVolume = videoPlayer.GetDirectAudioVolume(0);
        }

        // FADE OUT a negro
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);

            // Fade visual
            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = k;
            }

            // Fade audio del video
            if (fadeOutAudio && videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.SetDirectAudioVolume(0, Mathf.Lerp(startVolume, 0f, k));
            }

            yield return null;
        }

        // Asegurar fade completo
        if (_fadeGroup != null)
        {
            _fadeGroup.alpha = 1f;
        }

        // Detener video
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // Pequeña pausa para suavizar la transición
        yield return new WaitForSeconds(0.2f);

        // Cargar siguiente escena
        Debug.Log($"[IntroVideoController] Cargando escena: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        // Limpiar eventos
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        // Limpiar fade canvas
        if (_fadeCanvasGO != null)
        {
            Destroy(_fadeCanvasGO);
        }
    }
}
