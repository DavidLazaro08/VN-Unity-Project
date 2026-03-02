using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona el panel de elecciones:
/// - Muestra una pregunta y un listado de opciones.
/// - Cada botón dispara vn.OnChoiceSelected(...) con el DialogueLine elegido.
/// - Hace fade in/out del panel para que quede más "VN" y menos "menú cutre".
/// </summary>
public class ChoiceManager : MonoBehaviour
{
    // =========================================================
    //  UI
    // =========================================================

    [Header("UI")]
    public GameObject choicePanel;
    public TextMeshProUGUI questionText;
    public Button[] choiceButtons;

    // Referencia al VNDialogue activo (se inyecta al abrir elecciones)
    private VNDialogue _vn;

    // =========================================================
    //  CICLO DE VIDA
    // =========================================================

    private void Awake()
    {
        // Al arrancar, el panel de elecciones debería estar oculto
        if (choicePanel != null)
            choicePanel.SetActive(false);
    }

    // =========================================================
    //  API PÚBLICA
    // =========================================================

    /// <summary>
    /// Muestra el panel con una pregunta y las opciones disponibles.
    /// "speaker" en cada opción se usa como etiqueta del botón (si existe).
    /// "text" es el diálogo que se mostrará al elegir (lo gestiona VNDialogue).
    /// </summary>
    public void ShowChoices(string question, List<DialogueLine> options, VNDialogue vnDialogue)
    {
        _vn = vnDialogue;

        // Activar panel con fade
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            StartCoroutine(FadeInPanel(choicePanel));
        }

        // Pregunta superior
        if (questionText != null)
            questionText.text = question;

        // Pintar botones
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Count)
            {
                DialogueLine optionLine = options[i];

                // IMPORTANTE: copiamos a variable local para evitar bugs de closure en el loop
                DialogueLine chosen = optionLine;

                choiceButtons[i].gameObject.SetActive(true);

                // Texto del botón: si speaker trae la "etiqueta", la usamos; si no, usamos el propio texto
                TMP_Text btnText = choiceButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    string label = !string.IsNullOrEmpty(optionLine.speaker)
                        ? optionLine.speaker
                        : optionLine.text;

                    btnText.text = label.Trim();
                }

                // Limpiar listeners previos (muy importante si el panel se reutiliza)
                choiceButtons[i].onClick.RemoveAllListeners();

                choiceButtons[i].onClick.AddListener(() =>
                {
                    // Avisar a VNDialogue de la elección
                    if (_vn != null)
                        _vn.OnChoiceSelected(chosen);

                    // Ocultar panel con fade out
                    if (choicePanel != null)
                        StartCoroutine(FadeOutAndDisable(choicePanel));
                });
            }
            else
            {
                // No hay opción para este botón, lo apagamos
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // =========================================================
    //  FADES
    // =========================================================

    private IEnumerator FadeInPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    private IEnumerator FadeOutAndDisable(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();

        // Si no hay CanvasGroup, apagamos sin drama
        if (cg == null)
        {
            panel.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        cg.alpha = 0f;
        panel.SetActive(false);
    }
}