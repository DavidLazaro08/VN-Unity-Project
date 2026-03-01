using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay azul semitransparente reutilizable para escenas de visual novel.
/// Genera su propia UI en runtime (no necesita prefabs).
/// 
/// Se controla desde los CSV con el comando SHADOW en el campo cmd:
///   SHADOW=ON    → Fade in hasta alpha normal (0.25)
///   SHADOW=OFF   → Fade out hasta alpha 0
///   SHADOW=DEEP  → Fade in hasta alpha profundo (0.4)
/// </summary>
public class ShadowOverlayController : MonoBehaviour
{
    // =========================================================
    //  CONFIGURACIÓN (ajustable desde Inspector)
    // =========================================================

    [Header("Color del overlay")]
    [Tooltip("Color base del overlay (sin alpha).")]
    public Color overlayColor = new Color(8f / 255f, 18f / 255f, 55f / 255f, 1f);

    [Header("Alpha targets")]
    [Range(0f, 1f)] public float normalAlpha = 0.45f;
    [Range(0f, 1f)] public float deepAlpha   = 0.55f;

    [Header("Duración del fade (segundos)")]
    [Range(0.1f, 3f)] public float fadeDuration = 0.5f;

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================

    private Image _overlayImage;
    private Coroutine _fadeRoutine;

    // =========================================================
    //  INICIALIZACIÓN
    // =========================================================

    private void Awake()
    {
        CreateOverlayUI();
    }

    private void CreateOverlayUI()
    {
        // Buscar el canvas principal de la escena
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ShadowOverlay] No se encontró Canvas en la escena.");
            return;
        }

        // Crear el GameObject del overlay
        GameObject overlayGO = new GameObject("ShadowOverlay");
        overlayGO.transform.SetParent(canvas.transform, false);

        // Colocarlo detrás del UI de diálogo pero delante del fondo
        // (índice 1 suele estar tras el background pero antes del texto)
        overlayGO.transform.SetSiblingIndex(1);

        // Image fullscreen
        _overlayImage = overlayGO.AddComponent<Image>();
        _overlayImage.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
        _overlayImage.raycastTarget = false; // NO bloquear interacción

        // Stretch completo
        RectTransform rt = overlayGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // =========================================================
    //  API PÚBLICA
    // =========================================================

    /// <summary>
    /// Procesa un comando SHADOW desde el CSV.
    /// Valores válidos: "ON", "OFF", "DEEP", "1", "0"
    /// </summary>
    public void ApplyCommand(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        string upper = value.Trim().ToUpper();

        switch (upper)
        {
            case "ON":
            case "1":
                FadeTo(normalAlpha);
                break;

            case "OFF":
            case "0":
                FadeTo(0f);
                break;

            case "DEEP":
                FadeTo(deepAlpha);
                break;

            case "NIGHTFALL":
                // Fade muy lento a un color azul muy oscuro casi negro a lo largo de 45 segundos
                FadeToCustomColor(new Color(0.02f, 0.04f, 0.08f, 0.98f), 45f);
                break;

            case "BLACK":
                FadeToBlack();
                break;

            default:
                // Intentar parsear como float para alpha custom
                if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float custom))
                {
                    FadeTo(Mathf.Clamp01(custom));
                }
                else
                {
                    Debug.LogWarning($"[ShadowOverlay] Comando SHADOW no reconocido: '{value}'");
                }
                break;
        }
    }

    /// <summary>
    /// Fade suave hacia el alpha indicado.
    /// </summary>
    public void FadeTo(float targetAlpha)
    {
        if (_overlayImage == null) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        // Devolverlo detrás del texto por defecto
        _overlayImage.transform.SetSiblingIndex(1);

        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, false));
    }

    /// <summary>
    /// Fade hacia negro puro y opaco, cubriendo todo.
    /// </summary>
    public void FadeToBlack()
    {
        if (_overlayImage == null) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        // Ponerlo delante de todo (incluso del texto)
        _overlayImage.transform.SetAsLastSibling();

        _fadeRoutine = StartCoroutine(FadeRoutine(1f, true));
    }

    /// <summary>
    /// Fade personalizado hacia un color específico con duración específica. Útil para anocheceres.
    /// </summary>
    public void FadeToCustomColor(Color targetColor, float duration)
    {
        if (_overlayImage == null) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        // Devolverlo detrás del texto por defecto
        _overlayImage.transform.SetSiblingIndex(1);

        _fadeRoutine = StartCoroutine(FadeCustomRoutine(targetColor, duration));
    }

    // =========================================================
    //  CORRUTINAS DE FADE
    // =========================================================

    private IEnumerator FadeCustomRoutine(Color targetColor, float duration)
    {
        Color startColor = _overlayImage.color;
        
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            
            // Linear es más natural para fades muy largos
            _overlayImage.color = Color.Lerp(startColor, targetColor, k);
            yield return null;
        }

        _overlayImage.color = targetColor;
        _fadeRoutine = null;
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool toBlack)
    {
        Color startColor = _overlayImage.color;
        
        // Si vamos a negro, target = black sólido. Si no, target = color base con targetAlpha.
        Color targetColor = toBlack ? Color.black : new Color(overlayColor.r, overlayColor.g, overlayColor.b, targetAlpha);
        
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            // Ease in-out para suavidad
            float smooth = k * k * (3f - 2f * k);

            _overlayImage.color = Color.Lerp(startColor, targetColor, smooth);
            yield return null;
        }

        _overlayImage.color = targetColor;
        _fadeRoutine = null;
    }
}
