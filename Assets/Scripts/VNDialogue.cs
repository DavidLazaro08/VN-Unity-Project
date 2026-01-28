using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VNDialogue : MonoBehaviour
{
    // =========================================================
    //  UI BÁSICA (nombre + texto)
    // =========================================================
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    // =========================================================
    //  VISUAL DE PERSONAJES
    // =========================================================
    [Header("Personajes (visual)")]
    public VNCharacterSlots characterSlots;

    // =========================================================
    //  ESCENAS (CSV en Resources/Dialogue/)
    // =========================================================
    [Header("Escenas (CSV en Resources/Dialogue/)")]
    public List<string> sceneFiles = new List<string> { "intro", "scene_01", "scene_02" };

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================
    private int sceneIndex = 0;
    private int lineIndex = 0;

    private List<DialogueLine> currentLines = new();

    // =========================================================
    //  GUARDADO (PlayerPrefs) - simple y suficiente
    // =========================================================
    private const string SAVE_SCENE = "VN_SAVE_SCENE";
    private const string SAVE_LINE  = "VN_SAVE_LINE";

    // Para el menú "Continue"
    private const string KEY_HAS_SAVE = "VN_HAS_SAVE";
    private const string KEY_CONTINUE = "VN_CONTINUE";

    // =========================================================
    //  ARRANQUE (nuevo juego vs continue)
    // =========================================================
    private void Start()
    {
        // Si venimos del menú pulsando Continue, cargamos guardado
        if (PlayerPrefs.GetInt(KEY_CONTINUE, 0) == 1)
        {
            // Consumimos la bandera para que no se repita
            PlayerPrefs.SetInt(KEY_CONTINUE, 0);
            PlayerPrefs.Save();

            LoadGame();
            return;
        }

        // Inicio normal (nuevo juego)
        LoadScene(sceneIndex);
        ShowLine();
    }

    // =========================================================
    //  CONTROL DE FLUJO
    // =========================================================

    public void Next()
    {
        if (currentLines.Count == 0) return;

        lineIndex++;

        // Si se acaba el CSV actual, pasamos al siguiente
        if (lineIndex >= currentLines.Count)
        {
            sceneIndex++;
            if (sceneIndex >= sceneFiles.Count)
                sceneIndex = 0;

            LoadScene(sceneIndex);
        }

        ShowLine();
    }

    private void LoadScene(int index)
    {
        string fileName = sceneFiles[index];
        currentLines = VNSceneLoader.LoadFromResources(fileName);

        if (currentLines == null)
            currentLines = new List<DialogueLine>();
    }

    private void ShowLine()
    {
        if (currentLines.Count == 0) return;
        if (lineIndex < 0 || lineIndex >= currentLines.Count) return;

        DialogueLine line = currentLines[lineIndex];

        string speakerRaw = (line.speaker ?? "").Trim();
        string speakerUpper = speakerRaw.ToUpper();

        nameText.text = speakerRaw;
        dialogueText.text = line.text ?? "";

        // CMDs -> los aplica VNCharacterSlots
        if (characterSlots != null)
            characterSlots.ApplyCmd(line.cmd);

        // Narrador
        if (speakerUpper == "NARRADOR" || string.IsNullOrEmpty(speakerUpper))
        {
            if (characterSlots != null)
                characterSlots.NarratorMoment();
            return;
        }

        // Foco
        if (characterSlots != null)
            characterSlots.ApplyFocus(speakerUpper);
    }

    // =========================================================
    //  SAVE / LOAD (llamable desde UI)
    // =========================================================

    public void SaveGame()
    {
        PlayerPrefs.SetInt(SAVE_SCENE, sceneIndex);
        PlayerPrefs.SetInt(SAVE_LINE, lineIndex);

        // Marcamos que existe un guardado (para activar Continue en el menú)
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);

        PlayerPrefs.Save();
        Debug.Log($"[VN] Guardado: sceneIndex={sceneIndex}, lineIndex={lineIndex}");
    }

    public void LoadGame()
    {
        // Si no hay guardado, no hacemos nada
        if (PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) != 1)
        {
            Debug.LogWarning("[VN] No hay guardado todavía.");
            return;
        }

        sceneIndex = PlayerPrefs.GetInt(SAVE_SCENE, 0);
        lineIndex  = PlayerPrefs.GetInt(SAVE_LINE, 0);

        // Seguridad por si cambia la lista de escenas
        if (sceneIndex < 0 || sceneIndex >= sceneFiles.Count)
            sceneIndex = 0;

        LoadScene(sceneIndex);

        // Seguridad por si el CSV ahora tiene menos líneas
        if (lineIndex < 0) lineIndex = 0;
        if (lineIndex >= currentLines.Count) lineIndex = Mathf.Max(0, currentLines.Count - 1);

        ShowLine();

        Debug.Log($"[VN] Cargado: sceneIndex={sceneIndex}, lineIndex={lineIndex}");
    }

    public bool HasSave()
    {
        // Si quieres, puedes dejarlo, pero ahora la “fuente de verdad” es VN_HAS_SAVE.
        return PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;
    }
}
