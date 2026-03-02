using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Este script se encarga de apagar poco a poco la música que viene de la escena anterior.
/// 
/// En vez de cortar el audio de golpe al hacer un JUMP, guardamos los AudioSources
/// que deben desaparecer en PendingFades y, al cargar la nueva escena,
/// este componente les hace un fade-out suave.
/// 
/// Se marca como DontDestroyOnLoad para que el fade no se interrumpa
/// si hay otra transición rápida después.
/// 
/// Si por algún motivo la lista viene vacía, intenta buscar música en
/// DontDestroyOnLoad como respaldo.
/// </summary>
public class MusicTailFader : MonoBehaviour
{
    // -------------------------------------------------------
    // REGISTRO ESTÁTICO (lo rellena VNDialogue antes del LoadScene)
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
        // Evitar duplicados si por lo que sea hay más de uno en escena
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
            Debug.LogWarning(
                "[MusicTailFader] Sin fuentes para fadear. " +
                "Asegúrate de que el JUMP tiene SKIP_MUSIC_FADE=1 " +
                "y que MusicTailFader.PendingFades fue rellenado."
            );

            Destroy(gameObject);
            return;
        }

        StartCoroutine(FadeAllAndDestroy(targets));
    }

    // -------------------------------------------------------
    // RECOPILAR FUENTES
    // -------------------------------------------------------
    /// <summary>
    /// Prioridad 1: lista estática PendingFades registrada por VNDialogue.
    /// Fallback: escanear DontDestroyOnLoad por compatibilidad / robustez.
    /// </summary>
    private List<AudioSource> CollectTargets()
    {
        List<AudioSource> result = new List<AudioSource>();

        // --- 1) Fuentes registradas explícitamente ---
        foreach (AudioSource src in PendingFades)
        {
            if (src != null && src.isPlaying &&
                src.clip != null && src.clip.length >= minClipLength)
            {
                result.Add(src);

                Debug.Log(
                    $"[MusicTailFader] [Registrado] '{src.clip.name}' " +
                    $"en '{src.gameObject.name}' | vol={src.volume:F2}"
                );
            }
        }

        // Limpiamos el registro para que no se reutilice en otra escena por accidente
        PendingFades.Clear();

        // --- 2) Fallback: buscar música en DontDestroyOnLoad ---
        if (result.Count == 0)
        {
            AudioSource[] all = FindObjectsOfType<AudioSource>(true);

            foreach (AudioSource src in all)
            {
                if (src.gameObject == this.gameObject) continue;

                bool isFromDDOL = src.gameObject.scene.name == "DontDestroyOnLoad";
                bool isPlaying = src.isPlaying;
                bool isMusic = src.clip != null && src.clip.length >= minClipLength;

                if (isFromDDOL && isPlaying && isMusic)
                {
                    result.Add(src);

                    Debug.Log(
                        $"[MusicTailFader] [Fallback-DDOL] '{src.clip.name}' " +
                        $"en '{src.gameObject.name}' | vol={src.volume:F2}"
                    );
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

            // Curva suave: empieza lento y cae más fuerte al final (estilo “cola”)
            float curve = t * t; // ease-in cuadrático

            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null && sources[i].isPlaying)
                    sources[i].volume = Mathf.Lerp(startVols[i], 0f, curve);
            }

            yield return null;
        }

        // Apagado final
        foreach (var src in sources)
        {
            if (src != null)
            {
                src.volume = 0f;
                src.loop = false;
                src.Stop();
            }
        }

        Debug.Log("[MusicTailFader] Fade-out completado.");

        // Liberar singleton y autodestruir
        _instance = null;
        Destroy(gameObject);
    }
}