using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player Movement")]
    [SerializeField] private Rigidbody puppy;
    [SerializeField] private float movementspeed = 1f;
    [SerializeField] private float rotationspeed = 720f;
    private Vector3 movement;
    [Header("Player Action Command")]
    public float BarkingCooldown = 0.5f;
    public float AttackingCooldown = 1.0f;
    private bool isCooldown;
    [Header("Attack")]
    public BoxCollider Hitbox;
    public AudioSource audioSource;
   [Header("Sounds")]
    public AudioClip BarkSound;
    public AudioClip AttackSound;
    public AudioClip WalkSound;
    public float WalkSoundDelay = 0.4f; // หน่วงเวลาเสียงเดิน
    private float walkSoundTimer = 0f;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Update()
    {
        Movement(); ActionCommand();
    }
    void Movement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        movement = new Vector3(x, 0, z).normalized;
        float magnitude = movement.magnitude;
        magnitude = Mathf.Clamp01(magnitude);
        transform.Translate(movement * movementspeed * Time.deltaTime, Space.World);
        if (movement != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, rotationspeed * Time.deltaTime);
               if (Time.time >= walkSoundTimer)
              {audioSource.PlayOneShot(WalkSound);
            walkSoundTimer = Time.time + WalkSoundDelay; }

        }
        
    }
    void ActionCommand()
    {
        if (!isCooldown)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Debug.Log("Bark!");
                StartCoroutine(BarkingActionCommand());
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Log("Attack!");
                StartCoroutine(AttackActionCommand());
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Menu");
        }
    }

    IEnumerator BarkingActionCommand()
    {
        isCooldown = true;
        yield return new WaitForSeconds(BarkingCooldown);
        isCooldown = false;
        if (BarkSound != null)
        {audioSource.PlayOneShot(BarkSound);}
        
    }
    IEnumerator AttackActionCommand()
    {
        isCooldown = true;
        StartCoroutine(Attacking());
        yield return new WaitForSeconds(AttackingCooldown);
        isCooldown = false;
         if (AttackSound != null)
         {audioSource.PlayOneShot(AttackSound);}

    }

    IEnumerator Attacking()
    {
        Hitbox.enabled = true;
        yield return new WaitForSeconds(0.2f);
        Hitbox.enabled = false;
    }
}
