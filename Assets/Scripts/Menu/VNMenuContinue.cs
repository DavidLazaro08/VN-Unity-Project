using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Botón "Continuar" del menú.
/// Si existe un guardado, habilita el botón y, al pulsarlo, marca un flag
/// para que la escena del juego cargue desde el punto guardado.
/// </summary>
public class VNMenuContinue : MonoBehaviour
{
    [Header("Opcional: desactivar si no hay guardado")]
    public Button continueButton;

    [Header("Escena del juego")]
    public string gameSceneName = "Scene_Game";

    // Claves (deben coincidir con VNDialogue)
    private const string KEY_HAS_SAVE = "VN_HAS_SAVE";
    private const string KEY_CONTINUE = "VN_CONTINUE";

    private void Start()
    {
        bool hasSave = PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;

        if (continueButton != null)
            continueButton.interactable = hasSave;

#if UNITY_EDITOR
        Debug.Log($"[VNMenuContinue] Guardado detectado: {hasSave}");
#endif
    }

    public void ContinueGame()
    {
        // Señal para que VNDialogue, al arrancar, haga LoadGame()
        PlayerPrefs.SetInt(KEY_CONTINUE, 1);
        PlayerPrefs.Save();

        // Cargar la escena Unity donde se hizo el guardado (no siempre Scene_Game)
        string savedScene = PlayerPrefs.GetString("VN_SAVE_UNITY_SCENE", "");
        string targetScene = !string.IsNullOrEmpty(savedScene) ? savedScene : gameSceneName;

#if UNITY_EDITOR
        Debug.Log($"[VNMenuContinue] Cargando escena guardada: {targetScene}");
#endif

        SceneManager.LoadScene(targetScene);
    }
}