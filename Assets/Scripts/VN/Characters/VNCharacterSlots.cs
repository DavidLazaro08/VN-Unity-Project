using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VNCharacterSlots : MonoBehaviour
{
    // =========================================================
    //  REFERENCIAS UI (slots)
    // =========================================================

    [Header("Slots de personajes (UI Images)")]
    public Image leftImage;
    public Image rightImage;

    // =========================================================
    //  ANIMACIÓN / FOCO
    // =========================================================

    [Header("Animación / foco")]

    [Tooltip("Tiempo de desplazamiento al entrar/salir (más alto = más lento)")]
    public float showMoveTime = 0.25f;

    [Tooltip("Activar/Desactivar el efecto de foco (atenuar el que NO habla)")]
    public bool useFocusDimming = true;

    [Tooltip("Alpha del que NO habla (si useFocusDimming = true)")]
    [Range(0f, 1f)]
    public float dimAlpha = 0.80f;

    [Tooltip("Alpha del que habla")]
    [Range(0f, 1f)]
    public float litAlpha = 1f;

    [Tooltip("Si true: cuando habla NARRADOR, atenúa a ambos (solo si useFocusDimming = true)")]
    public bool narratorDimsBoth = false;

    // =========================================================
    //  POSICIONES (solo X)
    // =========================================================

    [Header("Posiciones (X)")]
    public float leftShownX = 180f;
    public float rightShownX = -180f;
    public float leftHiddenX = -1400f;
    public float rightHiddenX = 1400f;

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

    private RectTransform leftRT;
    private RectTransform rightRT;

    private float leftVel;
    private float rightVel;

    private float leftTargetX;
    private float rightTargetX;

    private bool leftVisible = false;
    private bool rightVisible = false;

    private string currentLeftId = "";
    private string currentRightId = "";
    private string currentLeftPose = "";
    private string currentRightPose = "";

    private float leftBaseY;
    private float rightBaseY;

    private Vector3 leftBaseScale = Vector3.one;
    private Vector3 rightBaseScale = Vector3.one;

    private float leftPhase;
    private float rightPhase;

    private const float snapEpsilon = 0.5f;

    // Coroutines de swap (para cancelar si llega otro comando)
    private Coroutine leftSwapRoutine;
    private Coroutine rightSwapRoutine;

    private void Awake()
    {
        if (leftImage != null) leftRT = leftImage.GetComponent<RectTransform>();
        if (rightImage != null) rightRT = rightImage.GetComponent<RectTransform>();

        // AUTO-FIX: Si en el Inspector se quedaron los valores viejos (130), forzamos los nuevos (180)
        if (Mathf.Abs(leftShownX - 130f) < 1f) leftShownX = 180f;
        if (Mathf.Abs(rightShownX + 130f) < 1f) rightShownX = -180f;

        leftTargetX = leftShownX;
        rightTargetX = rightShownX;

        // Guardamos Y y escala base (lo que tengas en el editor)
        if (leftRT != null)
        {
            leftBaseY = leftRT.anchoredPosition.y;
            leftBaseScale = leftRT.localScale;
        }

        if (rightRT != null)
        {
            rightBaseY = rightRT.anchoredPosition.y;
            rightBaseScale = rightRT.localScale;
        }

        leftPhase = UnityEngine.Random.Range(0f, 10f);
        rightPhase = UnityEngine.Random.Range(0f, 10f);

        // Arranque limpio
        HideLeftInstant();
        HideRightInstant();
    }

    private void Update()
    {
        // ---------------------------------------------------------
        // Entrada/salida por X (con snap para evitar “tic”)
        // ---------------------------------------------------------

        if (leftRT != null)
        {
            var p = leftRT.anchoredPosition;

            if (Mathf.Abs(p.x - leftTargetX) > snapEpsilon)
            {
                p.x = Mathf.SmoothDamp(p.x, leftTargetX, ref leftVel, showMoveTime);
            }
            else
            {
                p.x = leftTargetX;
                leftVel = 0f;
            }

            leftRT.anchoredPosition = p;
        }

        if (rightRT != null)
        {
            var p = rightRT.anchoredPosition;

            if (Mathf.Abs(p.x - rightTargetX) > snapEpsilon)
            {
                p.x = Mathf.SmoothDamp(p.x, rightTargetX, ref rightVel, showMoveTime);
            }
            else
            {
                p.x = rightTargetX;
                rightVel = 0f;
            }

            rightRT.anchoredPosition = p;
        }

        ApplyIdleMotion();
        AutoDisableWhenHidden();
    }

    // =========================================================
    //  API PÚBLICA (esto lo llamará luego VNDialogue)
    // =========================================================

    public void ApplyCmd(string cmdRaw)
    {
        if (string.IsNullOrWhiteSpace(cmdRaw)) return;

        // Limpiar comillas del CSV (vital si el parser las deja)
        cmdRaw = cmdRaw.Trim().Trim('"');

        string[] tokens = cmdRaw.Split(';');
        foreach (var t in tokens)
        {
            string token = t.Trim();
            if (token.Length == 0) continue;

            if (token.StartsWith("L=", StringComparison.OrdinalIgnoreCase))
                HandleSlotCommand(true, token.Substring(2).Trim());
            else if (token.StartsWith("R=", StringComparison.OrdinalIgnoreCase))
                HandleSlotCommand(false, token.Substring(2).Trim());
        }
    }

    public void ApplyFocus(string speakerUpper)
    {
        if (!useFocusDimming) return;
        if (string.IsNullOrEmpty(speakerUpper)) return;

        if (leftVisible && rightVisible)
        {
            if (speakerUpper == currentLeftId)
            {
                SetLeftLit(true);
                SetRightLit(false);
            }
            else if (speakerUpper == currentRightId)
            {
                SetLeftLit(false);
                SetRightLit(true);
            }
        }
        else if (leftVisible)
        {
            SetLeftLit(true);
        }
        else if (rightVisible)
        {
            SetRightLit(true);
        }
    }

    public void NarratorMoment()
    {
        if (!useFocusDimming || !narratorDimsBoth) return;

        if (leftVisible) SetLeftLit(false);
        if (rightVisible) SetRightLit(false);
    }

    // =========================================================
    //  CMD (L= / R=)
    // =========================================================

    private void HandleSlotCommand(bool isLeft, string value)
    {
        // ── HIDE / OFF ──────────────────────────────────────
        if (value.Equals("HIDE", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("OFF",  StringComparison.OrdinalIgnoreCase))
        {
            // Cancelar swap en curso si lo hay
            CancelSwap(isLeft);
            if (isLeft) HideLeft(); else HideRight();
            return;
        }

        // ── Parsear ID:POSE ─────────────────────────────────
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

        CharacterPoseEntry entry = FindEntry(id, pose);
        if (entry == null)
        {
            Debug.LogWarning($"No hay sprite para {id}:{pose} en el catálogo.");
            return;
        }

        // ── Decidir tipo de transición ──────────────────────
        string currentId   = isLeft ? currentLeftId   : currentRightId;
        bool   slotVisible  = isLeft ? leftVisible     : rightVisible;

        bool sameCharacter = !string.IsNullOrEmpty(currentId) &&
                             currentId == id;

        if (sameCharacter)
        {
            // ➊ Mismo personaje → solo cambiar pose (sin animación)
            if (isLeft)  { SetLeftSprite(id, entry);  currentLeftPose  = pose; }
            else         { SetRightSprite(id, entry); currentRightPose = pose; }
        }
        else if (slotVisible)
        {
            // ➋ Personaje diferente y slot visible → SWAP animado
            CancelSwap(isLeft);
            if (isLeft)
                leftSwapRoutine  = StartCoroutine(SwapCharacterRoutine(true,  id, pose, entry));
            else
                rightSwapRoutine = StartCoroutine(SwapCharacterRoutine(false, id, pose, entry));
        }
        else
        {
            // ➌ Slot vacío → primera aparición con slide-in
            if (isLeft)
            {
                SetLeftSprite(id, entry);
                currentLeftPose = pose;
                ShowLeft();
            }
            else
            {
                SetRightSprite(id, entry);
                currentRightPose = pose;
                ShowRight();
            }
        }
    }

    // =========================================================
    //  SWAP ANIMADO (coroutine)
    // =========================================================

    /// <summary>
    /// 1. Slide-out del personaje actual
    /// 2. Swap de sprite
    /// 3. Slide-in del personaje nuevo
    /// </summary>
    private IEnumerator SwapCharacterRoutine(bool isLeft, string newId, string newPose, CharacterPoseEntry entry)
    {
        // ── Paso 1: Slide-out ────────────────────────────────
        if (isLeft)
        {
            leftTargetX = leftHiddenX;
            leftVel = 0f;
        }
        else
        {
            rightTargetX = rightHiddenX;
            rightVel = 0f;
        }

        // Esperar a que la posición llegue al destino oculto
        yield return StartCoroutine(WaitForSlotHidden(isLeft));

        // ── Paso 2: Swap de sprite (invisible) ──────────────
        if (isLeft)
        {
            if (leftImage) leftImage.enabled = false;
            SetLeftSprite(newId, entry);
            currentLeftPose = newPose;
        }
        else
        {
            if (rightImage) rightImage.enabled = false;
            SetRightSprite(newId, entry);
            currentRightPose = newPose;
        }

        // ── Paso 3: Slide-in ─────────────────────────────────
        if (isLeft)
            ShowLeft();
        else
            ShowRight();

        // Limpiar referencia a la coroutine
        if (isLeft) leftSwapRoutine = null;
        else        rightSwapRoutine = null;
    }

    /// <summary>Espera hasta que el slot alcance su posición oculta.</summary>
    private IEnumerator WaitForSlotHidden(bool isLeft)
    {
        const float threshold = 5f; // margen aceptable para considerar "llegó"

        while (true)
        {
            if (isLeft && leftRT != null)
            {
                if (Mathf.Abs(leftRT.anchoredPosition.x - leftHiddenX) < threshold)
                    yield break;
            }
            else if (!isLeft && rightRT != null)
            {
                if (Mathf.Abs(rightRT.anchoredPosition.x - rightHiddenX) < threshold)
                    yield break;
            }
            else
            {
                yield break; // sin RT, no hay nada que esperar
            }

            yield return null; // siguiente frame
        }
    }

    /// <summary>Cancela un swap en curso si existe.</summary>
    private void CancelSwap(bool isLeft)
    {
        if (isLeft && leftSwapRoutine != null)
        {
            StopCoroutine(leftSwapRoutine);
            leftSwapRoutine = null;
        }
        else if (!isLeft && rightSwapRoutine != null)
        {
            StopCoroutine(rightSwapRoutine);
            rightSwapRoutine = null;
        }
    }

    // =========================================================
    //  SPRITES / SLOTS
    // =========================================================

    private CharacterPoseEntry FindEntry(string idUpper, string poseLower)
    {
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("[VNCharacterSlots] FindEntry: catálogo vacío o null.");
            return null;
        }

        // Debug específico para encontrar el fallo de 'amenaza'
        bool debugDeep = (poseLower.Contains("amenaza"));

        for (int i = 0; i < catalog.Count; i++)
        {
            var e = catalog[i];
            if (e == null || e.sprite == null) continue;

            // PROTECCIÓN CONTRA NULOS (Vital para evitar NRE si hay entradas vacías)
            if (string.IsNullOrEmpty(e.id) || string.IsNullOrEmpty(e.pose))
            {
                Debug.LogWarning($"[VNCharacterSlots] Entrada de catálogo {i} tiene ID o Pose vacíos. Ignorando.");
                continue;
            }

            string catId = e.id.Trim().ToUpper();
            string catPose = e.pose.Trim().ToLower();

            if (debugDeep)
            {
                Debug.Log($"[VNCharacterSlots] Comparando [{i}]: Cat('{catId}':'{catPose}') vs Buscado('{idUpper}':'{poseLower}')");
            }

            if (catId == idUpper && catPose == poseLower)
            {
                if (debugDeep) Debug.Log(" -> MATCH!");
                return e;
            }
        }
        
        Debug.LogWarning($"[VNCharacterSlots] FindEntry: NO se encontró match para {idUpper}:{poseLower}");
        return null;
    }

    private void SetLeftSprite(string idUpper, CharacterPoseEntry entry)
    {
        if (leftImage == null) return;

        leftImage.sprite = entry.sprite;
        leftImage.enabled = true;
        leftVisible = true;
        currentLeftId = idUpper;
        
        // Aplicar escala personalizada (baseScale * entry.scale)
        leftBaseScale = Vector3.one * entry.scale; 
        // Nota: Si quieres mantener escalado original del rect, usa leftRT.localScale = original * entry.scale
        // Pero aquí asumimos que reseteamos a scale
        if (leftRT != null) leftRT.localScale = leftBaseScale;

        var c = leftImage.color;
        c.a = litAlpha;
        leftImage.color = c;
    }

    private void SetRightSprite(string idUpper, CharacterPoseEntry entry)
    {
        if (rightImage == null) return;

        rightImage.sprite = entry.sprite;
        rightImage.enabled = true;
        rightVisible = true;
        currentRightId = idUpper;

        // Aplicar escala personalizada
        rightBaseScale = Vector3.one * entry.scale;
        if (rightRT != null) rightRT.localScale = rightBaseScale;

        var c = rightImage.color;
        c.a = litAlpha;
        rightImage.color = c;
    }

    // =========================================================
    //  SHOW / HIDE
    // =========================================================

    private void ShowLeft() => leftTargetX = leftShownX;
    private void ShowRight() => rightTargetX = rightShownX;

    private void HideLeft()
    {
        leftTargetX = leftHiddenX;
        leftVisible = false;
        currentLeftId = "";
        currentLeftPose = "";
        leftVel = 0f;
    }

    private void HideRight()
    {
        rightTargetX = rightHiddenX;
        rightVisible = false;
        currentRightId = "";
        currentRightPose = "";
        rightVel = 0f;
    }

    private void HideLeftInstant()
    {
        if (leftRT == null) return;
        leftRT.anchoredPosition = new Vector2(leftHiddenX, leftBaseY);
        leftRT.localScale = leftBaseScale;
        leftTargetX = leftHiddenX;
        leftVisible = false;
        if (leftImage) leftImage.enabled = false;
    }

    private void HideRightInstant()
    {
        if (rightRT == null) return;
        rightRT.anchoredPosition = new Vector2(rightHiddenX, rightBaseY);
        rightRT.localScale = rightBaseScale;
        rightTargetX = rightHiddenX;
        rightVisible = false;
        if (rightImage) rightImage.enabled = false;
    }

    private void SetLeftLit(bool lit)
    {
        if (!leftImage) return;
        var c = leftImage.color;
        c.a = lit ? litAlpha : dimAlpha;
        leftImage.color = c;
    }

    private void SetRightLit(bool lit)
    {
        if (!rightImage) return;
        var c = rightImage.color;
        c.a = lit ? litAlpha : dimAlpha;
        rightImage.color = c;
    }

    // =========================================================
    //  IDLE MOTION
    // =========================================================

    private void ApplyIdleMotion()
    {
        if (!idleMotion) return;

        if (leftRT && leftVisible)
        {
            float t = Time.time * idleSpeed + leftPhase;
            leftRT.anchoredPosition = new Vector2(leftRT.anchoredPosition.x, leftBaseY + Mathf.Sin(t) * idleYAmplitude);
            leftRT.localScale = leftBaseScale * (1f + Mathf.Sin(t * 0.9f) * idleScaleAmplitude);
        }

        if (rightRT && rightVisible)
        {
            float t = Time.time * idleSpeed + rightPhase;
            rightRT.anchoredPosition = new Vector2(rightRT.anchoredPosition.x, rightBaseY + Mathf.Sin(t) * idleYAmplitude);
            rightRT.localScale = rightBaseScale * (1f + Mathf.Sin(t * 0.9f) * idleScaleAmplitude);
        }
    }

    private void AutoDisableWhenHidden()
    {
        const float epsilon = 1.5f;

        if (leftRT && !leftVisible && leftImage && leftImage.enabled &&
            Mathf.Abs(leftRT.anchoredPosition.x - leftHiddenX) < epsilon)
            leftImage.enabled = false;

        if (rightRT && !rightVisible && rightImage && rightImage.enabled &&
            Mathf.Abs(rightRT.anchoredPosition.x - rightHiddenX) < epsilon)
            rightImage.enabled = false;
    }
}
