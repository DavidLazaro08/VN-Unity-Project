using UnityEngine;

/// <summary>
/// Coloca este script en la Main Camera de cada escena (o en un prefabricado que uses en todas).
/// Fuerza a que la cámara SIEMPRE renderice en 16:9 (1920x1080).
/// Si la pantalla del jugador es más cuadrada o alargada, añade barras negras (Letterbox/Pillarbox)
/// automáticamente.
/// Esto evita que el juego se estire, se deforme o que llueva fuera del encuadre.
/// </summary>
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [Header("Resolución Objetivo (Ratio)")]
    public float targetWidth = 16.0f;
    public float targetHeight = 9.0f;

    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        AdjustAspectRatio();
    }

    // Opcional: si permites redimensionar la ventana en PC, lo ajusta en vivo
    void Update()
    {
        AdjustAspectRatio();
    }

    private void AdjustAspectRatio()
    {
        // Calcular la proporción deseada (ej: 16/9 = 1.777)
        float targetAspect = targetWidth / targetHeight;

        // Proporción actual de la pantalla de Windows/Mac
        float windowAspect = (float)Screen.width / (float)Screen.height;

        // Calcular el factor de escala de la altura
        float scaleHeight = windowAspect / targetAspect;

        // Si la pantalla es más alta de lo que necesitamos (pantalla muy cuadrada, ej 16:10)
        // Añadimos barras negras ARRIBA y ABAJO (Letterbox)
        if (scaleHeight < 1.0f)
        {
            Rect rect = _cam.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f; // Centrarlo verticalmente

            _cam.rect = rect;
        }
        else // La pantalla es más ancha de lo normal (Monitor Ultrawide)
        {
            // Añadimos barras negras a los LADOS (Pillarbox)
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = _cam.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f; // Centrarlo horizontalmente
            rect.y = 0;

            _cam.rect = rect;
        }
    }
}
