using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coloca este componente en las escenas destino (Scene_Game3, Scene_Terraza, etc.).
/// 
/// Cuando VNDialogue.Jump.cs hace un salto con SKIP_MUSIC_FADE=1, registra los AudioSources
/// a fadear en MusicTailFader.PendingFades (lista estática). Este script los recoge en Start()
/// y hace un fade-out suave de fadeOutDuration segundos.
/// 
/// El script se hace DontDestroyOnLoad para sobrevivir si la escena es una cinemática corta
/// que transiciona a otra antes de que acabe el fade.
/// </summary>
public class MusicTailFader : MonoBehaviour
{
    // -------------------------------------------------------
    // REGISTRO ESTÁTICO — VNDialogue.Jump.cs escribe aquí
    // antes de cargar la escena nueva.
    // -------------------------------------------------------
    public static readonly List<AudioSource> PendingFades = new List<AudioSource>();

    // -------------------------------------------------------
    // CONFIGURACIÓN
    // -------------------------------------------------------
    [Header("Fade-out de música heredada")]
    [Tooltip("Duración total del fade-out (segundos) desde que carga la escena.")]
    [Range(1f, 30f)]
    public float fadeOutDuration = 15f;

    [Tooltip("Longitud mínima del clip (seg) para considerarlo música (excluye blips/SFX).")]
    [Range(1f, 30f)]
    public float minClipLength = 5f;

    // -------------------------------------------------------
    // SINGLETON LIGERO
    // -------------------------------------------------------
    private static MusicTailFader _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // Sobrevive a cambios de escena (cinemáticas cortas, etc.)
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        List<AudioSource> targets = CollectTargets();

        if (targets.Count == 0)
        {
            Debug.LogWarning("[MusicTailFader] Sin fuentes para fadear. " +
                             "Asegúrate de que el JUMP tiene SKIP_MUSIC_FADE=1 " +
                             "y que MusicTailFader.PendingFades fue rellenado.");
            Destroy(gameObject);
            return;
        }

        StartCoroutine(FadeAllAndDestroy(targets));
    }

    // -------------------------------------------------------
    // RECOPILAR FUENTES
    // -------------------------------------------------------
    /// <summary>
    /// Prioridad 1: lista estática PendingFades registrada por VNDialogue.Jump.cs.
    /// Fallback: busca en DontDestroyOnLoad (por compatibilidad con Scene_Game3).
    /// </summary>
    private List<AudioSource> CollectTargets()
    {
        List<AudioSource> result = new List<AudioSource>();

        // — Fuentes registradas explícitamente —
        foreach (AudioSource src in PendingFades)
        {
            if (src != null && src.isPlaying &&
                src.clip != null && src.clip.length >= minClipLength)
            {
                result.Add(src);
                Debug.Log($"[MusicTailFader] [Registrado] '{src.clip.name}' " +
                          $"en '{src.gameObject.name}' | vol={src.volume:F2}");
            }
        }
        PendingFades.Clear();

        // — Fallback: escanear DontDestroyOnLoad —
        if (result.Count == 0)
        {
            AudioSource[] all = FindObjectsOfType<AudioSource>(true);
            foreach (AudioSource src in all)
            {
                if (src.gameObject == this.gameObject) continue;

                bool isFromDDOL = src.gameObject.scene.name == "DontDestroyOnLoad";
                bool isPlaying  = src.isPlaying;
                bool isMusic    = src.clip != null && src.clip.length >= minClipLength;

                if (isFromDDOL && isPlaying && isMusic)
                {
                    result.Add(src);
                    Debug.Log($"[MusicTailFader] [Fallback-DDOL] '{src.clip.name}' " +
                              $"en '{src.gameObject.name}' | vol={src.volume:F2}");
                }
            }
        }

        return result;
    }

    // -------------------------------------------------------
    // FADE-OUT
    // -------------------------------------------------------
    private IEnumerator FadeAllAndDestroy(List<AudioSource> sources)
    {
        float[] startVols = new float[sources.Count];
        for (int i = 0; i < sources.Count; i++)
            startVols[i] = sources[i] != null ? sources[i].volume : 0f;

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float curve = t * t; // ease-in cuadrático

            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null && sources[i].isPlaying)
                    sources[i].volume = Mathf.Lerp(startVols[i], 0f, curve);
            }

            yield return null;
        }

        foreach (var src in sources)
        {
            if (src != null)
            {
                src.volume = 0f;
                src.loop   = false;
                src.Stop();
            }
        }

        Debug.Log("[MusicTailFader] Fade-out completado.");
        _instance = null;
        Destroy(gameObject);
    }
}
