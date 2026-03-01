using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica un zoom in suave a todo el contenido del Canvas de VN.
/// 
/// INSTALACIÓN:
///   - Añade este script a CUALQUIER game object en la escena.
///   - NO necesita ser el Canvas raíz.
///   - El script encuentra el Canvas automáticamente, crea un contenedor
///     de zoom interno y mueve todos los hijos del Canvas dentro de él.
///
/// USO desde CSV: ZOOM=RIGHT (usa valores por defecto)
///               ZOOM=RESET  (vuelve a escala 1)
///               ZOOM=1.3:5  (escala:segundos)
/// </summary>
public class CanvasZoomController : MonoBehaviour
{
    // =========================================================
    //  SINGLETON ESTÁTICO
    // =========================================================
    public static CanvasZoomController Instance { get; private set; }

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================
    private RectTransform _container;   // El contenedor que escalamos
    private Coroutine _zoomCo;
    private bool _initialized = false;

    // =========================================================
    //  CICLO DE VIDA
    // =========================================================
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Buscar el Canvas en la escena
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[CanvasZoomController] No se encontró Canvas en la escena.");
            return;
        }

        // Crear un contenedor vacío como hijo del Canvas
        GameObject containerGO = new GameObject("ZoomContainer");
        RectTransform containerRT = containerGO.AddComponent<RectTransform>();
        containerGO.transform.SetParent(canvas.transform, false);

        // Hacer que el contenedor cubra todo el Canvas
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.sizeDelta = Vector2.zero;
        containerRT.anchoredPosition = Vector2.zero;
        containerRT.localScale = Vector3.one;

        // Mover todos los hijos directos del Canvas al contenedor
        // (salvo el propio contenedor que acabamos de crear)
        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in canvas.transform)
        {
            if (child != containerRT.transform)
                children.Add(child);
        }
        foreach (Transform child in children)
        {
            child.SetParent(containerRT, true);
        }

        // Mover el contenedor al principio de la jerarquía
        containerRT.SetAsFirstSibling();

        _container = containerRT;
        _initialized = true;

        Debug.Log("[CanvasZoomController] Inicializado. Contenedor de zoom creado.");
    }

    // =========================================================
    //  API PÚBLICA
    // =========================================================

    /// <summary>
    /// Zoom in hacia el lado derecho de la pantalla (donde está Logan).
    /// scale: factor de escala (1.25 = 25% más cercano)
    /// duration: duración en segundos
    /// pivotX, pivotY: punto focal (0..1) donde 0.75 = 75% a la derecha
    /// </summary>
    public void ZoomTo(float scale, float duration, float pivotX = 0.75f, float pivotY = 0.45f)
    {
        if (!_initialized || _container == null)
        {
            Debug.LogWarning("[CanvasZoomController] No inicializado aún. Espera un frame.");
            return;
        }
        if (_zoomCo != null) StopCoroutine(_zoomCo);
        _zoomCo = StartCoroutine(ZoomRoutine(scale, duration, pivotX, pivotY));
    }

    /// <summary>
    /// Llamado desde VNDialogue cuando el CSV tiene ZOOM=valor.
    /// </summary>
    public static void ApplyCommand(string value)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[CanvasZoomController] No hay instancia. Añade un CanvasZoomController en la escena.");
            return;
        }

        value = value.Trim().ToUpper();
        float targetScale = 1.25f;
        float duration = 5f;

        if (value == "RESET")
        {
            Instance.ZoomTo(1f, 0.8f, 0.5f, 0.5f);
            return;
        }

        if (value.Contains(":"))
        {
            string[] parts = value.Split(':');
            float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out targetScale);
            if (parts.Length >= 2)
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out duration);
        }

        // ZOOM=RIGHT ó ZOOM=1 → valores por defecto (hacia Logan a la derecha)
        Instance.ZoomTo(targetScale, duration, 0.75f, 0.45f);
    }

    // =========================================================
    //  CORRUTINA DE ZOOM
    // =========================================================
    private IEnumerator ZoomRoutine(float targetScale, float duration, float pivotX, float pivotY)
    {
        Vector3 startScale = _container.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        Vector2 startPos = _container.anchoredPosition;

        // Al escalar desde el centro, calculamos el offset para simular
        // que el pivot del zoom es el punto de Logan (derecha)
        // offset = (pivot_normalizado - 0.5) * tamaño_canvas * (escala - 1)
        float refW = Screen.width;
        float refH = Screen.height;
        float dx = -(pivotX - 0.5f) * refW * (targetScale - 1f);
        float dy = -(pivotY - 0.5f) * refH * (targetScale - 1f);
        Vector2 targetPos = new Vector2(dx, dy);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float smooth = k * k * (3f - 2f * k);

            _container.localScale = Vector3.Lerp(startScale, endScale, smooth);
            _container.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);
            yield return null;
        }

        _container.localScale = endScale;
        _container.anchoredPosition = targetPos;
        _zoomCo = null;
    }
}
