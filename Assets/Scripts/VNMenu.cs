using UnityEngine;
using UnityEngine.SceneManagement;

public class VNMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Scene_Game");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
