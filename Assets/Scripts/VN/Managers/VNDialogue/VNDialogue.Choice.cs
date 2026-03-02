using System;
using System.Collections;
using UnityEngine;

public partial class VNDialogue
{
    /*
     * VNDialogue.Choice
     * -----------------
     * Gestiona el cierre de una elección (CHOICE): aplica el resultado,
     * guarda el estado (última opción), ajusta afinidad y decide si avanza automáticamente.
     */

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

            // Decisión moral de interceptación (insensible a mayúsculas)
            if (cId.ToUpper() == "TRUTH")
            {
                HandleTruthChoice(cOpt.ToUpper());
            }
        }

        // Afinidad
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

        // Mostrar frase elegida (sin enseñar "OPTION" como speaker)
        string realSpeaker = GetSpeakerFromCmd(cleanCmd);
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

        // Recolocar el índice para continuar justo después del bloque de opciones
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