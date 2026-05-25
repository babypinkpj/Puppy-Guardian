using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Collections;
using Unity.Networking.Transport.Relay;

public class OnlineSessionManager : MonoBehaviour
{
    [Header("UI - Player Settings")]
    public GameObject loginPanel;
    public GameObject sessionPanel;
    public TMP_InputField usernameInput;
    public TMP_Dropdown characterDropdown;
    
    [Header("UI - Room Settings")]
    public TMP_InputField roomNameInput;
    public TMP_InputField roomPasswordInput;

    [Header("UI - Host Popup")]
    public GameObject hostPopupPanel;

    [Header("UI - Join Password Popup")]
    public GameObject joinPasswordPopupPanel;
    public TMP_InputField joinPasswordInput;

    [Header("UI - Status Popup")]
    public GameObject statusPopupPanel;
    public TMP_Text statusPopupText;

    [Header("UI - Buttons & Lists")]
    public Button hostButton;
    public Button quickJoinButton;
    public Transform roomListContent;
    public GameObject roomEntryPrefab;
    public Button refreshListButton;
    public Button leaveButton;
    public TMP_Text statusText;

    [Header("Session Panel Additions")]
    public Transform playerListContent;
    public GameObject playerSlotPrefab;
    public TMP_Text countdownText;
    public Button readyButton;
    [Tooltip("Exact name of the scene to load when the game starts")]
    public string GameplaySceneName = "GameScene";
    [Tooltip("Override Dog Icons. Leave empty to use CosmeticsManager icons.")]
    public Sprite[] customDogIcons;
    private bool _hasSubscribedToList = false;
    private bool _hasTriggeredGameStartAnim = false;
    private bool _hasTriggeredGameStartAudio = false;

    [Header("Lobby Settings")]
    public string defaultLobbyName = "My Lobby";
    public int maxPlayers = 5;
    private Lobby currentLobby;
    private Lobby pendingJoinLobby;
    private Coroutine heartbeatCoroutine;
    private const string JoinCodeKey = "joinCode";

    [Header("Authentication")]
    public string authProfileName = "default";
    public int maxConnections = 4;
    private string currentRelayJoinCode;

    private void Start()
    {
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(false);
            leaveButton.onClick.AddListener(LeaveSession);
        }
        if (sessionPanel != null) sessionPanel.SetActive(false);
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyButtonClicked);
        if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
        if (joinPasswordPopupPanel != null) joinPasswordPopupPanel.SetActive(false);
        if (statusPopupPanel != null) statusPopupPanel.SetActive(false);
        
        ClearErrorText();
        SetStatus("Not Connected");
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (sessionPanel != null) sessionPanel.SetActive(true);
            if (leaveButton != null) leaveButton.gameObject.SetActive(true);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (sessionPanel != null) sessionPanel.SetActive(true);
            if (leaveButton != null) leaveButton.gameObject.SetActive(true);
            HideStatusPopup();
            SetStatus("Connected as Client!");
            RefreshSessionPlayerList();
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (loginPanel != null) loginPanel.SetActive(true);
            if (sessionPanel != null) sessionPanel.SetActive(false);
            if (leaveButton != null) leaveButton.gameObject.SetActive(false);
            HideStatusPopup();
            SetMenuButtonsInteractable(true);
        }
    }

    private async Task InitializeAndSignInAsync()
    {
        SetStatus("Initializing Unity Services...");
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions();
            string finalProfileName = authProfileName;
#if UNITY_EDITOR
            // Fix for ParrelSync: ensure clones have unique profiles
            finalProfileName += "_" + UnityEngine.Random.Range(0, 100000);
#endif
            if (!string.IsNullOrWhiteSpace(finalProfileName))
            {
                options.SetProfile(finalProfileName);
            }
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            SetStatus("Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        string playerId = AuthenticationService.Instance.PlayerId;
        SetStatus("Signed in: " + playerId);
    }

    public async void StartLobbyHost()
    {
        CloseHostPopup();
        SetMenuButtonsInteractable(false);
        try
        {
            await InitializeAndSignInAsync();
            ShowStatusPopup("Creating the server...");

            SetStatus("Creating Relay Allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            currentRelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string finalRoomName = (roomNameInput != null && !string.IsNullOrWhiteSpace(roomNameInput.text)) ? roomNameInput.text : defaultLobbyName;
            string password = (roomPasswordInput != null) ? roomPasswordInput.text : "";
            string hasPasswordFlag = string.IsNullOrEmpty(password) ? "0" : "1";

            SetStatus("Creating Lobby...");
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, currentRelayJoinCode) },
                    // S1 is used as an indexable field to filter quick joins (0 = no password, 1 = has password)
                    { "HasPassword", new DataObject(DataObject.VisibilityOptions.Public, hasPasswordFlag, DataObject.IndexOptions.S1) }
                }
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(finalRoomName, maxPlayers, options);
            
            StartLobbyHeartbeat();

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

            // Tell the ConnectionManager what the room password is so it can verify incoming connections
            ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
            if (connManager != null)
            {
                connManager.RoomPassword = password;
                
                string hostName = (usernameInput != null && !string.IsNullOrWhiteSpace(usernameInput.text)) ? usernameInput.text : "Player";
                int charId = (characterDropdown != null) ? characterDropdown.value : 0;
                int dogHatId = PlayerPrefs.GetInt("SelectedDogHat", 0);
                int robberHatId = PlayerPrefs.GetInt("SelectedRobberHat", 0);
                
                connManager.SetHostData(hostName, charId, dogHatId, robberHatId);
            }

            if (!PrepareConnectionPayload(password))
            {
                HideStatusPopup();
                SetMenuButtonsInteractable(true);
                return;
            }

            NetworkManager.Singleton.StartHost();
            HideStatusPopup();
            SetStatus("Started Host with Lobby: " + currentLobby.Name);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            HideStatusPopup();
            SetStatus("Create Lobby Host failed: " + e.Message);
            SetMenuButtonsInteractable(true);
        }
    }

    public async void QuickJoinLobbyClient()
    {
        SetMenuButtonsInteractable(false);
        try
        {
            await InitializeAndSignInAsync();
            ShowStatusPopup("Finding the server...");
            SetStatus("Finding available public Lobby...");

            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions
            {
                Filter = new List<QueryFilter>
                {
                    // Only quick join lobbies where S1 (HasPassword) is "0" (False)
                    new QueryFilter(QueryFilter.FieldOptions.S1, "0", QueryFilter.OpOptions.EQ)
                }
            };

            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            SetStatus("Joined Lobby: " + currentLobby.Name);

            string joinCode = currentLobby.Data[JoinCodeKey].Value;
            
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));

            if (!PrepareConnectionPayload(""))
            {
                HideStatusPopup();
                SetMenuButtonsInteractable(true);
                return;
            }

            NetworkManager.Singleton.StartClient();
            HideStatusPopup();
            SetStatus("Started Client from Lobby.");
        }
        catch (System.Exception e)
        {
            HideStatusPopup();
            SetErrorText("0 server found.");
            SetStatus("Quick Join failed (No public rooms found?): " + e.Message);
            SetMenuButtonsInteractable(true);
        }
    }

    public async void RefreshLobbyListAsync()
    {
        ClearErrorText();
        SetStatus("Refreshing room list...");
        SetMenuButtonsInteractable(false);
        try
        {
            await InitializeAndSignInAsync();

            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);

            foreach (Transform child in roomListContent)
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in lobbies.Results)
            {
                GameObject entry = Instantiate(roomEntryPrefab, roomListContent);
                RoomEntryUI entryUI = entry.GetComponent<RoomEntryUI>();
                
                bool isLocked = lobby.Data != null && lobby.Data.ContainsKey("HasPassword") && lobby.Data["HasPassword"].Value == "1";

                if (entryUI != null)
                {
                    if (entryUI.serverNameText != null) entryUI.serverNameText.text = lobby.Name;
                    if (entryUI.playerCountText != null) entryUI.playerCountText.text = $"{lobby.Players.Count} / {lobby.MaxPlayers} Players";
                    
                    if (entryUI.passwordRequiredText != null) 
                    {
                        entryUI.passwordRequiredText.gameObject.SetActive(isLocked);
                    }

                    if (entryUI.joinButton != null)
                    {
                        entryUI.joinButton.onClick.AddListener(() => OnJoinRoomClicked(lobby));
                    }
                }
                else
                {
                    // Fallback to old method just in case
                    TMP_Text textComponent = entry.GetComponentInChildren<TMP_Text>();
                    string lockText = isLocked ? "[LOCKED] " : "";
                    if (textComponent != null) textComponent.text = $"{lockText}{lobby.Name} ({lobby.Players.Count} / {lobby.MaxPlayers} Players)";
                    Button buttonComponent = entry.GetComponentInChildren<Button>();
                    if (buttonComponent != null) buttonComponent.onClick.AddListener(() => OnJoinRoomClicked(lobby));
                }
            }

            SetStatus($"Found {lobbies.Results.Count} available rooms.");
            SetMenuButtonsInteractable(true);
        }
        catch (System.Exception e)
        {
            SetStatus("Failed to refresh rooms: " + e.Message);
            SetMenuButtonsInteractable(true);
        }
    }

    public void OnJoinRoomClicked(Lobby lobby)
    {
        bool isLocked = lobby.Data != null && lobby.Data.ContainsKey("HasPassword") && lobby.Data["HasPassword"].Value == "1";
        if (isLocked)
        {
            pendingJoinLobby = lobby;
            OpenJoinPasswordPopup();
        }
        else
        {
            JoinSelectedLobbyClient(lobby, "");
        }
    }

    public void OpenJoinPasswordPopup()
    {
        ClearErrorText();
        if (joinPasswordPopupPanel != null) joinPasswordPopupPanel.SetActive(true);
        if (joinPasswordInput != null) joinPasswordInput.text = "";
    }

    public void CloseJoinPasswordPopup()
    {
        if (joinPasswordPopupPanel != null) joinPasswordPopupPanel.SetActive(false);
        pendingJoinLobby = null;
    }

    public void ConfirmJoinPassword()
    {
        string pw = (joinPasswordInput != null) ? joinPasswordInput.text : "";
        if (pendingJoinLobby != null)
        {
            JoinSelectedLobbyClient(pendingJoinLobby, pw);
        }
        CloseJoinPasswordPopup();
    }

    public async void JoinSelectedLobbyClient(Lobby selectedLobby, string password)
    {
        SetMenuButtonsInteractable(false);
        try
        {
            ShowStatusPopup("Joining the server...");
            SetStatus("Joining Lobby: " + selectedLobby.Name);

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(selectedLobby.Id);
            string joinCode = currentLobby.Data[JoinCodeKey].Value;
            
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));

            if (!PrepareConnectionPayload(password))
            {
                HideStatusPopup();
                SetMenuButtonsInteractable(true);
                return;
            }

            NetworkManager.Singleton.StartClient();
            HideStatusPopup();
            SetStatus("Started Client from Lobby.");
        }
        catch (System.Exception e)
        {
            HideStatusPopup();
            SetStatus("Failed to join room: " + e.Message);
            SetMenuButtonsInteractable(true);
        }
    }

    public void OpenHostPopup()
    {
        ClearErrorText();
        if (hostPopupPanel != null) hostPopupPanel.SetActive(true);
        
        // Clear inputs
        if (roomNameInput != null) roomNameInput.text = "";
        if (roomPasswordInput != null) roomPasswordInput.text = "";
    }

    public void CloseHostPopup()
    {
        if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
    }

    private void ShowStatusPopup(string message)
    {
        if (statusPopupPanel != null) statusPopupPanel.SetActive(true);
        if (statusPopupText != null) statusPopupText.text = message;
        SetStatus(message);
        ClearErrorText(); // Hide error text when starting a new action
    }

    private void HideStatusPopup()
    {
        if (statusPopupPanel != null) statusPopupPanel.SetActive(false);
    }

    public void SetMenuButtonsInteractable(bool state)
    {
        if (hostButton != null && hostButton.targetGraphic != null) hostButton.interactable = state;
        if (quickJoinButton != null && quickJoinButton.targetGraphic != null) quickJoinButton.interactable = state;
        if (refreshListButton != null && refreshListButton.targetGraphic != null) refreshListButton.interactable = state;
        if (usernameInput != null && usernameInput.targetGraphic != null) usernameInput.interactable = state;
        if (characterDropdown != null && characterDropdown.targetGraphic != null) characterDropdown.interactable = state;

        CosmeticsManager cosManager = FindAnyObjectByType<CosmeticsManager>();
        if (cosManager != null) cosManager.SetButtonsInteractable(state);
    }

    private bool PrepareConnectionPayload(string password)
    {
        // Try to get name from input field, otherwise use the saved name from ChangeNameUI
        string userName = "Player";
        if (usernameInput != null && !string.IsNullOrWhiteSpace(usernameInput.text))
        {
            userName = usernameInput.text.Trim();
        }
        else
        {
            userName = PlayerPrefs.GetString("PlayerName", "Player");
        }
        
        // Retrieve cosmetics selections from PlayerPrefs
        int characterId = PlayerPrefs.GetInt("SelectedDog", 0);
        int dogHatId = PlayerPrefs.GetInt("SelectedDogHat", 0);
        int robberHatId = PlayerPrefs.GetInt("SelectedRobberHat", 0);

        if (string.IsNullOrWhiteSpace(userName))
        {
            SetErrorText("Please enter username.");
            return false;
        }

        SetConnectionData(userName, characterId, dogHatId, robberHatId, password);
        Debug.Log($"[OnlineSessionManager] Payload prepared: {userName}|{characterId}|{dogHatId}|{robberHatId}|{password}");
        return true;
    }

    private void SetConnectionData(string username, int characterId, int dogHatId, int robberHatId, string password)
    {
        string payload = $"{username}|{characterId}|{dogHatId}|{robberHatId}|{password}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
    }

    private void StartLobbyHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
        }
        heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine());
    }

    private IEnumerator HeartbeatLobbyCoroutine()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(15f);
        while (currentLobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            yield return wait;
        }
    }

    private void SetStatus(string message)
    {
        // No longer updating statusText here so it doesn't show debug info
        Debug.Log("[OnlineSessionManager] " + message);
    }

    public void SetErrorText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }
    }

    public void ClearErrorText()
    {
        if (statusText != null)
        {
            statusText.text = "";
            statusText.gameObject.SetActive(false);
        }
    }

    public async void LeaveSession()
    {
        ClearErrorText();
        SetMenuButtonsInteractable(false);
        SetStatus("Leaving session...");

        try
        {
            if (currentLobby != null)
            {
                string playerId = AuthenticationService.Instance.PlayerId;
                if (currentLobby.HostId == playerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
                }
                currentLobby = null;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }

            SetStatus("Disconnected.");
            SetMenuButtonsInteractable(true);
        }
        catch (System.Exception e)
        {
            SetStatus("Failed to leave: " + e.Message);
            SetMenuButtonsInteractable(true);
        }
    }

    private void Update()
    {
        ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
        if (connManager != null)
        {
            if (!_hasSubscribedToList)
            {
                connManager.ConnectedPlayers.OnListChanged += OnLobbyPlayersChanged;
                _hasSubscribedToList = true;
            }

            if (countdownText != null && sessionPanel != null && sessionPanel.activeSelf)
            {
                if (connManager.ConnectedPlayers.Count < 2)
                {
                    if (!countdownText.gameObject.activeSelf) countdownText.gameObject.SetActive(true);
                    countdownText.text = "Required 2 players to start the game";
                    _hasTriggeredGameStartAnim = false;
                }
                else
                {
                    float time = connManager.CountdownTimer.Value;
                    if (time > 0f || time == 0f)
                    {
                        if (!countdownText.gameObject.activeSelf) countdownText.gameObject.SetActive(true);
                        
                        if (time > 0f)
                        {
                            countdownText.text = $"Game start in {Mathf.CeilToInt(time)}";
                        }
                        else
                        {
                            countdownText.text = "Game starting...";
                            
                            if (!_hasTriggeredGameStartAudio)
                            {
                                _hasTriggeredGameStartAudio = true;
                                AudioScript audioScript = FindAnyObjectByType<AudioScript>();
                                if (audioScript != null) audioScript.StartGameUI();
                                StartCoroutine(TriggerTransitionRoutine());
                            }
                        }
                    }
                    else
                    {
                        _hasTriggeredGameStartAnim = false;
                        _hasTriggeredGameStartAudio = false;
                        if (!countdownText.gameObject.activeSelf) countdownText.gameObject.SetActive(true);
                        
                        int readyCount = 0;
                        foreach(var p in connManager.ConnectedPlayers)
                        {
                            if (p.IsReady) readyCount++;
                        }
                        countdownText.text = $"Waiting player to be ready ({readyCount}/{connManager.ConnectedPlayers.Count})";
                    }
                }
            }
        }
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerState> changeEvent)
    {
        if (sessionPanel == null || !sessionPanel.activeSelf) return;
        RefreshSessionPlayerList();
    }

    public void RefreshSessionPlayerList()
    {
        if (playerListContent == null || playerSlotPrefab == null) return;
        
        ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
        if (connManager == null) return;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        CosmeticsManager cosmeticsManager = FindAnyObjectByType<CosmeticsManager>();

        foreach (var player in connManager.ConnectedPlayers)
        {
            GameObject slotObj = Instantiate(playerSlotPrefab, playerListContent);
            PlayerListSlotUI slotUI = slotObj.GetComponent<PlayerListSlotUI>();
            if (slotUI != null)
            {
                Sprite icon = null;
                if (cosmeticsManager != null)
                {
                    if (player.CharacterId == 4) 
                    {
                        if (cosmeticsManager.robberHatIcons != null && player.RobberHatId < cosmeticsManager.robberHatIcons.Length)
                            icon = cosmeticsManager.robberHatIcons[player.RobberHatId];
                    }
                    else
                    {
                        if (customDogIcons != null && customDogIcons.Length > 0 && player.CharacterId < customDogIcons.Length)
                        {
                            icon = customDogIcons[player.CharacterId];
                        }
                        else if (cosmeticsManager.dogIcons != null && player.CharacterId < cosmeticsManager.dogIcons.Length)
                        {
                            icon = cosmeticsManager.dogIcons[player.CharacterId];
                        }
                    }
                }
                
                bool isLocal = (player.ClientId == NetworkManager.Singleton.LocalClientId);
                slotUI.Setup(player.PlayerName.ToString(), icon, player.IsHost, player.IsReady, isLocal);
            }
        }
    }

    private System.Collections.IEnumerator TriggerTransitionRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        LobbyUI lobbyUI = FindAnyObjectByType<LobbyUI>();
        if (lobbyUI != null && lobbyUI.CircleTransition != null)
        {
            lobbyUI.CircleTransition.SetBool("OutIn", true);
            if (lobbyUI.LoadingText != null) lobbyUI.LoadingText.SetActive(true);
        }
    }

    private void OnReadyButtonClicked()
    {
        ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
        if (connManager != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            connManager.ToggleReadyServerRpc();
        }
    }
}
