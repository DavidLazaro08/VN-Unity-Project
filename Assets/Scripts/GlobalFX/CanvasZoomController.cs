using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CanvasZoomController
/// ------------------------------------------------------------
/// Esto lo uso para hacer un “zoom” suave en la interfaz de la VN,
/// como si acercáramos cámara a una zona (por ejemplo, hacia Logan).
///
/// La idea: creo un contenedor (ZoomContainer) dentro del Canvas y meto
/// dentro todos los elementos del Canvas. Luego escalo + desplazo ese contenedor.
///
/// Uso desde CSV:
///   - ZOOM=RIGHT      -> zoom por defecto hacia la derecha
///   - ZOOM=RESET      -> vuelve a normal
///   - ZOOM=1.3:5      -> escala 1.3 durante 5 segundos
/// </summary>
public class CanvasZoomController : MonoBehaviour
{
    // =========================================================
    //  SINGLETON (para poder llamarlo fácil desde comandos)
    // =========================================================
    public static CanvasZoomController Instance { get; private set; }

    // =========================================================
    //  ESTADO
    // =========================================================
    private RectTransform _container;  // Contenedor real que se escala
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
        // Pillamos el Canvas de la escena (el primero que encuentre).
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[CanvasZoomController] No se encontró Canvas en la escena.");
            return;
        }

        // Creamos un contenedor vacío para “agarrar” todo lo que haya en el Canvas.
        GameObject containerGO = new GameObject("ZoomContainer");
        RectTransform containerRT = containerGO.AddComponent<RectTransform>();
        containerGO.transform.SetParent(canvas.transform, false);

        // Lo hacemos full-screen dentro del Canvas.
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.sizeDelta = Vector2.zero;
        containerRT.anchoredPosition = Vector2.zero;
        containerRT.localScale = Vector3.one;

        // Movemos todos los hijos directos del Canvas dentro del contenedor,
        // así el zoom afecta a todo (diálogo, nombres, UI, etc.).
        List<Transform> children = new List<Transform>();
        foreach (Transform child in canvas.transform)
        {
            if (child != containerRT.transform)
                children.Add(child);
        }

        foreach (Transform child in children)
        {
            child.SetParent(containerRT, true);
        }

        // Lo ponemos el primero para que quede “de base” en la jerarquía.
        containerRT.SetAsFirstSibling();

        _container = containerRT;
        _initialized = true;

        Debug.Log("[CanvasZoomController] OK. ZoomContainer creado.");
    }

    // =========================================================
    //  API
    // =========================================================

    /// <summary>
    /// Hace un zoom hacia un punto (pivotX/pivotY).
    /// pivotX/pivotY van de 0..1 (0.75 sería bastante a la derecha).
    /// </summary>
    public void ZoomTo(float scale, float duration, float pivotX = 0.75f, float pivotY = 0.45f)
    {
        if (!_initialized || _container == null)
        {
            Debug.LogWarning("[CanvasZoomController] Aún no está inicializado (espera un frame).");
            return;
        }

        if (_zoomCo != null) StopCoroutine(_zoomCo);
        _zoomCo = StartCoroutine(ZoomRoutine(scale, duration, pivotX, pivotY));
    }

    /// <summary>
    /// Entrada cómoda para comandos tipo "ZOOM=RIGHT" desde VNDialogue.
    /// </summary>
    public static void ApplyCommand(string value)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[CanvasZoomController] No hay instancia en escena. Añade el script a un GameObject.");
            return;
        }

        value = (value ?? "").Trim().ToUpper();

        float targetScale = 1.25f;
        float duration = 5f;

        if (value == "RESET")
        {
            Instance.ZoomTo(1f, 0.8f, 0.5f, 0.5f);
            return;
        }

        // Formato: "1.3:5" (escala:segundos)
        if (value.Contains(":"))
        {
            string[] parts = value.Split(':');

            float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out targetScale);

            if (parts.Length >= 2)
            {
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out duration);
            }
        }

        // RIGHT (o cualquier otro valor simple) -> defaults hacia la derecha.
        Instance.ZoomTo(targetScale, duration, 0.75f, 0.45f);
    }

    // =========================================================
    //  CORRUTINA
    // =========================================================
    private IEnumerator ZoomRoutine(float targetScale, float duration, float pivotX, float pivotY)
    {
        Vector3 startScale = _container.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        Vector2 startPos = _container.anchoredPosition;

        // Simulamos “pivot” moviendo el contenedor según el punto objetivo.
        // (pivot - 0.5) * tamaño * (escala - 1)
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