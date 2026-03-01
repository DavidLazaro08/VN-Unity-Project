using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador ligero para el mini-juego de interceptación de datos.
/// Gestiona la selección de fragmentos, evaluación del resultado,
/// y modificación de humanidad. NO modifica la arquitectura central.
/// </summary>
public class DataInterceptController : MonoBehaviour
{
    // =========================================================
    //  CONFIGURACIÓN
    // =========================================================

    [Header("UI References")]
    [Tooltip("Panel raíz que contiene los botones de fragmentos. Se activa/desactiva.")]
    public GameObject fragmentPanel;

    [Tooltip("Texto superior del panel (instrucciones / contador).")]
    public TextMeshProUGUI headerText;

    [Tooltip("Los 6 botones de fragmentos (asignar en Inspector).")]
    public Button[] fragmentButtons;

    [Header("Resultado UI")]
    [Tooltip("Referencia al VNDialogue para inyectar resultado.")]
    public VNDialogue vnDialogue;

    [Tooltip("Popup de afinidad (opcional, mismo que usa VNDialogue).")]
    public AffinityPopupUI affinityPopup;

    [Header("Reconstrucción Dinámica")]
    [Tooltip("Texto donde se ve la reconstrucción PROYECTO/OBJETO/ESTADO.")]
    public TextMeshProUGUI reconstructionText;

    // =========================================================
    //  DATOS DE FRAGMENTOS
    // =========================================================

    [Serializable]
    public class DataFragment
    {
        public string id;
        public string displayText;
        public bool isOptimal;
        public string field; // "PROYECTO", "OBJETO", "ESTADO"
        public string revealValue; // Lo que rellena en el terminal
    }

    private readonly DataFragment[] fragments = new DataFragment[]
    {
        // Óptimos (Correctos)
        new DataFragment { id = "summ",      displayText = "[ARCHIVE SUMM-- // restringido]", isOptimal = true,  field = "PROYECTO", revealValue = "SUMMER (RE-ACT)" },
        new DataFragment { id = "core",      displayText = "[neural core // activo]",        isOptimal = true,  field = "OBJETO",   revealValue = "NÚCLEO NEURAL (VERIFICADO)" },
        new DataFragment { id = "traslado",  displayText = "[AV-G // traslado nocturno]",    isOptimal = true,  field = "ESTADO",   revealValue = "EN CURSO (TRASLADO NOCTURNO)" },
        
        // Distractores (Incorrectos)
        new DataFragment { id = "error7",    displayText = "[ERROR // sector 7]",            isOptimal = false, field = "OBJETO",   revealValue = "[ERROR_CORRUPCION_SECTOR_7]" },
        new DataFragment { id = "calib",     displayText = "[calibración conductual]",      isOptimal = false, field = "ESTADO",   revealValue = "[CALIBRACIÓN_PENDIENTE]" },
        new DataFragment { id = "prot",      displayText = "[protocolo interno 12-B]",      isOptimal = false, field = "PROYECTO", revealValue = "[DATOS_AMBIGUOS_B12]" },
    };

    // =========================================================
    //  ESTADO INTERNO
    // =========================================================

    private List<int> selectedIndices = new List<int>();
    private int[] shuffledMapping; // Mapeo de botón -> fragmento real
    private const int MAX_SELECTIONS = 3;
    private bool _isActive = false;

    // Colores del panel estilo terminal (paleta azul/cian)
    private readonly Color COLOR_NORMAL    = new Color(0.4f, 0.85f, 1.0f, 1f);
    private readonly Color COLOR_SELECTED  = new Color(0.2f, 0.7f, 1.0f, 1f);
    private readonly Color COLOR_DISABLED  = new Color(0.25f, 0.28f, 0.35f, 0.7f);
    private readonly Color COLOR_BG_NORMAL   = new Color(0.01f, 0.03f, 0.07f, 1f);
    private readonly Color COLOR_BG_SELECTED = new Color(0.03f, 0.08f, 0.18f, 1f);

    // Countdown timer
    private const float COUNTDOWN_DURATION = 22f;
    private TextMeshProUGUI _timerText;
    private Image _timerBar;
    private GameObject _timerRoot;
    private Coroutine _countdownCo;
    private bool _timedOut = false;

    // Evento que emite cuando termina la selección
    public event Action<bool> OnInterceptComplete;

    // =========================================================
    //  CICLO DE VIDA
    // =========================================================

    private void Awake()
    {
        if (fragmentPanel != null)
            fragmentPanel.SetActive(false);
    }

    // =========================================================
    //  API PÚBLICA
    // =========================================================

    /// <summary>
    /// Activa el panel de selección de fragmentos.
    /// Llamado desde VNDialogue cuando se encuentra ACT=INTERCEPT_START.
    /// </summary>
    public void ShowFragmentPanel()
    {
        if (_isActive) return;
        _isActive = true;

        selectedIndices.Clear();
        ShuffleFragments(); // <--- Aleatorizar
        _timedOut = false;

        // 1. Detectar si ya tenemos botones asignados (escena previa del usuario)
        bool hasButtons = (fragmentButtons != null && fragmentButtons.Length > 0);
        
        // 2. Si no hay terminal de texto, intentar añadirlo al panel existente o crear todo
        if (reconstructionText == null)
        {
            if (hasButtons)
            {
                // El usuario ya tiene su UI, solo inyectamos el terminal arriba
                Transform targetParent = fragmentPanel != null ? fragmentPanel.transform : fragmentButtons[0].transform.parent;
                InjectTerminalOnly(targetParent);
            }
            else
            {
                // No hay nada, generar todo de cero
                GenerateFullUI();
            }
        }
        
        // 3. Si no hay panel pero sí botones, auto-detectar panel
        if (fragmentPanel == null && hasButtons)
        {
            fragmentPanel = fragmentButtons[0].transform.parent.gameObject;
        }

        if (fragmentPanel != null)
        {
            fragmentPanel.SetActive(true);
            StartCoroutine(FadeInPanel(fragmentPanel)); // <--- Animación
            SpawnCountdownTimer(fragmentPanel.transform);
            _countdownCo = StartCoroutine(CountdownRoutine());
        }

        UpdateHeader();
        UpdateReconstructionText();
        SetupButtons();

        Debug.Log("[DataInterceptController] Panel de fragmentos activado.");
    }

    private void ShuffleFragments()
    {
        shuffledMapping = new int[fragments.Length];
        for (int i = 0; i < fragments.Length; i++) shuffledMapping[i] = i;

        // Fisher-Yates shuffle
        for (int i = shuffledMapping.Length - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            int temp = shuffledMapping[i];
            shuffledMapping[i] = shuffledMapping[r];
            shuffledMapping[r] = temp;
        }
    }

    private IEnumerator FadeInPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        
        cg.alpha = 0;
        float elapsed = 0;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        cg.alpha = 1;
    }

    private IEnumerator FadeOutPanel(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) yield break;

        float elapsed = 0;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }
        cg.alpha = 0;
    }

    // =========================================================
    //  AUTO-GENERACIÓN DE UI
    // =========================================================

    private void InjectTerminalOnly(Transform parent)
    {
        // Evitar inyectar múltiples veces si el objeto persiste
        if (parent.Find("ReconstructionArea") != null) return;

        Debug.Log("[DataInterceptController] Inyectando terminal en panel existente...");
        GameObject reconArea = CreateTerminalTerminal(parent);
        
        // Ponerlo abajo (al final) y asegurar que tenga espacio en un VerticalLayout
        reconArea.transform.SetAsLastSibling();
    }

    private void GenerateFullUI()
    {
        Debug.Log("[DataInterceptController] Generando UI completa...");

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject panelGO = new GameObject("FragmentPanel");
        panelGO.transform.SetParent(canvas.transform, false);
        fragmentPanel = panelGO;

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.15f);
        panelRect.anchorMax = new Vector2(0.85f, 0.85f);
        panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;

        panelGO.AddComponent<Image>().color = new Color(0.01f, 0.02f, 0.05f, 1f);
        VerticalLayoutGroup vLayout = panelGO.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(30, 30, 30, 30);
        vLayout.spacing = 20;
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

        // Header
        GameObject headerGO = new GameObject("Header");
        headerGO.transform.SetParent(panelGO.transform, false);
        headerText = headerGO.AddComponent<TextMeshProUGUI>();
        headerText.text = "RECONSTRUIR TRANSMISIÓN  [0/3]";
        headerText.fontSize = 24; headerText.color = COLOR_NORMAL;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontStyle = FontStyles.Bold;

        // Terminal
        CreateTerminalTerminal(panelGO.transform);

        // Grid
        GameObject gridGO = new GameObject("ButtonGrid");
        gridGO.transform.SetParent(panelGO.transform, false);
        GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(300, 50); grid.spacing = new Vector2(20, 15);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;

        fragmentButtons = new Button[fragments.Length];
        for (int i = 0; i < fragments.Length; i++)
        {
            GameObject btnGO = new GameObject($"FragBtn_{i}");
            btnGO.transform.SetParent(gridGO.transform, false);
            btnGO.AddComponent<Image>().color = COLOR_BG_NORMAL;
            Button btn = btnGO.AddComponent<Button>();
            
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = textGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one; tRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI btnText = textGO.AddComponent<TextMeshProUGUI>();
            btnText.text = fragments[i].displayText;
            btnText.fontSize = 15; btnText.color = COLOR_NORMAL;
            btnText.alignment = TextAlignmentOptions.Center;

            fragmentButtons[i] = btn;
        }
    }

    private GameObject CreateTerminalTerminal(Transform parent)
    {
        // Contenedor con Fondo (Image)
        GameObject reconArea = new GameObject("ReconstructionArea");
        reconArea.transform.SetParent(parent, false);
        
        // Posición ORIGINAL del panel de reconstrucción
        RectTransform rtPanel = reconArea.AddComponent<RectTransform>();
        rtPanel.anchorMin        = new Vector2(0.5f, 0f);
        rtPanel.anchorMax        = new Vector2(0.5f, 0f);
        rtPanel.pivot            = new Vector2(0f, 0f);
        rtPanel.anchoredPosition = new Vector2(-550f, 15f);
        rtPanel.sizeDelta        = new Vector2(600f, 120f);


        Image img = reconArea.AddComponent<Image>();
        img.color = new Color(0.01f, 0.03f, 0.08f, 1f);
        
        // Texto hijo (TextMeshPro)
        GameObject textGO = new GameObject("ReconstructionText");
        textGO.transform.SetParent(reconArea.transform, false);
        RectTransform rtText = textGO.AddComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero; rtText.anchorMax = Vector2.one; rtText.sizeDelta = Vector2.zero;

        reconstructionText = textGO.AddComponent<TextMeshProUGUI>();
        reconstructionText.fontSize = 20;
        reconstructionText.color = COLOR_NORMAL;
        reconstructionText.alignment = TextAlignmentOptions.Left;
        reconstructionText.margin = new Vector4(20, 10, 20, 10);
        
        return reconArea;
    }

    // =========================================================
    //  LÓGICA
    // =========================================================

    private void SetupButtons()
    {
        for (int i = 0; i < fragmentButtons.Length; i++)
        {
            fragmentButtons[i].gameObject.SetActive(true);
            
            // Usar el mapeo aleatorio para el texto
            int realIndex = shuffledMapping[i];
            TMP_Text btnText = fragmentButtons[i].GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = fragments[realIndex].displayText;

            bool isSelected = selectedIndices.Contains(realIndex);
            UpdateButtonVisual(i, isSelected);

            int buttonSlot = i;
            fragmentButtons[i].onClick.RemoveAllListeners();
            fragmentButtons[i].onClick.AddListener(() => OnFragmentClicked(buttonSlot));
            
            // BLOQUEO DE DECISIÓN: 
            // 1. Si ya está seleccionado, no se puede tocar (irreversible).
            // 2. Si ya llegamos al máximo, no se puede tocar nada más.
            fragmentButtons[i].interactable = !isSelected && (selectedIndices.Count < MAX_SELECTIONS);
        }
    }

    private void OnFragmentClicked(int buttonSlot)
    {
        if (!_isActive) return;

        int realIndex = shuffledMapping[buttonSlot];

        // Solo permitir añadir si no está ya y no hemos llegado al tope
        // (Eliminada la capacidad de deseleccionar)
        if (!selectedIndices.Contains(realIndex) && selectedIndices.Count < MAX_SELECTIONS)
        {
            selectedIndices.Add(realIndex);
        }

        UpdateHeader();
        UpdateReconstructionText();
        SetupButtons();

        if (selectedIndices.Count >= MAX_SELECTIONS)
        {
            StartCoroutine(EvaluateAfterDelay(1.5f));
        }
    }

    private void UpdateReconstructionText()
    {
        if (reconstructionText == null) return;

        string proy = "_______";
        string obj = "_______";
        string est = "_______";

        foreach (int idx in selectedIndices)
        {
            var frag = fragments[idx];
            string val = frag.revealValue;
            if (!frag.isOptimal) val = $"<s>{val}</s>";

            if (frag.field == "PROYECTO") proy = $"<color=#{(frag.isOptimal ? "FFFF4B" : "FF4B4B")}>{val}</color>";
            if (frag.field == "OBJETO")   obj  = $"<color=#{(frag.isOptimal ? "FFFF4B" : "FF4B4B")}>{val}</color>";
            if (frag.field == "ESTADO")   est  = $"<color=#{(frag.isOptimal ? "FFFF4B" : "FF4B4B")}>{val}</color>";
        }

        reconstructionText.text = $"PROYECTO: {proy}\nOBJETO:   {obj}\nESTADO:   {est}";
    }

    private void UpdateButtonVisual(int index, bool selected)
    {
        TMP_Text btnText = fragmentButtons[index].GetComponentInChildren<TMP_Text>();
        Image btnImage = fragmentButtons[index].GetComponent<Image>();
        if (btnText != null) btnText.color = selected ? COLOR_SELECTED : COLOR_NORMAL;
        if (btnImage != null) btnImage.color = selected ? COLOR_BG_SELECTED : COLOR_BG_NORMAL;
    }

    private void UpdateHeader()
    {
        if (headerText == null) return;
        headerText.text = $"RECONSTRUIR TRANSMISIÓN  [{selectedIndices.Count}/{MAX_SELECTIONS}]";
        headerText.color = selectedIndices.Count >= MAX_SELECTIONS ? COLOR_SELECTED : COLOR_NORMAL;
    }

    // =========================================================
    //  EVALUACIÓN
    // =========================================================

    private IEnumerator EvaluateAfterDelay(float delay)
    {
        // Parar el countdown si sigue activo
        if (_countdownCo != null) { StopCoroutine(_countdownCo); _countdownCo = null; }
        if (_timerRoot != null) { Destroy(_timerRoot); _timerRoot = null; }

        foreach (var b in fragmentButtons) b.interactable = false;
        
        // Retardo para dejar ver el terminal completo (2.5s total)
        yield return new WaitForSeconds(delay + 1.0f);
        
        // Iniciar desvanecimiento antes de desactivar
        if (fragmentPanel != null)
        {
            yield return StartCoroutine(FadeOutPanel(fragmentPanel));
        }

        EvaluateSelection();
    }

    private void EvaluateSelection()
    {
        int optimalCount = 0;
        foreach (int idx in selectedIndices) if (fragments[idx].isOptimal) optimalCount++;

        bool isOptimal = (optimalCount == 3) && !_timedOut;
        VNGameState.SetInterceptSuccess(isOptimal);

        int affinityDelta = isOptimal ? +1 : -1;
        VNGameState.AddAffinityDamiao(affinityDelta);
        if (affinityPopup != null) affinityPopup.ShowDelta(affinityDelta);

        if (fragmentPanel != null) fragmentPanel.SetActive(false);
        if (headerText != null) headerText.gameObject.SetActive(false);
        
        _isActive = false;
        OnInterceptComplete?.Invoke(isOptimal);
    }

    // =========================================================
    //  COUNTDOWN TIMER
    // =========================================================

    private void SpawnCountdownTimer(Transform parent)
    {
        // Colocamos el timer a la DERECHA del cuadro de reconstrucción (PROYECTO/OBJETO/ESTADO)
        // El cuadro de reconstrucción: anchorMin/Max=(0.5,0), pivot=(0,0), pos=(-550,15), size=(600,120)
        // El timer se coloca en el hueco a su derecha: pos=(70,15), size=(200,120)
        _timerRoot = new GameObject("_CountdownRoot");
        _timerRoot.transform.SetParent(parent, false);

        RectTransform rtRoot = _timerRoot.AddComponent<RectTransform>();
        // Timer a la IZQUIERDA del panel de reconstrucción:
        // Reconstrucción: anchor(0.5,0), pos(-550,15), size(600,120)
        // Timer: anchor(0.5,0), pos(-780,15), size(210,120)  → queda justo a su izquierda
        rtRoot.anchorMin        = new Vector2(0.5f, 0f);
        rtRoot.anchorMax        = new Vector2(0.5f, 0f);
        rtRoot.pivot            = new Vector2(0f, 0f);
        rtRoot.anchoredPosition = new Vector2(-780f, 15f);
        rtRoot.sizeDelta        = new Vector2(210f, 120f);

        // Fondo oscuro OPACO (para legibilidad sobre el vídeo)
        GameObject bgGO = new GameObject("TimerBg");
        bgGO.transform.SetParent(_timerRoot.transform, false);
        RectTransform rtBg = bgGO.AddComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = rtBg.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.01f, 0.03f, 0.08f, 1f);

        // Etiqueta «// TIEMPO RESTANTE» pequeña arriba
        GameObject labelGO = new GameObject("TimerLabel");
        labelGO.transform.SetParent(_timerRoot.transform, false);
        RectTransform rtLabel = labelGO.AddComponent<RectTransform>();
        rtLabel.anchorMin = new Vector2(0f, 0.72f);
        rtLabel.anchorMax = new Vector2(1f, 1f);
        rtLabel.offsetMin = rtLabel.offsetMax = Vector2.zero;
        TextMeshProUGUI labelTxt = labelGO.AddComponent<TextMeshProUGUI>();
        labelTxt.text = "// TIEMPO RESTANTE";
        labelTxt.fontSize = 10;
        labelTxt.color = new Color(0.4f, 0.85f, 1f, 0.7f);
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.fontStyle = FontStyles.Bold;
        labelTxt.margin = new Vector4(6, 6, 6, 0);

        // Número grande centrado
        GameObject numGO = new GameObject("TimerNumber");
        numGO.transform.SetParent(_timerRoot.transform, false);
        RectTransform rtNum = numGO.AddComponent<RectTransform>();
        rtNum.anchorMin = new Vector2(0f, 0.18f);
        rtNum.anchorMax = new Vector2(1f, 0.72f);
        rtNum.offsetMin = rtNum.offsetMax = Vector2.zero;
        _timerText = numGO.AddComponent<TextMeshProUGUI>();
        _timerText.text = "22";
        _timerText.fontSize = 46;
        _timerText.color = new Color(0.4f, 0.85f, 1f, 1f);
        _timerText.alignment = TextAlignmentOptions.Center;
        _timerText.fontStyle = FontStyles.Bold;

        // Barra de progreso horizontal (fondo + relleno)
        GameObject barBgGO = new GameObject("TimerBarBg");
        barBgGO.transform.SetParent(_timerRoot.transform, false);
        RectTransform rtBarBg = barBgGO.AddComponent<RectTransform>();
        rtBarBg.anchorMin = new Vector2(0.05f, 0.03f);
        rtBarBg.anchorMax = new Vector2(0.95f, 0.16f);
        rtBarBg.offsetMin = rtBarBg.offsetMax = Vector2.zero;
        Image barBgImg = barBgGO.AddComponent<Image>();
        barBgImg.color = new Color(0.1f, 0.15f, 0.2f, 1f);

        GameObject barFillGO = new GameObject("TimerBarFill");
        barFillGO.transform.SetParent(barBgGO.transform, false);
        RectTransform rtBarFill = barFillGO.AddComponent<RectTransform>();
        rtBarFill.anchorMin = Vector2.zero;
        rtBarFill.anchorMax = Vector2.one;
        rtBarFill.offsetMin = rtBarFill.offsetMax = Vector2.zero;
        _timerBar = barFillGO.AddComponent<Image>();
        _timerBar.color = new Color(0.4f, 0.85f, 1f, 1f);
        _timerBar.type = Image.Type.Filled;
        _timerBar.fillMethod = Image.FillMethod.Horizontal;
        _timerBar.fillAmount = 1f;

        // Fade in suave del conjunto
        CanvasGroup cg = _timerRoot.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        StartCoroutine(FadeInTimer(cg));
    }

    private IEnumerator FadeInTimer(CanvasGroup cg)
    {
        float t = 0f;
        while (t < 0.6f) { t += Time.deltaTime; cg.alpha = t / 0.6f; yield return null; }
        cg.alpha = 1f;
    }

    private IEnumerator CountdownRoutine()
    {
        float remaining = COUNTDOWN_DURATION;
        Color cyan   = new Color(0.4f, 0.85f, 1f, 1f);
        Color orange = new Color(1f,   0.55f, 0f, 1f);
        Color red    = new Color(1f,   0.15f, 0.1f, 1f);

        while (remaining > 0f && _isActive)
        {
            remaining -= Time.deltaTime;
            float ratio = Mathf.Clamp01(remaining / COUNTDOWN_DURATION);

            // Color interpola cian -> naranja -> rojo
            Color col = ratio > 0.35f
                ? Color.Lerp(orange, cyan, (ratio - 0.35f) / 0.65f)
                : Color.Lerp(red, orange, ratio / 0.35f);

            int secs = Mathf.CeilToInt(remaining);

            if (_timerText != null)
            {
                _timerText.text = secs.ToString("D2");
                _timerText.color = col;
                // Pulso en los últimos 3 segundos
                if (remaining <= 3f)
                {
                    float pulse = Mathf.Abs(Mathf.Sin(Time.time * 5f));
                    _timerText.transform.localScale = Vector3.one * (1f + pulse * 0.08f);
                }
                else
                {
                    _timerText.transform.localScale = Vector3.one;
                }
            }
            if (_timerBar != null)
            {
                _timerBar.fillAmount = ratio;
                _timerBar.color = col;
            }

            yield return null;
        }

        // Tiempo agotado — si toca, auto-rellenar con elecciones malas
        if (_isActive)
        {
            _timedOut = true;
            AutoFillWithBadChoices();
        }
    }

    private void AutoFillWithBadChoices()
    {
        Debug.Log("[DataInterceptController] TIMEOUT: auto-rellenando con elecciones incorrectas.");

        // Flash rojo en el timer
        if (_timerText != null)
        {
            _timerText.text = "--";
            _timerText.color = new Color(1f, 0.1f, 0.1f, 1f);
        }
        if (_timerBar != null) _timerBar.fillAmount = 0f;

        // Rellenar slots que faltan con fragmentos no-óptimos no usados
        List<int> nonOptimalAvail = new List<int>();
        for (int i = 0; i < fragments.Length; i++)
        {
            if (!fragments[i].isOptimal && !selectedIndices.Contains(i))
                nonOptimalAvail.Add(i);
        }
        // Si se acaban los no-óptimos, usar cualquiera que falte
        List<int> anyAvail = new List<int>();
        for (int i = 0; i < fragments.Length; i++)
            if (!selectedIndices.Contains(i)) anyAvail.Add(i);

        while (selectedIndices.Count < MAX_SELECTIONS)
        {
            if (nonOptimalAvail.Count > 0)
            {
                int pick = nonOptimalAvail[0];
                nonOptimalAvail.RemoveAt(0);
                selectedIndices.Add(pick);
            }
            else if (anyAvail.Count > 0)
            {
                int pick = anyAvail[0];
                anyAvail.RemoveAt(0);
                if (!selectedIndices.Contains(pick))
                    selectedIndices.Add(pick);
            }
            else break;
        }

        UpdateReconstructionText();
        UpdateHeader();
        StartCoroutine(EvaluateAfterDelay(1.0f));
    }

    // =========================================================
    //  RESULTADO — Texto narrativo según evaluación
    // =========================================================

    /// <summary>
    /// Devuelve las líneas de resultado para inyectar en el diálogo.
    /// Llamado por VNDialogue tras recibir OnInterceptComplete.
    /// </summary>
    public static string[] GetResultLines(bool optimal)
    {
        if (optimal)
        {
            return new string[]
            {
                "<size=85%><color=#FFFF4B>PROYECTO SUMMER REACTIVADO</color></size>",
                "<size=85%><color=#FFFF4B>NÚCLEO NEURAL EN TRASLADO NOCTURNO</color></size>",
                "<size=85%><color=#FFFF4B>AUTORIZACIÓN CORPORATIVA ACTIVA</color></size>"
            };
        }
        else
        {
            return new string[]
            {
                "<size=85%><color=#FF4B4B>TRANSFERENCIA DETECTADA</color></size>",
                "<size=85%><color=#FF4B4B>DATOS INCOMPLETOS</color></size>",
                "<size=85%><color=#FF4B4B>AUTORIZACIÓN NO VERIFICADA</color></size>"
            };
        }
    }
}
