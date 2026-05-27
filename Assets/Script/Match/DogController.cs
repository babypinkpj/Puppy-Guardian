using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;

namespace Match
{
    public class DogController : MatchPlayer
    {
        [Header("Dog Abilities")]
        public float barkCooldown = 2f;
        public float biteCooldown = 30f;
        public float smellCooldown = 45f;
        
        [Header("Audio")]
        public AudioClip barkClip;
        public AudioClip biteClip;
        public AudioClip biteHitClip;
        public AudioClip hurtClip;
        public AudioSource audioSource;

        private float _lastBarkTime = -100f;
        private float _lastBiteTime = -100f;
        private float _lastSmellTime = -100f;
        
        private bool _speedUpTriggered = false;

        public void OnBark(InputAction.CallbackContext context)
        {
            if (!IsOwner || context.phase != InputActionPhase.Started) return;

            if (Time.time - _lastBarkTime >= barkCooldown)
            {
                _lastBarkTime = Time.time;
                BarkServerRpc();
                BlockSprintForDuration(2f); // Bark prevents sprinting for 2s
            }
        }

        public void OnBite(InputAction.CallbackContext context)
        {
            if (!IsOwner || context.phase != InputActionPhase.Started) return;

            if (Time.time - _lastBiteTime >= biteCooldown)
            {
                _lastBiteTime = Time.time;
                StartCoroutine(BiteRoutine());
                // Bite does NOT block sprint per requirements
            }
        }

        [Rpc(SendTo.Server)]
        private void BarkServerRpc()
        {
            BarkClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void BarkClientRpc()
        {
            if (audioSource && barkClip) audioSource.PlayOneShot(barkClip);

            // Taunt robber (Show indicator on robber's screen)
            if (NetworkManager.Singleton.LocalClientId == MatchManager.Instance.RobberClientId.Value)
            {
                MatchUIManager.Instance?.ShowAnnouncementRpc("A dog barked!");
                // TODO: Instantiate visual indicator pointing to transform.position
            }
        }

        private IEnumerator BiteRoutine()
        {
            yield return new WaitForSeconds(0.5f); // 0.5s hitbox delay

            BiteServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void BiteServerRpc()
        {
            // Check for robber in range
            RobberController robber = FindAnyObjectByType<RobberController>();
            bool hitRobber = false;
            if (robber != null && Vector3.Distance(transform.position, robber.transform.position) <= 2.5f)
            {
                robber.TakeDamageServerRpc(5f);
                robber.ApplyStunRpc(4f);
                hitRobber = true;
            }

            // Apply self slowness II for 2s
            ApplySlownessClientRpc(2, 2f);
            BiteClientRpc(hitRobber);
        }

        [Rpc(SendTo.Everyone)]
        private void BiteClientRpc(bool hitRobber)
        {
            if (audioSource && biteClip) audioSource.PlayOneShot(biteClip);
            if (hitRobber && audioSource && biteHitClip) audioSource.PlayOneShot(biteHitClip);
        }

        [Rpc(SendTo.Owner)]
        private void ApplySlownessClientRpc(int level, float duration)
        {
            ApplySlowness(level, duration);
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOwner) return;

            UpdateCooldownUI();
            HandlePassives();
        }

        private void UpdateCooldownUI()
        {
            float barkRemaining = Mathf.Max(0, barkCooldown - (Time.time - _lastBarkTime));
            float biteRemaining = Mathf.Max(0, biteCooldown - (Time.time - _lastBiteTime));
            MatchUIManager.Instance?.UpdateAbilityCooldown(false, 0, barkRemaining, barkCooldown);
            MatchUIManager.Instance?.UpdateAbilityCooldown(false, 1, biteRemaining, biteCooldown);
        }

        private void HandlePassives()
        {
            // 1. Smell Passive (every 45s)
            if (Time.time - _lastSmellTime >= smellCooldown)
            {
                _lastSmellTime = Time.time;
                MatchUIManager.Instance?.ShowAnnouncementRpc("Smell: The Robber's location is revealed!");
                // TODO: Show visual indicator for Robber's location
            }

            // 2. Speed Up Passive (HP < 15%)
            if (!_speedUpTriggered && HP.Value > 0 && (HP.Value / maxHP) < 0.15f)
            {
                _speedUpTriggered = true;
                ApplySpeedUp(1, 7f); // Speed I for 7s
            }
        }

        protected override void HandleDeath()
        {
            if (IsServer)
            {
                MatchManager.Instance.OnDogDefeated();
                // NetworkObject.Despawn(); // Despawn or show death animation
                gameObject.SetActive(false); // Quick hack to hide body, use despawn or death state in prod
                DespawnServerRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void DespawnServerRpc()
        {
            NetworkObject.Despawn();
        }

        protected override void PlayHurtSound()
        {
            if (audioSource && hurtClip) audioSource.PlayOneShot(hurtClip);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOwner) return;

            if (other.CompareTag("PoliceCar") && MatchManager.Instance.CurrentState.Value == MatchManager.MatchState.Escape)
            {
                MatchManager.Instance.OnDogEscaped();
                gameObject.SetActive(false); // Hide dog when escaped
            }
        }
    }
}
