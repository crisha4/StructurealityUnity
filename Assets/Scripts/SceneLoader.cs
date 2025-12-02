using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Load scene by name
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    // Load MainMenu specifically
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    // Reload current scene
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // Quit application
    public void QuitApp()
    {
        Application.Quit();
        Debug.Log("App quit!");
    }
}