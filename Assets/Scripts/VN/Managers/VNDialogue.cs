using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private string _jumpTargetScene = "";      // Escena CSV destino
    private string _jumpTargetLine = "";       // Línea destino (END o número)
    private string _jumpUnityScene = "";       // Escena Unity destino (opcional)
    
    // Tiempo de espera antes del salto (configurable)
    private const float JUMP_WAIT_TIME = 1.5f;

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
            _actActive = true;

            string cmd = (line.cmd ?? "").Trim().Trim('"');
            _pendingActId = ParseValue(cmd, "ACT");

            if (nameText != null) nameText.text = "";
            if (dialogueText != null)
            {
                dialogueText.text = line.text ?? "[Acción]";
                dialogueText.color = new Color(1f, 0.9f, 0.5f);
            }
            
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
            Debug.Log($"[VNDialogue] JUMP detectado! Target: {ParseValue((line.cmd ?? "").Trim().Trim('\"'), "JUMP_SCENE")}");
            
            _isJumping = true;

            string cmd = (line.cmd ?? "").Trim().Trim('"');
            _jumpTargetScene = ParseValue(cmd, "JUMP_SCENE");
            _jumpTargetLine = ParseValue(cmd, "JUMP_LINE");
            _jumpUnityScene = ParseValue(cmd, "JUMP_UNITY_SCENE");  // Opcional: cambiar escena Unity
            
            // Detectar si se debe mantener la música (sin fade out)
            string skipMusicFade = ParseValue(cmd, "SKIP_MUSIC_FADE");
            if (skipMusicFade == "1" || skipMusicFade.ToUpper() == "TRUE")
            {
                VNTransitionFlags.SkipMusicFadeOnce = true;
                Debug.Log("[VNDialogue] SKIP_MUSIC_FADE activado. La música continuará sin fade.");
            }

            Debug.Log($"[VNDialogue] JUMP configurado: CSV={_jumpTargetScene}, Line={_jumpTargetLine}, UnityScene={_jumpUnityScene}");

            // Mostrar texto opcional durante la espera
            if (nameText != null) nameText.text = "";
            if (dialogueText != null)
            {
                dialogueText.text = line.text ?? "...";
                dialogueText.color = Color.white;
            }

            // Iniciar salto automático con delay
            StartCoroutine(JumpAfterDelay());

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
            ApplyCmdToSlots(line.cmd);

        // Narrador
        if (speakerUpper == "NARRADOR" || string.IsNullOrEmpty(speakerUpper))
        {
            if (characterSlots != null)
                NarratorMomentToSlots();
            return;
        }

        // Foco
        if (characterSlots != null)
            ApplyFocusToSlots(speakerUpper);
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
            ApplyCmdToSlots(cleanCmd);

        if (characterSlots != null && !string.IsNullOrEmpty(speakerToShow))
            ApplyFocusToSlots(speakerToShow.Trim().ToUpper());

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

    // =========================================================
    //  JUMP - Coroutine para salto automático
    // =========================================================
    
    private IEnumerator JumpAfterDelay()
    {
        Debug.Log($"[VNDialogue] JumpAfterDelay iniciado. Esperando {JUMP_WAIT_TIME} segundos...");
        
        // Esperar tiempo configurado antes del salto
        yield return new WaitForSeconds(JUMP_WAIT_TIME);

        Debug.Log($"[VNDialogue] Espera completada. Buscando escena CSV '{_jumpTargetScene}'...");

        // Buscar índice de la escena CSV destino
        int targetSceneIndex = sceneFiles.IndexOf(_jumpTargetScene);
        
        if (targetSceneIndex < 0)
        {
            Debug.LogError($"[VNDialogue] JUMP: Escena CSV '{_jumpTargetScene}' no encontrada en sceneFiles!");
            _isJumping = false;
            AdvanceLineAndShow();
            yield break;
        }

        Debug.Log($"[VNDialogue] Escena CSV encontrada en índice {targetSceneIndex}.");

        // Si se especifica JUMP_UNITY_SCENE, cargar esa escena de Unity con fade
        if (!string.IsNullOrEmpty(_jumpUnityScene))
        {
            Debug.Log($"[VNDialogue] Preparando fade y carga de escena Unity: {_jumpUnityScene}");
            
            // Si el flag de música está activo, persistir AudioSources
            if (VNTransitionFlags.SkipMusicFadeOnce)
            {
                Debug.Log("[VNDialogue] Buscando AudioSources de música para persistir...");
                
                // Buscar todos los AudioSources activos en la escena
                AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
                foreach (AudioSource audioSource in audioSources)
                {
                    if (audioSource.isPlaying)
                    {
                        DontDestroyOnLoad(audioSource.gameObject);
                        Debug.Log($"[VNDialogue] AudioSource '{audioSource.gameObject.name}' marcado como persistente.");
                    }
                }
                
                // Consumir el flag (ya se usó)
                VNTransitionFlags.SkipMusicFadeOnce = false;
            }
            
            // Crear canvas de fade temporal
            GameObject fadeCanvasGO = new GameObject("JumpFadeCanvas");
            Canvas canvas = fadeCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            
            fadeCanvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            fadeCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Panel negro con CanvasGroup
            GameObject panelGO = new GameObject("FadePanel");
            panelGO.transform.SetParent(fadeCanvasGO.transform, false);
            
            UnityEngine.UI.Image img = panelGO.AddComponent<UnityEngine.UI.Image>();
            img.color = Color.black;
            img.raycastTarget = true;
            
            RectTransform rt = panelGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            
            CanvasGroup fadeGroup = panelGO.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = true;
            
            DontDestroyOnLoad(fadeCanvasGO);
            
            // FADE OUT a negro (0.8 segundos)
            float fadeTime = 0.8f;
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / fadeTime);
                fadeGroup.alpha = k;
                yield return null;
            }
            fadeGroup.alpha = 1f;
            
            Debug.Log($"[VNDialogue] Fade completado. Cargando escena...");
            
            // Guardar datos para que la nueva escena los cargue
            PlayerPrefs.SetInt("JUMP_SCENE_INDEX", targetSceneIndex);
            PlayerPrefs.SetString("JUMP_TARGET_LINE", _jumpTargetLine);
            PlayerPrefs.SetInt("JUMP_ACTIVE", 1);
            PlayerPrefs.SetInt("JUMP_FADE_IN", 1);  // Señal para hacer fade in
            PlayerPrefs.Save();
            
            // Cargar escena Unity
            SceneManager.LoadScene(_jumpUnityScene);
            
            yield break;
        }

        // Si NO hay JUMP_UNITY_SCENE, solo cambiar CSV en la misma escena Unity
        Debug.Log($"[VNDialogue] Sin cambio de escena Unity. Cargando CSV...");
        
        sceneIndex = targetSceneIndex;
        LoadScene(sceneIndex);

        Debug.Log($"[VNDialogue] CSV cargado. Total líneas: {currentLines.Count}. Posicionando en '{_jumpTargetLine}'...");

        // Posicionar en la línea destino
        if (_jumpTargetLine == "END")
        {
            lineIndex = Mathf.Max(0, currentLines.Count - 1);
            Debug.Log($"[VNDialogue] Posicionado en END (línea {lineIndex})");
        }
        else if (int.TryParse(_jumpTargetLine, out int targetLine))
        {
            lineIndex = Mathf.Clamp(targetLine, 0, currentLines.Count - 1);
            Debug.Log($"[VNDialogue] Posicionado en línea específica {lineIndex}");
        }
        else
        {
            lineIndex = 0;
            Debug.Log($"[VNDialogue] JUMP_LINE inválido, posicionado en inicio");
        }

        _isJumping = false;
        Debug.Log($"[VNDialogue] JUMP completado. Mostrando línea...");
        ShowLine();
    }

    // =========================================================
    //  HELPERS PARA COMPATIBILIDAD CON AMBOS TIPOS DE SLOTS
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

    /// <summary>
    /// Fade In desde negro después de un JUMP con transición de escena Unity.
    /// Busca el canvas de fade creado por JumpAfterDelay y lo desvanece.
    /// </summary>
    private IEnumerator JumpFadeInRoutine()
    {
        Debug.Log("[VNDialogue] JumpFadeInRoutine iniciado. Buscando canvas de fade...");
        
        // Buscar el canvas de fade que dejó la escena anterior
        GameObject fadeCanvasGO = GameObject.Find("JumpFadeCanvas");
        
        if (fadeCanvasGO == null)
        {
            Debug.LogWarning("[VNDialogue] No se encontró JumpFadeCanvas. Mostrando sin fade.");
            ShowLine();
            yield break;
        }
        
        CanvasGroup fadeGroup = fadeCanvasGO.GetComponentInChildren<CanvasGroup>();
        
        if (fadeGroup == null)
        {
            Debug.LogWarning("[VNDialogue] No se encontró CanvasGroup en JumpFadeCanvas.");
            Destroy(fadeCanvasGO);
            ShowLine();
            yield break;
        }
        
        Debug.Log("[VNDialogue] Canvas de fade encontrado. Iniciando fade in...");
        
        // Asegurar que empieza en negro
        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = true;
        
        // Pequeño delay antes de empezar el fade
        yield return new WaitForSeconds(0.3f);
        
        // Mostrar el diálogo mientras está en negro
        ShowLine();
        
        // FADE IN desde negro (1 segundo)
        float fadeTime = 1f;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            fadeGroup.alpha = 1f - k;  // De 1 a 0
            yield return null;
        }
        
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        
        Debug.Log("[VNDialogue] Fade in completado. Destruyendo canvas.");
        
        // Destruir el canvas de fade
        Destroy(fadeCanvasGO);
    }
}
