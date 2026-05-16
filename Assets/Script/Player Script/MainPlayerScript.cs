using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class MainPlayerScript : NetworkBehaviour
{
    public float speed = 5.0f;
    public float rotationSpeed = 120.0f;

    private Rigidbody rb;
    private Vector2 moveInput;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 5, -5);
    private PlayerInput playerInput;

    [Header("Attack Settings")]
    [SerializeField] private AttackHitbox attackHitbox;
    public int damage = 10;
    public float attackRange = 2.0f;
    public float attackCooldown = 1.0f;
    public float lastAttackTime = 0f;
    public float attackDuration = 0.5f;
    public bool isAttacking = false;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        attackHitbox = GetComponentInChildren<AttackHitbox>();
        playerInput = GetComponent<PlayerInput>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;

        float dt = Time.fixedDeltaTime;

        // World-based movement (NOT affected by rotation)
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);

        if (move.sqrMagnitude > 0.001f)
        {
            move.Normalize();

            // Move
            rb.MovePosition(rb.position + move * speed * dt);

            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotationSpeed * dt
            ));
        }

        rb.angularVelocity = Vector3.zero;
    }

    public override void OnNetworkSpawn()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (playerInput != null)
        {
            playerInput.enabled = IsOwner; // 🔥 THIS FIXES CLIENT INPUT
        }
        if (cam != null)
        {
            AudioListener listener = cam.GetComponent<AudioListener>();

            if (!IsOwner)
            {
                // Disable camera + audio for other players
                cam.gameObject.SetActive(false);

                if (listener != null)
                    listener.enabled = false;
            }
            else
            {
                // Enable only for local player
                cam.gameObject.SetActive(true);

                if (listener != null)
                    listener.enabled = true;
            }
        }
    }
    private void LateUpdate()
    {
        if (!IsOwner || playerCamera == null) return;

        playerCamera.transform.position = transform.position + cameraOffset;
        playerCamera.transform.LookAt(transform);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!context.performed) return;

        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        AttackServerRpc();
        Invoke(nameof(ResetAttack), attackDuration);
    }

    public void ResetAttack()
    {
        isAttacking = false;
    }

    [ServerRpc]
    private void AttackServerRpc(ServerRpcParams rpcParams = default)
    {
        // ✅ Activate hitbox ON SERVER
        if (attackHitbox != null)
        {
            attackHitbox.Activate(OwnerClientId, damage);
        }

        // Disable later
        Invoke(nameof(DisableHitbox), attackDuration);

        // Visual/sound for everyone
        ActivateHitboxClientRpc();
        PlayAttackSoundClientRpc();
    }

    [ClientRpc]
    private void PlayAttackSoundClientRpc()
    {

        if (audioSource == null)
        {
            Debug.LogError("AudioSource is NULL!");
            return;
        }

        audioSource.PlayOneShot(attackSound);
    }

    [ClientRpc]
    private void ActivateHitboxClientRpc()
    {
        if (!IsServer) // 👈 important
        {
            if (attackHitbox != null)
            {
                attackHitbox.Activate(OwnerClientId, damage);
            }
        }
    }

    private void DisableHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.Deactivate();
        }
    }
}

