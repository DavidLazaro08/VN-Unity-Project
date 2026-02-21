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

    // Colores del panel estilo terminal
    private readonly Color COLOR_NORMAL    = new Color(0.0f, 0.85f, 0.55f, 1f);  // Verde terminal
    private readonly Color COLOR_SELECTED  = new Color(1.0f, 0.95f, 0.3f, 1f);   // Amarillo destacado
    private readonly Color COLOR_DISABLED  = new Color(0.3f, 0.3f, 0.3f, 0.6f);  // Gris apagado
    private readonly Color COLOR_BG_NORMAL = new Color(0.05f, 0.08f, 0.05f, 0.9f); // Fondo oscuro
    private readonly Color COLOR_BG_SELECTED = new Color(0.1f, 0.15f, 0.05f, 0.95f); // Fondo seleccionado

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

        panelGO.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.04f, 0.95f);
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
        
        // Ajustar anchors para alinear a la izquierda (X = -550)
        RectTransform rtPanel = reconArea.AddComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.5f, 0);
        rtPanel.anchorMax = new Vector2(0.5f, 0);
        rtPanel.pivot = new Vector2(0, 0);
        rtPanel.anchoredPosition = new Vector2(-550, 15); // Un poco más abajo (antes 50)
        rtPanel.sizeDelta = new Vector2(600, 120); // Tamaño fijo razonable

        Image img = reconArea.AddComponent<Image>();
        img.color = new Color(0, 0.1f, 0.05f, 0.9f); // Más opaco
        
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

        bool isOptimal = (optimalCount == 3);
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
