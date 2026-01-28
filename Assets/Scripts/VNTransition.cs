using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VNTransition : MonoBehaviour
{
    [Header("Fade Panel (CanvasGroup)")]
    public CanvasGroup fadeGroup;

    [Header("Audio (opcional)")]
    public AudioSource musicSource;

    [Header("Config")]
    [Range(0.1f, 3f)] public float fadeTime = 1f;
    public string targetSceneName = "Scene_Game";

    private bool _running = false;

    private void Awake()
    {
        // Arranque limpio
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }

    public void StartGameWithFade()
    {
        if (_running) return;
        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        _running = true;

        // Bloqueamos clicks durante transición
        if (fadeGroup != null)
            fadeGroup.blocksRaycasts = true;

        float startVol = musicSource ? musicSource.volume : 0f;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);

            // Fade a negro
            if (fadeGroup != null)
                fadeGroup.alpha = k;

            // Fade out música
            if (musicSource != null)
                musicSource.volume = Mathf.Lerp(startVol, 0f, k);

            yield return null;
        }

        // Aseguramos valores finales
        if (fadeGroup != null) fadeGroup.alpha = 1f;
        if (musicSource != null) musicSource.volume = 0f;

        // Cargamos escena
        SceneManager.LoadScene(targetSceneName);
    }
}
