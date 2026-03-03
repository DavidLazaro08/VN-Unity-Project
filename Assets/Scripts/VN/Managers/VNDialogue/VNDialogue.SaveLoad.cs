using System.Collections.Generic;
using UnityEngine;

public partial class VNDialogue
{
    /*
     * VNDialogue.SaveLoad
     * -------------------
     * Guardado sencillo por índice: recuerda en qué CSV (sceneIndex) y en qué línea (lineIndex)
     * se encuentra el jugador para permitir “Continuar” desde el menú.
     *
     * El estado narrativo adicional (afinidad, flags, etc.) se gestiona en VNGameState.
     */

    // =========================================================
    //  SAVE / LOAD CONSTANTS
    // =========================================================
    private const string SAVE_SCENE       = "VN_SAVE_SCENE";
    private const string SAVE_LINE         = "VN_SAVE_LINE";
    private const string SAVE_UNITY_SCENE  = "VN_SAVE_UNITY_SCENE"; // Escena Unity donde se guardó

    private const string KEY_HAS_SAVE = "VN_HAS_SAVE";
    private const string KEY_CONTINUE = "VN_CONTINUE"; // Se usa desde Start() en otro partial

    // =========================================================
    //  SAVE / LOAD METHODS
    // =========================================================

    public void SaveGame()
    {
        // Fail-safe: evitar guardar índices inválidos si se llama en un momento extraño.
        int safeScene = Mathf.Clamp(sceneIndex, 0, Mathf.Max(0, sceneFiles.Count - 1));
        int safeLine  = Mathf.Max(0, lineIndex);
        string unityScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

#if UNITY_EDITOR
        Debug.Log($"[VNDialogue] SaveGame -> sceneIndex={safeScene}, lineIndex={safeLine}, unityScene={unityScene}");
#endif

        PlayerPrefs.SetInt(SAVE_SCENE, safeScene);
        PlayerPrefs.SetInt(SAVE_LINE, safeLine);
        PlayerPrefs.SetString(SAVE_UNITY_SCENE, unityScene);
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        if (PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) != 1)
        {
#if UNITY_EDITOR
            Debug.Log("[VNDialogue] LoadGame -> no hay partida guardada.");
#endif
            return;
        }

        sceneIndex = PlayerPrefs.GetInt(SAVE_SCENE, 0);
        lineIndex  = PlayerPrefs.GetInt(SAVE_LINE, 0);

#if UNITY_EDITOR
        Debug.Log($"[VNDialogue] LoadGame -> sceneIndex={sceneIndex}, lineIndex={lineIndex}");
#endif

        if (sceneIndex < 0 || sceneIndex >= sceneFiles.Count)
            sceneIndex = 0;

        // Cargar CSV sin resetear lineIndex (se valida después)
        string fileName = sceneFiles[sceneIndex];
        currentLines = VNSceneLoader.LoadFromResources(fileName) ?? new List<DialogueLine>();

        waitingForChoice = false;
        choiceNextLineIndex = -1;

        StopTypewriterIfRunning();

        // Validar lineIndex tras cargar el CSV
        if (lineIndex < 0) lineIndex = 0;
        if (lineIndex >= currentLines.Count)
            lineIndex = Mathf.Max(0, currentLines.Count - 1);

        // Asegurar estilo base si venimos de estados especiales
        if (dialogueText != null)
            dialogueText.color = _baseColor;

        ShowLine();

#if UNITY_EDITOR
        Debug.Log($"[VNDialogue] LoadGame -> reanudado en línea {lineIndex}.");
#endif
    }

    public bool HasSave()
    {
        return PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;
    }
}