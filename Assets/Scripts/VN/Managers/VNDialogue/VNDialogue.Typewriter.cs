using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
    // =========================================================
    //  TYPEWRITER & TEXT STYLING FIELDS
    // =========================================================
    
    [Header("Text Presentation")]
    public bool enableTypewriter = true;
    [Range(10f, 100f)]
    public float typewriterCharsPerSecond = 40f;
    public Color waitStyleColor = new Color(0.85f, 0.85f, 0.85f, 1f);  // Gris claro OPACO
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
    /// Detiene el typewriter si está corriendo actualmente.
    /// </summary>
    private void StopTypewriterIfRunning()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }
    }

    /// <summary>
    /// Completa instantáneamente el typewriter actual.
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

        float delay = 1f / typewriterCharsPerSecond;
        dialogueText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            dialogueText.text += fullText[i];
            yield return new WaitForSeconds(delay);
        }

        _typewriterComplete = true;
        _typewriterCoroutine = null;
    }

    /// <summary>
    /// Restaura el estilo normal del texto (color, font style, tamaño).
    /// </summary>
    private void ApplyNormalStyle()
    {
        if (dialogueText == null) return;

        dialogueText.color = _baseColor;
        dialogueText.fontStyle = _baseStyle;
        dialogueText.fontSize = _baseFontSize;
    }

    /// <summary>
    /// Aplica el estilo WAIT/NARRADOR (cursiva, color atenuado, tamaño opcional).
    /// </summary>
    private void ApplyWaitStyle()
    {
        if (dialogueText == null) return;

        dialogueText.fontStyle = FontStyles.Italic;
        dialogueText.color = waitStyleColor;
        
        // Opcional: reducir tamaño (solo si tenemos un tamaño base válido)
        if (waitStyleSizeMultiplier < 1.0f && _baseFontSize > 0f)
        {
            dialogueText.fontSize = _baseFontSize * waitStyleSizeMultiplier;
        }
    }
}
