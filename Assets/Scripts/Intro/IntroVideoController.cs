using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// IntroVideoController
/// ------------------------------------------------------------
/// Lleva la intro en vídeo:
/// - reproduce el vídeo
/// - te deja saltarlo (tecla o click)
/// - al acabar (o al saltar) hace fundido a negro y carga la siguiente escena
///
/// Lleva varios “planes B” para detectar el final del vídeo, porque a veces
/// parece que Unity no se porta igual en todos los PCs/codecs.
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
        // Limpiamos cualquier canvas de fade que haya quedado de un salto anterior (ej. VNDialogue.Jump)
        GameObject oldJumpCanvas = GameObject.Find("JumpFadeCanvas");
        if (oldJumpCanvas != null)
        {
            Debug.Log("[IntroVideo] Destruyendo JumpFadeCanvas residual de la escena anterior.");
            Destroy(oldJumpCanvas);
        }

        // Buscar VideoPlayer si no está asignado
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();

            if (videoPlayer == null)
            {
                videoPlayer = FindObjectOfType<VideoPlayer>();
            }

            if (videoPlayer == null)
            {
                Debug.LogError("[IntroVideo] No encuentro ningún VideoPlayer. Cargo la siguiente escena.");
                SceneManager.LoadScene(nextSceneName);
                return;
            }
        }

        // Crear sistema de fade (negro por encima de todo)
        CreateFadeSystem();

        // Evento de “se acabó el vídeo”
        videoPlayer.loopPointReached += OnVideoFinished;

        // Reproducir
        videoPlayer.Play();
        _videoStarted = true;

        // Duración por si necesitamos el plan B del tiempo
        _videoDuration = videoPlayer.length;
        Debug.Log($"[IntroVideo] Empezó el vídeo ({_videoDuration:0.0}s). Pulsa una tecla o click para saltar.");
    }

    void Update()
    {
        if (_isTransitioning) return;

        _elapsedTime += Time.deltaTime;

        // Input para saltar intro (NEW INPUT SYSTEM)
        if (allowSkip && _elapsedTime >= minTimeBeforeSkip)
        {
            bool skipInput = false;

            // Teclado
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                skipInput = true;
            }

            // Ratón
            if (!skipInput && Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame ||
                    Mouse.current.middleButton.wasPressedThisFrame)
                {
                    skipInput = true;
                }
            }

            // Gamepad (lo básico)
            if (!skipInput && Gamepad.current != null && Gamepad.current.allControls.Count > 0)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)
                {
                    skipInput = true;
                }
            }

            if (skipInput)
            {
                Debug.Log("[IntroVideo] Saltado por el usuario.");
                SkipIntro();
                return;
            }
        }

        // Detectar fin de vídeo (varios métodos por seguridad)
        if (_videoStarted && videoPlayer != null)
        {
            bool videoStopped = !videoPlayer.isPlaying && _elapsedTime > 1f;

            bool nearEnd = videoPlayer.frameCount > 0 &&
                           videoPlayer.frame >= (long)(videoPlayer.frameCount - 5);

            bool timeNearEnd = videoPlayer.length > 0 &&
                               videoPlayer.time >= (videoPlayer.length - 0.1);

            bool hardTimeLimit = _videoDuration > 0 && _elapsedTime > (_videoDuration + 0.5f);

            if (videoStopped || nearEnd || timeNearEnd || hardTimeLimit)
            {
                Debug.Log("[IntroVideo] Terminó el vídeo. Paso a la siguiente escena.");
                StartTransition();
            }
        }
    }

    /// <summary>
    /// Crea el sistema de fade out (canvas overlay con panel negro).
    /// </summary>
    private void CreateFadeSystem()
    {
        _fadeCanvasGO = new GameObject("IntroFadeCanvas");

        Canvas canvas = _fadeCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        _fadeCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        _fadeCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Panel negro full-screen
        GameObject panelGO = new GameObject("FadePanel");
        panelGO.transform.SetParent(_fadeCanvasGO.transform, false);

        UnityEngine.UI.Image img = panelGO.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        RectTransform rt = panelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        _fadeGroup = panelGO.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Callback cuando el video termina de reproducirse.
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[IntroVideo] Terminó (evento).");
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
        StartCoroutine(FadeOutAndLoad());
    }

    /// <summary>
    /// Apaga la cámara de FX (lluvia), para que no se vea por encima del negro.
    /// </summary>
    private void DisableFXCamera()
    {
        GameObject fxCam = GameObject.Find("FX_Camera");
        if (fxCam != null)
        {
            fxCam.SetActive(false);
            Debug.Log("[IntroVideo] FX_Camera OFF.");
        }
    }

    /// <summary>
    /// Corrutina que hace fade out a negro y carga la siguiente escena.
    /// </summary>
    private IEnumerator FadeOutAndLoad()
    {
        _isTransitioning = true;

        if (_fadeGroup != null)
        {
            _fadeGroup.blocksRaycasts = true;
        }

        float startVolume = 0f;
        if (fadeOutAudio && videoPlayer != null)
        {
            startVolume = videoPlayer.GetDirectAudioVolume(0);
        }

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);

            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = k;
            }

            if (fadeOutAudio && videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.SetDirectAudioVolume(0, Mathf.Lerp(startVolume, 0f, k));
            }

            yield return null;
        }

        if (_fadeGroup != null)
        {
            _fadeGroup.alpha = 1f;
        }

        DisableFXCamera();

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log($"[IntroVideo] Cargando: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        if (_fadeCanvasGO != null)
        {
            Destroy(_fadeCanvasGO);
        }
    }
}