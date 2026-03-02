using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// Controla el cambio dinámico de fondos de vídeo durante el diálogo.
/// - Carga clips desde Resources.
/// - Opcionalmente hace fade a negro usando VNTransition.
/// - Espera a que el vídeo esté "prepared" para evitar parpadeos.
/// - Puede activar/desactivar lluvia durante el corte a negro.
/// </summary>
public class VideoBackgroundController : MonoBehaviour
{
    // =========================================================
    //  REFERENCIAS
    // =========================================================

    [Header("Referencias")]
    [Tooltip("VideoPlayer que reproduce el fondo. Si está vacío, busca en este objeto.")]
    public VideoPlayer videoPlayer;

    [Tooltip("Sistema de transiciones para fades. Si está vacío, busca en la escena.")]
    public VNTransition transitionSystem;

    // =========================================================
    //  CONFIGURACIÓN
    // =========================================================

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

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================

    private string _currentVideoName = "";
    private Coroutine _fadeCoroutine;

    // Flag para sincronización: se pone a true cuando el VideoPlayer termina Prepare()
    private bool _isPrepared = false;

    // =========================================================
    //  CICLO DE VIDA
    // =========================================================

    private void Awake()
    {
        // 1) Auto-find VideoPlayer si no está asignado
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogWarning("[VideoBackgroundController] No se encontró VideoPlayer. Asigna uno en el Inspector.");
        }
        else
        {
            if (debugLogs)
                Debug.Log($"[VideoBackgroundController] VideoPlayer encontrado: {videoPlayer.name}");

            // Por si acaso venimos de hot-reloads o reinicios raros, limpiamos eventos
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }

        // 2) Auto-find VNTransition si no está asignado
        if (transitionSystem == null)
        {
            transitionSystem = FindObjectOfType<VNTransition>();
            if (transitionSystem != null && debugLogs)
                Debug.Log($"[VideoBackgroundController] VNTransition encontrado: {transitionSystem.name}");
        }
    }

    // =========================================================
    //  API PÚBLICA
    // =========================================================

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
        // Sanitizar entrada (por si viene con comillas del CSV)
        videoName = (videoName ?? "").Trim().Trim('"');

        if (debugLogs)
        {
            Debug.Log($"[VideoBackgroundController] SetBackground: '{videoName}' (FadeOut={fadeOut}, FadeIn={fadeIn}, Rain={rainState})");
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[VideoBackgroundController] No hay VideoPlayer asignado.");
            return;
        }

        // Caso: nombre vacío. Normalmente no pasará, pero mejor robusto.
        if (string.IsNullOrEmpty(videoName))
        {
            if (rainState.HasValue) SetRain(rainState.Value);
            Debug.LogWarning("[VideoBackgroundController] Nombre de vídeo vacío.");
            return;
        }

        // Si ya está ese vídeo, no lo reiniciamos.
        // Pero si nos piden lluvia on/off, eso sí lo aplicamos.
        if (_currentVideoName == videoName)
        {
            if (rainState.HasValue) SetRain(rainState.Value);

            if (debugLogs)
                Debug.Log($"[VideoBackgroundController] El vídeo '{videoName}' ya está activo.");

            return;
        }

        // Si hay una transición en curso, la paramos para que no se solape
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        // Si hay fade y existe VNTransition, usamos rutina
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

    // =========================================================
    //  RUTINA CON FADE
    // =========================================================

    private IEnumerator SetBackgroundWithFadeRoutine(string videoName, bool fadeOut, bool fadeIn, bool? rainState)
    {
        if (debugLogs)
            Debug.Log($"[VideoBackgroundController] Transición con fade a: {videoName}");

        if (transitionSystem == null)
        {
            Debug.LogWarning("[VideoBackgroundController] No hay VNTransition. Cambiando sin fade.");
            if (rainState.HasValue) SetRain(rainState.Value);
            ChangeVideoClip(videoName);
            yield break;
        }

        // Esperar un frame para asegurar Awake() de VNTransition
        yield return null;

        if (transitionSystem.fadeGroup == null)
        {
            Debug.LogWarning("[VideoBackgroundController] VNTransition.fadeGroup es null. Cambiando sin fade.");
            if (rainState.HasValue) SetRain(rainState.Value);
            ChangeVideoClip(videoName);
            yield break;
        }

        // Cancelar el fade-in inicial del VNTransition para que no compita con nosotros
        transitionSystem.CancelInitialFade();

        // -------------------------
        // FADE OUT (a negro)
        // -------------------------
        if (fadeOut && transitionSystem.fadeGroup.alpha < 0.95f)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        else if (fadeOut)
        {
            // Si ya estamos prácticamente en negro, lo dejamos clavado
            transitionSystem.fadeGroup.alpha = 1f;
            transitionSystem.fadeGroup.blocksRaycasts = true;
        }

        // -------------------------
        // PANTALLA NEGRA: hacemos el cambio sin que se note
        // -------------------------

        if (rainState.HasValue)
            SetRain(rainState.Value);

        ChangeVideoClip(videoName);

        // Esperar a Prepare() (con timeout para evitar cuelgues)
        float timeout = 2.0f;
        while (!_isPrepared && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (timeout <= 0f)
        {
            Debug.LogWarning($"[VideoBackgroundController] Timeout esperando Prepare() del vídeo: {videoName}");
        }

        // -------------------------
        // FADE IN (desde negro)
        // -------------------------
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
            fadeGroup.alpha = Mathf.Lerp(startAlpha, 0f, k);
            yield return null;
        }

        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
    }

    // =========================================================
    //  CAMBIO DE CLIP
    // =========================================================

    private void ChangeVideoClip(string videoName)
    {
        // IMPORTANTE: marcamos como NO preparado desde ya (aunque falle la carga)
        _isPrepared = false;

        string fullPath = $"{videoResourcePath}/{videoName}";

        if (debugLogs)
            Debug.Log($"[VideoBackgroundController] Cargando: Resources/{fullPath}");

        VideoClip newClip = Resources.Load<VideoClip>(fullPath);

        if (newClip == null)
        {
            Debug.LogError(
                "[VideoBackgroundController] ❌ No se encontró el vídeo.\n" +
                $"  Ruta: Resources/{fullPath}"
            );
            return;
        }

        if (debugLogs)
            Debug.Log($"[VideoBackgroundController] ✅ VideoClip cargado: {newClip.name}");

        // Cambiar y preparar
        videoPlayer.Stop();
        videoPlayer.clip = newClip;
        videoPlayer.time = 0;

        // Asegurar que el evento se suscribe una sola vez
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.Prepare();

        _currentVideoName = videoName;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // Quitamos la suscripción para no duplicar callbacks
        vp.prepareCompleted -= OnVideoPrepared;

        vp.Play();
        _isPrepared = true;

        if (debugLogs)
            Debug.Log($"[VideoBackgroundController] ▶️ Reproduciendo: {_currentVideoName}");
    }

    // =========================================================
    //  LLUVIA (FX)
    // =========================================================

    /// <summary>
    /// Activa o desactiva el efecto de lluvia.
    /// Busca/instancia un objeto llamado "lluviaFx".
    /// </summary>
    public void SetRain(bool active)
    {
        if (debugLogs)
            Debug.Log($"[VideoBackgroundController] SetRain: {active}");

        GameObject existingRain = GameObject.Find("lluviaFx");

        if (active)
        {
            if (existingRain == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Efectoscine/lluviaFx");
                if (prefab != null)
                {
                    existingRain = Instantiate(prefab);
                    existingRain.name = "lluviaFx";
                    existingRain.transform.position = new Vector3(0f, 12f, 5f);

                    int fxLayer = LayerMask.NameToLayer("FX");
                    if (fxLayer != -1) SetLayerRecursively(existingRain, fxLayer);

                    if (debugLogs) Debug.Log("[VideoBackgroundController] 🌧️ Lluvia activada (instanciada).");
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
                if (debugLogs) Debug.Log("[VideoBackgroundController] ☀️ Lluvia desactivada (destruida).");
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // =========================================================
    //  UTILIDAD
    // =========================================================

    /// <summary>
    /// Resetea el tracking del vídeo actual (útil al cambiar de escena).
    /// </summary>
    public void ResetTracking()
    {
        _currentVideoName = "";
    }
}