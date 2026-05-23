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
        //StaticData.chaosmode = false;
    }
    private void Start()
    {
        disableinteractableingamemode();
        ReturnFromTutorial.interactable = false;
        MusicSlide.interactable = false;
        SFXSlide.interactable = false;
        SettingReturn.interactable = false;
        CircleTransition.SetTrigger("isLoad");
        isstart = true;
    }
    public void pressedPlay()
    {
        Debug.Log("Press Play");
        PlayButton.interactable = false;
        SettingButton.interactable = false;
        ExitButton.interactable = false;
        StartCoroutine(goToGamemode());
    }

    public void pressedStetting()
    {
        Debug.Log("Press Setting");
        PlayButton.interactable = false;
        SettingButton.interactable = false;
        ExitButton.interactable = false;
        StartCoroutine(goToSetting());
    }
    public void pressedExit()
    {
        Debug.Log("Press Exit");
        PlayButton.interactable = false;
        SettingButton.interactable = false;
        ExitButton.interactable = false;
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
    }
    public void pressedReturn()
    {
        Debug.Log("Press Return");
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
        ReturnFromTutorial.interactable = false;
        Tutorial.SetBool("Tutorial", false);
        StartCoroutine(backtoGamemode());
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
       // StaticData.chaosmode = true;
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
