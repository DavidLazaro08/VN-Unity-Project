using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
    // =========================================================
    //  CHOICE AUTO-ADVANCE
    // =========================================================
    [Header("Choice Auto-Advance")]
    public bool autoAdvanceAfterChoice = true;
    public float choiceAutoAdvanceDelay = 5.0f;

    private Coroutine _choiceAutoCo;
    private bool _autoAdvancingChoice = false;

    // =========================================================
    //  CHOICE METHODS
    // =========================================================

    /// <summary>
    /// Llamado desde ChoiceManager cuando el usuario selecciona una opción.
    /// </summary>
    public void OnChoiceSelected(DialogueLine chosenLine)
    {
        waitingForChoice = false;

        string cleanCmd = (chosenLine.cmd ?? "").Trim().Trim('"');

        // Estado de elección (id/op)
        string cId = ParseValue(cleanCmd, "CHOICE_ID");
        string cOpt = ParseValue(cleanCmd, "CHOICE_OPT");

        if (!string.IsNullOrEmpty(cId))
        {
            VNGameState.SetLastChoice(cId, cOpt);

            // Decisión moral de interceptación (Insensible a mayúsculas)
            if (cId.ToUpper() == "TRUTH")
            {
                HandleTruthChoice(cOpt.ToUpper());
            }
        }

        // Afinidad (ya existente)
        string affStr = ParseValue(cleanCmd, "AFF_DAMIAO");
        if (!string.IsNullOrEmpty(affStr))
        {
            if (int.TryParse(affStr, out int delta))
            {
                VNGameState.AddAffinityDamiao(delta);

                if (affinityPopup != null)
                    affinityPopup.ShowDelta(delta);
            }
        }

        // Mostrar frase elegida
        string realSpeaker = GetSpeakerFromCmd(cleanCmd);
        // Si no hay un personaje real asociado, no mostrar nada en el nombre
        // (evita que aparezca "OPTION" como speaker visiblemente)
        string speakerToShow = string.IsNullOrEmpty(realSpeaker) ? "" : realSpeaker;

        if (nameText != null) nameText.text = speakerToShow;
        if (dialogueText != null)
        {
            dialogueText.text = chosenLine.text ?? "";
            dialogueText.color = Color.white;
        }

        if (characterSlots != null)
            ApplyCmdToSlots(cleanCmd);

        if (characterSlots != null && !string.IsNullOrEmpty(speakerToShow))
            ApplyFocusToSlots(speakerToShow.Trim().ToUpper());

        if (choiceNextLineIndex >= 0)
            lineIndex = choiceNextLineIndex - 1;

        // Auto-advance tras choice
        if (autoAdvanceAfterChoice)
        {
            if (_choiceAutoCo != null) StopCoroutine(_choiceAutoCo);
            _autoAdvancingChoice = true;
            _choiceAutoCo = StartCoroutine(AutoAdvanceChoice());
        }
    }

    /// <summary>
    /// Corrutina para auto-avanzar después de una elección.
    /// </summary>
    private IEnumerator AutoAdvanceChoice()
    {
        yield return new WaitForSeconds(choiceAutoAdvanceDelay);
        _autoAdvancingChoice = false;
        Next();
    }

    /// <summary>
    /// Deducción simple del speaker desde cmd (para choices).
    /// </summary>
    private string GetSpeakerFromCmd(string cmd)
    {
        if (string.IsNullOrEmpty(cmd)) return "";

        string u = cmd.ToUpper();

        if (u.Contains("L=LOGAN")) return "LOGAN";
        if (u.Contains("R=DAMIAO")) return "DAMIAO";
        if (u.Contains("L=LAZARUS")) return "LAZARUS";
        if (u.Contains("R=LAZARUS")) return "LAZARUS";

        return "";
    }
}
