using UnityEngine;

/// <summary>
/// Controla la reproducción de sonidos cortos (blips) mientras se escribe el texto.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TextBlipController : MonoBehaviour
{
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

    private AudioSource _audioSource;
    private int _charCounter = 0;
    private float _lastBlipTime = -1f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Forzar configuración 2D fail-safe
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; 
        _audioSource.loop = false;
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
