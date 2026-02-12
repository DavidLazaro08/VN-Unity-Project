using UnityEngine;
using UnityEngine.SceneManagement;

public class VNMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Scene_Intro");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
