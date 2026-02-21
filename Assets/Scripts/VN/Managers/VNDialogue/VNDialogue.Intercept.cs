using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class VNDialogue
{
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
    /// Llamar desde Start() o manualmente al configurar la escena.
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
            Debug.Log("[VNDialogue] InterceptController conectado.");
        }
    }

    /// <summary>
    /// Maneja la acción ACT=INTERCEPT_START.
    /// Activa el panel de fragmentos del DataInterceptController.
    /// </summary>
    private void HandleInterceptStart()
    {
        if (interceptController == null)
        {
            Debug.LogWarning("[VNDialogue] No hay InterceptController asignado. Saltando interceptación.");
            AdvanceLineAndShow();
            return;
        }

        // Asegurar que estamos suscritos
        interceptController.OnInterceptComplete -= OnInterceptDone;
        interceptController.OnInterceptComplete += OnInterceptDone;

        _interceptWaitingResult = true;
        interceptController.ShowFragmentPanel();
    }

    /// <summary>
    /// Callback cuando el jugador completa la selección de fragmentos.
    /// </summary>
    private void OnInterceptDone(bool optimal)
    {
        _lastInterceptOptimal = optimal;
        _interceptWaitingResult = false;

        // Guardar resultado como una "elección virtual" para permitir ramas en el CSV
        VNGameState.SetLastChoice("INTERCEPT", optimal ? "SUCCESS" : "FAIL");

        Debug.Log($"[VNDialogue] Interceptación terminada. Óptimo: {optimal}. Avanzando diálogo.");

        // Avanzar diálogo
        _actActive = false;
        _pendingActId = "";
        AdvanceLineAndShow();
    }

    /// <summary>
    /// Maneja ACT=INTERCEPT_RESULT.
    /// Muestra el texto de resultado dinámico según la evaluación.
    /// </summary>
    private void HandleInterceptResult()
    {
        bool optimal = VNGameState.GetInterceptSuccess();
        string[] resultLines = DataInterceptController.GetResultLines(optimal);

        // Mostrar resultado como secuencia de texto
        StartCoroutine(ShowInterceptResultSequence(resultLines));
    }

    private IEnumerator ShowInterceptResultSequence(string[] lines)
    {
        if (nameText != null) nameText.text = "";

        if (dialogueText != null)
        {
            // Unir todas las líneas con saltos de línea para mostrar el bloque completo
            dialogueText.text = string.Join("\n", lines);
            ApplyWaitStyle();
        }

        // Esperar click único para todo el bloque
        _isWaiting = true;
        _typewriterComplete = true;

        while (_isWaiting)
        {
            yield return null;
        }

        // NO llamar a AdvanceLineAndShow() aquí. 
        // VNDialogue.Next() ya lo hace cuando _isWaiting pasa a false por click del usuario.
    }

    /// <summary>
    /// Maneja la decisión moral TRUTH (le cuentas todo / parcial).
    /// Guarda toldFullTruth en VNGameState.
    /// </summary>
    private void HandleTruthChoice(string choiceOpt)
    {
        bool tellAll = (choiceOpt == "TODO");
        VNGameState.SetToldFullTruth(tellAll);

        Debug.Log($"[VNDialogue] Decisión moral: toldFullTruth = {tellAll}");
    }
}
