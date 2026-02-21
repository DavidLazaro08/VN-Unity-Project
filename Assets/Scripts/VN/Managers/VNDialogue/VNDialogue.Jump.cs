using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
    // =========================================================
    //  JUMP SYSTEM CONSTANTS & FIELDS
    // =========================================================
    
    // Tiempo de espera antes del salto (configurable)
    private const float JUMP_WAIT_TIME = 1.5f;

    // =========================================================
    //  JUMP COROUTINES
    // =========================================================

    private IEnumerator JumpAfterDelay(string jumpTargetScene, string jumpTargetLine, string jumpUnityScene)
    {
        Debug.Log($"[VNDialogue] JumpAfterDelay iniciado. Esperando {JUMP_WAIT_TIME} segundos...");
        
        // Esperar tiempo configurado antes del salto
        yield return new WaitForSeconds(JUMP_WAIT_TIME);

        Debug.Log($"[VNDialogue] Espera completada. Buscando escena CSV '{jumpTargetScene}'...");

        // Buscar índice de la escena CSV destino
        int targetSceneIndex = sceneFiles.IndexOf(jumpTargetScene);
        
        if (targetSceneIndex < 0)
        {
            // Si hay JUMP_UNITY_SCENE, el CSV estará en la NUEVA escena, no en la actual.
            // Usamos índice 0 como fallback — la nueva escena cargará su propio CSV.
            if (!string.IsNullOrEmpty(jumpUnityScene))
            {
                Debug.Log($"[VNDialogue] JUMP: CSV '{jumpTargetScene}' no está en sceneFiles actual, pero hay JUMP_UNITY_SCENE='{jumpUnityScene}'. Usando índice 0.");
                targetSceneIndex = 0;
            }
            else
            {
                Debug.LogError($"[VNDialogue] JUMP: Escena CSV '{jumpTargetScene}' no encontrada en sceneFiles!");
                _isJumping = false;
                AdvanceLineAndShow();
                yield break;
            }
        }

        Debug.Log($"[VNDialogue] Escena CSV resuelta en índice {targetSceneIndex}.");

        // Si se especifica JUMP_UNITY_SCENE, cargar esa escena de Unity con fade
        if (!string.IsNullOrEmpty(jumpUnityScene))
        {
            Debug.Log($"[VNDialogue] Preparando fade y carga de escena Unity: {jumpUnityScene}");
            
            // Guardar el estado del flag localmente para usarlo en toda la rutina de salto
            bool persistentMusic = VNTransitionFlags.SkipMusicFadeOnce;

            // Si el flag de música está activo, persistir AudioSources
            if (persistentMusic)
            {
                Debug.Log("[VNDialogue] Buscando AudioSources de música para persistir...");
                
                // Buscar todos los AudioSources activos en la escena
                AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource.isPlaying)
                    {
                        DontDestroyOnLoad(audioSource.gameObject);
                        Debug.Log($"[VNDialogue] AudioSource '{audioSource.gameObject.name}' marcado como persistente.");
                    }
                }
                
                // Consumir el flag (ya se registró el deseo de persistencia)
                VNTransitionFlags.SkipMusicFadeOnce = false;
            }
            
            // Crear canvas de fade temporal
            GameObject fadeCanvasGO = new GameObject("JumpFadeCanvas");
            Canvas canvas = fadeCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            
            fadeCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            fadeCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Panel negro con CanvasGroup
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
            
            CanvasGroup fadeGroup = panelGO.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = true;
            
            // No usamos DontDestroyOnLoad — queremos que este panel muera 
            // al cargar la siguiente escena (transición normal).
            
            // Capurar AudioSources para el fade (solo si no saltamos el fade)
            List<AudioSource> musicToFade = new List<AudioSource>();
            List<float> initialVolumes = new List<float>();
            
            if (!persistentMusic)
            {
                AudioSource[] sources = FindObjectsOfType<AudioSource>();
                foreach (var s in sources)
                {
                    if (s.isPlaying && s.volume > 0)
                    {
                        musicToFade.Add(s);
                        initialVolumes.Add(s.volume);
                    }
                }
            }

            // FADE OUT a negro y FADE de música (0.8 segundos)
            float fadeTime = 0.8f;
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeTime);
                
                // Visual
                fadeGroup.alpha = k;
                
                // Audio
                for (int i = 0; i < musicToFade.Count; i++)
                {
                    if (musicToFade[i] != null)
                        musicToFade[i].volume = initialVolumes[i] * (1f - k);
                }
                
                yield return null;
            }
            fadeGroup.alpha = 1f;

            // Asegurar volumen 0 al final
            foreach (var s in musicToFade) if (s != null) s.volume = 0;
            
            Debug.Log($"[VNDialogue] Fade completado. Cargando escena...");
            
            // Guardar datos para que la nueva escena los cargue
            PlayerPrefs.SetInt("JUMP_SCENE_INDEX", targetSceneIndex);
            PlayerPrefs.SetString("JUMP_TARGET_LINE", jumpTargetLine);
            PlayerPrefs.SetInt("JUMP_ACTIVE", 1);
            PlayerPrefs.SetInt("JUMP_FADE_IN", 1);  // Señal para hacer fade in
            PlayerPrefs.Save();
            
            // Cargar escena Unity
            SceneManager.LoadScene(jumpUnityScene);
            
            yield break;
        }

        // Si NO hay JUMP_UNITY_SCENE, solo cambiar CSV en la misma escena Unity
        Debug.Log($"[VNDialogue] Sin cambio de escena Unity. Cargando CSV...");
        
        sceneIndex = targetSceneIndex;
        LoadScene(sceneIndex);

        Debug.Log($"[VNDialogue] CSV cargado. Total líneas: {currentLines.Count}. Posicionando en '{jumpTargetLine}'...");

        // Posicionar en la línea destino
        if (jumpTargetLine == "END")
        {
            lineIndex = Mathf.Max(0, currentLines.Count - 1);
            Debug.Log($"[VNDialogue] Posicionado en END (línea {lineIndex})");
        }
        else if (int.TryParse(jumpTargetLine, out int targetLine))
        {
            lineIndex = Mathf.Clamp(targetLine, 0, currentLines.Count - 1);
            Debug.Log($"[VNDialogue] Posicionado en línea específica {lineIndex}");
        }
        else
        {
            lineIndex = 0;
            Debug.Log($"[VNDialogue] JUMP_LINE inválido, posicionado en inicio");
        }

        _isJumping = false;
        ShowLine();
    }

    /// <summary>
    /// Fade In desde negro después de un JUMP con transición de escena Unity.
    /// Busca el canvas de fade creado por JumpAfterDelay y lo desvanece.
    /// </summary>
    private IEnumerator JumpFadeInRoutine()
    {
        Debug.Log("[VNDialogue] JumpFadeInRoutine iniciado. Buscando canvas de fade...");
        
        // Buscar el canvas de fade que dejó la escena anterior
        GameObject fadeCanvasGO = GameObject.Find("JumpFadeCanvas");
        
        if (fadeCanvasGO == null)
        {
            Debug.LogWarning("[VNDialogue] No se encontró JumpFadeCanvas. Mostrando sin fade.");
            ShowLine();
            yield break;
        }
        
        CanvasGroup fadeGroup = fadeCanvasGO.GetComponentInChildren<CanvasGroup>();
        
        if (fadeGroup == null)
        {
            Debug.LogWarning("[VNDialogue] No se encontró CanvasGroup en JumpFadeCanvas.");
            Destroy(fadeCanvasGO);
            ShowLine();
            yield break;
        }
        
        Debug.Log("[VNDialogue] Canvas de fade encontrado. Iniciando fade in...");
        
        // Asegurar que empieza en negro
        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = true;
        
        // Pequeño delay antes de empezar el fade
        yield return new WaitForSeconds(0.3f);
        
        // Mostrar el diálogo mientras está en negro
        ShowLine();
        
        // FADE IN desde negro (1 segundo)
        float fadeTime = 1f;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            fadeGroup.alpha = 1f - k;  // De 1 a 0
            yield return null;
        }
        
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        
        Debug.Log("[VNDialogue] Fade in completado. Destruyendo canvas.");
        
        // Destruir el canvas de fade
        Destroy(fadeCanvasGO);
    }
}
