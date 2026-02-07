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
    public float choiceAutoAdvanceDelay = 5.0f; // Tiempo de espera antes de saltar

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
    //  GUARDADO (PlayerPrefs)
    // =========================================================
    private const string SAVE_SCENE = "VN_SAVE_SCENE";
    private const string SAVE_LINE  = "VN_SAVE_LINE";

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

        // NUEVA PARTIDA: Reiniciamos afinidad y elecciones
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
        if (_autoAdvancingChoice) return; // Bloqueamos input durante el auto-advance
        if (currentLines.Count == 0) return;

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
    }

    private void ShowLine()
    {
        if (currentLines.Count == 0) return;
        if (lineIndex < 0 || lineIndex >= currentLines.Count) return;

        DialogueLine line = currentLines[lineIndex];

        string speakerRaw = (line.speaker ?? "").Trim();
        string speakerUpper = speakerRaw.ToUpper();

        // Si es un marcador de fin de rama que nos encontramos al ejecutar normal, saltamos
        if (speakerUpper == "BRANCH_END")
        {
            Next();
            return;
        }

        // =====================================================
        //  BRANCHING (Lógica de Post-Choice)
        // =====================================================
        if (speakerUpper == "BRANCH")
        {
            // Parseamos ID y OPT del comando (quitando comillas si las hay)
            string cmd = (line.cmd ?? "").Trim().Trim('"');
            string reqId = ParseValue(cmd, "CHOICE_ID");
            string reqOpt = ParseValue(cmd, "CHOICE_OPT");

            string lastId = VNGameState.GetLastChoiceId();
            string lastOpt = VNGameState.GetLastChoiceOpt();

            // ¿Coincide con la última elección?
            bool match = (reqId == lastId && reqOpt == lastOpt);

            if (match)
            {
                // COINCIDE: Entramos en el bloque.
                // Simplemente avanzamos a la siguiente línea (que es el contenido del branch)
                Next(); 
            }
            else
            {
                // NO COINCIDE: Saltamos todo el bloque hasta el siguiente BRANCH 
                // o hasta que se acabe la zona de branching.
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

            // Opciones: van justo después del CHOICE
            List<DialogueLine> options = new List<DialogueLine>();
            int optionIndex = lineIndex + 1;

            // IMPORTANTE:
            // Aquí asumimos que las opciones son líneas normales
            // y que acaban cuando llega una línea "NORMAL" que ya no es opción.
            // Como tu CSV no marca "ENDCHOICE", lo más robusto es:
            // - Tomar SOLO tantas opciones como botones tengas (3)
            // - y luego continuar.
            int maxOptions = 0;
            if (choiceManager != null && choiceManager.choiceButtons != null)
                maxOptions = choiceManager.choiceButtons.Length;

            while (optionIndex < currentLines.Count && options.Count < maxOptions)
            {
                // Si te encuentras otro CHOICE, paras (por seguridad)
                string sp = (currentLines[optionIndex].speaker ?? "").Trim().ToUpper();
                if (sp == "CHOICE") break;

                options.Add(currentLines[optionIndex]);
                optionIndex++;
            }

            // Guardamos dónde continuar después de elegir
            choiceNextLineIndex = optionIndex;

            // Mostramos el panel
            if (choiceManager != null)
                choiceManager.ShowChoices(line.text, options, this);

            return;
        }

        // =====================================================
        //  DIÁLOGO NORMAL
        // =====================================================
        nameText.text = speakerRaw;
        dialogueText.text = line.text ?? "";

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
        // Avanzamos uno desde el BRANCH actual
        lineIndex++; 

        while (lineIndex < currentLines.Count)
        {
            string sp = (currentLines[lineIndex].speaker ?? "").Trim().ToUpper();
            
            // Si encontramos otro BRANCH, paramos (se evaluará en el siguiente path)
            if (sp == "BRANCH")
            {
                ShowLine();
                return;
            }

            // Si encontrado un marcador de FIN DE RAMA, paramos de saltar
            // y mostramos la siguiente línea (el contenido común/merged).
            if (sp == "BRANCH_END")
            {
                lineIndex++; // Saltamos la línea del marcador para situarnos en el contenido
                ShowLine();
                return;
            }
            
            // Si seguimos encontrando texto normal, lo saltamos (es parte del bloque incorrecto).
            lineIndex++;
        }

        // Si llegamos al final, intentamos mostrar (o finaliza escena)
        ShowLine();
    }

    // Helper para extraer valores "CLAVE=VALOR" del string cmd (separado por ;)
    private string ParseValue(string cmd, string key)
    {
        if (string.IsNullOrEmpty(cmd)) return "";
        
        string[] parts = cmd.Split(';');
        foreach (var p in parts)
        {
            string clean = p.Trim();
            if (clean.StartsWith(key + "="))
            {
                return clean.Substring(key.Length + 1);
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

        // ✅ LIMPIEZA CLAVE: quitar comillas y espacios raros del CSV
        string cleanCmd = (chosenLine.cmd ?? "").Trim().Trim('"');

        // 1. PROCESAR ESTADO (CHOICE_ID, OPT, AFFINITY)
        string cId = ParseValue(cleanCmd, "CHOICE_ID");
        string cOpt = ParseValue(cleanCmd, "CHOICE_OPT");
        
        if (!string.IsNullOrEmpty(cId))
        {
            VNGameState.SetLastChoice(cId, cOpt);
        }

        string affStr = ParseValue(cleanCmd, "AFF_DAMIAO");
        if (!string.IsNullOrEmpty(affStr))
        {
            if (int.TryParse(affStr, out int delta))
            {
                VNGameState.AddAffinityDamiao(delta);
                
                // Visual Popup (Manual only)
                if (affinityPopup != null) affinityPopup.ShowDelta(delta);
            }
        }

        // 2. MOSTRAR FRASE ELEGIDA
        string realSpeaker = GetSpeakerFromCmd(cleanCmd);
        string speakerToShow = string.IsNullOrEmpty(realSpeaker) ? chosenLine.speaker : realSpeaker;

        nameText.text = speakerToShow;
        dialogueText.text = chosenLine.text ?? "";

        if (characterSlots != null)
            characterSlots.ApplyCmd(cleanCmd);

        if (characterSlots != null && !string.IsNullOrEmpty(speakerToShow))
            characterSlots.ApplyFocus(speakerToShow.Trim().ToUpper());

        if (choiceNextLineIndex >= 0)
            lineIndex = choiceNextLineIndex - 1;

        // 3. AUTO-ADVANCE LOGIC
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

        // Si en cmd pones L=LOGAN:... asumimos speaker LOGAN
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
        lineIndex  = PlayerPrefs.GetInt(SAVE_LINE, 0);

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
