using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace Match
{
    public class MatchUIManager : NetworkBehaviour
    {
        public static MatchUIManager Instance { get; private set; }

        [Header("Global UI")]
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI announcementText;
        public TextMeshProUGUI subtitleTitleText;
        public TextMeshProUGUI subtitleDescText;
        public TextMeshProUGUI objectiveText;

        [Header("Player Status UI")]
        public Image hpBar;
        public TMP_Text hpText;
        public Image staminaBar;
        public TextMeshProUGUI staminaText;
        
        [Header("Effects UI")]
        public Transform effectsContainer;
        public GameObject effectPrefab; // Text with duration

        [Header("Abilities UI")]
        public GameObject[] dogAbilityIcons; // 0 = Bark, 1 = Bite
        public Image[] dogAbilityCooldowns;
        public TMP_Text[] dogAbilityCooldownTexts;
        
        public GameObject[] robberAbilityIcons; // 0 = Kick, 1 = Pick/Drop, 2 = Use
        public Image[] robberAbilityCooldowns;
        public TMP_Text[] robberAbilityCooldownTexts;

        [Header("Inventory UI (Robber)")]
        public GameObject inventoryPanel;
        public Image[] inventorySlots; // 2 slots

        [Header("Player List")]
        public Transform playerListContainer;
        public GameObject playerListEntryPrefab;

        private Dictionary<ulong, MatchPlayerSlotUI> _playerSlots = new Dictionary<ulong, MatchPlayerSlotUI>();
        private bool _hasBuiltPlayerList = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            if (MatchManager.Instance != null && MatchManager.Instance.CurrentState.Value != MatchManager.MatchState.Waiting)
            {
                float time = MatchManager.Instance.MatchTimer.Value;
                if (time < 0) time = 0;
                int min = Mathf.FloorToInt(time / 60);
                int sec = Mathf.FloorToInt(time % 60);
                if (timerText != null) timerText.text = $"{min:00}:{sec:00}";
            }
        }

        [Rpc(SendTo.Everyone)]
        public void ShowSubtitleRpc(string title, string description)
        {
            if (subtitleTitleText != null) subtitleTitleText.text = title;
            if (subtitleDescText != null) subtitleDescText.text = description;
            
            StopCoroutine(HideSubtitleRoutine());
            StartCoroutine(HideSubtitleRoutine());
        }

        private IEnumerator HideSubtitleRoutine()
        {
            if (subtitleTitleText != null) subtitleTitleText.gameObject.SetActive(true);
            if (subtitleDescText != null) subtitleDescText.gameObject.SetActive(true);
            yield return new WaitForSeconds(5f);
            if (subtitleTitleText != null) subtitleTitleText.gameObject.SetActive(false);
            if (subtitleDescText != null) subtitleDescText.gameObject.SetActive(false);
        }

        [Rpc(SendTo.Everyone)]
        public void ShowAnnouncementRpc(string message)
        {
            if (announcementText != null)
            {
                announcementText.text = message;
                announcementText.gameObject.SetActive(true);
                StopCoroutine(HideAnnouncementRoutine());
                StartCoroutine(HideAnnouncementRoutine());
            }
        }

        private IEnumerator HideAnnouncementRoutine()
        {
            yield return new WaitForSeconds(4f);
            if (announcementText != null) announcementText.gameObject.SetActive(false);
        }

        [Rpc(SendTo.Everyone)]
        public void UpdateObjectiveRpc(int radarsFixed, int maxRadars, int dogsDefeated, int maxDogs)
        {
            if (objectiveText != null && MatchManager.Instance != null)
            {
                bool isRobber = NetworkManager.Singleton.LocalClientId == MatchManager.Instance.RobberClientId.Value;
                if (isRobber)
                {
                    objectiveText.text = $"{dogsDefeated}/{maxDogs} dogs defeated";
                }
                else
                {
                    objectiveText.text = $"{radarsFixed}/{maxRadars} tasks completed";
                }
            }
        }

        // Called locally by the player's controller
        public void SetupLocalUI(bool isRobber)
        {
            if (isRobber)
            {
                foreach (var icon in dogAbilityIcons) if (icon) icon.SetActive(false);
                foreach (var icon in robberAbilityIcons) if (icon) icon.SetActive(true);
                if (inventoryPanel) inventoryPanel.SetActive(true);
            }
            else
            {
                foreach (var icon in dogAbilityIcons) if (icon) icon.SetActive(true);
                foreach (var icon in robberAbilityIcons) if (icon) icon.SetActive(false);
                if (inventoryPanel) inventoryPanel.SetActive(false);
            }
        }

        public void UpdateHP(float current, float max)
        {
            if (hpBar) hpBar.fillAmount = current / max;
            if (hpText) hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        public void UpdateStamina(float current, float max)
        {
            if (staminaBar) staminaBar.fillAmount = current / max;
            if (staminaText) staminaText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        public void UpdateAbilityCooldown(bool isRobber, int abilityIndex, float timeRemaining, float maxCooldown)
        {
            bool isOnCooldown = timeRemaining > 0.01f;
            float fillAmount = isOnCooldown ? (timeRemaining / maxCooldown) : 0f;
            string timeText = isOnCooldown ? Mathf.CeilToInt(timeRemaining).ToString() : "";

            if (isRobber)
            {
                if (abilityIndex < robberAbilityCooldowns.Length && robberAbilityCooldowns[abilityIndex])
                    robberAbilityCooldowns[abilityIndex].fillAmount = fillAmount;
                
                if (abilityIndex < robberAbilityCooldowns.Length && robberAbilityCooldownTexts != null && abilityIndex < robberAbilityCooldownTexts.Length && robberAbilityCooldownTexts[abilityIndex])
                    robberAbilityCooldownTexts[abilityIndex].text = timeText;
            }
            else
            {
                if (abilityIndex < dogAbilityCooldowns.Length && dogAbilityCooldowns[abilityIndex])
                    dogAbilityCooldowns[abilityIndex].fillAmount = fillAmount;
                    
                if (abilityIndex < dogAbilityCooldowns.Length && dogAbilityCooldownTexts != null && abilityIndex < dogAbilityCooldownTexts.Length && dogAbilityCooldownTexts[abilityIndex])
                    dogAbilityCooldownTexts[abilityIndex].text = timeText;
            }
        }

        // UI representation of Robber's inventory (Requires an item icon sprite)
        public void UpdateInventorySlot(int slotIndex, Sprite itemSprite)
        {
            if (slotIndex < inventorySlots.Length && inventorySlots[slotIndex])
            {
                inventorySlots[slotIndex].sprite = itemSprite;
                inventorySlots[slotIndex].gameObject.SetActive(itemSprite != null);
            }
        }

        [Rpc(SendTo.Everyone)]
        public void BuildPlayerListRpc(ulong robberId)
        {
            if (_hasBuiltPlayerList || playerListContainer == null || playerListEntryPrefab == null) return;

            ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
            CosmeticsManager cosmeticsManager = FindAnyObjectByType<CosmeticsManager>();
            if (connManager == null) return;

            foreach (var player in connManager.ConnectedPlayers)
            {
                GameObject slotObj = Instantiate(playerListEntryPrefab, playerListContainer);
                MatchPlayerSlotUI slotUI = slotObj.GetComponent<MatchPlayerSlotUI>();
                if (slotUI != null)
                {
                    bool isRobber = player.ClientId == robberId;
                    
                    Sprite icon = null;
                    if (cosmeticsManager != null)
                    {
                        if (isRobber)
                        {
                            if (cosmeticsManager.robberHatIcons != null && player.RobberHatId < cosmeticsManager.robberHatIcons.Length)
                                icon = cosmeticsManager.robberHatIcons[player.RobberHatId];
                        }
                        else
                        {
                            if (cosmeticsManager.dogIcons != null && player.CharacterId < cosmeticsManager.dogIcons.Length)
                                icon = cosmeticsManager.dogIcons[player.CharacterId];
                        }
                    }

                    // For now max HP is 100
                    slotUI.Setup(player.PlayerName.ToString(), icon, 100f, 100f);
                    
                    _playerSlots[player.ClientId] = slotUI;

                    if (isRobber)
                    {
                        slotObj.transform.SetSiblingIndex(0); // Force robber to the top
                    }
                }
            }
            _hasBuiltPlayerList = true;
        }

        public void UpdatePlayerListHP(ulong clientId, float currentHP, float maxHP)
        {
            if (_playerSlots.TryGetValue(clientId, out MatchPlayerSlotUI slot))
            {
                slot.UpdateHP(currentHP, maxHP);
            }
        }
    }
}
