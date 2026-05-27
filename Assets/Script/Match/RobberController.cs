using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace Match
{
    public class RobberController : MatchPlayer
    {
        [Header("Robber Abilities")]
        public float kickCooldown = 5f;
        public float itemManageCooldown = 1f;
        public float useItemCooldown = 20f;

        [Header("Audio")]
        public AudioClip kickClip;
        public AudioClip kickHitClip;
        public AudioClip pickItemClip;
        public AudioClip useItemClip;
        public AudioClip hurtClip;
        public AudioSource audioSource;

        private float _lastKickTime = -100f;
        private float _lastManageTime = -100f;
        private float _lastUseItemTime = -100f;

        // Inventory
        private RobberItem[] _inventory = new RobberItem[2];
        private int _selectedSlot = 0; // 0 or 1

        // Passives
        private bool _panicky1Triggered = false; // 2/3 time (3:20 or 200s left)
        private bool _panicky2Triggered = false; // 1/3 time (1:40 or 100s left)
        private bool _panickyPoliceTriggered = false;
        private float _panickySpeedBonus = 0f;
        private const float MAX_PANICKY_BONUS = 0.15f;
        
        private float _lastGoggleTime = -100f;
        private float _goggleCooldown = 40f;

        public bool IsMuteShoeActive { get; private set; } = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                // Hack Radar passive: Show all radars
                // Implementation will be in Radar script to check if LocalPlayer is Robber and enable outline
            }
        }

        // --- Inputs ---
        public void OnKick(InputAction.CallbackContext context)
        {
            if (!IsOwner || context.phase != InputActionPhase.Started) return;
            if (Time.time - _lastKickTime >= kickCooldown)
            {
                _lastKickTime = Time.time;
                BlockSprintForDuration(2f); // Kick cancels/blocks sprint for 2s
                
                if (animator) animator.SetTrigger("Kick");
                
                StartCoroutine(KickRoutine());
            }
        }

        public void OnSelectSlot1(InputAction.CallbackContext context)
        {
            if (!IsOwner || context.phase != InputActionPhase.Started) return;
            if (Time.time - _lastManageTime >= itemManageCooldown)
            {
                _lastManageTime = Time.time;
                _selectedSlot = 0;
                MatchUIManager.Instance?.ShowAnnouncementRpc("Switched to Slot 1");
            }
        }

        public void OnSelectSlot2(InputAction.CallbackContext context)
        {
            if (!IsOwner || context.phase != InputActionPhase.Started) return;
            if (Time.time - _lastManageTime >= itemManageCooldown)
            {
                _lastManageTime = Time.time;
                _selectedSlot = 1;
                MatchUIManager.Instance?.ShowAnnouncementRpc("Switched to Slot 2");
            }
        }

        public void OnDropItem(InputAction.CallbackContext context)
        {
            if (!IsOwner || context.phase != InputActionPhase.Started) return;
            if (Time.time - _lastManageTime >= itemManageCooldown)
            {
                _lastManageTime = Time.time;
                if (_inventory[_selectedSlot] != null)
                {
                    _inventory[_selectedSlot] = null;
                    UpdateInventoryUI();
                    MatchUIManager.Instance?.ShowAnnouncementRpc("Item dropped.");
                }
            }
        }

        public void OnUseItem(InputAction.CallbackContext context)
        {
            if (!IsOwner || context.phase != InputActionPhase.Started) return;
            if (Time.time - _lastUseItemTime >= useItemCooldown)
            {
                RobberItem item = _inventory[_selectedSlot];
                if (item != null)
                {
                    _lastUseItemTime = Time.time;
                    BlockSprintForDuration(2f); // Use item cancels/blocks sprint for 2s
                    
                    if (audioSource && useItemClip) audioSource.PlayOneShot(useItemClip);
                    
                    // Trigger item effect
                    item.UseItem(this);
                    
                    // Consume item
                    _inventory[_selectedSlot] = null;
                    UpdateInventoryUI();
                }
            }
        }

        // --- Ability Logic ---
        private IEnumerator KickRoutine()
        {
            if (audioSource && kickClip) audioSource.PlayOneShot(kickClip);
            yield return new WaitForSeconds(0.5f); // Hitbox delay

            KickServerRpc();
        }

        [Rpc(SendTo.Server)]
        private void KickServerRpc()
        {
            bool hitDog = false;
            // Find nearby dogs in front
            DogController[] dogs = FindObjectsByType<DogController>(FindObjectsSortMode.None);
            foreach (var dog in dogs)
            {
                if (Vector3.Distance(transform.position, dog.transform.position) <= 3f)
                {
                    dog.TakeDamageServerRpc(30f);
                    hitDog = true;
                    // Mute shoe disappears when hitting a dog
                    DisableMuteShoeClientRpc(); 
                }
            }

            if (hitDog)
            {
                KickHitClientRpc();
                // Apply self slowness II for 3s ONLY if hit successful
                ApplySlownessClientRpc(2, 3f);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void KickHitClientRpc()
        {
            if (audioSource && kickHitClip) audioSource.PlayOneShot(kickHitClip);
        }

        [Rpc(SendTo.Owner)]
        private void ApplySlownessClientRpc(int level, float duration)
        {
            ApplySlowness(level, duration);
        }

        // --- Inventory ---
        public void PickupItem(RobberItem item)
        {
            if (!IsOwner) return;
            
            // Try selected slot first
            if (_inventory[_selectedSlot] == null)
            {
                _inventory[_selectedSlot] = item;
                if (audioSource && pickItemClip) audioSource.PlayOneShot(pickItemClip);
                UpdateInventoryUI();
                item.gameObject.SetActive(false); // Hide in world
            }
            else // Try other slot
            {
                int otherSlot = (_selectedSlot == 0) ? 1 : 0;
                if (_inventory[otherSlot] == null)
                {
                    _inventory[otherSlot] = item;
                    if (audioSource && pickItemClip) audioSource.PlayOneShot(pickItemClip);
                    UpdateInventoryUI();
                    item.gameObject.SetActive(false); // Hide in world
                }
            }
        }

        private void UpdateInventoryUI()
        {
            for (int i = 0; i < 2; i++)
            {
                MatchUIManager.Instance?.UpdateInventorySlot(i, _inventory[i]?.icon);
            }
        }

        // --- Passives ---
        protected override void Update()
        {
            base.Update();
            if (!IsOwner) return;

            UpdateCooldownUI();
            HandlePassives();
        }

        private void UpdateCooldownUI()
        {
            float kickRemaining = Mathf.Max(0, kickCooldown - (Time.time - _lastKickTime));
            float useItemRemaining = Mathf.Max(0, useItemCooldown - (Time.time - _lastUseItemTime));
            MatchUIManager.Instance?.UpdateAbilityCooldown(true, 0, kickRemaining, kickCooldown);
            MatchUIManager.Instance?.UpdateAbilityCooldown(true, 2, useItemRemaining, useItemCooldown);
        }

        private void HandlePassives()
        {
            if (MatchManager.Instance.CurrentState.Value != MatchManager.MatchState.Playing && MatchManager.Instance.CurrentState.Value != MatchManager.MatchState.Escape) 
                return;

            float timeRemaining = MatchManager.Instance.MatchTimer.Value;

            // 1. Panicky Passive
            if (!_panicky1Triggered && timeRemaining <= 200f && MatchManager.Instance.CurrentState.Value == MatchManager.MatchState.Playing)
            {
                _panicky1Triggered = true;
                ApplyPanickyBoost();
            }
            if (!_panicky2Triggered && timeRemaining <= 100f && MatchManager.Instance.CurrentState.Value == MatchManager.MatchState.Playing)
            {
                _panicky2Triggered = true;
                ApplyPanickyBoost();
            }
            if (!_panickyPoliceTriggered && MatchManager.Instance.CurrentState.Value == MatchManager.MatchState.Escape)
            {
                _panickyPoliceTriggered = true;
                ApplyPanickyBoost();
            }

            // 2. Goggle Vision (Every 40s)
            if (Time.time - _lastGoggleTime >= _goggleCooldown)
            {
                _lastGoggleTime = Time.time;
                MatchUIManager.Instance?.ShowAnnouncementRpc("Goggle Vision: Dogs revealed for 3s!");
                // Implementation handled by toggling Outline on Dogs for 3s locally
            }
        }

        private void ApplyPanickyBoost()
        {
            if (_panickySpeedBonus < MAX_PANICKY_BONUS)
            {
                _panickySpeedBonus += 0.05f; // 5% boost
                speedMultiplier += 0.05f; 
                MatchUIManager.Instance?.ShowAnnouncementRpc($"Panicky: Speed increased by {_panickySpeedBonus * 100}%");
            }
        }

        // --- Item Effects called locally ---
        public void EnableMuteShoe()
        {
            StartCoroutine(MuteShoeRoutine());
        }

        private IEnumerator MuteShoeRoutine()
        {
            IsMuteShoeActive = true;
            MatchUIManager.Instance?.ShowAnnouncementRpc("Mute Shoe Active! Footsteps silenced.");
            yield return new WaitForSeconds(30f);
            IsMuteShoeActive = false;
        }

        [Rpc(SendTo.Everyone)]
        public void DisableMuteShoeClientRpc()
        {
            IsMuteShoeActive = false;
        }

        protected override void PlayHurtSound()
        {
            if (audioSource && hurtClip) audioSource.PlayOneShot(hurtClip);
        }
    }
}
