using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject choicePanel;
    public TextMeshProUGUI questionText;
    public Button[] choiceButtons;

    private VNDialogue vn;

    private void Awake()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
    }

    public void ShowChoices(string question, List<DialogueLine> options, VNDialogue vnDialogue)
    {
        vn = vnDialogue;

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            StartCoroutine(FadeInPanel(choicePanel));
        }

        if (questionText != null)
        {
            questionText.text = question;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Count)
            {
                DialogueLine optionLine = options[i];
                DialogueLine chosen = optionLine;

                choiceButtons[i].gameObject.SetActive(true);

                TMP_Text btnText = choiceButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    // speaker = etiqueta del botón ("Respuesta cínica", "Contar a Damiao...")
                    // text   = diálogo que aparece en pantalla tras elegir
                    string label = !string.IsNullOrEmpty(optionLine.speaker) ? optionLine.speaker : optionLine.text;
                    btnText.text = label.Trim();
                }

                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() =>
                {
                    if (vn != null)
                        vn.OnChoiceSelected(chosen);

                    if (choicePanel != null)
                        StartCoroutine(FadeOutAndDisable(choicePanel));
                });
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator FadeInPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        
        cg.alpha = 0;
        float elapsed = 0;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        cg.alpha = 1;
    }

    private IEnumerator FadeOutAndDisable(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            panel.SetActive(false);
            yield break;
        }

        float elapsed = 0;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }
        cg.alpha = 0;
        panel.SetActive(false);
    }
}

