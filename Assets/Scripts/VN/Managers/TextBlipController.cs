using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla los "blips" (soniditos cortos) que acompañan al typewriter.
/// - Suena cada N caracteres (charsPerBlip)
/// - Con un mínimo de tiempo entre blips (minIntervalSeconds) para no saturar
/// - Pitch configurable por personaje (speakerId)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TextBlipController : MonoBehaviour
{
    // =========================================================
    //  TIPOS
    // =========================================================

    [System.Serializable]
    public class SpeakerPitchProfile
    {
        public string speakerId;
        [Range(0.5f, 1.5f)]
        public float pitch = 1.0f;
    }

    // =========================================================
    //  CONFIG (INSPECTOR)
    // =========================================================

    [Header("Audio Settings")]
    public AudioClip blipClip;

    [Range(0f, 1f)]
    public float blipVolume = 0.25f;

    [Header("Throttling (control de ritmo)")]
    [Tooltip("Suena cada N caracteres.")]
    public int charsPerBlip = 2;

    [Tooltip("Tiempo mínimo entre blips para evitar efecto ametralladora.")]
    public float minIntervalSeconds = 0.05f;

    [Header("Filtros")]
    public bool ignoreWhitespace = true;
    public bool ignorePunctuation = true;
    public string punctuationSet = ".,;:!?¿¡";

    [Header("Pitch Control")]
    public float defaultPitch = 1.0f;
    public float narratorPitch = 0.90f;

    [Tooltip("Pitches específicos por personaje (speakerId -> pitch).")]
    public List<SpeakerPitchProfile> speakerPitches = new List<SpeakerPitchProfile>()
    {
        new SpeakerPitchProfile { speakerId = "LOGAN",      pitch = 0.92f },
        new SpeakerPitchProfile { speakerId = "DAMIAO",     pitch = 0.98f },
        new SpeakerPitchProfile { speakerId = "LAZARUS",    pitch = 0.72f },
        new SpeakerPitchProfile { speakerId = "LIRA",       pitch = 1.06f },
        new SpeakerPitchProfile { speakerId = "TRUE-FELLA", pitch = 0.86f },
        new SpeakerPitchProfile { speakerId = "RONNALD",    pitch = 0.78f },
        new SpeakerPitchProfile { speakerId = "EL VIEJO",   pitch = 0.82f }
    };

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================

    private AudioSource _audioSource;

    private int _charCounter = 0;
    private float _lastBlipTime = -1f;

    private float _currentPitch = 1.0f;

    // Búsqueda rápida O(1): "LOGAN" -> 0.92f, etc.
    private Dictionary<string, float> _pitchLookup;

    // =========================================================
    //  CICLO DE VIDA
    // =========================================================

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        // Config 2D fail-safe (por si alguien toca el AudioSource sin querer)
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.loop = false;

        // Pitch inicial
        _currentPitch = defaultPitch;
        _audioSource.pitch = _currentPitch;

        // Evitar valores que romperían lógica (mod 0)
        if (charsPerBlip < 1) charsPerBlip = 1;

        BuildPitchLookup();
    }

    // =========================================================
    //  PITCH POR PERSONAJE
    // =========================================================

    private void BuildPitchLookup()
    {
        _pitchLookup = new Dictionary<string, float>();

        foreach (var profile in speakerPitches)
        {
            if (string.IsNullOrEmpty(profile.speakerId)) continue;

            string key = profile.speakerId.Trim().ToUpperInvariant();

            if (!_pitchLookup.ContainsKey(key))
            {
                _pitchLookup.Add(key, profile.pitch);
            }
            else
            {
                // Si hay duplicados, nos quedamos con el último (y lo avisamos)
                _pitchLookup[key] = profile.pitch;
                Debug.LogWarning($"[TextBlipController] Speaker ID duplicado: '{profile.speakerId}'. Se usará el último valor.");
            }
        }
    }

    /// <summary>
    /// Ajusta el pitch del AudioSource.
    /// </summary>
    public void SetPitch(float pitch)
    {
        if (_audioSource == null) return;

        _currentPitch = pitch;
        _audioSource.pitch = _currentPitch;
    }

    /// <summary>
    /// Configura el pitch según el personaje que habla.
    /// Pásale el speaker ya en mayúsculas si tú lo manejas así, o da igual: lo normalizamos.
    /// </summary>
    public void ApplySpeaker(string speakerUpper)
    {
        // 1) Caso base: vacío -> narrador
        if (string.IsNullOrEmpty(speakerUpper))
        {
            SetPitch(narratorPitch);
            return;
        }

        string key = speakerUpper.Trim().ToUpperInvariant();

        // 2) Comandos del parser: por si llegan aquí por error
        if (key == "WAIT" || key == "ACT" || key == "CHOICE" || key == "JUMP" || key == "BRANCH")
        {
            SetPitch(defaultPitch);
            return;
        }

        // 3) Narrador explícito
        if (key == "NARRADOR")
        {
            SetPitch(narratorPitch);
            return;
        }

        // 4) Lookup por personaje
        if (_pitchLookup != null && _pitchLookup.TryGetValue(key, out float pitch))
        {
            SetPitch(pitch);
        }
        else
        {
            // 5) Fallback
            SetPitch(defaultPitch);
        }
    }

    // =========================================================
    //  LLAMADA DESDE TYPEWRITER
    // =========================================================

    /// <summary>
    /// Llamado por cada carácter impreso.
    /// isSkippingOrInstantComplete: true si el jugador está skippeando o el texto aparece de golpe.
    /// </summary>
    public void OnCharTyped(char c, bool isSkippingOrInstantComplete)
    {
        // Si estamos saltando, o no hay clip, o no hay AudioSource... no hacemos nada
        if (isSkippingOrInstantComplete || blipClip == null || _audioSource == null) return;

        // 1) Filtro de espacios
        if (ignoreWhitespace && char.IsWhiteSpace(c)) return;

        // 2) Filtro de puntuación (sin crear strings)
        if (ignorePunctuation && !string.IsNullOrEmpty(punctuationSet) && punctuationSet.IndexOf(c) >= 0) return;

        // 3) Throttle por tiempo
        if (Time.time - _lastBlipTime < minIntervalSeconds) return;

        // 4) Throttle por conteo
        _charCounter++;
        if ((_charCounter % charsPerBlip) != 0) return;

        // 5) Play
        _audioSource.PlayOneShot(blipClip, blipVolume);
        _lastBlipTime = Time.time;
    }

    /// <summary>
    /// Reinicia el contador (útil al empezar una línea nueva).
    /// </summary>
    public void ResetCounter()
    {
        _charCounter = 0;
    }
}