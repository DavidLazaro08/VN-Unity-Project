using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal del juego.
/// Permite iniciar una nueva partida o salir.
/// </summary>
public class VNMenu : MonoBehaviour
{
    [Header("Escena inicial")]
    public string introSceneName = "Scene_Intro";

    public void StartGame()
    {
        SceneManager.LoadScene(introSceneName);
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
            Debug.Log("[VNMenu] QuitGame() -> saliendo (en Editor paro el Play Mode).");
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
}