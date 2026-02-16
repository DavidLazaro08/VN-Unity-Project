using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Controla el cambio dinámico de fondos de vídeo durante el diálogo.
/// </summary>
public class VideoBackgroundController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("VideoPlayer que reproduce el fondo. Si está vacío, busca en este objeto.")]
    public VideoPlayer videoPlayer;

    [Tooltip("Sistema de transiciones para fades. Si está vacío, busca en la escena.")]
    public VNTransition transitionSystem;

    [Header("Configuración")]
    [Tooltip("Carpeta dentro de Resources donde están los vídeos (sin 'Resources/' al inicio)")]
    public string videoResourcePath = "Art/Backgrounds/Video";

    [Header("Fade Settings")]
    [Tooltip("Duración del fade (entrada + salida)")]
    [Range(0.1f, 2f)]
    public float fadeDuration = 0.5f;

    [Tooltip("Activar fades automáticos entre vídeos")]
    public bool autoFadeEnabled = true;

    [Header("Debug")]
    public bool debugLogs = true;

    private string _currentVideoName = "";
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        // Auto-find VideoPlayer si no está asignado
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning("[VideoBackgroundController] No se encontró VideoPlayer. Asigna uno en el Inspector.");
        }
        else if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] VideoPlayer encontrado: {videoPlayer.name}");
        }

        // Auto-find VNTransition si no está asignado
        if (transitionSystem == null)
        {
            transitionSystem = FindObjectOfType<VNTransition>();
            if (transitionSystem != null && debugLogs)
            {
                Debug.Log($"[VideoBackgroundController] VNTransition encontrado: {transitionSystem.name}");
            }
        }
    }

    /// <summary>
    /// Cambia el fondo de vídeo al especificado (sin extensión).
    /// Ejemplo: SetBackground("Scene_Game3_02")
    /// </summary>
    public void SetBackground(string videoName)
    {
        SetBackground(videoName, autoFadeEnabled, autoFadeEnabled);
    }

    /// <summary>
    /// Cambia el fondo con control manual de fade.
    /// </summary>
    public void SetBackground(string videoName, bool fadeOut, bool fadeIn)
    {
        // Sanitizar entrada
        videoName = (videoName ?? "").Trim().Trim('"');

        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] SetBackground llamado: '{videoName}' (FadeOut={fadeOut}, FadeIn={fadeIn})");
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[VideoBackgroundController] No hay VideoPlayer asignado.");
            return;
        }

        if (string.IsNullOrEmpty(videoName))
        {
            Debug.LogWarning("[VideoBackgroundController] Nombre de vídeo vacío.");
            return;
        }

        // Si ya está ese vídeo, no reiniciar
        if (_currentVideoName == videoName)
        {
            if (debugLogs)
            {
                Debug.Log($"[VideoBackgroundController] El vídeo '{videoName}' ya está activo.");
            }
            return;
        }

        // Si hay fade activo, detenerlo
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        // Decidir si usar fade o cambio directo
        if ((fadeOut || fadeIn) && transitionSystem != null)
        {
            _fadeCoroutine = StartCoroutine(SetBackgroundWithFadeRoutine(videoName, fadeOut, fadeIn));
        }
        else
        {
            // Cambio directo sin fade
            ChangeVideoClip(videoName);
        }
    }

    private IEnumerator SetBackgroundWithFadeRoutine(string videoName, bool fadeOut, bool fadeIn)
    {
        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] Iniciando transición con fade a: {videoName}");
        }

        // Verificar que tenemos transitionSystem
        if (transitionSystem == null)
        {
            Debug.LogWarning("[VideoBackgroundController] No hay VNTransition asignado. Cambiando sin fade.");
            ChangeVideoClip(videoName);
            yield break;
        }

        // Esperar un frame para asegurar que VNTransition.Awake() se ejecutó
        yield return null;

        // Verificar que fadeGroup existe (VNTransition lo crea en Awake)
        if (transitionSystem.fadeGroup == null)
        {
            Debug.LogWarning("[VideoBackgroundController] VNTransition.fadeGroup es null. Cambiando sin fade.");
            ChangeVideoClip(videoName);
            yield break;
        }

        // Fade OUT (a negro)
        if (fadeOut)
        {
            yield return StartCoroutine(FadeToBlack());
        }

        // Cambiar vídeo mientras está en negro
        ChangeVideoClip(videoName);

        // Pequeña pausa para asegurar que el vídeo está listo
        yield return new WaitForSeconds(0.1f);

        // Fade IN (desde negro)
        if (fadeIn)
        {
            yield return StartCoroutine(FadeFromBlack());
        }

        _fadeCoroutine = null;
    }

    private IEnumerator FadeToBlack()
    {
        CanvasGroup fadeGroup = transitionSystem.fadeGroup;
        if (fadeGroup == null) yield break;

        fadeGroup.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            fadeGroup.alpha = k;
            yield return null;
        }

        fadeGroup.alpha = 1f;
    }

    private IEnumerator FadeFromBlack()
    {
        CanvasGroup fadeGroup = transitionSystem.fadeGroup;
        if (fadeGroup == null) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            fadeGroup.alpha = 1f - k;
            yield return null;
        }

        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
    }

    private void ChangeVideoClip(string videoName)
    {
        // Cargar vídeo desde Resources
        string fullPath = $"{videoResourcePath}/{videoName}";

        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] Cargando: Resources/{fullPath}");
        }

        VideoClip newClip = Resources.Load<VideoClip>(fullPath);

        if (newClip == null)
        {
            Debug.LogError($"[VideoBackgroundController] ❌ No se encontró el vídeo.\n" +
                          $"  Ruta: Resources/{fullPath}");
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] ✅ VideoClip cargado: {newClip.name}");
        }

        // Cambiar y reproducir
        videoPlayer.Stop();
        videoPlayer.clip = newClip;
        videoPlayer.time = 0;
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;

        _currentVideoName = videoName;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();

        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] ▶️ Reproduciendo: {_currentVideoName}");
        }
    }

    /// <summary>
    /// Resetea el tracking del vídeo actual (útil al cambiar de escena).
    /// </summary>
    public void ResetTracking()
    {
        _currentVideoName = "";
    }
}
