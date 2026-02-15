using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue : MonoBehaviour
{
    // =========================================================
    //  UI BÁSICA (nombre + texto)
    // =========================================================
    [Header("UI")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Audio (Text Blips)")]
    public TextBlipController blipController;

    [Header("Afinidad")]
    public AffinityPopupUI affinityPopup;



    // =========================================================
    //  TEXT PRESENTATION
    // =========================================================




    // =========================================================
    //  VISUAL DE PERSONAJES
    // =========================================================
    [Header("Personajes (visual)")]
    public Component characterSlots;

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
    private string _waitType = "";        // Tipo de WAIT (CLICK, SILENCE, BEAT, FINAL_BEAT)

    // Evento para ACT: se dispara cuando el jugador confirma la acción
    public event Action<string> OnActTriggered;

    // =========================================================
    //  JUMP - Salto automático entre escenas
    // =========================================================
    private bool _isJumping = false;           // JUMP activo




    // =========================================================
    //  ARRANQUE
    // =========================================================
    private void Start()
    {
        // Capturar estilo base del dialogueText SIEMPRE (antes de cualquier return)
        if (dialogueText != null)
        {
            _baseColor = dialogueText.color;
            _baseStyle = dialogueText.fontStyle;
            _baseFontSize = dialogueText.fontSize;
        }
        
        // Detectar si venimos de un JUMP de otra escena Unity
        if (PlayerPrefs.GetInt("JUMP_ACTIVE", 0) == 1)
        {
            PlayerPrefs.SetInt("JUMP_ACTIVE", 0);
            
            sceneIndex = PlayerPrefs.GetInt("JUMP_SCENE_INDEX", 0);
            string jumpTargetLine = PlayerPrefs.GetString("JUMP_TARGET_LINE", "0");
            bool needsFadeIn = PlayerPrefs.GetInt("JUMP_FADE_IN", 0) == 1;
            
            if (needsFadeIn)
            {
                PlayerPrefs.SetInt("JUMP_FADE_IN", 0);
            }
            
            PlayerPrefs.Save();
            
            Debug.Log($"[VNDialogue] Start: JUMP detectado. Cargando CSV índice {sceneIndex}, línea '{jumpTargetLine}', FadeIn={needsFadeIn}");
            
            LoadScene(sceneIndex);
            
            // Posicionar en la línea destino
            if (jumpTargetLine == "END")
            {
                lineIndex = Mathf.Max(0, currentLines.Count - 1);
            }
            else if (int.TryParse(jumpTargetLine, out int targetLine))
            {
                lineIndex = Mathf.Clamp(targetLine, 0, currentLines.Count - 1);
            }
            else
            {
                lineIndex = 0;
            }
            
            Debug.Log($"[VNDialogue] JUMP aplicado. Posicionado en línea {lineIndex}");
            
            // Si necesita fade in, buscamos el canvas de fade y hacemos fade in
            if (needsFadeIn)
            {
                StartCoroutine(JumpFadeInRoutine());
            }
            else
            {
                ShowLine();
            }
            
            return;
        }
        
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
        if (_isJumping) return;  // Bloquear input durante salto automático
        if (currentLines.Count == 0) return;

        // Si typewriter está corriendo, completarlo en lugar de avanzar
        if (_typewriterCoroutine != null && !_typewriterComplete)
        {
            CompleteTypewriter();
            return;
        }

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

        // Detener typewriter si está corriendo
        StopTypewriterIfRunning();

        // Por si venimos de un ACT anterior, aseguramos estado visual
        if (dialogueText != null)
            dialogueText.color = _baseColor;
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
            StopTypewriterIfRunning();
            
            _isWaiting = true;

            if (nameText != null) nameText.text = "";
            if (dialogueText != null)
            {
                dialogueText.text = line.text ?? "...";
                ApplyWaitStyle();
            }
            
            _typewriterComplete = true;  // WAIT siempre muestra texto completo

            // Procesar comandos (poses, etc.) antes de pausar
            string cmd = (line.cmd ?? "").Trim().Trim('"');
            
            // Parsear tipo de WAIT (CLICK, SILENCE, BEAT, FINAL_BEAT)
            _waitType = ParseValue(cmd, "WAIT");
            
            // Aplicar comandos de personajes (L=LOGAN:pose, R=DAMIAO:pose, etc.)
            if (characterSlots != null && !string.IsNullOrEmpty(cmd))
            {
                ApplyCmdToSlots(cmd);
                
                // Aplicar foco si hay comando de personaje
                string cmdUpper = cmd.ToUpper();
                if (cmdUpper.Contains("L=LOGAN") || cmdUpper.Contains("C=LOGAN"))
                    ApplyFocusToSlots("LOGAN");
                else if (cmdUpper.Contains("R=DAMIAO") || cmdUpper.Contains("C=DAMIAO"))
                    ApplyFocusToSlots("DAMIAO");
                else if (cmdUpper.Contains("L=LAZARUS") || cmdUpper.Contains("R=LAZARUS") || cmdUpper.Contains("C=LAZARUS"))
                    ApplyFocusToSlots("LAZARUS");
            }
            
            // Opcionalmente ocultar personajes si se especifica WAIT_HIDE=1
            if (characterSlots != null && ParseValue(cmd, "WAIT_HIDE") == "1")
            {
                NarratorMomentToSlots();
            }
            // De lo contrario, los personajes permanecen visibles para dramatismo

            return;
        }

        // =====================================================
        //  ACT (Micro-acción con evento real)
        // =====================================================
        if (speakerUpper == "ACT")
        {
            StopTypewriterIfRunning();
            
            _actActive = true;

            string cmd = (line.cmd ?? "").Trim().Trim('"');
            _pendingActId = ParseValue(cmd, "ACT");

            if (nameText != null) nameText.text = "";
            if (dialogueText != null)
            {
                dialogueText.text = line.text ?? "[Acción]";
                dialogueText.color = new Color(1f, 0.9f, 0.5f);
            }
            
            _typewriterComplete = true;  // ACT muestra texto completo
            
            // Aplicar comandos de personajes (L=LOGAN:pose, R=DAMIAO:pose, C=LOGAN:pose, etc.)
            if (characterSlots != null && !string.IsNullOrEmpty(cmd))
            {
                ApplyCmdToSlots(cmd);
            }

            return;
        }

        // =====================================================
        //  JUMP (Salto automático a otra escena)
        // =====================================================
        if (speakerUpper == "JUMP")
        {
            StopTypewriterIfRunning();
            
            Debug.Log($"[VNDialogue] JUMP detectado! Target: {ParseValue((line.cmd ?? "").Trim().Trim('\"'), "JUMP_SCENE")}");
            
            _isJumping = true;

            string cmd = (line.cmd ?? "").Trim().Trim('"');
            string jumpTargetScene = ParseValue(cmd, "JUMP_SCENE");
            string jumpTargetLine = ParseValue(cmd, "JUMP_LINE");
            string jumpUnityScene = ParseValue(cmd, "JUMP_UNITY_SCENE");  // Opcional: cambiar escena Unity
            
            // Detectar si se debe mantener la música (sin fade out)
            string skipMusicFade = ParseValue(cmd, "SKIP_MUSIC_FADE");
            if (skipMusicFade == "1" || skipMusicFade.ToUpper() == "TRUE")
            {
                VNTransitionFlags.SkipMusicFadeOnce = true;
                Debug.Log("[VNDialogue] SKIP_MUSIC_FADE activado. La música continuará sin fade.");
            }

            Debug.Log($"[VNDialogue] JUMP configurado: CSV={jumpTargetScene}, Line={jumpTargetLine}, UnityScene={jumpUnityScene}");

            // Mostrar texto opcional durante la espera
            if (nameText != null) nameText.text = "";
            if (dialogueText != null)
            {
                dialogueText.text = line.text ?? "...";
                dialogueText.color = Color.white;
            }
            
            _typewriterComplete = true;  // JUMP muestra texto completo

            // Iniciar salto automático con delay
            StartCoroutine(JumpAfterDelay(jumpTargetScene, jumpTargetLine, jumpUnityScene));

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
            StopTypewriterIfRunning();
            
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
            
            _typewriterComplete = true;  // CHOICE muestra texto completo

            return;
        }

        // =====================================================
        //  DIÁLOGO NORMAL
        // =====================================================
        if (nameText != null)
        {
            nameText.text = speakerRaw;
            ApplySpeakerNameStyle(speakerUpper, speakerRaw);
        }
        
        if (dialogueText != null)
        {
            _currentFullText = line.text ?? "";
            
            // Evitar reiniciar typewriter si es la misma línea
            bool isSameLine = (lineIndex == _lastRenderedLineIndex && _currentFullText == _lastRenderedFullText);
            
            if (!isSameLine)
            {
                // Restaurar estilo normal
                ApplyNormalStyle();
                
                if (enableTypewriter)
                {
                    _typewriterComplete = false;
                    _typewriterCoroutine = StartCoroutine(TypewriterRoutine(_currentFullText));
                }
                else
                {
                    dialogueText.text = _currentFullText;
                    _typewriterComplete = true;
                }
                
                _lastRenderedLineIndex = lineIndex;
                _lastRenderedFullText = _currentFullText;
            }
        }

        if (characterSlots != null)
            ApplyCmdToSlots(line.cmd);

        // Narrador
        if (speakerUpper == "NARRADOR" || string.IsNullOrEmpty(speakerUpper))
        {
            if (nameText != null) nameText.text = "";
            if (dialogueText != null)
                ApplyWaitStyle();  // NARRADOR usa el mismo estilo que WAIT
            
            if (characterSlots != null)
                NarratorMomentToSlots();
            return;
        }

        // Foco
        if (characterSlots != null)
            ApplyFocusToSlots(speakerUpper);
    }

}