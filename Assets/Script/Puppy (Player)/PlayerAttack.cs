using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public enum Team
    {
        Puppy,
        Robber
    }

    [Header("Team")]
    public Team playerTeam;

    [Header("Attack")]
    public BoxCollider hitbox;
    public float attackCooldown = 1f;
    public float attackDuration = 0.2f;
    public int damage = 10;

    private bool isCooldown;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip dogAttackSound;
    public AudioClip robberAttackSound;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (hitbox != null)
        {
            hitbox.enabled = false;
        }
    }

    private void Update()
    {
        AttackInput();
    }

    void AttackInput()
    {
        if (isCooldown) return;

        // ปุ่มโจมตี
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        isCooldown = true;

        // เปิด hitbox
        hitbox.enabled = true;

        // เล่นเสียงตามทีม
        if (playerTeam == Team.Puppy)
        {
            if (dogAttackSound != null)
                audioSource.PlayOneShot(dogAttackSound);

            Debug.Log("Dog Bite!");
        }
        else
        {
            if (robberAttackSound != null)
                audioSource.PlayOneShot(robberAttackSound);

            Debug.Log("Robber Kick!");
        }

        yield return new WaitForSeconds(attackDuration);

        // ปิด hitbox
        hitbox.enabled = false;

        yield return new WaitForSeconds(attackCooldown);

        isCooldown = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ห้ามตีทีมเดียวกัน
        PlayerAttack target = other.GetComponent<PlayerAttack>();

        if (target == null) return;

        if (target.playerTeam == playerTeam) return;

        // หา Health
        PlayerHealth health = other.GetComponent<PlayerHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);

            Debug.Log(gameObject.name + " attacked " + other.name);
        }
    }
}
