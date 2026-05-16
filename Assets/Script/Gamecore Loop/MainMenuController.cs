using UnityEngine;
using UnityEngine.SceneManagement; 

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel; 

    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "LobbyScene"; 


    public void PlayGame()
    {
        
        SceneManager.LoadScene(lobbySceneName);
    }

   
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

 
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

 
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit(); // ปิดเกม (จะได้ผลเมื่อ Build ออกไปเป็นตัวเกมแล้ว)
    }
}