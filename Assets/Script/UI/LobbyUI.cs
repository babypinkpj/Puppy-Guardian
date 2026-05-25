using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [Header("MenuButton")]
    public Button PlayButton;
    public Button SettingButton;
    public Button ExitButton;
    public Button CosmeticsButton;

    [Header("SettingButton")]
    public Slider MusicSlide;
    public Slider SFXSlide;
    public Button SettingReturn;

    [Header("GamemodeButton")]
    public Button CasualButton;
    public Button ChaosButton;
    public Button ReturnButton;
    public Button TutorialButton;
    public Button ReturnFromTutorial;

    [Header("Cosmetics")]
    public Button ReturnFromCosmetics;

    [Header("Description")]
    public GameObject CasualDesc;
    public GameObject ChaosDesc;
    private bool isstart = false;

    [Header("Animation Stuff")]
    public Animator BGGamemode;
    public Animator Gamemode;
    public Animator Tutorial;
    public Animator CircleTransition;
    public GameObject LoadingText;
    public Animator Setting;
    public Animator CosmeticsAnimator;
    public Animator BGCosmetics;

    void disableinteractableingamemode()
    {
        CasualButton.interactable = false;
        ChaosButton.interactable = false;
        ReturnButton.interactable = false;
        TutorialButton.interactable = false;
    }
    void ableinteractableingamemode()
    {
        CasualButton.interactable = true;
        ChaosButton.interactable = true;
        ReturnButton.interactable = true;
        TutorialButton.interactable = true;
    }
    private void Awake()
    {
        Time.timeScale = 1;
        StaticData.chaosmode = false;
    }
    [Header("Boot Loading")]
    public AudioScript audioScript;

    private void Start()
    {
        disableinteractableingamemode();
        ReturnFromTutorial.interactable = false;
        MusicSlide.interactable = false;
        SFXSlide.interactable = false;
        SettingReturn.interactable = false;
        if (ReturnFromCosmetics != null) ReturnFromCosmetics.interactable = false;
        
        StartCoroutine(BootUpSequence());
    }

    IEnumerator BootUpSequence()
    {
        // 1. Show existing LoadingText
        if (LoadingText != null) LoadingText.SetActive(true);
        
        // 2. Wait a few seconds for assets/services to settle
        yield return new WaitForSeconds(3f);
        
        if (LoadingText != null) LoadingText.SetActive(false);

        // 3. Trigger the circle transition to reveal the menu
        if (CircleTransition != null) CircleTransition.SetTrigger("isLoad");
        isstart = true;

        // 4. Play the music PERFECTLY in sync with the circle transition
        if (audioScript != null)
        {
            audioScript.PlayMenuIntro();
        }
    }

    public void pressedPlay()
    {
        Debug.Log("Press Play");
        PlayButton.interactable = false;
        SettingButton.interactable = false;
        ExitButton.interactable = false;
        if (CosmeticsButton != null) CosmeticsButton.interactable = false;
        StartCoroutine(goToGamemode());
    }

    public void pressedCosmetics()
    {
        Debug.Log("Press Cosmetics");
        PlayButton.interactable = false;
        SettingButton.interactable = false;
        ExitButton.interactable = false;
        if (CosmeticsButton != null) CosmeticsButton.interactable = false;
        StartCoroutine(goToCosmetics());
    }

    public void pressedReturnCosmetics()
    {
        Debug.Log("Press Return Cosmetics");
        FindAnyObjectByType<OnlineSessionManager>()?.ClearErrorText();
        if (ReturnFromCosmetics != null) ReturnFromCosmetics.interactable = false;
        if (BGCosmetics != null) BGCosmetics.SetBool("Gamemode", false);
        if (CosmeticsAnimator != null) CosmeticsAnimator.SetBool("Gamemode", false); // Using same parameter name assuming it mirrors Gamemode
        StartCoroutine(goToStart());
    }

    public void pressedStetting()
    {
        Debug.Log("Press Setting");
        PlayButton.interactable = false;
        SettingButton.interactable = false;
        ExitButton.interactable = false;
        if (CosmeticsButton != null) CosmeticsButton.interactable = false;
        StartCoroutine(goToSetting());
    }
    public void pressedExit()
    {
        Debug.Log("Press Exit");
        PlayButton.interactable = false;
        SettingButton.interactable = false;
        ExitButton.interactable = false;
        if (CosmeticsButton != null) CosmeticsButton.interactable = false;
        StartCoroutine(ExitTimer());
    }

    IEnumerator goToSetting()
    {
        yield return new WaitForSeconds(0.1f);
        Setting.SetBool("isSetting", true);
        yield return new WaitForSeconds(0.5f);
        MusicSlide.interactable = true;
        SFXSlide.interactable = true;
        SettingReturn.interactable = true;
    }
    public void pressedReturnSetting()
    {
        Debug.Log("Press Return");
        FindAnyObjectByType<OnlineSessionManager>()?.ClearErrorText();
        Setting.SetBool("isSetting", false);
        MusicSlide.interactable = false;
        SFXSlide.interactable = false;
        SettingReturn.interactable = false;
        StartCoroutine(goToStart());
    }

    IEnumerator goToStart()
    {
        yield return new WaitForSeconds(0.5f);
        PlayButton.interactable = true;
        SettingButton.interactable = true;
        ExitButton.interactable = true;
        if (CosmeticsButton != null) CosmeticsButton.interactable = true;
    }
    public void pressedReturn()
    {
        Debug.Log("Press Return");
        FindAnyObjectByType<OnlineSessionManager>()?.ClearErrorText();
        isstart = true;
        disableinteractableingamemode();
        BGGamemode.SetBool("Gamemode", false);
        Gamemode.SetBool("Gamemode", false);
        StartCoroutine(gotoMenu());
    }

    public void pressedTutorial()
    {
        Debug.Log("Press Tutorial");
        disableinteractableingamemode();
        Tutorial.SetBool("Tutorial", true);
        StartCoroutine(gotoTutorial());
    }

    public void pressedReturntoGamemode()
    {
        Debug.Log("Press Return");
        FindAnyObjectByType<OnlineSessionManager>()?.ClearErrorText();
        ReturnFromTutorial.interactable = false;
        Tutorial.SetBool("Tutorial", false);
        StartCoroutine(backtoGamemode());
    }

    IEnumerator goToCosmetics()
    {
        Debug.Log("Starting goToCosmetics Coroutine...");
        yield return new WaitForSeconds(0.4f);
        if (BGCosmetics != null) BGCosmetics.SetBool("Gamemode", true);
        if (CosmeticsAnimator != null) CosmeticsAnimator.SetBool("Gamemode", true);
        yield return new WaitForSeconds(1.5f);
        if (ReturnFromCosmetics != null) ReturnFromCosmetics.interactable = true;
    }

    public void enterCasualDesc()
    {
        if(!isstart) CasualDesc.SetActive(true);
    }
    public void exitCasualDesc() 
    {
        CasualDesc.SetActive(false);
    }
    public void enterChaosDesc()
    {
        if (!isstart) ChaosDesc.SetActive(true);
    }
    public void exitChaosDesc() 
    {
        ChaosDesc.SetActive(false);
    }

    public void startgame(int sceneID)
    {
        Debug.Log("Press Start");
        isstart = true;
        CasualDesc.SetActive(false);
        ChaosDesc.SetActive(false);
        disableinteractableingamemode();
        StartCoroutine(setup(sceneID));
    }
    public void activateChaos()
    {
        StaticData.chaosmode = true;
    }
    IEnumerator setup(int sceneID)
    {
        yield return new WaitForSeconds(1.5f);
        CircleTransition.SetBool("OutIn", true);
        yield return new WaitForSeconds(0.5f);
        LoadingText.SetActive(true);
        SceneManager.LoadScene(sceneID);
    }
    IEnumerator backtoGamemode()
    {
        yield return new WaitForSeconds(1);
        ableinteractableingamemode();
        isstart = false;
    }
    IEnumerator gotoTutorial()
    {
        yield return new WaitForSeconds(1);
        ReturnFromTutorial.interactable = true;
    }
    IEnumerator gotoMenu()
    {
        yield return new WaitForSeconds(1.5f);
        PlayButton.interactable = true;
        SettingButton.interactable = true;
        ExitButton.interactable = true;
        if (CosmeticsButton != null) CosmeticsButton.interactable = true;
    }
    IEnumerator goToGamemode()
    {
        yield return new WaitForSeconds(0.4f);
        BGGamemode.SetBool("Gamemode", true);
        Gamemode.SetBool("Gamemode", true);
        yield return new WaitForSeconds(1.5f);
        ableinteractableingamemode();
        isstart = false;
    }
    IEnumerator ExitTimer()
    {
        yield return new WaitForSeconds(0.4f);
        Application.Quit();
    }

}
