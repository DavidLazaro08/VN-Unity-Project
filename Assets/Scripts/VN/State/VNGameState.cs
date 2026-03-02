using UnityEngine;

public static class VNGameState
{
    /*
     * VNGameState
     * -----------
     * Estado persistente de la Visual Novel.
     * Centraliza variables de historia (afinidad, última elección, flags) usando PlayerPrefs.
     * Está pensado para que el sistema de diálogo pueda ramificar escenas sin depender de la UI.
     */

    // PlayerPrefs keys
    private const string KEY_AFFINITY_DAMIAO  = "VN_AFF_DAMIAO";
    private const string KEY_LAST_CHOICE_ID   = "VN_LAST_CHOICE_ID";
    private const string KEY_LAST_CHOICE_OPT  = "VN_LAST_CHOICE_OPT";

    // Interceptación / flags narrativos
    private const string KEY_INTERCEPT_SUCCESS = "VN_INTERCEPT_SUCCESS";
    private const string KEY_TOLD_FULL_TRUTH   = "VN_TOLD_FULL_TRUTH";

    // -------------------------------------------------------------------------
    // AFINIDAD (DAMIAO)
    // -------------------------------------------------------------------------

    public static int GetAffinityDamiao()
    {
        return PlayerPrefs.GetInt(KEY_AFFINITY_DAMIAO, 0);
    }

    public static void AddAffinityDamiao(int delta)
    {
        if (delta == 0) return;

        int current = GetAffinityDamiao();
        int next = current + delta;

        PlayerPrefs.SetInt(KEY_AFFINITY_DAMIAO, next);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log($"[VNGameState] Afinidad Damiao: {current} -> {next}");
#endif
    }

    // -------------------------------------------------------------------------
    // ÚLTIMA ELECCIÓN (para branching)
    // -------------------------------------------------------------------------

    public static void SetLastChoice(string choiceId, string choiceOpt)
    {
        PlayerPrefs.SetString(KEY_LAST_CHOICE_ID, choiceId ?? "");
        PlayerPrefs.SetString(KEY_LAST_CHOICE_OPT, choiceOpt ?? "");
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log($"[VNGameState] Última elección: ID={choiceId}, OPT={choiceOpt}");
#endif
    }

    public static string GetLastChoiceId()
    {
        return PlayerPrefs.GetString(KEY_LAST_CHOICE_ID, "");
    }

    public static string GetLastChoiceOpt()
    {
        return PlayerPrefs.GetString(KEY_LAST_CHOICE_OPT, "");
    }

    // -------------------------------------------------------------------------
    // INTERCEPTACIÓN (minijuego) / FLAGS
    // -------------------------------------------------------------------------

    public static bool GetInterceptSuccess()
    {
        return PlayerPrefs.GetInt(KEY_INTERCEPT_SUCCESS, 0) == 1;
    }

    public static void SetInterceptSuccess(bool success)
    {
        PlayerPrefs.SetInt(KEY_INTERCEPT_SUCCESS, success ? 1 : 0);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log($"[VNGameState] InterceptSuccess = {success}");
#endif
    }

    public static bool GetToldFullTruth()
    {
        return PlayerPrefs.GetInt(KEY_TOLD_FULL_TRUTH, 0) == 1;
    }

    public static void SetToldFullTruth(bool full)
    {
        PlayerPrefs.SetInt(KEY_TOLD_FULL_TRUTH, full ? 1 : 0);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log($"[VNGameState] ToldFullTruth = {full}");
#endif
    }

    // -------------------------------------------------------------------------
    // UTILIDADES
    // -------------------------------------------------------------------------

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KEY_AFFINITY_DAMIAO);
        PlayerPrefs.DeleteKey(KEY_LAST_CHOICE_ID);
        PlayerPrefs.DeleteKey(KEY_LAST_CHOICE_OPT);
        PlayerPrefs.DeleteKey(KEY_INTERCEPT_SUCCESS);
        PlayerPrefs.DeleteKey(KEY_TOLD_FULL_TRUTH);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        Debug.Log("[VNGameState] Estado reiniciado.");
#endif
    }
}