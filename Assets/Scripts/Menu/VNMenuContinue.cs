using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VNMenuContinue : MonoBehaviour
{
    [Header("Opcional: desactivar si no hay guardado")]
    public Button continueButton;

    [Header("Escena del juego")]
    public string gameSceneName = "Scene_Game";

    // Claves (tienen que coincidir con las de VNDialogue)
    private const string KEY_HAS_SAVE = "VN_HAS_SAVE";
    private const string KEY_CONTINUE = "VN_CONTINUE";

    private void Start()
    {
        // Si no hay guardado, desactivamos el botón (si lo asignas)
        if (continueButton != null)
        {
            bool hasSave = PlayerPrefs.GetInt(KEY_HAS_SAVE, 0) == 1;
            continueButton.interactable = hasSave;
        }
    }

    public void ContinueGame()
    {
        // Marcamos que al entrar a Scene_Game queremos cargar
        PlayerPrefs.SetInt(KEY_CONTINUE, 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }
}
