using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BackgroundPan
/// Añade un movimiento suave al fondo usando las UV del RawImage.
/// Es un desplazamiento sutil tipo “respiración” para que la imagen
/// no quede completamente estática.
///
/// No modifica escala ni posición del objeto, solo la textura.
/// </summary>
public class BackgroundPan : MonoBehaviour
{
    public Vector2 direction = new Vector2(1f, 0f); // Dirección base del desplazamiento
    public float speed = 0.02f;                     // Velocidad del movimiento
    public float amplitude = 0.03f;                 // Cuánto se desplaza (0.01–0.06 suele ir bien)

    private RawImage rawImage;
    private Vector2 startPos;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        startPos = rawImage.uvRect.position;
    }

    void Update()
    {
        // Movimiento suave ida/vuelta (0..1)
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;

        Vector2 offset = direction.normalized * (t * amplitude);

        rawImage.uvRect = new Rect(startPos + offset, rawImage.uvRect.size);
    }
}