using System;
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
    public float leftShownX = 130f;
    public float rightShownX = -130f;
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

    private float leftBaseY;
    private float rightBaseY;

    private Vector3 leftBaseScale = Vector3.one;
    private Vector3 rightBaseScale = Vector3.one;

    private float leftPhase;
    private float rightPhase;

    private const float snapEpsilon = 0.5f;

    private void Awake()
    {
        if (leftImage != null) leftRT = leftImage.GetComponent<RectTransform>();
        if (rightImage != null) rightRT = rightImage.GetComponent<RectTransform>();

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
        if (value.Equals("HIDE", StringComparison.OrdinalIgnoreCase))
        {
            if (isLeft) HideLeft(); else HideRight();
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

        Sprite spr = FindSprite(id, pose);
        if (spr == null)
        {
            Debug.LogWarning($"No hay sprite para {id}:{pose} en el catálogo.");
            return;
        }

        if (isLeft)
        {
            SetLeftSprite(id, spr);
            ShowLeft();
        }
        else
        {
            SetRightSprite(id, spr);
            ShowRight();
        }
    }

    // =========================================================
    //  SPRITES / SLOTS
    // =========================================================

    private Sprite FindSprite(string idUpper, string poseLower)
    {
        foreach (var e in catalog)
        {
            if (e == null || e.sprite == null) continue;

            if (e.id.Trim().ToUpper() == idUpper &&
                e.pose.Trim().ToLower() == poseLower)
                return e.sprite;
        }
        return null;
    }

    private void SetLeftSprite(string idUpper, Sprite spr)
    {
        if (leftImage == null) return;

        leftImage.sprite = spr;
        leftImage.enabled = true;
        leftVisible = true;
        currentLeftId = idUpper;

        var c = leftImage.color;
        c.a = litAlpha;
        leftImage.color = c;
    }

    private void SetRightSprite(string idUpper, Sprite spr)
    {
        if (rightImage == null) return;

        rightImage.sprite = spr;
        rightImage.enabled = true;
        rightVisible = true;
        currentRightId = idUpper;

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
        leftVel = 0f;
    }

    private void HideRight()
    {
        rightTargetX = rightHiddenX;
        rightVisible = false;
        currentRightId = "";
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
