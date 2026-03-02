using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
    /*
     * VNDialogue.Jump
     * --------------
     * Maneja el comando JUMP: puede saltar a otro CSV dentro de la misma escena Unity,
     * o cargar otra escena Unity y reanudar el CSV/línea de destino.
     *
     * Incluye dos modos de transición:
     * - Fundido a negro clásico (con fade-in en la escena nueva).
     * - Crossfade usando captura de pantalla (sin pantallazo negro).
     */

    // =========================================================
    //  JUMP SYSTEM CONSTANTS & FIELDS
    // =========================================================

    // Tiempo de espera antes del salto (configurable desde CSV con DELAY=)
    private const float JUMP_WAIT_TIME = 1.5f;

    // =========================================================
    //  JUMP COROUTINES
    // =========================================================

    private IEnumerator JumpAfterDelay(
        string jumpTargetScene,
        string jumpTargetLine,
        string jumpUnityScene,
        float delayTime = JUMP_WAIT_TIME,
        bool useCrossfade = false
    )
    {
#if UNITY_EDITOR
        Debug.Log($"[VNDialogue] JUMP: esperando {delayTime:0.00}s | crossfade={useCrossfade}");
#endif

        yield return new WaitForSeconds(delayTime);

        // Buscar índice de la escena CSV destino
        int targetSceneIndex = sceneFiles.IndexOf(jumpTargetScene);

        if (targetSceneIndex < 0)
        {
            // Si hay JUMP_UNITY_SCENE, el CSV estará en la nueva escena, no en la actual.
            // Usamos índice 0 como fallback: la escena nueva cargará su propio listado.
            if (!string.IsNullOrEmpty(jumpUnityScene))
            {
#if UNITY_EDITOR
                Debug.Log($"[VNDialogue] JUMP: CSV '{jumpTargetScene}' no está en sceneFiles actual. Hay cambio de escena Unity, uso índice 0.");
#endif
                targetSceneIndex = 0;
            }
            else
            {
                Debug.LogError($"[VNDialogue] JUMP: no se encuentra el CSV '{jumpTargetScene}' en sceneFiles.");
                _isJumping = false;
                AdvanceLineAndShow();
                yield break;
            }
        }

        // Si se especifica JUMP_UNITY_SCENE, cargar escena Unity
        if (!string.IsNullOrEmpty(jumpUnityScene))
        {
#if UNITY_EDITOR
            Debug.Log($"[VNDialogue] JUMP: cargando escena Unity '{jumpUnityScene}' (CSV idx {targetSceneIndex}, line '{jumpTargetLine}')");
#endif

            // Guardar el estado del flag localmente para usarlo en toda la rutina
            bool persistentMusic = VNTransitionFlags.SkipMusicFadeOnce;

            // Si el flag de música está activo, persistir AudioSources
            if (persistentMusic)
            {
#if UNITY_EDITOR
                Debug.Log("[VNDialogue] JUMP: persistencia de música activada (SkipMusicFadeOnce).");
#endif

                AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource.isPlaying)
                    {
                        // DontDestroyOnLoad solo funciona en root GameObjects.
                        GameObject rootGO = audioSource.transform.root.gameObject;
                        DontDestroyOnLoad(rootGO);

                        // Registrar en el fader para aplicar desvanecimientos después si procede.
                        MusicTailFader.PendingFades.Add(audioSource);

#if UNITY_EDITOR
                        Debug.Log($"[VNDialogue] JUMP: música persistida -> '{audioSource.gameObject.name}' (root '{rootGO.name}')");
#endif
                    }
                }

                // Consumir el flag
                VNTransitionFlags.SkipMusicFadeOnce = false;
            }

            // Crear canvas de fade temporal (solo para modo “fundido a negro”)
            GameObject fadeCanvasGO = null;
            CanvasGroup fadeGroup = null;

            // Capturar AudioSources para el fade (solo si no persistimos música)
            List<AudioSource> musicToFade = new List<AudioSource>();
            List<float> initialVolumes = new List<float>();

            if (!persistentMusic)
            {
                AudioSource[] sources = FindObjectsOfType<AudioSource>();
                foreach (var s in sources)
                {
                    if (s.isPlaying && s.volume > 0f)
                    {
                        musicToFade.Add(s);
                        initialVolumes.Add(s.volume);
                    }
                }
            }

            if (useCrossfade)
            {
                // Crossfade: no creamos panel negro
#if UNITY_EDITOR
                Debug.Log("[VNDialogue] JUMP: crossfade activo (sin panel negro).");
#endif
                yield return new WaitForEndOfFrame();
                SceneCrossfader.StartCrossfade(3.5f);

                // Esperar un par de frames extra para asegurar que el overlay está listo
                yield return null;
                yield return null;

                // Audio: fade rápido si corresponde
                if (musicToFade.Count > 0)
                {
                    float fadeTime = 0.5f;
                    float t = 0f;
                    while (t < fadeTime)
                    {
                        t += Time.deltaTime;
                        float k = Mathf.Clamp01(t / fadeTime);

                        for (int i = 0; i < musicToFade.Count; i++)
                        {
                            if (musicToFade[i] != null)
                                musicToFade[i].volume = initialVolumes[i] * (1f - k);
                        }

                        yield return null;
                    }

                    foreach (var s in musicToFade)
                        if (s != null) s.volume = 0f;
                }
            }
            else
            {
                // Fundido a negro clásico + fade de música sincronizados
                fadeCanvasGO = new GameObject("JumpFadeCanvas");

                // IMPORTANTE:
                // Si vamos a hacer fade-in en la escena nueva, este canvas debe sobrevivir al LoadScene.
                DontDestroyOnLoad(fadeCanvasGO);

                Canvas canvas = fadeCanvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32767;

                fadeCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                fadeCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                GameObject panelGO = new GameObject("FadePanel");
                panelGO.transform.SetParent(fadeCanvasGO.transform, false);

                UnityEngine.UI.Image img = panelGO.AddComponent<UnityEngine.UI.Image>();
                img.color = Color.black;
                img.raycastTarget = true;

                RectTransform rt = panelGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                fadeGroup = panelGO.AddComponent<CanvasGroup>();
                fadeGroup.alpha = 0f;
                fadeGroup.blocksRaycasts = true;

                // Duración de fade de música (flag del CSV o por defecto)
                float duration = VNTransitionFlags.MusicFadeDuration > 0f
                    ? VNTransitionFlags.MusicFadeDuration
                    : 0.8f;

                VNTransitionFlags.MusicFadeDuration = 0f; // Reset

                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / duration);

                    // Visual y audio a la vez
                    fadeGroup.alpha = k;

                    for (int i = 0; i < musicToFade.Count; i++)
                    {
                        if (musicToFade[i] != null)
                            musicToFade[i].volume = initialVolumes[i] * (1f - k);
                    }

                    yield return null;
                }

                fadeGroup.alpha = 1f;
                foreach (var s in musicToFade)
                    if (s != null) s.volume = 0f;
            }

            // Guardar datos para que la escena nueva retome el salto
            PlayerPrefs.SetInt("JUMP_SCENE_INDEX", targetSceneIndex);
            PlayerPrefs.SetString("JUMP_TARGET_LINE", jumpTargetLine);
            PlayerPrefs.SetInt("JUMP_ACTIVE", 1);

            // Señal para hacer fade-in desde negro SOLO si no hay crossfade
            PlayerPrefs.SetInt("JUMP_FADE_IN", useCrossfade ? 0 : 1);

            PlayerPrefs.Save();

#if UNITY_EDITOR
            Debug.Log("[VNDialogue] JUMP: cargando escena Unity...");
#endif
            SceneManager.LoadScene(jumpUnityScene);
            yield break;
        }

        // Si NO hay JUMP_UNITY_SCENE, solo cambiar CSV en la misma escena Unity
        sceneIndex = targetSceneIndex;
        LoadScene(sceneIndex);

        // Posicionar en la línea destino
        if (jumpTargetLine == "END")
        {
            lineIndex = Mathf.Max(0, currentLines.Count - 1);
        }
        else if (int.TryParse(jumpTargetLine, out int targetLine))
        {
            lineIndex = Mathf.Clamp(targetLine, 0, currentLines.Count - 1);
        }
        else
        {
            lineIndex = 0;
        }

        _isJumping = false;
        ShowLine();
    }

    /// <summary>
    /// Fade-in desde negro después de un JUMP entre escenas Unity.
    /// Busca el canvas temporal ("JumpFadeCanvas"), hace el fade-in y lo destruye.
    /// </summary>
    private IEnumerator JumpFadeInRoutine()
    {
#if UNITY_EDITOR
        Debug.Log("[VNDialogue] JUMP: fade-in en escena nueva.");
#endif

        GameObject fadeCanvasGO = GameObject.Find("JumpFadeCanvas");

        if (fadeCanvasGO == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[VNDialogue] JUMP: no se encontró JumpFadeCanvas. Se continúa sin fade-in.");
#endif
            ShowLine();
            yield break;
        }

        CanvasGroup fadeGroup = fadeCanvasGO.GetComponentInChildren<CanvasGroup>();

        if (fadeGroup == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[VNDialogue] JUMP: JumpFadeCanvas no tiene CanvasGroup. Se destruye y se continúa.");
#endif
            Destroy(fadeCanvasGO);
            ShowLine();
            yield break;
        }

        // Asegurar negro al inicio
        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = true;

        // Pequeño margen para que la escena nueva se estabilice
        yield return new WaitForSeconds(0.3f);

        // Mostramos la línea ya (tapada por el negro)
        ShowLine();

        // Fade-in
        float fadeTime = 1f;
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

        Destroy(fadeCanvasGO);
    }
}

// =========================================================
//  SCENECROSSFADER (Transición fluida sin pantallazo negro)
// =========================================================
public class SceneCrossfader : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private float _fadeDuration;

    public static void StartCrossfade(float duration)
    {
        // Captura de pantalla (se asume que esto se llama tras WaitForEndOfFrame)
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // Overlay persistente
        GameObject go = new GameObject("SceneCrossfader_Persistent");
        DontDestroyOnLoad(go);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        GameObject imgGo = new GameObject("Screenshot");
        imgGo.transform.SetParent(go.transform, false);

        UnityEngine.UI.RawImage raw = imgGo.AddComponent<UnityEngine.UI.RawImage>();
        raw.texture = tex;

        RectTransform rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        CanvasGroup cg = imgGo.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        SceneCrossfader fader = go.AddComponent<SceneCrossfader>();
        fader._canvasGroup = cg;
        fader._fadeDuration = duration;
    }

    private void Awake()
    {
        // Awake para asegurar que el evento esté conectado incluso si se carga escena rápido
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(FadeOutAndDie());
    }

    private IEnumerator FadeOutAndDie()
    {
        // Un frame para que la escena nueva pinte
        yield return null;

        float t = 0f;
        while (t < _fadeDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = 1f - (t / _fadeDuration);
            yield return null;
        }

        // Limpiar textura temporal
        var raw = GetComponentInChildren<UnityEngine.UI.RawImage>();
        if (raw != null && raw.texture != null)
            Destroy(raw.texture);

        Destroy(gameObject);
    }
}