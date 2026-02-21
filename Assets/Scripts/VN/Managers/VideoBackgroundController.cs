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
    private bool _isPrepared = false; // Flag para sincronización

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
            
            // Asegurarnos de limpiar eventos al inicio
            videoPlayer.prepareCompleted -= OnVideoPrepared;
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
        SetBackground(videoName, autoFadeEnabled, autoFadeEnabled, null);
    }

    /// <summary>
    /// Cambia el fondo con control manual de fade y opción de cambiar lluvia sincronizada.
    /// </summary>
    public void SetBackground(string videoName, bool fadeOut, bool fadeIn, bool? rainState = null)
    {
        // Sanitizar entrada
        videoName = (videoName ?? "").Trim().Trim('"');

        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] SetBackground llamado: '{videoName}' (FadeOut={fadeOut}, FadeIn={fadeIn}, Rain={rainState})");
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[VideoBackgroundController] No hay VideoPlayer asignado.");
            return;
        }

        if (string.IsNullOrEmpty(videoName))
        {
            // Si solo queremos cambiar lluvia sin cambiar vídeo (? - usualmente no pasa aquí, pero por robustez)
            if (rainState.HasValue) SetRain(rainState.Value);
            Debug.LogWarning("[VideoBackgroundController] Nombre de vídeo vacío.");
            return;
        }

        // Si ya está ese vídeo, no reiniciar, PERO si hay cambio de lluvia, aplicarlo
        if (_currentVideoName == videoName)
        {
            if (rainState.HasValue) SetRain(rainState.Value);
            
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
            _fadeCoroutine = StartCoroutine(SetBackgroundWithFadeRoutine(videoName, fadeOut, fadeIn, rainState));
        }
        else
        {
            // Cambio directo sin fade
            if (rainState.HasValue) SetRain(rainState.Value);
            ChangeVideoClip(videoName);
        }
    }

    private IEnumerator SetBackgroundWithFadeRoutine(string videoName, bool fadeOut, bool fadeIn, bool? rainState)
    {
        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] Iniciando transición con fade a: {videoName}");
        }

        // Verificar que tenemos transitionSystem
        if (transitionSystem == null)
        {
            Debug.LogWarning("[VideoBackgroundController] No hay VNTransition asignado. Cambiando sin fade.");
            if (rainState.HasValue) SetRain(rainState.Value);
            ChangeVideoClip(videoName);
            yield break;
        }

        // Esperar un frame para asegurar que VNTransition.Awake() se ejecutó
        yield return null;

        // Verificar que fadeGroup existe (VNTransition lo crea en Awake)
        if (transitionSystem.fadeGroup == null)
        {
            Debug.LogWarning("[VideoBackgroundController] VNTransition.fadeGroup es null. Cambiando sin fade.");
            if (rainState.HasValue) SetRain(rainState.Value);
            ChangeVideoClip(videoName);
            yield break;
        }

        // NUEVO: Cancelar cualquier fade inicial de VNTransition para que NO compita con nosotros
        transitionSystem.CancelInitialFade();

        // Fade OUT (a negro)
        // OPTIMIZACIÓN: Si ya estamos en negro (alpha ~1), no hace falta fade out
        if (fadeOut && transitionSystem.fadeGroup.alpha < 0.95f)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        else if (fadeOut)
        {
            // Aseguramos que bloquee raycasts aunque no hagamos la rutina
            transitionSystem.fadeGroup.alpha = 1f;
            transitionSystem.fadeGroup.blocksRaycasts = true;
        }

        // --- PANTALLA NEGRA ---
        
        // 1. Aplicar cambio de lluvia (así no se ve el corte)
        if (rainState.HasValue)
        {
            SetRain(rainState.Value);
        }

        // 2. Cambiar vídeo
        ChangeVideoClip(videoName);

        // 3. Esperar a que el vídeo esté PREPARADO (evita parpadeo)
        // Usamos un timeout de 2 segundos por seguridad
        float timeout = 2.0f;
        while (!_isPrepared && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (timeout <= 0)
        {
            Debug.LogWarning($"[VideoBackgroundController] Tiempo de espera agostado para cargar vídeo: {videoName}");
        }

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

        float startAlpha = fadeGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            // LERPEAR desde el alpha actual para evitar parpadeos
            fadeGroup.alpha = Mathf.Lerp(startAlpha, 1f, k);
            yield return null;
        }

        fadeGroup.alpha = 1f;
    }

    private IEnumerator FadeFromBlack()
    {
        CanvasGroup fadeGroup = transitionSystem.fadeGroup;
        if (fadeGroup == null) yield break;

        float startAlpha = fadeGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            // LERPEAR desde el alpha actual hacia transparente
            fadeGroup.alpha = Mathf.Lerp(startAlpha, 0f, k);
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

        // Marcar como NO preparado
        _isPrepared = false;

        // Cambiar y reproducir
        videoPlayer.Stop();
        videoPlayer.clip = newClip;
        videoPlayer.time = 0;
        
        // Aseguramos que nos suscribimos solo una vez
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        
        videoPlayer.Prepare();

        _currentVideoName = videoName;
    }

    /// <summary>
    /// Activa o desactiva el efecto de lluvia.
    /// </summary>
    public void SetRain(bool active)
    {
        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] SetRain llamado: {active}");
        }

        GameObject existingRain = GameObject.Find("lluviaFx");

        if (active)
        {
            if (existingRain == null)
            {
                // Instanciar
                GameObject prefab = Resources.Load<GameObject>("Efectoscine/lluviaFx");
                if (prefab != null)
                {
                    existingRain = Instantiate(prefab);
                    existingRain.name = "lluviaFx";
                    // Posición estándar usada en RainEffectSetup
                    existingRain.transform.position = new Vector3(0f, 12f, 5f);
                    
                    int fxLayer = LayerMask.NameToLayer("FX");
                    if (fxLayer != -1) SetLayerRecursively(existingRain, fxLayer);
                    
                    if (debugLogs) Debug.Log("[VideoBackgroundController] 🌧️ Lluvia activada (Instanciada).");
                }
                else
                {
                    Debug.LogError("[VideoBackgroundController] ❌ No se encontró prefab 'Resources/Efectoscine/lluviaFx'");
                }
            }
            else
            {
                if (debugLogs) Debug.Log("[VideoBackgroundController] ℹ️ Lluvia ya estaba activa.");
            }
        }
        else
        {
            if (existingRain != null)
            {
                Destroy(existingRain);
                if (debugLogs) Debug.Log("[VideoBackgroundController] ☀️ Lluvia desactivada (Destruida).");
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // Desuscribir
        vp.prepareCompleted -= OnVideoPrepared;
        
        vp.Play();
        _isPrepared = true; // Marcar como listo

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
