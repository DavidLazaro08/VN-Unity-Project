using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
    // =========================================================
    //  COMMAND PARSING & HELPERS
    // =========================================================

    /// <summary>
    /// Extrae valores "CLAVE=VALOR" del string cmd (separado por ;)
    /// </summary>
    private string ParseValue(string cmd, string key)
    {
        if (string.IsNullOrEmpty(cmd)) return "";

        string[] parts = cmd.Split(';');
        foreach (var p in parts)
        {
            string clean = p.Trim();
            if (clean.StartsWith(key + "=", StringComparison.Ordinal))
            {
                return clean.Substring(key.Length + 1).Trim();
            }
        }
        return "";
    }

    /// <summary>
    /// Avanza línea a línea hasta encontrar otro BRANCH, un BRANCH_END, o salir de la zona
    /// </summary>
    private void SkipBranchBlock()
    {
        lineIndex++;

        while (lineIndex < currentLines.Count)
        {
            string sp = (currentLines[lineIndex].speaker ?? "").Trim().ToUpper();

            if (sp == "BRANCH")
            {
                ShowLine();
                return;
            }

            if (sp == "BRANCH_END")
            {
                lineIndex++;
                ShowLine();
                return;
            }

            lineIndex++;
        }

        ShowLine();
    }

    // =========================================================
    //  CHARACTER SLOTS HELPERS
    // =========================================================

    private void ApplyCmdToSlots(string cmd)
    {
        if (characterSlots == null) return;
        characterSlots.SendMessage("ApplyCmd", cmd, SendMessageOptions.DontRequireReceiver);
    }

    private void ApplyFocusToSlots(string speaker)
    {
        if (characterSlots == null) return;
        characterSlots.SendMessage("ApplyFocus", speaker, SendMessageOptions.DontRequireReceiver);
    }

    private void NarratorMomentToSlots()
    {
        if (characterSlots == null) return;
        characterSlots.SendMessage("NarratorMoment", SendMessageOptions.DontRequireReceiver);
    }
}
