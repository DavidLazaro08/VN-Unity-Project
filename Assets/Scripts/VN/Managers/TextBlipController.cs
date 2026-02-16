using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla la reproducción de sonidos cortos (blips) mientras se escribe el texto.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TextBlipController : MonoBehaviour
{
    [System.Serializable]
    public class SpeakerPitchProfile
    {
        public string speakerId;
        [Range(0.5f, 1.5f)] public float pitch = 1.0f;
    }

    [Header("Audio Settings")]
    public AudioClip blipClip;
    [Range(0f, 1f)]
    public float blipVolume = 0.25f;

    [Header("Throttling (Control de ritmo)")]
    [Tooltip("Suena cada N caracteres")]
    public int charsPerBlip = 2;
    [Tooltip("Tiempo mínimo entre sonidos para evitar saturación")]
    public float minIntervalSeconds = 0.05f;

    [Header("Filtros")]
    public bool ignoreWhitespace = true;
    public bool ignorePunctuation = true;
    public string punctuationSet = ".,;:!?¿¡";

    [Header("Pitch Control")]
    public float defaultPitch = 1.0f;
    public float narratorPitch = 0.90f;

    [Tooltip("Configura aquí los pitches específicos por personaje.")]
    public List<SpeakerPitchProfile> speakerPitches = new List<SpeakerPitchProfile>()
    {
        new SpeakerPitchProfile { speakerId = "LOGAN", pitch = 0.92f },
        new SpeakerPitchProfile { speakerId = "DAMIAO", pitch = 0.98f },
        new SpeakerPitchProfile { speakerId = "LAZARUS", pitch = 0.72f },
        new SpeakerPitchProfile { speakerId = "LIRA", pitch = 1.06f },
        new SpeakerPitchProfile { speakerId = "TRUE-FELLA", pitch = 0.86f },
        new SpeakerPitchProfile { speakerId = "RONNALD", pitch = 0.78f },
        new SpeakerPitchProfile { speakerId = "EL VIEJO", pitch = 0.82f }
    };

    private AudioSource _audioSource;
    private int _charCounter = 0;
    private float _lastBlipTime = -1f;
    private float _currentPitch = 1.0f;

    // Diccionario para búsqueda rápida (O(1))
    private Dictionary<string, float> _pitchLookup;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Forzar configuración 2D fail-safe
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; 
        _audioSource.loop = false;
        
        // Inicializar pitch
        _currentPitch = defaultPitch;
        _audioSource.pitch = _currentPitch;

        // Construir diccionario de pitches
        BuildPitchLookup();
    }

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
                _pitchLookup[key] = profile.pitch; // Sobreescribimos si hay duplicados
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
    /// </summary>
    public void ApplySpeaker(string speakerUpper)
    {
        // 1. Caso base: Sin speaker o vacío -> Narrador
        if (string.IsNullOrEmpty(speakerUpper))
        {
            SetPitch(narratorPitch);
            return;
        }

        string key = speakerUpper.Trim().ToUpperInvariant();

        // 2. Comandos especiales que deben ignorarse (o resetear a default si se llaman por error)
        if (key == "WAIT" || key == "ACT" || key == "CHOICE" || key == "JUMP" || key == "BRANCH")
        {
            SetPitch(defaultPitch);
            return;
        }

        // 3. Caso explícito Narrador
        if (key == "NARRADOR")
        {
            SetPitch(narratorPitch);
            return;
        }

        // 4. Búsqueda en diccionario configurable
        if (_pitchLookup != null && _pitchLookup.TryGetValue(key, out float pitch))
        {
            SetPitch(pitch);
        }
        else
        {
            // 5. Fallback: No encontrado -> Default
            SetPitch(defaultPitch);
        }
    }

    /// <summary>
    /// Llamado desde el Typewriter por cada carácter impreso.
    /// </summary>
    public void OnCharTyped(char c, bool isSkippingOrInstantComplete)
    {
        // Fail-safe: Si no hay clip o estamos saltando, fuera.
        if (isSkippingOrInstantComplete || blipClip == null || _audioSource == null) return;

        // 1. Filtro de espacios
        if (ignoreWhitespace && char.IsWhiteSpace(c)) return;

        // 2. Filtro de puntuación
        if (ignorePunctuation && punctuationSet.Contains(c.ToString())) return;

        // 3. Throttle por tiempo (evitar ametralladora)
        if (Time.time - _lastBlipTime < minIntervalSeconds) return;

        // 4. Throttle por conteo de caracteres
        _charCounter++;
        if (_charCounter % charsPerBlip != 0) return;

        // 5. Play
        // Nota: _audioSource.pitch ya fue configurado en ApplySpeaker/SetPitch, no hace falta setearlo aquí.
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
