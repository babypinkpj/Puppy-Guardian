using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Rigidbody puppy;
    [SerializeField] public float movementspeed = 1f;
    [SerializeField] private float rotationspeed = 720f;
    private Vector3 movement;
    private bool isCooldown = false;
    private bool stillSlow;
    public float orginialspeed;
    public Animator animator;
    public Transform[] spawnRng;
    public GameObject centerposition;

    [Header("Barking Action Command")]
    public float BarkingCooldown = 0.5f;
    public float BarkingSlowness = 20f;
    public float BarkingSlowDuration = 1f;
    private Coroutine BarkingState;

    [Header("Attack Action Command")]
    public float AttackingCooldown = 1.0f;
    public float AttackingSlowness = 60f;
    public float AttackingSlowDuration = 1f;
    private Coroutine AttackState;

    [Header("Hitbox")]
    public BoxCollider AttackHitbox;
    public BoxCollider BarkingHitbox;

    [Header("Being Attacked")]
    public float knockbackForce = 20f;
    public float stunTime = 4f;
    private bool move = true;
    private bool isStunning = false;

    [Header("Sounds")]
    public AudioClip BarkSound;
    public AudioClip AttackSound;
    public AudioClip WalkSound, sfxOuch, sfxImpact;
    public float WalkSoundDelay = 0.4f;
    private float walkSoundTimer = 0f;
    AudioSource audioSource;
    public AudioGameplay audioGameplay;

    [Header("UI")]
    [SerializeField] public int HP;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectMask2D HPBar;
    [SerializeField] private float maxMask;
    public int MaxHP;
    public GameObject dangerImage;

    [Header("Chaos")]
    [SerializeField] public float baseSpeed, baseStunTime, baseCooldown1, baseCooldown2;
    public EventInfo info;
    public ChaosMode chaosMode;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        orginialspeed = movementspeed;
        //HP SYSTEM
        MaxHP = HP;
        baseSpeed = movementspeed;
        baseStunTime = stunTime;
        baseCooldown1 = BarkingCooldown;
        baseCooldown2 = AttackingCooldown;
        int rng = Random.Range(0, spawnRng.Length);
        centerposition.transform.position = spawnRng[rng].position;
    }
    private void Update()
    {
        if (gameManager.isStartgame == true && gameManager.isPausing == false && gameManager.isGameover == false)
        {
            Movement(); ActionCommand();
        }
        HPSystem(HP);
        if (HP < MaxHP * 25 / 100)
        {
            dangerImage.SetActive(true);
        }
        else if (HP >= MaxHP * 25 / 100)
        {
            dangerImage.SetActive(false);
        }
        if(movement == Vector3.zero)
        {
            animator.SetBool("isWalk", false);
        }
    }

    public void Movement()
    {
        if (move) 
        {
            animator.SetBool("isWalk", true);
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            movement = new Vector3(x, 0, z).normalized;
            float magnitude = movement.magnitude;
            magnitude = Mathf.Clamp01(magnitude);
            puppy.linearVelocity = movement * movementspeed;
        }
        if (movement != Vector3.zero)
        {
        if (Time.time >= walkSoundTimer && move)
        {
            audioSource.PlayOneShot(WalkSound);
            walkSoundTimer = Time.time + WalkSoundDelay;
        }
        Quaternion rotation = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, rotationspeed * Time.deltaTime);
    }
    }
    public void ActionCommand()
    {
        if(!isStunning)
        {
            if (!isCooldown)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (stillSlow)
                    {
                        Debug.Log("slowness still on");
                        if (BarkingState != null)
                        {
                            StopCoroutine(BarkingState);
                            BarkingState = null;
                        }
                        if (AttackState != null)
                        {
                            StopCoroutine(AttackState);
                            AttackState = null;
                        }
                        movementspeed = orginialspeed;
                        stillSlow = false;
                    }
                    Debug.Log("Bark!");
                    StartCoroutine(BarkingActionCommand());
                    BarkingState = StartCoroutine(Slowness(BarkingSlowness, BarkingSlowDuration));
                    audioSource.PlayOneShot(BarkSound);
                }
                else if (Input.GetKeyDown(KeyCode.X))
                {
                    if (stillSlow)
                    {
                        Debug.Log("slowness still on");
                        if (BarkingState != null)
                        {
                            StopCoroutine(BarkingState);
                            BarkingState = null;
                        }
                        if (AttackState != null)
                        {
                            StopCoroutine(AttackState);
                            AttackState = null;
                        }
                        movementspeed = orginialspeed;
                        stillSlow = false;
                    }
                    Debug.Log("Attack!");
                    StartCoroutine(AttackActionCommand());
                    AttackState = StartCoroutine(Slowness(AttackingSlowness, AttackingSlowDuration));
                    audioSource.PlayOneShot(AttackSound);
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Menu");
        }
    }

    public void HPSystem(int HP)
    {
        float targetwidth = HP * maxMask / MaxHP;
        float newRightMask = maxMask - targetwidth;
        Vector4 padding = HPBar.padding;
        padding.z = newRightMask;
        HPBar.padding = padding;
    }
    IEnumerator BarkingActionCommand()
    {
        StartCoroutine(Barking());
        animator.SetTrigger("bark");
        yield return new WaitForSeconds(BarkingCooldown);
    }
    IEnumerator AttackActionCommand()
    {
        StartCoroutine(Attacking());
        animator.SetTrigger("bite");
        yield return new WaitForSeconds(AttackingCooldown);
    }
    IEnumerator Slowness(float actionSlowness, float slowDuration)
    {
        isCooldown = true;
        stillSlow = true;
        float slowness = movementspeed * actionSlowness / 100;
        float currentspeed = movementspeed;
        movementspeed = movementspeed - slowness;
        yield return new WaitForSeconds(slowDuration);
        stillSlow = false;
        movementspeed = currentspeed;
        BarkingState = null;
        AttackState = null;
        isCooldown = false;
    }
    IEnumerator Attacking()
    {
        AttackHitbox.enabled = true;
        yield return new WaitForSeconds(0.2f);
        AttackHitbox.enabled = false;
    }
    IEnumerator Barking()
    {
        BarkingHitbox.enabled = true;
        yield return new WaitForSeconds(0.2f);
        BarkingHitbox.enabled = false;
    }
    public void knockedback(Transform source) //โจนเล่นงานใส่ player
    {
        Time.timeScale = 0.3f;
        float slowtime = 0.5f;
        movementspeed = 0f;
        move = false; isStunning = true;
        Vector3 knockbackDirection = (transform.position - source.position).normalized;
        puppy.AddForce(knockbackDirection * knockbackForce,ForceMode.Impulse);
        Invoke("timeScale", slowtime);
        Invoke("playerstunned", stunTime);
    }
    void timeScale()
    {
        Time.timeScale = 1.0f;
    }
    void playerstunned()
    {
        movementspeed = orginialspeed;
        move = true;
        animator.SetBool("isHurt", false);
        isStunning = false;
    }
    public void healPlayer(int heal)
    {
        HP = HP + heal;
        if(HP > 100) HP = 100;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Robber Hitbox")) //โจมตีใส่
        {
            RobberAI robber = Object.FindAnyObjectByType<RobberAI>();
            Debug.Log("Hurt!");

            if (robber!= null)
            {
                audioSource.PlayOneShot(sfxImpact); audioSource.PlayOneShot(sfxOuch);
                animator.SetBool("isHurt", true);
                gameManager.PlayerHurt();
                HP = HP - robber.attackpoint;
                knockedback(robber.transform);
                if (HP <= 0)
                {
                    audioGameplay.songSource.Stop();
                    gameManager.gameoverdesc.text = "The puppy won't wake up. It need rest...";
                    gameManager.isGameover = true;
                    gameManager.gameover();
                }
            }
        }

    }
}
