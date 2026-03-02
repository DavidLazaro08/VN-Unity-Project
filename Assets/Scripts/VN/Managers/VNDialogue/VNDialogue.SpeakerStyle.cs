using System.Collections;
using TMPro;
using UnityEngine;

public partial class VNDialogue
{
    /*
     * VNDialogue.SpeakerStyle
     * -----------------------
     * Aplica estilos visuales al nombre del hablante (color y micro fade-in opcional).
     * Esto se usa desde ShowLine() para que cada personaje tenga una identidad clara.
     */

    // =========================================================
    //  SPEAKER NAME STYLING FIELDS
    // =========================================================

    [Header("Speaker Name Colors")]
    public Color loganNameColor = new Color(0.4f, 0.7f, 1f, 1f);          // Azul claro
    public Color damiaoNameColor = new Color(0.85f, 0.55f, 0.95f, 1f);    // Lila / violeta suave
    public Color lazarusNameColor = new Color(0.7f, 0.5f, 0.9f, 1f);      // Púrpura
    public Color narratorNameColor = new Color(0.6f, 0.6f, 0.6f, 1f);     // Gris

    public Color antirobotsNameColor = new Color(1f, 0.3f, 0.25f, 1f);    // Rojo eléctrico
    public Color truefellaNameColor = new Color(1f, 0.75f, 0.3f, 1f);     // Ámbar cálido
    public Color siluetaNameColor = new Color(0.5f, 0.55f, 0.7f, 1f);     // Gris-azul apagado

    public Color liraNameColor = new Color(0.3f, 0.85f, 0.75f, 1f);       // Teal suave
    public Color viejoNameColor = new Color(0.85f, 0.7f, 0.35f, 1f);      // Ocre dorado
    public Color ronnNameColor = new Color(0.35f, 0.7f, 0.65f, 1f);       // Azul-verde acero

    public bool enableNameFade = true;
    public float nameFadeDuration = 0.12f;

    private string _lastSpeaker = "";
    private Coroutine _nameFadeCoroutine = null;

    // =========================================================
    //  SPEAKER NAME STYLING METHODS
    // =========================================================

    /// <summary>
    /// Aplica el color del nombre según el personaje.
    /// Si el hablante cambia y el fade está activo, hace un micro fade-in.
    /// </summary>
    private void ApplySpeakerNameStyle(string speakerUpper, string speakerRaw)
    {
        if (nameText == null) return;

        Color targetColor;

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
                targetColor = Color.white;
                break;
        }

        bool speakerChanged = (_lastSpeaker != speakerRaw);

        if (enableNameFade && speakerChanged && !string.IsNullOrEmpty(speakerRaw))
        {
            if (_nameFadeCoroutine != null)
            {
                StopCoroutine(_nameFadeCoroutine);
                _nameFadeCoroutine = null;
            }

            _nameFadeCoroutine = StartCoroutine(NameFadeRoutine(targetColor));
        }
        else
        {
            nameText.color = targetColor;
        }

        _lastSpeaker = speakerRaw;
    }

    /// <summary>
    /// Micro fade-in del nombre del hablante.
    /// </summary>
    private IEnumerator NameFadeRoutine(Color targetColor)
    {
        if (nameText == null) yield break;

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

        nameText.color = targetColor;
        _nameFadeCoroutine = null;
    }
}