using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneScript : MonoBehaviour
{
    private int sceneToLoad;
    public AudioSource audioSource;
    public AudioClip buttonSound;

   public void LoadSceneByIndex(int index)
    {
        PlaySoundAndChange(index);
    }
        private void PlaySoundAndChange(int sceneIndex)
    {
        if (audioSource != null && buttonSound != null)
        {
            audioSource.PlayOneShot(buttonSound);
            Invoke(nameof(LoadScene), buttonSound.length);
            sceneToLoad = sceneIndex;
        }
        else
        {
            Debug.Log("Load Scene Game");
            SceneManager.LoadScene(sceneIndex);
        }
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    
    public void QuitGame() 
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
