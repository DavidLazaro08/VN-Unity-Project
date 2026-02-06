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
            choicePanel.SetActive(true);

        if (questionText != null)
            questionText.text = question;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Count)
            {
                DialogueLine optionLine = options[i];
                DialogueLine chosen = optionLine;

                choiceButtons[i].gameObject.SetActive(true);

                TMP_Text btnText = choiceButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                    btnText.text = chosen.text;

                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() =>
                {
                    if (vn != null)
                        vn.OnChoiceSelected(chosen);

                    if (choicePanel != null)
                        choicePanel.SetActive(false);
                });
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }
}
