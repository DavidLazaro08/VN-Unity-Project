using System;
using System.Collections;
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

    [Header("Afinidad")]
    public AffinityPopupUI affinityPopup;

    [Header("Choice Auto-Advance")]
    public bool autoAdvanceAfterChoice = true;
    public float choiceAutoAdvanceDelay = 5.0f;

    private Coroutine _choiceAutoCo;
    private bool _autoAdvancingChoice = false;

    // =========================================================
    //  VISUAL DE PERSONAJES
    // =========================================================
    [Header("Personajes (visual)")]
    public VNCharacterSlots characterSlots;

    // =========================================================
    //  CHOICES
    // =========================================================
    [Header("Choices")]
    public ChoiceManager choiceManager;

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

    private bool waitingForChoice = false;

    // Guardamos dónde termina el bloque de opciones del CHOICE actual
    private int choiceNextLineIndex = -1;

    // =========================================================
    //  WAIT & ACT - Estados explícitos
    // =========================================================
    private bool _isWaiting = false;      // WAIT activo
    private bool _actActive = false;      // ACT activo
    private string _pendingActId = "";    // ID de la acción pendiente

    // Evento para ACT: se dispara cuando el jugador confirma la acción
    public event Action<string> OnActTriggered;

    // =========================================================
    //  GUARDADO (PlayerPrefs)
    // =========================================================
    private const string SAVE_SCENE = "VN_SAVE_SCENE";
    private const string SAVE_LINE = "VN_SAVE_LINE";

    private const string KEY_HAS_SAVE = "VN_HAS_SAVE";
    private const string KEY_CONTINUE = "VN_CONTINUE";

    // =========================================================
    //  ARRANQUE
    // =========================================================
    private void Start()
    {
        if (PlayerPrefs.GetInt(KEY_CONTINUE, 0) == 1)
        {
            PlayerPrefs.SetInt(KEY_CONTINUE, 0);
            PlayerPrefs.Save();
            LoadGame();
            return;
        }

        // Nueva partida: reiniciamos afinidad y elecciones
        VNGameState.ResetAll();

        LoadScene(sceneIndex);
        ShowLine();
    }

    // =========================================================
    //  CONTROL DE FLUJO
    // =========================================================
    public void Next()
    {
        if (waitingForChoice) return;
        if (_autoAdvancingChoice) return;
        if (currentLines.Count == 0) return;

        // WAIT: salir del estado de espera con el siguiente input
        if (_isWaiting)
        {
            _isWaiting = false;

            // Limpiamos el texto del beat (opcional, pero mantiene la escena limpia)
            if (dialogueText != null) dialogueText.text = "";
            if (nameText != null) nameText.text = "";

            AdvanceLineAndShow();
            return;
        }

        // ACT: confirmar acción y disparar evento
        if (_actActive)
        {
            _actActive = false;

            if (dialogueText != null)
                dialogueText.color = Color.white;

            if (!string.IsNullOrEmpty(_pendingActId))
            {
                OnActTriggered?.Invoke(_pendingActId);
                _pendingActId = "";
            }

            AdvanceLineAndShow();
            return;
        }

        // Flujo normal
        AdvanceLineAndShow();
    }

    private void AdvanceLineAndShow()
    {
        lineIndex++;

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

        lineIndex = 0;
        waitingForChoice = false;
        choiceNextLineIndex = -1;

        // Por si venimos de un ACT anterior, aseguramos estado visual
        if (dialogueText != null)
            dialogueText.color = Color.white;
    }

    private void ShowLine()
    {
        if (currentLines.Count == 0) return;
        if (lineIndex < 0 || lineIndex >= currentLines.Count) return;

        DialogueLine line = currentLines[lineIndex];

        string speakerRaw = (line.speaker ?? "").Trim();
        string speakerUpper = speakerRaw.ToUpper();

        // Si es un marcador de fin de rama que nos encontramos al ejecutar normal, lo saltamos
        if (speakerUpper == "BRANCH_END")
        {
            Next();
            return;
        }

        // =====================================================
        //  WAIT (Pausa real con estado explícito)
        // =====================================================
        if (speakerUpper == "WAIT")
        {
            _isWaiting = true;

            if (nameText != null) nameText.text = "";
            if (dialogueText != null) dialogueText.text = line.text ?? "...";

            // Importante: NO tocamos characterSlots aquí.
            // El objetivo es que el silencio sea dramático con los personajes en pantalla.
            return;
        }

        // =====================================================
        //  ACT (Micro-acción con evento real)
        // =====================================================
        if (speakerUpper == "ACT")
        {
            _actActive = true;

            string cmd = (line.cmd ?? "").Trim().Trim('"');
            _pendingActId = ParseValue(cmd, "ACT");

            if (nameText != null) nameText.text = "";
            if (dialogueText != null)
            {
                dialogueText.text = line.text ?? "[Acción]";
                dialogueText.color = new Color(1f, 0.9f, 0.5f);
            }

            return;
        }

        // =====================================================
        //  BRANCHING (Lógica de Post-Choice)
        // =====================================================
        if (speakerUpper == "BRANCH")
        {
            string cmd = (line.cmd ?? "").Trim().Trim('"');
            string reqId = ParseValue(cmd, "CHOICE_ID");
            string reqOpt = ParseValue(cmd, "CHOICE_OPT");

            string lastId = VNGameState.GetLastChoiceId();
            string lastOpt = VNGameState.GetLastChoiceOpt();

            bool match = (reqId == lastId && reqOpt == lastOpt);

            if (match)
            {
                Next();
            }
            else
            {
                SkipBranchBlock();
            }
            return;
        }

        // =====================================================
        //  CHOICE
        // =====================================================
        if (speakerUpper == "CHOICE")
        {
            waitingForChoice = true;

            List<DialogueLine> options = new List<DialogueLine>();
            int optionIndex = lineIndex + 1;

            int maxOptions = 0;
            if (choiceManager != null && choiceManager.choiceButtons != null)
                maxOptions = choiceManager.choiceButtons.Length;

            while (optionIndex < currentLines.Count && options.Count < maxOptions)
            {
                string sp = (currentLines[optionIndex].speaker ?? "").Trim().ToUpper();
                if (sp == "CHOICE") break;

                options.Add(currentLines[optionIndex]);
                optionIndex++;
            }

            choiceNextLineIndex = optionIndex;

            if (choiceManager != null)
                choiceManager.ShowChoices(line.text, options, this);

            return;
        }

        // =====================================================
        //  DIÁLOGO NORMAL
        // =====================================================
        if (nameText != null) nameText.text = speakerRaw;
        if (dialogueText != null)
        {
            dialogueText.text = line.text ?? "";
            dialogueText.color = Color.white;
        }

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

    // Avanza línea a línea hasta encontrar otro BRANCH, un BRANCH_END, o salir de la zona
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

    // Extrae valores "CLAVE=VALOR" del string cmd (separado por ;)
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

    // =========================================================
    //  LLAMADO DESDE ChoiceManager
    // =========================================================
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
        string speakerToShow = string.IsNullOrEmpty(realSpeaker) ? chosenLine.speaker : realSpeaker;

        if (nameText != null) nameText.text = speakerToShow;
        if (dialogueText != null)
        {
            dialogueText.text = chosenLine.text ?? "";
            dialogueText.color = Color.white;
        }

        if (characterSlots != null)
            characterSlots.ApplyCmd(cleanCmd);

        if (characterSlots != null && !string.IsNullOrEmpty(speakerToShow))
            characterSlots.ApplyFocus(speakerToShow.Trim().ToUpper());

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

    private IEnumerator AutoAdvanceChoice()
    {
        yield return new WaitForSeconds(choiceAutoAdvanceDelay);
        _autoAdvancingChoice = false;
        Next();
    }

    // Deducción simple del speaker desde cmd (para choices)
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

    // =========================================================
    //  SAVE / LOAD
    // =========================================================
    public void SaveGame()
    {
        PlayerPrefs.SetInt(SAVE_SCENE, sceneIndex);
        PlayerPrefs.SetInt(SAVE_LINE, lineIndex);
        PlayerPrefs.SetInt(KEY_HAS_SAVE, 1);
        PlayerPrefs.Save();
    }

    public void LoadGame()
    {
        if (PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) != 1)
            return;

        sceneIndex = PlayerPrefs.GetInt(SAVE_SCENE, 0);
        lineIndex = PlayerPrefs.GetInt(SAVE_LINE, 0);

        if (sceneIndex < 0 || sceneIndex >= sceneFiles.Count)
            sceneIndex = 0;

        LoadScene(sceneIndex);

        if (lineIndex < 0) lineIndex = 0;
        if (lineIndex >= currentLines.Count)
            lineIndex = Mathf.Max(0, currentLines.Count - 1);

        ShowLine();
    }

    public bool HasSave()
    {
        return PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;
    }
}
