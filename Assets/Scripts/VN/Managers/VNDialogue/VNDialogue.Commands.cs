using System;
using UnityEngine;

public partial class VNDialogue
{
    /*
     * VNDialogue.Commands
     * -------------------
     * Este archivo contiene utilidades relacionadas con el procesamiento
     * de comandos del CSV y el soporte a bloques especiales como BRANCH.
     *
     * Aquí no se gestiona el flujo principal del diálogo,
     * solo funciones auxiliares que mantienen limpio el núcleo.
     */

    // =========================================================
    //  COMMAND PARSING & HELPERS
    // =========================================================

    /// <summary>
    /// Extrae valores "CLAVE=VALOR" de un string cmd separado por ';'.
    /// Devuelve cadena vacía si la clave no existe.
    /// </summary>
    private string ParseValue(string cmd, string key)
    {
        if (string.IsNullOrEmpty(cmd)) return "";

        string[] parts = cmd.Split(';');

        foreach (var p in parts)
        {
            string clean = p.Trim();

            if (clean.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                return clean.Substring(key.Length + 1).Trim();
            }
        }

        return "";
    }

    /// <summary>
    /// Salta líneas hasta encontrar el siguiente bloque válido
    /// (otro BRANCH, un BRANCH_END o el final).
    /// Se usa cuando la rama actual no coincide con la elección previa.
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
    //  CHARACTER SLOT HELPERS
    // =========================================================

    /// <summary>
    /// Envía el comando de pose al sistema de slots de personajes.
    /// </summary>
    private void ApplyCmdToSlots(string cmd)
    {
        if (characterSlots == null)
        {
            Debug.LogError(
                "[VNDialogue] characterSlots no está asignado en el Inspector. " +
                "No se pueden aplicar poses o cambios de personaje."
            );
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[VNDialogue] ApplyCmd -> {characterSlots.name} | cmd='{cmd}'");
#endif

        try
        {
            characterSlots.SendMessage(
                "ApplyCmd",
                cmd,
                SendMessageOptions.RequireReceiver
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[VNDialogue] El objeto asignado en characterSlots no contiene un método ApplyCmd(string).\n" +
                $"Detalle: {e.Message}"
            );
        }
    }

    /// <summary>
    /// Aplica foco visual al personaje que está hablando.
    /// </summary>
    private void ApplyFocusToSlots(string speaker)
    {
        if (characterSlots == null) return;

        characterSlots.SendMessage(
            "ApplyFocus",
            speaker,
            SendMessageOptions.DontRequireReceiver
        );
    }

    /// <summary>
    /// Ajusta los slots para un momento de narrador (sin foco activo).
    /// </summary>
    private void NarratorMomentToSlots()
    {
        if (characterSlots == null) return;

        characterSlots.SendMessage(
            "NarratorMoment",
            SendMessageOptions.DontRequireReceiver
        );
    }
}