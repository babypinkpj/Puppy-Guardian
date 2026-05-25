using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // เพิ่ม

public class RobberMovement : NetworkBehaviour // MonoBehaviour → NetworkBehaviour
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
    public float WalkSoundDelay = 0.4f;
    private float walkSoundTimer = 0f;

    // ✅ เพิ่ม OnNetworkSpawn แทน Start สำหรับ network setup
    public override void OnNetworkSpawn()
    {
        audioSource = GetComponent<AudioSource>();
        // ถ้าไม่ใช่เจ้าของ → ปิด input ทิ้งเลย
        if (!IsOwner)
        {
            enabled = false; // ปิด script นี้ทั้งหมด
            return;
        }
    }

    private void Update()
    {
        if (!IsOwner) return; // ✅ guard ไว้ด้วยเผื่อ
        Movement();
        ActionCommand();
    }

    void Movement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        movement = new Vector3(x, 0, z).normalized;

        transform.Translate(movement * movementspeed * Time.deltaTime, Space.World);

        if (movement != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, rotation, rotationspeed * Time.deltaTime);

           if (Time.time >= walkSoundTimer && WalkSound != null)
{
    audioSource.PlayOneShot(WalkSound);
    walkSoundTimer = Time.time + WalkSoundDelay;
}
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
                BarkServerRpc(); // ✅ บอก server ว่า Bark
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                Debug.Log("Attack!");
                StartCoroutine(AttackActionCommand());
                AttackServerRpc(); // ✅ บอก server ว่า Attack
            }
        }
    }

    // ✅ RPC — client บอก server ว่า Bark
    [ServerRpc]
    private void BarkServerRpc()
    {
        BarkClientRpc(); // server broadcast ให้ทุกคนเห็น
    }

    // ✅ RPC — server broadcast ให้ทุก client เล่น animation/sound
    [ClientRpc]
    private void BarkClientRpc()
    {
        // TODO: เล่น Bark animation ตรงนี้
        if (BarkSound != null)
            audioSource.PlayOneShot(BarkSound);
    }

    // ✅ RPC — client บอก server ว่า Attack
    [ServerRpc]
    private void AttackServerRpc()
    {
        AttackClientRpc();
    }

    [ClientRpc]
    private void AttackClientRpc()
    {
        // TODO: เล่น Attack animation ตรงนี้
        StartCoroutine(Attacking());
        if (AttackSound != null)
            audioSource.PlayOneShot(AttackSound);
    }

    IEnumerator BarkingActionCommand()
    {
        isCooldown = true;
        yield return new WaitForSeconds(BarkingCooldown);
        isCooldown = false;
    }

    IEnumerator AttackActionCommand()
    {
        isCooldown = true;
        yield return new WaitForSeconds(AttackingCooldown);
        isCooldown = false;
    }

    IEnumerator Attacking()
    {
        Hitbox.enabled = true;
        yield return new WaitForSeconds(0.2f);
        Hitbox.enabled = false;
    }
}