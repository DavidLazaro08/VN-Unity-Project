using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VNSingleCharacterSlot : MonoBehaviour
{
    // =========================================================
    //  REFERENCIAS UI (slot central)
    // =========================================================

    [Header("Slot de personaje central (UI Image)")]
    public Image centerImage;

    // =========================================================
    //  ANIMACIÓN / FOCO
    // =========================================================

    [Header("Animación / foco")]

    [Tooltip("Tiempo de desplazamiento al entrar/salir (más alto = más lento)")]
    public float showMoveTime = 0.25f;

    [Tooltip("Alpha del personaje (siempre visible en escenas de un solo personaje)")]
    [Range(0f, 1f)]
    public float litAlpha = 1f;

    // =========================================================
    //  POSICIONES (solo X)
    // =========================================================

    [Header("Posiciones (X)")]
    public float centerShownX = 0f;      // Centrado
    public float centerHiddenX = -1400f; // Fuera de pantalla (izquierda)

    // =========================================================
    //  IDLE MOTION (micro movimiento)
    // =========================================================

    [Header("Idle Motion (micro movimiento)")]
    public bool idleMotion = true;
    public float idleYAmplitude = 5f;
    public float idleSpeed = 1.1f;
    public float idleScaleAmplitude = 0.008f;

    // =========================================================
    //  CATÁLOGO DE SPRITES (id + pose)
    // =========================================================

    [Header("Catálogo de sprites (id + pose)")]
    public List<CharacterPoseEntry> catalog = new();

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================

    private RectTransform centerRT;

    private float centerVel;
    private float centerTargetX;

    private bool centerVisible = false;
    private string currentCenterId = "";

    private float centerBaseY;
    private Vector3 centerBaseScale = Vector3.one;

    private float centerPhase;

    private const float snapEpsilon = 0.5f;

    private void Awake()
    {
        if (centerImage != null) centerRT = centerImage.GetComponent<RectTransform>();

        centerTargetX = centerShownX;

        // Guardamos Y y escala base (lo que tengas en el editor)
        if (centerRT != null)
        {
            centerBaseY = centerRT.anchoredPosition.y;
            centerBaseScale = centerRT.localScale;
        }

        centerPhase = UnityEngine.Random.Range(0f, 10f);

        // Arranque limpio
        HideCenterInstant();
    }

    private void Start()
    {
        // Validación crítica en runtime
        if (centerImage == null)
        {
            Debug.LogError("[VNSingleCharacterSlot] CENTER IMAGE NO ASIGNADO en el Inspector. El personaje no se mostrará. Asigna el Image del objeto CharacterCenter al campo 'Center Image'.", this);
        }
        else
        {
            Debug.Log($"[VNSingleCharacterSlot] Center Image asignado correctamente: {centerImage.name}", this);
        }

        // Validación del catálogo
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("[VNSingleCharacterSlot] El catálogo de sprites está vacío. No se podrán mostrar personajes.", this);
        }
        else
        {
            Debug.Log($"[VNSingleCharacterSlot] Catálogo cargado con {catalog.Count} sprites.", this);
        }
    }

    private void Update()
    {
        // ---------------------------------------------------------
        // Entrada/salida por X (con snap para evitar "tic")
        // ---------------------------------------------------------

        if (centerRT != null)
        {
            var p = centerRT.anchoredPosition;

            if (Mathf.Abs(p.x - centerTargetX) > snapEpsilon)
            {
                p.x = Mathf.SmoothDamp(p.x, centerTargetX, ref centerVel, showMoveTime);
            }
            else
            {
                p.x = centerTargetX;
                centerVel = 0f;
            }

            centerRT.anchoredPosition = p;
        }

        ApplyIdleMotion();
        AutoDisableWhenHidden();
    }

    // =========================================================
    //  API PÚBLICA (compatible con VNDialogue)
    // =========================================================

    public void ApplyCmd(string cmdRaw)
    {
        if (string.IsNullOrWhiteSpace(cmdRaw))
        {
            Debug.Log("[VNSingleCharacterSlot] ApplyCmd llamado con comando vacío.");
            return;
        }

        Debug.Log($"[VNSingleCharacterSlot] ApplyCmd recibido: '{cmdRaw}'");

        // Limpiar comillas del CSV
        cmdRaw = cmdRaw.Trim().Trim('"');

        string[] tokens = cmdRaw.Split(';');
        foreach (var t in tokens)
        {
            string token = t.Trim();
            if (token.Length == 0) continue;

            // Acepta L=, C=, o R= (todos muestran en el centro)
            if (token.StartsWith("L=", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("C=", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("R=", StringComparison.OrdinalIgnoreCase))
            {
                string prefix = token.Substring(0, 2);
                string value = token.Substring(2).Trim();
                Debug.Log($"[VNSingleCharacterSlot] Comando detectado: {prefix} -> '{value}' (se aplicará al CENTER)");
                HandleSlotCommand(value);
            }
        }
    }

    public void ApplyFocus(string speakerUpper)
    {
        // En escenas de un solo personaje, siempre está iluminado
        if (centerVisible)
            SetCenterLit(true);
    }

    public void NarratorMoment()
    {
        // En escenas de un solo personaje, no atenuamos
        // (el personaje permanece visible para dramatismo)
    }

    // =========================================================
    //  CMD (L= / C= / R=)
    // =========================================================

    private void HandleSlotCommand(string value)
    {
        if (value.Equals("HIDE", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[VNSingleCharacterSlot] Comando HIDE recibido, ocultando personaje central.");
            HideCenter();
            return;
        }

        string id;
        string pose;

        int colon = value.IndexOf(':');
        if (colon >= 0)
        {
            id = value[..colon].Trim().ToUpper();
            pose = value[(colon + 1)..].Trim().ToLower();
        }
        else
        {
            id = value.Trim().ToUpper();
            pose = "normal";
        }

        Debug.Log($"[VNSingleCharacterSlot] Buscando sprite: ID='{id}', POSE='{pose}'");

        Sprite spr = FindSprite(id, pose);
        if (spr == null)
        {
            Debug.LogWarning($"[VNSingleCharacterSlot] NO SE ENCONTRÓ sprite para {id}:{pose} en el catálogo. Verifica que el catálogo tenga esta combinación.", this);
            Debug.Log($"[VNSingleCharacterSlot] Catálogo actual tiene {catalog.Count} entradas.");
            return;
        }

        Debug.Log($"[VNSingleCharacterSlot] Sprite encontrado: '{spr.name}'. Aplicando al CENTER.");
        SetCenterSprite(id, spr);
        ShowCenter();
    }

    // =========================================================
    //  SPRITES / SLOTS
    // =========================================================

    private Sprite FindSprite(string idUpper, string poseLower)
    {
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("[VNSingleCharacterSlot] FindSprite: catálogo vacío o null.");
            return null;
        }

        foreach (var e in catalog)
        {
            if (e == null || e.sprite == null) continue;

            if (e.id.Trim().ToUpper() == idUpper &&
                e.pose.Trim().ToLower() == poseLower)
            {
                Debug.Log($"[VNSingleCharacterSlot] FindSprite: MATCH encontrado -> {e.id}:{e.pose} = {e.sprite.name}");
                return e.sprite;
            }
        }
        
        Debug.LogWarning($"[VNSingleCharacterSlot] FindSprite: NO se encontró match para {idUpper}:{poseLower}");
        return null;
    }

    private void SetCenterSprite(string idUpper, Sprite spr)
    {
        if (centerImage == null)
        {
            Debug.LogError("[VNSingleCharacterSlot] SetCenterSprite: centerImage es NULL. No se puede aplicar el sprite.", this);
            return;
        }

        Debug.Log($"[VNSingleCharacterSlot] SetCenterSprite: Aplicando sprite '{spr.name}' al Image '{centerImage.name}'");

        centerImage.sprite = spr;
        centerImage.enabled = true;
        centerVisible = true;
        currentCenterId = idUpper;

        var c = centerImage.color;
        c.a = litAlpha;
        centerImage.color = c;

        Debug.Log($"[VNSingleCharacterSlot] Sprite aplicado exitosamente. Visible={centerVisible}, Alpha={c.a}");
    }

    // =========================================================
    //  SHOW / HIDE
    // =========================================================

    private void ShowCenter() => centerTargetX = centerShownX;

    private void HideCenter()
    {
        centerTargetX = centerHiddenX;
        centerVisible = false;
        currentCenterId = "";
        centerVel = 0f;
    }

    private void HideCenterInstant()
    {
        if (centerRT == null) return;
        centerRT.anchoredPosition = new Vector2(centerHiddenX, centerBaseY);
        centerRT.localScale = centerBaseScale;
        centerTargetX = centerHiddenX;
        centerVisible = false;
        if (centerImage) centerImage.enabled = false;
    }

    private void SetCenterLit(bool lit)
    {
        if (!centerImage) return;
        var c = centerImage.color;
        c.a = lit ? litAlpha : litAlpha; // Siempre lit en escenas de un personaje
        centerImage.color = c;
    }

    // =========================================================
    //  IDLE MOTION
    // =========================================================

    private void ApplyIdleMotion()
    {
        if (!idleMotion) return;

        if (centerRT && centerVisible)
        {
            float t = Time.time * idleSpeed + centerPhase;
            centerRT.anchoredPosition = new Vector2(centerRT.anchoredPosition.x, centerBaseY + Mathf.Sin(t) * idleYAmplitude);
            centerRT.localScale = centerBaseScale * (1f + Mathf.Sin(t * 0.9f) * idleScaleAmplitude);
        }
    }

    private void AutoDisableWhenHidden()
    {
        const float epsilon = 1.5f;

        if (centerRT && !centerVisible && centerImage && centerImage.enabled &&
            Mathf.Abs(centerRT.anchoredPosition.x - centerHiddenX) < epsilon)
            centerImage.enabled = false;
    }
}
