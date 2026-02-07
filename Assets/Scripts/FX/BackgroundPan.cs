using UnityEngine;
using UnityEngine.UI;

public class BackgroundPan : MonoBehaviour
{
    public Vector2 direction = new Vector2(1f, 0f); // dirección base
    public float speed = 0.02f;                      // velocidad real
    public float amplitude = 0.03f;                  // cuánto se mueve (0.01-0.06)

    private RawImage rawImage;
    private Vector2 startPos;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        startPos = rawImage.uvRect.position;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f; // 0..1
        Vector2 offset = direction.normalized * (t * amplitude);
        rawImage.uvRect = new Rect(startPos + offset, rawImage.uvRect.size);
    }
}
