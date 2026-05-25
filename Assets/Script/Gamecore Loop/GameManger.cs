using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
//using Cinemachine;
using Unity.VisualScripting;

public class GameManager: MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField] public RobberSpawner startspawn;

    [Header("StartGame")]
    public bool isStartgame = false;
    public bool isPausing = false;
    public int gameCountdown = 3;
    public Animator transition;
    public GameObject countdowntext;
    public TextMeshProUGUI text;
    public Animator countdownanima;
    public Animator darkBG;
    public Animator mainUI;
    public Animator pauseUI;

    [Header("Healer")]
    public GameObject healItem;
    public float spawnTimer = 60f;
    public float despawnTimer = 30f;
    public Transform[] healPosition;

    [Header("Cinemachine")]
    //public CinemachineVirtualCamera virtualCamera;
    //private CinemachineBasicMultiChannelPerlin shakeCamera;
    public float setShakeAmplitude = 1.0f;
    public float setShakeFrequency = 1.0f;
    public float setShakeDuration = 1.0f;
    public float FieldOfView = 65;
    private float CurrentFieldOfView;

    [Header("StartGameTransition")]
    public float startDuration = 4f;
    public Color startColor;
    public Color endColor;
    public float colorDuration = 2f;
    public Image startgameImage;

    [Header("ChaosCountdown")]
    public Color blackColorStart;
    public Color blackColorEnd;
    public Image blackImage;
    public float appearDuration;
    private Color chaosSelectionColorStart;
    private Color chaosSelectionColorEnd;

    [Header("HarmScreen")]
    public Image HarmImage;
    public Color HarmColorStart;
    public Color HarmColorEnd;
    public float HarmDuration;

    [Header("Pause")]
    public Button resumeButton;
    public Button restartButton;
    public Button menuButton;

    [Header("Timer")]
    public TMP_Text  timerText;
    public float gameTime = 30f; // เวลาทั้งหมด (วินาที)
    private bool i60left= false;
    private bool i60right= false;

    [Header("Gameover")]
    public bool isGameover = false;
    public Button restartButton1;
    public Button menuButton1;
    public Animator gameoverAnimation;
    public bool isTimeup = false;
    public TMP_Text gameoverdesc;
    public AudioGameplay audioGameplay;

    [Header("Chaos Mode")]
    public float chaoscooldown;
    public Animator chaosInfo;
    private List<int> eventsDone = new List<int>();

    //find script
    ChaosMode chaosMode;
    EventInfo eventInfo;
    PlayerMovement playerMovement;
    ItemSpawnRNG itemSpawnRNG;
    public bool conditioning = false;
    public bool isJustice = false;
    public bool isRobust = false;
    public bool isCatthief = false;


    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        Result.RestartResult();
        audioGameplay.SFXSource.volume = 1f;
        Time.timeScale = 1;
    }

    private void Start()
    {
        chaosMode = Object.FindFirstObjectByType<ChaosMode>();
        eventInfo = Object.FindFirstObjectByType<EventInfo>();
        playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        itemSpawnRNG = Object.FindFirstObjectByType<ItemSpawnRNG>();
        //shakeCamera = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (chaosMode == null) Debug.Log("Failed to find ChaosMode object!");
        if (playerMovement == null) Debug.Log("Failed to find PlayerMovment object!");
        if (eventInfo == null) Debug.Log("Failed to find EventInfo object!");
        CurrentFieldOfView = FieldOfView;
        StartCoroutine(DelayLoading());
        interactablefalse();
        restartButton1.interactable = false;
        menuButton1.interactable = false;
        //virtualCamera.m_Lens.FieldOfView = FieldOfView / 2;
        StartCoroutine(stratTransition(FieldOfView / 2, CurrentFieldOfView, startDuration));
    }

    IEnumerator stratTransition(float start, float end, float duration)
    {
        float transition = 0f;
        while (transition < duration)
        {
            transition += Time.deltaTime;
            float t = transition / duration;
            //virtualCamera.m_Lens.FieldOfView = Mathf.SmoothStep(start, end, t);
            yield return null;
        }
    }
    private void Update()
    {
        if (isStartgame && !isGameover)
        {
            if (gameTime > 0)
            {
                    gameTime -= Time.deltaTime;
                if (gameTime <= 0.99)
                {
                    gameTime = 0;
                    isTimeup = true;
                    StartCoroutine(Timeup(2));
                }
                if(gameTime <= 60.99 && !i60left)
                {
                    audioGameplay.SFXSource.PlayOneShot(audioGameplay.sfxNear);
                    audioGameplay.songSource.pitch = 1.35f;
                    i60left = true;
                    mainUI.SetTrigger("game60");
                    StartCoroutine(FadeVolume(audioGameplay, 1.35f, 1f));
                }
                if(gameTime <= 10  && !i60right)
                {
                    audioGameplay.SFXSource.PlayOneShot(audioGameplay.sfxCountdown10);
                    i60right = true;
                    mainUI.SetTrigger("game10");
                }
            }
        }
        UpdateTimerUI();
        if (Input.GetKeyDown(KeyCode.Escape) && isStartgame && !isGameover) 
        {
            if (!isPausing)
            {
                StartCoroutine(FadeVolume(audioGameplay, 0.3f, 0.5f));
                resumeButton.interactable = true;
                restartButton.interactable = true;
                menuButton.interactable = true;
                darkBG.SetBool("isDark", true);
                pauseUI.SetBool("ispause", true );
                isPausing = true;
                Time.timeScale = 0f;
            }
            else if (isPausing) 
            {
                StartCoroutine(FadeVolume(audioGameplay, 1, 0.5f));
                interactablefalse();
                darkBG.SetBool("isDark", false);
                pauseUI.SetBool("ispause", false);
                isPausing = false;
                Time.timeScale = 1;
            }
        }

    }
    IEnumerator FadeVolume(AudioGameplay audioSource, float targetVolume, float duration)
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
    IEnumerator FadePitch(AudioGameplay audioSource, float targetPitch, float duration)
    {
        float startPitch = audioSource.songSource.pitch;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            audioSource.songSource.pitch = Mathf.Lerp(startPitch, targetPitch, time / duration);
            yield return null;
        }
        audioSource.songSource.pitch = targetPitch;
    }
    IEnumerator Timeup(int sceneid)
    {
        audioGameplay.SFXSource.PlayOneShot(audioGameplay.sfxTime);
        itemSpawnRNG.setItem();
        audioGameplay.songSource.Stop();
        countdowntext.SetActive(true);
        text.text = "TIMES UP";
        countdownanima.SetTrigger("num");
        Time.timeScale = 0.15f;
        yield return new WaitForSecondsRealtime(5);
        transition.SetBool("OutIn", true);
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(sceneid);
    }
    public void Screen()
    {
        StartCoroutine(ImpactScreenTransition(startgameImage, startColor, endColor, colorDuration));
    }
    IEnumerator HealItem()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnTimer);
            int randomspot = Random.Range(0, healPosition.Length);
            GameObject heal = Instantiate(healItem);
            HealScript deleteHeal = heal.GetComponent<HealScript>();
            deleteHeal.randomspwn = healPosition[randomspot];
            Debug.Log("Healer spawn");
            StartCoroutine(DespawnHealItem(deleteHeal));
            yield return null;
        }
    }
    IEnumerator DespawnHealItem(HealScript deleteHeal)
    {
        yield return new WaitForSeconds(despawnTimer);
        deleteHeal.despawn();
    }
    public void resume()
    {
        interactablefalse();
        darkBG.SetBool("isDark", false);
        pauseUI.SetBool("ispause", false);
        isPausing = false;
        StartCoroutine(FadeVolume(audioGameplay, 1, 0.5f));
        Time.timeScale = 1;
    }
    public void restart(int sceneid)
    {
        restartButton1.interactable = false;
        menuButton1.interactable = false;
        interactablefalse();
        StartCoroutine(restartActive(sceneid));
    }
    IEnumerator restartActive(int sceneid)
    {
        yield return new WaitForSecondsRealtime(1);
        StartCoroutine(FadeVolume(audioGameplay, 0, 0.5f));
        transition.SetBool("OutIn", true);
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(sceneid);

    }
    void interactablefalse()
    {
        resumeButton.interactable = false;
        restartButton.interactable = false;
        menuButton.interactable = false;
    }
    IEnumerator DelayLoading()
    {
        // Fake network loading delay
        countdowntext.SetActive(true);
        text.text = "LOADING...";
        yield return new WaitForSeconds(3); // extra loading time to let network sync

        transition.SetTrigger("isLoadIngame");
        yield return new WaitForSeconds(1);
        StartCoroutine(countdown());
    }
    IEnumerator countdown()
    {
        countdowntext.SetActive(true);
        
        // Start riser SFX as countdown begins
        audioGameplay.PlayStartRiser();

        while (gameCountdown > 0)
        {
            audioGameplay.PlayNumberBeep(); // Play beep per number
            countdownanima.SetTrigger("num");
            text.text = gameCountdown.ToString();
            yield return new WaitForSeconds(1);
            gameCountdown--;
        }
        
        countdownanima.SetTrigger("text");
        mainUI.SetTrigger("gamestart");
        text.text = "START";
        
        // Play the big Game Start sound + actual music exactly here
        audioGameplay.PlayGameStart(); 
        
        StartCoroutine(ImpactScreenTransition(startgameImage, startColor, endColor, colorDuration));
        if (StaticData.chaosmode == true)
        {
            chaosmode();
        }
        isStartgame = true;
        StartCoroutine(HealItem());
        startspawn.triggerStartGame();
        darkBG.SetBool("isDark", false);
        yield return new WaitForSeconds(2);
        countdowntext.SetActive(false);

        if (StaticData.chaosmode == true)
        {
            StartCoroutine(chaosinfo());
        }
    }

    IEnumerator ImpactScreenTransition(Image impactscreen, Color startcolor, Color endcolor, float duration)
    {
        Debug.Log("screen");
        float transition = 0f;
        while (transition < duration)
        {
            transition += Time.deltaTime;
            float t = transition / duration;
            impactscreen.color = Color.Lerp(startcolor, endcolor, t);
            yield return null;
        }
        yield return null;
    }
    IEnumerator chaosinfo()
    {
        yield return new WaitForSeconds(0.5f);
        chaosInfo.SetTrigger("isStart");
    }
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(gameTime / 60);
            int seconds = Mathf.FloorToInt(gameTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void gameover()
    {
        StartCoroutine(FadeVolume(audioGameplay, 0, 1f));
        playerMovement.animator.SetBool("isWalk", false);
        mainUI.SetTrigger("gameover");
        StartCoroutine(gameoveractive());
        ChaosMode chaosMode = Object.FindFirstObjectByType<ChaosMode>();
        chaosMode.eventnameobject.SetActive(false);
        chaosMode.descriptionobject.SetActive(false);
    }

    IEnumerator gameoveractive()
    {
        yield return new WaitForSeconds(2);
        audioGameplay.SFXSource.pitch = 1f;
        audioGameplay.songSource.loop = false;
        audioGameplay.songSource.volume = 1;
        audioGameplay.songSource.clip = audioGameplay.gameoverSong;
        audioGameplay.songSource.Play();
        darkBG.SetBool("isDark", true);
        gameoverAnimation.SetTrigger("gameover");
        yield return new WaitForSeconds(1);
        restartButton1.interactable = true;
        menuButton1.interactable = true;
    }

    public void chaosmode()
    {
        StartCoroutine(runchaos());
    }

    IEnumerator runchaos()
    {
        if (gameTime > 30) {
        Debug.Log("Chaos Event run");
        float pre_chaos = chaoscooldown * 80 / 100;
        float aware_chaos = chaoscooldown - pre_chaos;
        yield return new WaitForSeconds(pre_chaos);
            FieldOfView = 60;
            StartCoroutine(ImpactScreenTransition(blackImage, blackColorStart, blackColorEnd, aware_chaos));
            StartCoroutine(stratTransition(CurrentFieldOfView, FieldOfView, aware_chaos));
        yield return new WaitForSeconds(aware_chaos);
        int randomEvent = 0;
            do
            {
                randomEvent = Random.Range(0, 8);
                //randomEvent = 2;
            }
            while(eventsDone.Contains(randomEvent));
            eventsDone.Add(randomEvent);
            chaosActivate(randomEvent);
        StartCoroutine(runchaos());
        StartCoroutine(stratTransition(FieldOfView, CurrentFieldOfView, 1f));
        FieldOfView = CurrentFieldOfView;
            audioGameplay.SFXSource.PlayOneShot(audioGameplay.sfxchaos);
        }
    }

    void chaosActivate(int setEvent)
    {
        switch (setEvent)
        {
            case 0:
                chaosMode.EventNofi(eventInfo.eventName1, eventInfo.eventDesc1, eventInfo.color1);
                chaosSelectionColorStart = eventInfo.color1; chaosSelectionColorEnd = eventInfo.color1;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(WeightDown());
                break;
            case 1:
                chaosMode.EventNofi(eventInfo.eventName2, eventInfo.eventDesc2, eventInfo.color2);
                chaosSelectionColorStart = eventInfo.color2; chaosSelectionColorEnd = eventInfo.color2;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(SuperConditioning());
                break;
            case 2: // BIRD
                chaosMode.EventNofi(eventInfo.eventName3, eventInfo.eventDesc3, eventInfo.color3);
                chaosSelectionColorStart = eventInfo.color3; chaosSelectionColorEnd = eventInfo.color3;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(PigeonInvasion());
                //player don't affect
                break;
            case 3: // LIGHT OUT
                chaosMode.EventNofi(eventInfo.eventName4, eventInfo.eventDesc4, eventInfo.color4);
                chaosSelectionColorStart = eventInfo.color4; chaosSelectionColorEnd = eventInfo.color4;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(LightOut());
                break;
            case 4:
                chaosMode.EventNofi(eventInfo.eventName5, eventInfo.eventDesc5, eventInfo.color5);
                chaosSelectionColorStart = eventInfo.color5; chaosSelectionColorEnd = eventInfo.color5;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(Justice());
                break;
                
            case 5:
                chaosMode.EventNofi(eventInfo.eventName6, eventInfo.eventDesc6, eventInfo.color6);
                chaosSelectionColorStart = eventInfo.color6; chaosSelectionColorEnd = eventInfo.color6;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(Robust());
                break;
            case 6:
                chaosMode.EventNofi(eventInfo.eventName7, eventInfo.eventDesc7, eventInfo.color7);
                chaosSelectionColorStart = eventInfo.color7; chaosSelectionColorEnd = eventInfo.color7;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(DirtyDuty());
                break;
            case 7:
                chaosMode.EventNofi(eventInfo.eventName8, eventInfo.eventDesc8, eventInfo.color8);
                chaosSelectionColorStart = eventInfo.color8; chaosSelectionColorEnd = eventInfo.color8;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(CatThief());
                break;
            case 8:
                chaosMode.EventNofi(eventInfo.eventName9, eventInfo.eventDesc9, eventInfo.color9);
                chaosSelectionColorStart = eventInfo.color9; chaosSelectionColorEnd = eventInfo.color9;
                chaosSelectionColorStart.a = 0.5f; chaosSelectionColorEnd.a = 0;
                StartCoroutine(Tiredness());
                break;
        }
        StartCoroutine(ImpactScreenTransition(blackImage, chaosSelectionColorStart, chaosSelectionColorEnd, appearDuration));
    }

    IEnumerator WeightDown()
    {
        playerMovement.movementspeed = playerMovement.movementspeed * (100 - eventInfo.num1_1) / 100;
        playerMovement.orginialspeed = playerMovement.orginialspeed * (100 - eventInfo.num1_1) / 100;
        yield return new WaitForSeconds(eventInfo.timer);
        playerMovement.movementspeed = playerMovement.baseSpeed;
        playerMovement.orginialspeed = playerMovement.baseSpeed;
    }
    IEnumerator SuperConditioning()
    {
        conditioning = true;
        yield return new WaitForSeconds(eventInfo.timer);
        conditioning = false;
    }
    //PIGEON SECTION
    IEnumerator PigeonInvasion()
    {
        eventInfo.isPigeonActive = true;
        audioGameplay.SFXSource.PlayOneShot(audioGameplay.sfxbirds);
        StartCoroutine(spawnPigeon());
        yield return new WaitForSeconds(eventInfo.timer);
        eventInfo.isPigeonActive = false;
    }
    IEnumerator spawnPigeon()
    {
        while (eventInfo.isPigeonActive)
        {
            int randomspot = Random.Range(0, eventInfo.pigeonSpawner.Length);
            GameObject pigeonItself = Instantiate(eventInfo.pigeon, eventInfo.pigeonSpawner[randomspot]);
            PigeonPrefab prefab = pigeonItself.GetComponent<PigeonPrefab>();
            prefab.bird = eventInfo.pigeonSpawner[randomspot];
            prefab.spawnOrder = randomspot;
            yield return new WaitForSeconds(eventInfo.spawnCooldown);
        }
    }
    //END THE PIGEON
    //LIGHTOUT SECTION
    IEnumerator LightOut()
    {
        audioGameplay.SFXSource.PlayOneShot(audioGameplay.sfxpowerdown);
        eventInfo.light.SetActive(false);
        eventInfo.lightscree.gameObject.SetActive(true);
        eventInfo.flash.gameObject.SetActive(true);
        StartCoroutine(flash());
        yield return new WaitForSeconds(20f);
        eventInfo.light.SetActive(true);
        StartCoroutine(blackscreen());
        yield return null;
    }
    IEnumerator flash()
    {
        Color color = eventInfo.flash.color;
        float startalpha = color.a;  float endalpha = 0f;
        float duration = 1f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(startalpha, endalpha, t/duration);
            eventInfo.flash.color = color;
            yield return null;
        }
        color.a = endalpha;
        eventInfo.flash.color = color;
    }
    IEnumerator blackscreen()
    {
        Color color = eventInfo.lightscree.color;
        float startalpha = color.a; float endalpha = 0f;
        float duration = 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(startalpha, endalpha, t / duration);
            eventInfo.lightscree.color = color;
            yield return null;
        }
        color.a = endalpha;
        eventInfo.lightscree.color = color;
    }
    //END OF LIGHTOUT SECTION :3
    IEnumerator Justice()
    {
        if(playerMovement.HP < playerMovement.MaxHP * 25 /100) isJustice = true;
        yield return new WaitForSeconds(eventInfo.timer);
        isJustice = false;
    }
    IEnumerator Robust()
    {
        isRobust = true;
        yield return new WaitForSeconds(eventInfo.timer);
        isRobust = false;
    }
    IEnumerator DirtyDuty()
    {
        playerMovement.stunTime = playerMovement.stunTime * (100 +  eventInfo.num7_1) / 100;
        yield return new WaitForSeconds(eventInfo.timer);
        playerMovement.stunTime = playerMovement.baseStunTime;
    }
    IEnumerator CatThief()
    {
        isCatthief = true;
        yield return new WaitForSeconds(eventInfo.timer);
        isCatthief = false;
    }
    IEnumerator Tiredness()
    {
        playerMovement.BarkingCooldown = playerMovement.BarkingCooldown * (100 + eventInfo.num9_1) / 100;
        playerMovement.AttackingCooldown = playerMovement.AttackingCooldown * (100 + eventInfo.num9_1) / 100;
        yield return new WaitForSeconds(eventInfo.timer);
        playerMovement.BarkingCooldown = playerMovement.baseCooldown1;
        playerMovement.AttackingCooldown = playerMovement.baseCooldown2;
    }
    public void PlayerHurt()
    {
        FieldOfView = 55;
        StartCoroutine(shakeTrigger(setShakeDuration));
        StartCoroutine(ImpactScreenTransition(HarmImage, HarmColorStart, HarmColorEnd, HarmDuration));
        StartCoroutine(stratTransition(FieldOfView, CurrentFieldOfView, HarmDuration / 2));
        FieldOfView = CurrentFieldOfView;
    }

    IEnumerator shakeTrigger(float duration)
    {
        //shakeCamera.m_AmplitudeGain = setShakeAmplitude;
        //shakeCamera.m_FrequencyGain = setShakeFrequency;
        float transition = 0f;
        while (transition < duration)
        {
            transition += Time.deltaTime;
            float t = transition / duration;
            //shakeCamera.m_FrequencyGain = Mathf.Lerp(shakeCamera.m_FrequencyGain, 0, t);
            //shakeCamera.m_AmplitudeGain = Mathf.Lerp(shakeCamera.m_AmplitudeGain, 0, t);
            yield return null;
        }
    }
}
