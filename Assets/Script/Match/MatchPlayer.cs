using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

namespace Match
{
    public class MatchPlayer : NetworkBehaviour
    {
        public NetworkVariable<bool> IsRobber = new NetworkVariable<bool>(false);
        public NetworkVariable<float> HP = new NetworkVariable<float>(100f);
        public NetworkVariable<float> Stamina = new NetworkVariable<float>(100f);

        [Header("Stats")]
        public float maxHP = 100f;
        public float maxStamina = 100f;
        public float baseSpeed = 5f;
        public float sprintMultiplier = 1.5f;
        public float staminaDrainRate = 15f;
        public float staminaRegenRate = 10f;

        [Header("Components")]
        public CharacterController controller;
        public Animator animator;

        // Input Actions
        private Vector2 moveInput;
        private bool isSprinting;
        
        // Status Effects
        protected bool isStunned = false;
        protected float speedMultiplier = 1f;
        protected bool isAbilityBlockingSprint = false;

        private float currentSpeed;
        private Transform mainCameraTransform;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                MatchUIManager.Instance?.SetupLocalUI(IsRobber.Value);
                if (Camera.main != null) mainCameraTransform = Camera.main.transform;
            }
            if (IsServer)
            {
                HP.Value = maxHP;
                Stamina.Value = maxStamina;
            }

            HP.OnValueChanged += (oldVal, newVal) => {
                MatchUIManager.Instance?.UpdatePlayerListHP(OwnerClientId, newVal, maxHP);
            };
        }

        [Rpc(SendTo.Everyone)]
        public void SetupVisualsRpc(int dogHatId, int robberHatId)
        {
            // TODO: Attach hats using CosmeticsManager logic
        }

        // --- Input Handling using New Input System ---
        public void OnMove(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;
            if (context.started) isSprinting = true;
            else if (context.canceled) isSprinting = false;
        }

        protected virtual void Update()
        {
            if (!IsOwner) return;

            UpdateUI();

            if (MatchManager.Instance.CurrentState.Value != MatchManager.MatchState.Playing && 
                MatchManager.Instance.CurrentState.Value != MatchManager.MatchState.Escape)
            {
                return;
            }

            if (isStunned) return;

            HandleMovement();
            HandleStamina();
        }

        private void HandleMovement()
        {
            currentSpeed = baseSpeed * speedMultiplier;
            
            bool canSprint = isSprinting && Stamina.Value > 0 && !isAbilityBlockingSprint;
            if (canSprint)
            {
                currentSpeed *= sprintMultiplier;
            }

            Vector3 move = Vector3.zero;

            if (mainCameraTransform != null)
            {
                Vector3 forward = mainCameraTransform.forward;
                Vector3 right = mainCameraTransform.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                move = right * moveInput.x + forward * moveInput.y;
            }
            else
            {
                move = transform.right * moveInput.x + transform.forward * moveInput.y;
            }

            if (controller != null)
            {
                // Apply simple gravity
                controller.Move((move * currentSpeed + Physics.gravity) * Time.deltaTime);
            }

            // Rotate player to face movement direction
            if (move != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }

            // Sync Animations for everyone (Dog and Robber)
            if (animator != null)
            {
                float speedPercent = move.magnitude * (canSprint ? 1f : 0.5f);
                animator.SetFloat("Speed", speedPercent);
                animator.SetBool("isWalk", move.magnitude > 0.1f);
            }
        }

        private void HandleStamina()
        {
            if (isSprinting && moveInput.magnitude > 0 && Stamina.Value > 0 && !isAbilityBlockingSprint)
            {
                UpdateStaminaServerRpc(-staminaDrainRate * Time.deltaTime);
            }
            else if (Stamina.Value < maxStamina)
            {
                UpdateStaminaServerRpc(staminaRegenRate * Time.deltaTime);
            }
        }

        private void UpdateUI()
        {
            MatchUIManager.Instance?.UpdateHP(HP.Value, maxHP);
            MatchUIManager.Instance?.UpdateStamina(Stamina.Value, maxStamina);
        }

        [Rpc(SendTo.Server)]
        protected void UpdateStaminaServerRpc(float amount)
        {
            Stamina.Value = Mathf.Clamp(Stamina.Value + amount, 0, maxStamina);
        }

        [Rpc(SendTo.Server)]
        public void TakeDamageServerRpc(float amount)
        {
            HP.Value = Mathf.Clamp(HP.Value - amount, 0, maxHP);
            
            if (amount > 0)
            {
                PlayHurtSoundClientRpc();
            }

            if (HP.Value <= 0)
            {
                HandleDeath();
            }
        }

        [Rpc(SendTo.Everyone)]
        protected void PlayHurtSoundClientRpc()
        {
            PlayHurtSound();
            
            if (animator != null)
            {
                StartCoroutine(HurtAnimRoutine());
            }
        }

        private IEnumerator HurtAnimRoutine()
        {
            animator.SetBool("isHurt", true);
            yield return new WaitForSeconds(0.5f); // Automatically turn off after 0.5s
            if (animator != null) animator.SetBool("isHurt", false);
        }

        protected virtual void PlayHurtSound()
        {
            // Override in subclasses
        }

        protected virtual void HandleDeath()
        {
            // Override in DogController
        }

        // --- Status Effects ---
        public void ApplySlowness(int level, float duration)
        {
            StartCoroutine(SlownessRoutine(level, duration));
        }

        private IEnumerator SlownessRoutine(int level, float duration)
        {
            float reduction = level == 1 ? 0.8f : 0.5f; // Slowness I = 20% slow, Slowness II = 50% slow
            speedMultiplier *= reduction;
            
            if (IsOwner) MatchUIManager.Instance?.ShowAnnouncementRpc($"Slowness {level} applied!");
            
            yield return new WaitForSeconds(duration);
            
            speedMultiplier /= reduction;
        }

        public void ApplySpeedUp(int level, float duration)
        {
            StartCoroutine(SpeedUpRoutine(level, duration));
        }

        private IEnumerator SpeedUpRoutine(int level, float duration)
        {
            float boost = level == 1 ? 1.2f : 1.5f;
            speedMultiplier *= boost;
            
            if (IsOwner) MatchUIManager.Instance?.ShowAnnouncementRpc($"Speed {level} applied!");
            
            yield return new WaitForSeconds(duration);
            
            speedMultiplier /= boost;
        }

        [Rpc(SendTo.Everyone)]
        public void ApplyStunRpc(float duration)
        {
            StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            isStunned = true;
            if (IsOwner) MatchUIManager.Instance?.ShowAnnouncementRpc("Stunned!");
            yield return new WaitForSeconds(duration);
            isStunned = false;
        }

        public void ApplyBlur(float duration)
        {
            // TODO: Trigger a PostProcessing effect for blur
            if (IsOwner) MatchUIManager.Instance?.ShowAnnouncementRpc("Vision Blurred!");
        }

        // --- Ability Utilities ---
        protected void BlockSprintForDuration(float duration)
        {
            StartCoroutine(BlockSprintRoutine(duration));
        }

        private IEnumerator BlockSprintRoutine(float duration)
        {
            isAbilityBlockingSprint = true;
            yield return new WaitForSeconds(duration);
            isAbilityBlockingSprint = false;
        }
    }
}
