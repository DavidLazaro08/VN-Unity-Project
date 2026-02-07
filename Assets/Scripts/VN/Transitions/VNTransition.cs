using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VNTransition : MonoBehaviour
{
    // ---------------------------------------------------------
    // REFERENCIAS
    // ---------------------------------------------------------

    [Header("Panel de Fade (CanvasGroup)")]
    public CanvasGroup fadeGroup;

    [Header("Audio (opcional)")]
    public AudioSource musicSource;

    // ---------------------------------------------------------
    // CONFIGURACIÓN
    // ---------------------------------------------------------

    [Header("Configuración")]
    [Range(0.1f, 3f)]
    public float fadeTime = 1f;

    public string targetSceneName = "Scene_Game";

    // Evita disparar transiciones simultáneas
    private bool _running = false;

    // ---------------------------------------------------------
    // OPCIONES DE ARRANQUE
    // ---------------------------------------------------------

    [Header("Opciones de inicio")]
    public bool fadeInOnStart = true;

    [Tooltip("Retardo inicial para permitir que vídeo/FX arranquen detrás del negro")]
    public float startFadeDelay = 0.5f;

    // Canvas Overlay creado en runtime para cubrir cualquier cámara/FX/UI
    private GameObject _globalCanvasGO;

    // ---------------------------------------------------------
    // CICLO DE VIDA
    // ---------------------------------------------------------

    private void Awake()
    {
        // Canvas global en Overlay con prioridad máxima
        _globalCanvasGO = new GameObject("GlobalFadeCanvas");

        Canvas canvas = _globalCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        _globalCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        _globalCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Si hay un CanvasGroup asignado (panel ya existente), se reubica en el canvas global
        if (fadeGroup != null)
        {
            fadeGroup.transform.SetParent(_globalCanvasGO.transform, false);

            RectTransform rt = fadeGroup.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }
        }
        else
        {
            // Generación automática de un panel negro full-screen (Image + CanvasGroup)
            GameObject panelImg = new GameObject("AutoFadePanel");
            panelImg.transform.SetParent(_globalCanvasGO.transform, false);

            UnityEngine.UI.Image img = panelImg.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.black;
            img.raycastTarget = true;

            RectTransform rt = panelImg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            fadeGroup = panelImg.AddComponent<CanvasGroup>();
        }

        // Estado inicial del telón
        fadeGroup.alpha = fadeInOnStart ? 1f : 0f;
        fadeGroup.blocksRaycasts = fadeInOnStart;
    }

    private void Start()
    {
        // Fade In al arrancar la escena (negro -> transparente)
        if (fadeInOnStart)
            StartCoroutine(FadeInRoutine());
    }

    // ---------------------------------------------------------
    // API PÚBLICA
    // ---------------------------------------------------------

    public void StartGameWithFade()
    {
        if (_running) return;
        StartCoroutine(FadeAndLoad());
    }

    // ---------------------------------------------------------
    // CORRUTINAS
    // ---------------------------------------------------------

    /// <summary>
    /// Fade inicial: Negro -> Transparente.
    /// Útil para ocultar frames de carga, vídeo iniciando, FX, etc.
    /// </summary>
    private IEnumerator FadeInRoutine()
    {
        _running = true;

        fadeGroup.blocksRaycasts = true;

        if (startFadeDelay > 0f)
            yield return new WaitForSeconds(startFadeDelay);

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);

            fadeGroup.alpha = 1f - k;
            yield return null;
        }

        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        _running = false;
    }

    /// <summary>
    /// Fade Out + carga de escena. Mantiene negro hasta que la nueva escena
    /// levanta su propio Fade In.
    /// </summary>
    private IEnumerator FadeAndLoad()
    {
        _running = true;

        if (_globalCanvasGO != null)
            DontDestroyOnLoad(_globalCanvasGO);

        DontDestroyOnLoad(gameObject);

        fadeGroup.blocksRaycasts = true;

        float startVol = musicSource ? musicSource.volume : 0f;

        // FADE OUT a negro
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);

            fadeGroup.alpha = k;
            if (musicSource != null)
                musicSource.volume = Mathf.Lerp(startVol, 0f, k);

            yield return null;
        }

        fadeGroup.alpha = 1f;

        // Carga de escena (con pequeño colchón)
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        yield return new WaitForSeconds(0.5f);

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // “Solape” de negro para evitar huecos visuales
        yield return new WaitForSeconds(0.5f);

        // Limpieza del sistema anterior
        if (_globalCanvasGO != null)
            Destroy(_globalCanvasGO);

        Destroy(gameObject);
    }
}
