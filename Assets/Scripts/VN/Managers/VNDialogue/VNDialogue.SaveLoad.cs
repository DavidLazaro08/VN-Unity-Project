using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
    // =========================================================
    //  SAVE / LOAD CONSTANTS
    // =========================================================
    private const string SAVE_SCENE = "VN_SAVE_SCENE";
    private const string SAVE_LINE = "VN_SAVE_LINE";

    private const string KEY_HAS_SAVE = "VN_HAS_SAVE";
    private const string KEY_CONTINUE = "VN_CONTINUE";

    // =========================================================
    //  SAVE / LOAD METHODS
    // =========================================================

    public void SaveGame()
    {
        Debug.Log($"[VNDialogue] SaveGame() llamado - Guardando sceneIndex={sceneIndex}, lineIndex={lineIndex}");
        PlayerPrefs.SetInt(SAVE_SCENE, sceneIndex);
        PlayerPrefs.SetInt(SAVE_LINE, lineIndex);
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
        PlayerPrefs.Save();
        Debug.Log("[VNDialogue] Partida guardada correctamente");
    }

    public void LoadGame()
    {
        if (PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) != 1)
        {
            Debug.Log("[VNDialogue] LoadGame() - No hay partida guardada");
            return;
        }

        sceneIndex = PlayerPrefs.GetInt(SAVE_SCENE, 0);
        lineIndex = PlayerPrefs.GetInt(SAVE_LINE, 0);
        
        Debug.Log($"[VNDialogue] LoadGame() - Cargando sceneIndex={sceneIndex}, lineIndex={lineIndex}");

        if (sceneIndex < 0 || sceneIndex >= sceneFiles.Count)
            sceneIndex = 0;

        // Cargar la escena SIN resetear lineIndex
        string fileName = sceneFiles[sceneIndex];
        currentLines = VNSceneLoader.LoadFromResources(fileName);

        if (currentLines == null)
            currentLines = new List<DialogueLine>();

        waitingForChoice = false;
        choiceNextLineIndex = -1;

        // Detener typewriter si está corriendo
        StopTypewriterIfRunning();

        // Validar lineIndex después de cargar las líneas
        if (lineIndex < 0) lineIndex = 0;
        if (lineIndex >= currentLines.Count)
            lineIndex = Mathf.Max(0, currentLines.Count - 1);

        // Por si venimos de un ACT anterior, aseguramos estado visual
        if (dialogueText != null)
            dialogueText.color = _baseColor;

        ShowLine();
        Debug.Log($"[VNDialogue] Partida cargada correctamente en línea {lineIndex}");
    }

    public bool HasSave()
    {
        return PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;
    }
}
