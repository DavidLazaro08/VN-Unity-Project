using System.Collections;
using UnityEngine;

public partial class VNDialogue
{
    /*
     * VNDialogue.Intercept
     * --------------------
     * Conecta el flujo del CSV con el minijuego de interceptación.
     * Permite lanzar el panel, bloquear el avance mientras se juega
     * y guardar el resultado como estado para ramificar la historia.
     */

    // =========================================================
    //  INTERCEPT SYSTEM
    // =========================================================

    [Header("Interceptación (opcional)")]
    public DataInterceptController interceptController;

    // Estado interno del intercept
    private bool _interceptWaitingResult = false;
    private bool _lastInterceptOptimal = false;

    /// <summary>
    /// Inicializa la conexión con el DataInterceptController.
    /// Se llama desde Start() y también sirve como fail-safe si falta la referencia.
    /// </summary>
    public void InitInterceptSystem()
    {
        if (interceptController == null)
        {
            interceptController = FindObjectOfType<DataInterceptController>();
        }

        if (interceptController != null)
        {
            interceptController.OnInterceptComplete -= OnInterceptDone;
            interceptController.OnInterceptComplete += OnInterceptDone;

#if UNITY_EDITOR
            Debug.Log("[VNDialogue] InterceptController conectado.");
#endif
        }
    }

    /// <summary>
    /// Maneja ACT=INTERCEPT_START.
    /// Activa el panel de fragmentos del DataInterceptController.
    /// </summary>
    private void HandleInterceptStart()
    {
        if (interceptController == null)
        {
            Debug.LogWarning("[VNDialogue] Interceptación: no hay DataInterceptController en la escena. Se continúa sin minijuego.");
            AdvanceLineAndShow();
            return;
        }

        // Asegurar suscripción (por si el controlador se activó/recreó)
        interceptController.OnInterceptComplete -= OnInterceptDone;
        interceptController.OnInterceptComplete += OnInterceptDone;

        _interceptWaitingResult = true;
        interceptController.ShowFragmentPanel();
    }

    /// <summary>
    /// Callback cuando el jugador completa la selección de fragmentos.
    /// Guarda el resultado para permitir ramificación posterior.
    /// </summary>
    private void OnInterceptDone(bool optimal)
    {
        _lastInterceptOptimal = optimal;
        _interceptWaitingResult = false;

        // Guardar resultado como "elección virtual" para poder usar BRANCH en el CSV
        VNGameState.SetLastChoice("INTERCEPT", optimal ? "SUCCESS" : "FAIL");

#if UNITY_EDITOR
        Debug.Log($"[VNDialogue] Interceptación finalizada. Resultado: {(optimal ? "SUCCESS" : "FAIL")}");
#endif

        // Volver al flujo del diálogo
        _actActive = false;
        _pendingActId = "";
        AdvanceLineAndShow();
    }

    /// <summary>
    /// Maneja ACT=INTERCEPT_RESULT.
    /// Muestra el texto de resultado según el estado guardado.
    /// </summary>
    private void HandleInterceptResult()
    {
        bool optimal = VNGameState.GetInterceptSuccess();
        string[] resultLines = DataInterceptController.GetResultLines(optimal);

        StartCoroutine(ShowInterceptResultSequence(resultLines));
    }

    private IEnumerator ShowInterceptResultSequence(string[] lines)
    {
        if (nameText != null) nameText.text = "";

        if (dialogueText != null)
        {
            dialogueText.text = string.Join("\n", lines);
            ApplyWaitStyle();
        }

        // Se muestra como bloque único y se espera un click para continuar
        _isWaiting = true;
        _typewriterComplete = true;

        while (_isWaiting)
        {
            yield return null;
        }

        // No avanzamos aquí: VNDialogue.Next() ya avanza cuando el usuario rompe el WAIT.
    }

    /// <summary>
    /// Guarda la decisión moral TRUTH (si se cuenta todo o no) en VNGameState.
    /// </summary>
    private void HandleTruthChoice(string choiceOpt)
    {
        bool tellAll = (choiceOpt == "TODO");
        VNGameState.SetToldFullTruth(tellAll);

#if UNITY_EDITOR
        Debug.Log($"[VNDialogue] Decisión moral (TRUTH): toldFullTruth = {tellAll}");
#endif
    }
}