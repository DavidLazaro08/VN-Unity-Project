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

    // Coroutines de fade-out
    private Coroutine leftFadeRoutine;
    private Coroutine rightFadeRoutine;
    private const float FADE_OUT_DURATION = 1.5f;

    // IDs de personajes que usan glitch al cambiar de pose
    private static readonly HashSet<string> glitchCharacterIds = new HashSet<string>
    {
        "SILUETA", "SOMBRA"
    };

    // Coroutines de glitch
    private Coroutine leftGlitchRoutine;
    private Coroutine rightGlitchRoutine;

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
    //  ZOOM SPRITE (para efectos cinematográficos)
    // =========================================================

    private Coroutine _rightZoomCo;

    /// <summary>
    /// Hace un zoom in suave en el sprite del slot derecho.
    /// targetScale: escala final (ej. 1.3f).
    /// duration: segundos para llegar.
    /// También desplaza el sprite un poco hacia el centro para dar sensación de acercamiento.
    /// </summary>
    public void ZoomRightSprite(float targetScale, float duration)
    {
        if (rightRT == null) return;
        if (_rightZoomCo != null) StopCoroutine(_rightZoomCo);
        _rightZoomCo = StartCoroutine(ZoomSpriteRoutine(rightRT, rightBaseScale, targetScale, duration, rightBaseY));
    }

    private IEnumerator ZoomSpriteRoutine(RectTransform rt, Vector3 baseScale, float targetScale, float duration, float baseY)
    {
        Vector3 startScale = rt.localScale;
        Vector3 endScale = baseScale * targetScale;

        // Además del zoom de escala, desplazamos el sprite hacia el centro (táctica cinematográfica de zoom-to-subject)
        Vector2 startPos = rt.anchoredPosition;
        // Desplazamos hacia la izquierda (centro) una fracción del ancho
        Vector2 endPos = new Vector2(rt.anchoredPosition.x * 0.7f, baseY);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float smooth = k * k * (3f - 2f * k);

            // Actualizamos rightBaseScale temporalmente para que idle motion use la nueva base
            // y no lute contra el zoom. Recalculamos desde la escala zoomed.
            rt.localScale = Vector3.Lerp(startScale, endScale, smooth);
            rt.anchoredPosition = new Vector2(
                Mathf.Lerp(startPos.x, endPos.x, smooth),
                rt.anchoredPosition.y   // mantener Y del idle motion
            );

            yield return null;
        }

        // Actualizar la base de escala con el zoom final para que idle motion no luche
        rightBaseScale = endScale;
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
        // ── HIDE / OFF ──────────────────────────────────────────
        if (value.Equals("HIDE", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("OFF",  StringComparison.OrdinalIgnoreCase))
        {
            // Cancelar swap en curso si lo hay
            CancelSwap(isLeft);
            CancelFade(isLeft);
            if (isLeft) HideLeft(); else HideRight();
            return;
        }

        // ── FADE (desaparecer gradual) ────────────────────────────
        if (value.Equals("FADE", StringComparison.OrdinalIgnoreCase))
        {
            CancelSwap(isLeft);
            CancelFade(isLeft);
            if (isLeft)
                leftFadeRoutine = StartCoroutine(FadeOutSlot(true));
            else
                rightFadeRoutine = StartCoroutine(FadeOutSlot(false));
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
            // ➁ Mismo personaje → cambiar pose
            // Si es personaje con glitch, usar transición especial
            if (glitchCharacterIds.Contains(id))
            {
                CancelGlitch(isLeft);
                if (isLeft)
                    leftGlitchRoutine = StartCoroutine(GlitchPoseSwap(true, id, pose, entry));
                else
                    rightGlitchRoutine = StartCoroutine(GlitchPoseSwap(false, id, pose, entry));
            }
            else
            {
                if (isLeft)  { SetLeftSprite(id, entry);  currentLeftPose  = pose; }
                else         { SetRightSprite(id, entry); currentRightPose = pose; }
            }
        }
        else if (slotVisible)
        {
            // ➂ Personaje diferente y slot visible → SWAP animado
            CancelSwap(isLeft);
            if (isLeft)
                leftSwapRoutine  = StartCoroutine(SwapCharacterRoutine(true,  id, pose, entry));
            else
                rightSwapRoutine = StartCoroutine(SwapCharacterRoutine(false, id, pose, entry));
        }
        else
        {
            // ➃ Slot vacío → primera aparición
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

            // No aplicamos glitch continuo a poses normales al aparecer
            // Solo si entra ya directamente en una pose inestable (como fragmentada)
            if (glitchCharacterIds.Contains(id) && pose.Contains("fragmentada"))
            {
                CancelGlitch(isLeft);
                if (isLeft)
                    leftGlitchRoutine = StartCoroutine(GlitchContinuous(true, true));
                else
                    rightGlitchRoutine = StartCoroutine(GlitchContinuous(false, true));
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

    /// <summary>Cancela un fade en curso si existe.</summary>
    private void CancelFade(bool isLeft)
    {
        if (isLeft && leftFadeRoutine != null)
        {
            StopCoroutine(leftFadeRoutine);
            leftFadeRoutine = null;
        }
        else if (!isLeft && rightFadeRoutine != null)
        {
            StopCoroutine(rightFadeRoutine);
            rightFadeRoutine = null;
        }
    }

    /// <summary>
    /// Fade out gradual del sprite del personaje (alpha → 0).
    /// Al terminar, oculta el slot limpiamente.
    /// </summary>
    private IEnumerator FadeOutSlot(bool isLeft)
    {
        Image img = isLeft ? leftImage : rightImage;
        if (img == null) yield break;

        Color c = img.color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < FADE_OUT_DURATION)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / FADE_OUT_DURATION);
            float smooth = k * k * (3f - 2f * k); // smoothstep

            c.a = Mathf.Lerp(startAlpha, 0f, smooth);
            img.color = c;
            yield return null;
        }

        c.a = 0f;
        img.color = c;

        // Ocultar slot limpiamente
        if (isLeft)
        {
            leftVisible = false;
            currentLeftId = "";
            currentLeftPose = "";
            img.enabled = false;
            leftFadeRoutine = null;
        }
        else
        {
            rightVisible = false;
            currentRightId = "";
            currentRightPose = "";
            img.enabled = false;
            rightFadeRoutine = null;
        }
    }

    // =========================================================
    //  GLITCH — Interferencia de señal (SILUETA / SOMBRA)
    // =========================================================

    private void CancelGlitch(bool isLeft)
    {
        if (isLeft && leftGlitchRoutine != null)
        {
            StopCoroutine(leftGlitchRoutine);
            leftGlitchRoutine = null;
        }
        else if (!isLeft && rightGlitchRoutine != null)
        {
            StopCoroutine(rightGlitchRoutine);
            rightGlitchRoutine = null;
        }
    }

    /// <summary>
    /// Glitch al cambiar de pose: flicker rápido + jitter horizontal,
    /// swap de sprite en medio del caos, luego estabilizar.
    /// </summary>
    private IEnumerator GlitchPoseSwap(bool isLeft, string id, string newPose, CharacterPoseEntry entry)
    {
        Image img = isLeft ? leftImage : rightImage;
        RectTransform rt = isLeft ? leftRT : rightRT;
        if (img == null || rt == null) yield break;

        float originalX = rt.anchoredPosition.x;
        Color c = img.color;
        float baseAlpha = c.a;

        // ── Fase 1: Interferencia (antes del swap) ──
        int flickerCount = 4;
        for (int i = 0; i < flickerCount; i++)
        {
            // Bajar alpha + desplazar
            c.a = UnityEngine.Random.Range(0.1f, 0.4f);
            img.color = c;
            rt.anchoredPosition = new Vector2(
                originalX + UnityEngine.Random.Range(-6f, 6f),
                rt.anchoredPosition.y
            );
            yield return new WaitForSeconds(0.04f);

            // Subir alpha
            c.a = UnityEngine.Random.Range(0.6f, baseAlpha);
            img.color = c;
            rt.anchoredPosition = new Vector2(
                originalX + UnityEngine.Random.Range(-3f, 3f),
                rt.anchoredPosition.y
            );
            yield return new WaitForSeconds(0.04f);
        }

        // ── Fase 2: Swap de sprite (en el momento de "peor señal") ──
        c.a = 0.1f;
        img.color = c;

        if (isLeft)  { SetLeftSprite(id, entry);  currentLeftPose  = newPose; }
        else         { SetRightSprite(id, entry); currentRightPose = newPose; }

        yield return new WaitForSeconds(0.03f);

        // ── Fase 3: Estabilización ──
        c = img.color;
        c.a = 0.5f;
        img.color = c;
        rt.anchoredPosition = new Vector2(originalX + UnityEngine.Random.Range(-2f, 2f), rt.anchoredPosition.y);
        yield return new WaitForSeconds(0.05f);

        c.a = baseAlpha;
        img.color = c;
        rt.anchoredPosition = new Vector2(originalX, rt.anchoredPosition.y);

        if (isLeft) leftGlitchRoutine = null;
        else        rightGlitchRoutine = null;

        // ── Fase 4: Glitch Continuo (solo para poses inestables) ──
        if (newPose.Contains("fragmentada"))
        {
            if (isLeft) leftGlitchRoutine = StartCoroutine(GlitchContinuous(true, true));
            else        rightGlitchRoutine = StartCoroutine(GlitchContinuous(false, true));
        }
    }

    /// <summary>
    /// Mantiene un nivel de glitch (flicker y jitter leve) constante.
    /// Si isIntense es true (ej: pose fragmentada), el efecto es más rápido y errático.
    /// </summary>
    private IEnumerator GlitchContinuous(bool isLeft, bool isIntense)
    {
        Image img = isLeft ? leftImage : rightImage;
        RectTransform rt = isLeft ? leftRT : rightRT;
        if (img == null || rt == null) yield break;

        float originalX = rt.anchoredPosition.x;
        Color c = img.color;
        float baseAlpha = c.a; // Usamos el alpha base (puede verse afectado por foco)

        // Parámetros de glitch según intensidad
        float jitterAmount = isIntense ? 8f : 3f;
        float flickerProb = isIntense ? 0.8f : 0.7f;
        float minAlphaDrop = isIntense ? 0.05f : 0.2f;
        
        float minWait = isIntense ? 0.02f : 0.05f;
        float maxWait = isIntense ? 0.06f : 0.15f;
        float resetWait = isIntense ? 0.01f : 0.1f;

        while (true) // Bucle infinito hasta que se cancele la corrutina
        {
            // Pequeño jitter
            rt.anchoredPosition = new Vector2(
                originalX + UnityEngine.Random.Range(-jitterAmount, jitterAmount),
                rt.anchoredPosition.y
            );

            // Flicker aleatorio
            if (UnityEngine.Random.value > (1f - flickerProb)) // probabilidad de bajar el alpha
            {
                c.a = UnityEngine.Random.Range(minAlphaDrop, 0.6f) * baseAlpha; // Alpha reducido
            }
            else
            {
                c.a = baseAlpha; // Alpha normal
            }
            img.color = c;

            // Esperar un frame aleatorio para que sea caótico
            yield return new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait));
            
            // Restablecer posición para que no se quede desplazado mucho tiempo
            rt.anchoredPosition = new Vector2(originalX, rt.anchoredPosition.y);
            yield return new WaitForSeconds(UnityEngine.Random.Range(resetWait, resetWait * 2f));
        }
    }



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
