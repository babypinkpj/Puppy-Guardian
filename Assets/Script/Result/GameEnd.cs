using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameEnd : MonoBehaviour
{
    public List<GameObject> gameObjects = new List<GameObject>();
    public TextMeshProUGUI savedscore, broenscore, stolenscore;
    public Animator animator;
    public AudioResult audioResult;
    public Button restart, menu;
    public ParticleSystem particle;

    private void Awake()
    {
        Time.timeScale = 1.0f;
    }
    void Start()
    {
        StartCoroutine(result());
        Debug.Log(Result.itemlist.Count);
        Debug.Log(Result.itembroken.Count);
        Debug.Log(Result.itemstolen.Count);
    }
    IEnumerator result()
    {
        yield return new WaitForSeconds(0.4f);
        audioResult.drumloop();
        yield return new WaitForSeconds(2.6f);
        for (int i = 0; i < Result.itemlist.Count; i++)
        {
            if (!Result.itembroken.Contains(i) && !Result.itemstolen.Contains(i))
            {
                Debug.Log(i + " is safed");
                gameObjects[i].SetActive(true);
                audioResult.SFXSource.PlayOneShot(audioResult.sfxPop);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Debug.Log(i + " is gone");
                yield return null;
            }
        }
        Debug.Log("Finishing count items");
        yield return null;
        StartCoroutine(score());
    }

    IEnumerator score()
    {
        int saved = Result.itemlist.Count - Result.itembroken.Count - Result.itemstolen.Count;
        savedscore.text = saved.ToString();
        broenscore.text = Result.itembroken.Count.ToString();
        stolenscore.text = Result.itemstolen.Count.ToString();
        yield return new WaitForSeconds(1.5f);
        animator.SetTrigger("isScore");
        audioResult.enddrumloop();
        yield return new WaitForSeconds(1.5f);
        particle.Play();
        yield return new WaitForSeconds(3f);
        audioResult.startsongagain();
        restart.interactable = true;
        menu.interactable = true;

    }
    public void changescene(int sceneid)
    {
        restart.interactable = false;
        menu.interactable = false;
        StartCoroutine(restartActive(sceneid));
    }
    IEnumerator restartActive(int sceneid)
    {
        yield return new WaitForSecondsRealtime(1);
        StartCoroutine(FadeVolume(audioResult, 0, 2f));
        animator.SetTrigger("isOut");
        yield return new WaitForSecondsRealtime(2.2f);
        SceneManager.LoadScene(sceneid);
    }
    IEnumerator FadeVolume(AudioResult audioSource, float targetVolume, float duration)
    {
        float startVolume = audioSource.songSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            audioSource.songSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }

        audioSource.songSource.volume = targetVolume;
    }
}

