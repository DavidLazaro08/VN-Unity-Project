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
    // UTILIDADES
    // --------------------------------------------------------------------------------

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(KEY_AFFINITY_DAMIAO);
        PlayerPrefs.DeleteKey(KEY_LAST_CHOICE_ID);
        PlayerPrefs.DeleteKey(KEY_LAST_CHOICE_OPT);
        PlayerPrefs.Save();
        Debug.Log("[VNGameState] Estado reiniciado.");
    }
}
