using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
    // =========================================================
    //  SPEAKER NAME STYLING FIELDS
    // =========================================================
    
    [Header("Speaker Name Colors")]
    public Color loganNameColor = new Color(0.4f, 0.7f, 1f, 1f);          // Azul claro
    public Color damiaoNameColor = new Color(0.85f, 0.55f, 0.95f, 1f);    // Lila / violeta suave
    public Color lazarusNameColor = new Color(0.7f, 0.5f, 0.9f, 1f);     // Púrpura
    public Color narratorNameColor = new Color(0.6f, 0.6f, 0.6f, 1f);    // Gris
    public Color antirobotsNameColor = new Color(1f, 0.3f, 0.25f, 1f);   // Rojo eléctrico
    public Color truefellaNameColor = new Color(1f, 0.75f, 0.3f, 1f);    // Ámbar cálido
    public Color siluetaNameColor = new Color(0.5f, 0.55f, 0.7f, 1f);    // Gris-azul apagado
    public Color liraNameColor = new Color(0.3f, 0.85f, 0.75f, 1f);      // Teal suave
    public Color viejoNameColor = new Color(0.85f, 0.7f, 0.35f, 1f);     // Ocre dorado
    public Color ronnNameColor = new Color(0.35f, 0.7f, 0.65f, 1f);      // Azul-verde acero
    public bool enableNameFade = true;
    public float nameFadeDuration = 0.12f;

    private string _lastSpeaker = "";
    private Coroutine _nameFadeCoroutine = null;

    // =========================================================
    //  SPEAKER NAME STYLING METHODS
    // =========================================================

    /// <summary>
    /// Aplica el color correspondiente al nombre del hablante según el personaje.
    /// Opcionalmente añade un micro fade-in si el hablante cambió.
    /// </summary>
    private void ApplySpeakerNameStyle(string speakerUpper, string speakerRaw)
    {
        if (nameText == null) return;

        // Determinar color según personaje
        Color targetColor = Color.white; // Color por defecto

        switch (speakerUpper)
        {
            case "LOGAN":
                targetColor = loganNameColor;
                break;
            case "DAMIAO":
                targetColor = damiaoNameColor;
                break;
            case "LAZARUS":
                targetColor = lazarusNameColor;
                break;
            case "ANTIROBOTS":
                targetColor = antirobotsNameColor;
                break;
            case "TRUE-FELLA":
                targetColor = truefellaNameColor;
                break;
            case "SILUETA":
            case "SOMBRA":
                targetColor = siluetaNameColor;
                break;
            case "LIRA":
                targetColor = liraNameColor;
                break;
            case "VIEJO":
                targetColor = viejoNameColor;
                break;
            case "RONN":
                targetColor = ronnNameColor;
                break;
            case "NARRADOR":
            case "WAIT":
            case "ACT":
                targetColor = narratorNameColor;
                break;
            default:
                // Personajes no definidos usan color blanco
                targetColor = Color.white;
                break;
        }

        // Si el hablante cambió y el fade está habilitado, hacer micro fade-in
        bool speakerChanged = (_lastSpeaker != speakerRaw);
        
        if (enableNameFade && speakerChanged && !string.IsNullOrEmpty(speakerRaw))
        {
            // Detener fade anterior si existe
            if (_nameFadeCoroutine != null)
            {
                StopCoroutine(_nameFadeCoroutine);
            }

            _nameFadeCoroutine = StartCoroutine(NameFadeRoutine(targetColor));
        }
        else
        {
            // Aplicar color directamente sin fade
            nameText.color = targetColor;
        }

        _lastSpeaker = speakerRaw;
    }

    /// <summary>
    /// Corrutina que hace un micro fade-in del nombre del hablante.
    /// </summary>
    private IEnumerator NameFadeRoutine(Color targetColor)
    {
        if (nameText == null) yield break;

        // Empezar con alpha 0
        Color startColor = targetColor;
        startColor.a = 0f;
        nameText.color = startColor;

        float elapsed = 0f;
        while (elapsed < nameFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / nameFadeDuration);
            
            Color currentColor = targetColor;
            currentColor.a = Mathf.Lerp(0f, targetColor.a, t);
            nameText.color = currentColor;

            yield return null;
        }

        // Asegurar color final
        nameText.color = targetColor;
        _nameFadeCoroutine = null;
    }
}
