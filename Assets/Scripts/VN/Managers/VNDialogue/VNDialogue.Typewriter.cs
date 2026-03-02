using System.Collections;
using TMPro;
using UnityEngine;

public partial class VNDialogue
{
    /*
     * VNDialogue.Typewriter
     * ---------------------
     * Gestiona la presentación del texto: efecto typewriter y estilos básicos.
     * La lógica de avance (Next/ShowLine) decide cuándo iniciar o completar el efecto.
     */

    // =========================================================
    //  TYPEWRITER & TEXT STYLING FIELDS
    // =========================================================

    [Header("Text Presentation")]
    public bool enableTypewriter = true;

    [Range(10f, 100f)]
    public float typewriterCharsPerSecond = 40f;

    public Color waitStyleColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Range(0.8f, 1.0f)]
    public float waitStyleSizeMultiplier = 0.9f;

    // Base style preservation
    private Color _baseColor = Color.white;
    private FontStyles _baseStyle = FontStyles.Normal;
    private float _baseFontSize = 0f;

    // Typewriter state
    private Coroutine _typewriterCoroutine = null;
    private bool _typewriterComplete = false;
    private string _currentFullText = "";
    private int _lastRenderedLineIndex = -1;
    private string _lastRenderedFullText = "";

    // =========================================================
    //  TYPEWRITER METHODS
    // =========================================================

    /// <summary>
    /// Detiene el typewriter si está en ejecución.
    /// </summary>
    private void StopTypewriterIfRunning()
    {
        if (_typewriterCoroutine == null) return;

        StopCoroutine(_typewriterCoroutine);
        _typewriterCoroutine = null;
    }

    /// <summary>
    /// Completa instantáneamente el texto de la línea actual.
    /// </summary>
    private void CompleteTypewriter()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }

        if (dialogueText != null)
        {
            dialogueText.text = _currentFullText;
        }

        _typewriterComplete = true;
    }

    /// <summary>
    /// Corrutina que revela el texto carácter por carácter.
    /// </summary>
    private IEnumerator TypewriterRoutine(string fullText)
    {
        if (dialogueText == null) yield break;

        // Reiniciar contador de blips al empezar la línea
        if (blipController != null) blipController.ResetCounter();

        float delay = 1f / typewriterCharsPerSecond;
        dialogueText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            char currentChar = fullText[i];
            dialogueText.text += currentChar;

            // Sonido por carácter (si existe controller)
            if (blipController != null)
            {
                blipController.OnCharTyped(currentChar, false);
            }

            yield return new WaitForSeconds(delay);
        }

        _typewriterComplete = true;
        _typewriterCoroutine = null;
    }

    /// <summary>
    /// Restaura el estilo normal del texto (color, estilo y tamaño).
    /// </summary>
    private void ApplyNormalStyle()
    {
        if (dialogueText == null) return;

        dialogueText.color = _baseColor;
        dialogueText.fontStyle = _baseStyle;
        dialogueText.fontSize = _baseFontSize;
    }

    /// <summary>
    /// Aplica el estilo de WAIT/NARRADOR (cursiva y color atenuado).
    /// </summary>
    private void ApplyWaitStyle()
    {
        if (dialogueText == null) return;

        dialogueText.fontStyle = FontStyles.Italic;
        dialogueText.color = waitStyleColor;

        // Reducir tamaño si existe un tamaño base válido
        if (waitStyleSizeMultiplier < 1.0f && _baseFontSize > 0f)
        {
            dialogueText.fontSize = _baseFontSize * waitStyleSizeMultiplier;
        }
    }
}