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
            // Comparación de clave insensible a mayúsculas
            if (clean.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
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
        if (characterSlots == null)
        {
             Debug.LogError($"[VNDialogue] ❌ ERROR CRÍTICO: 'characterSlots' es NULL. No se pueden mostrar personajes. Comando ignorado: '{cmd}'\n" +
                            "Asegúrate de asignar el objeto 'CharacterSlots' (o 'CharacterCenter') en el Inspector de VNDialogue.");
             return;
        }

        // Debug info para el usuario
        Debug.Log($"[VNDialogue] Intentando enviar comando '{cmd}' a '{characterSlots.name}' (Tipo: {characterSlots.GetType().Name}). Objeto Activo: {characterSlots.gameObject.activeInHierarchy}");

        // Forzamos RequireReceiver para que Unity grite si no encuentra el script 'VNSingleCharacterSlot'
        try 
        {
            characterSlots.SendMessage("ApplyCmd", cmd, SendMessageOptions.RequireReceiver);
        }
        catch (Exception e)
        {
            Debug.LogError($"[VNDialogue] ❌ ERROR: El objeto '{characterSlots.name}' NO TIENE un script con el método 'ApplyCmd'.\n" +
                           $"Asegúrate de que el objeto asignado en 'Character Slots' tiene el script 'VNSingleCharacterSlot' (o 'VNAICombinedSlot').\n" +
                           $"Detalle: {e.Message}");
        }
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
