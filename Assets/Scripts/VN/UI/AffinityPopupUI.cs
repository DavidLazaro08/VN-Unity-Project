using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra un pequeño popup visual cuando cambia la afinidad.
/// </summary>
public class AffinityPopupUI : MonoBehaviour
{
    [Header("Configuración UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI deltaText;
    
    [Header("Animación")]
    public float duration = 1.5f;
    public float moveDistance = 50f;

    private Coroutine _animRoutine;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowDelta(int delta)
    {
        if (canvasGroup == null || deltaText == null) return;

        // Texto: "+1", "-1" o "=" si es 0
        if (delta > 0)
        {
            deltaText.text = $"+{delta}";
            deltaText.color = Color.green; // Opcional: Verde
        }
        else if (delta < 0)
        {
            deltaText.text = $"{delta}";
            deltaText.color = Color.red;   // Opcional: Rojo
        }
        else
        {
            deltaText.text = "="; // Neutro
            deltaText.color = Color.white; // Opcional: Blanco/Gris
        }

        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        // Posición inicial (asumimos que está anclado donde queremos que empiece)
        // Para simplificar, usaremos localPosition si está en un layout libre, 
        // o punch scale si está fijo. Haremos un fade in/out + subida simple.
        
        RectTransform rt = deltaText.rectTransform;
        Vector3 startPos = rt.anchoredPosition;
        Vector3 targetPos = startPos + new Vector3(0, moveDistance, 0);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration; // 0 a 1

            // Animación alpha: sube rápido, mantiene, baja al final
            float alpha = 1f;
            if (normalized < 0.2f) alpha = normalized / 0.2f;
            else if (normalized > 0.8f) alpha = 1f - ((normalized - 0.8f) / 0.2f);

            canvasGroup.alpha = alpha;

            // Animación posición: sube suavemente
            rt.anchoredPosition = Vector3.Lerp(startPos, targetPos, normalized);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        rt.anchoredPosition = startPos; // Reset
    }
}
