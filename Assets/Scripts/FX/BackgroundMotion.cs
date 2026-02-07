using UnityEngine;
using UnityEngine.UI;

public class BackgroundMotion : MonoBehaviour
{
    [Header("Pan (UV)")]
    public Vector2 direction = new Vector2(1f, 0f);
    public float panSpeed = 0.15f;
    public float panAmplitude = 0.03f;

    [Header("Zoom (RectTransform)")]
    public float startScale = 1.15f;  // empieza más cerca
    public float endScale = 1.00f;    // termina normal
    public float zoomDuration = 12f;  // segundos

    private RawImage rawImage;
    private RectTransform rt;
    private Vector2 startUVPos;
    private float startTime;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        rt = GetComponent<RectTransform>();

        startUVPos = rawImage.uvRect.position;
        startTime = Time.time;

        rt.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        // PAN ida/vuelta
        float tPan = (Mathf.Sin(Time.time * panSpeed) + 1f) / 2f; // 0..1
        Vector2 offset = direction.normalized * (tPan * panAmplitude);
        rawImage.uvRect = new Rect(startUVPos + offset, rawImage.uvRect.size);

        // ZOOM out progresivo
        float tZoom = Mathf.Clamp01((Time.time - startTime) / zoomDuration);
        float s = Mathf.Lerp(startScale, endScale, tZoom);
        rt.localScale = Vector3.one * s;
    }
}
