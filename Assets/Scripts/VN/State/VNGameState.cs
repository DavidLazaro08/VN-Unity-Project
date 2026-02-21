using UnityEngine;

/// <summary>
/// Gestor de estado persistente pa la Visual Novel.
/// Guarda afinidad y elecciones usando PlayerPrefs.
/// </summary>
public static class VNGameState
{
    // Claves de PlayerPrefs
    private const string KEY_AFFINITY_DAMIAO = "VN_AFF_DAMIAO";
    private const string KEY_LAST_CHOICE_ID  = "VN_LAST_CHOICE_ID";
    private const string KEY_LAST_CHOICE_OPT = "VN_LAST_CHOICE_OPT";

    // Interceptación
    private const string KEY_INTERCEPT_SUCCESS = "VN_INTERCEPT_SUCCESS";
    private const string KEY_TOLD_FULL_TRUTH   = "VN_TOLD_FULL_TRUTH";

    // --------------------------------------------------------------------------------
    // AFINIDAD (DAMIAO)
    // --------------------------------------------------------------------------------

    public static int GetAffinityDamiao()
    {
        return PlayerPrefs.GetInt(KEY_AFFINITY_DAMIAO, 0);
    }

    public static void AddAffinityDamiao(int delta)
    {
        if (delta == 0) return;

        int current = GetAffinityDamiao();
        PlayerPrefs.SetInt(KEY_AFFINITY_DAMIAO, current + delta);
        PlayerPrefs.Save();
        
        Debug.Log($"[VNGameState] Afinidad Damiao: {current} -> {current + delta}");
    }

    // --------------------------------------------------------------------------------
    // ÚLTIMA ELECCIÓN (Para Branching)
    // --------------------------------------------------------------------------------

    public static void SetLastChoice(string choiceId, string choiceOpt)
    {
        PlayerPrefs.SetString(KEY_LAST_CHOICE_ID, choiceId ?? "");
        PlayerPrefs.SetString(KEY_LAST_CHOICE_OPT, choiceOpt ?? "");
        PlayerPrefs.Save();

        Debug.Log($"[VNGameState] Choice guardada: ID={choiceId}, OPT={choiceOpt}");
    }

    public static string GetLastChoiceId()
    {
        return PlayerPrefs.GetString(KEY_LAST_CHOICE_ID, "");
    }

    public static string GetLastChoiceOpt()
    {
        return PlayerPrefs.GetString(KEY_LAST_CHOICE_OPT, "");
    }

    // --------------------------------------------------------------------------------
    // INTERCEPTACIÓN (Mini-juego)
    // --------------------------------------------------------------------------------

    public static bool GetInterceptSuccess()
    {
        return PlayerPrefs.GetInt(KEY_INTERCEPT_SUCCESS, 0) == 1;
    }

    public static void SetInterceptSuccess(bool success)
    {
        PlayerPrefs.SetInt(KEY_INTERCEPT_SUCCESS, success ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[VNGameState] InterceptSuccess = {success}");
    }

    public static bool GetToldFullTruth()
    {
        return PlayerPrefs.GetInt(KEY_TOLD_FULL_TRUTH, 0) == 1;
    }

    public static void SetToldFullTruth(bool full)
    {
        PlayerPrefs.SetInt(KEY_TOLD_FULL_TRUTH, full ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[VNGameState] ToldFullTruth = {full}");
    }

    // --------------------------------------------------------------------------------
    // UTILIDADES
    // --------------------------------------------------------------------------------

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KEY_AFFINITY_DAMIAO);
        PlayerPrefs.DeleteKey(KEY_LAST_CHOICE_ID);
        PlayerPrefs.DeleteKey(KEY_LAST_CHOICE_OPT);
        PlayerPrefs.DeleteKey(KEY_INTERCEPT_SUCCESS);
        PlayerPrefs.DeleteKey(KEY_TOLD_FULL_TRUTH);
        PlayerPrefs.Save();
        Debug.Log("[VNGameState] Estado reiniciado.");
    }
}
