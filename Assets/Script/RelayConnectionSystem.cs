using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Netcode;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;


public class RelayConnectionSystem : MonoBehaviour
{
    [Header("UI References (For MOOK to connect)")]
    [Tooltip("Input field for player's username")]
    public TMP_InputField usernameInput;
    
    [Tooltip("Button to start host/join matchmaking")]
    public Button startButton;
    
    [Tooltip("Button to leave the game session")]
    public Button leaveButton;
    
    [Tooltip("Text to display current connection status")]
    public TMP_Text statusText;

    [Header("Settings")]
    [Tooltip("Max players allowed in the game")]
    public int maxPlayers = 4;
    public TMP_Dropdown characterDropdown;
    
    [Tooltip("Unity Services Profile Name (use different profiles if testing on same machine)")]
    public string authProfileName = "default";
    
    [Tooltip("The name of the Lobby to search or create")]
    public string lobbyName = "PuppyGuardianLobby";

    public GameObject loginPanel;
    private Lobby currentLobby;
    private Coroutine lobbyHeartbeatCoroutine;
    private const string JoinCodeKey = "RelayJoinCode";

    private void Start()
    {
        // Initial UI State
        if (leaveButton != null) leaveButton.gameObject.SetActive(false);
        if (startButton != null) startButton.interactable = true;
        SetStatus("Ready to Connect");
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    /// <summary>
    /// Call this function when the player clicks the Start button.
    /// It automatically signs in and joins an existing lobby. If none is found, it hosts a new one.
    /// </summary>
    public async void StartMatchmaking()
    {
        if (startButton != null) startButton.interactable = false;

        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("Error: Please enter a username.");
            if (startButton != null) startButton.interactable = true;
            return;
        }

        try
        {
            // 1. Initialize Unity Services & Sign In
            await InitializeAndSignInAsync();

            // 2. Try to join an existing game (Client flow)
            SetStatus("Searching for available games...");
            try
            {
                await QuickJoinLobbyAndRelayAsync(username);
            }
            catch (LobbyServiceException)
            {
                // 3. If no lobby is found, create a new one (Host flow)
                SetStatus("No active games found. Hosting a new game...");
                await HostLobbyAndRelayAsync(username);
            }
        }
        catch (Exception e)
        {
            SetStatus($"Error: {e.Message}");
            if (startButton != null) startButton.interactable = true;
        }
    }

    /// <summary>
    /// Initialize Unity Gaming Services and sign in anonymously.
    /// </summary>
    private async Task InitializeAndSignInAsync()
    {
        SetStatus("Initializing Services...");

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions();
            if (!string.IsNullOrWhiteSpace(authProfileName))
            {
                options.SetProfile(authProfileName);
            }
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            SetStatus("Signing in...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log($"[RelayConnection] Signed in as Player ID: {AuthenticationService.Instance.PlayerId}");
    }

    /// <summary>
    /// Join an existing Lobby and connect to the corresponding Relay server.
    /// </summary>
    private async Task QuickJoinLobbyAndRelayAsync(string username)
    {
        // Find and join a lobby
        QuickJoinLobbyOptions lobbyOptions = new QuickJoinLobbyOptions();
        currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(lobbyOptions);

        // Get the relay join code from lobby data
        if (currentLobby.Data == null || !currentLobby.Data.ContainsKey(JoinCodeKey))
        {
            throw new Exception("Lobby data is missing Relay Join Code.");
        }
        string relayJoinCode = currentLobby.Data[JoinCodeKey].Value;

        // Join Relay server using the code
        SetStatus($"Connecting to Relay (Code: {relayJoinCode})...");
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

        // Update UnityTransport with Relay server data
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));
        // Setup Username as connection payload
        SetConnectionPayload(username);

        // Start client in NetworkManager
        NetworkManager.Singleton.StartClient();

        UpdateUIOnConnected(true);
        SetStatus("Connecting...");
    }

    /// <summary>
    /// Create a new Relay allocation, retrieve its join code, and register it inside a new Lobby.
    /// </summary>
    private async Task HostLobbyAndRelayAsync(string username)
    {
        // 1. Create Relay Allocation
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // 2. Create Lobby with Relay Join Code in the metadata
        CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                {
                    JoinCodeKey,
                   new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode)

                }
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);

        // 3. Keep the lobby alive with heartbeats
        StartLobbyHeartbeat();

        // 4. Update UnityTransport with Relay server data
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

        // Setup Username as connection payload
        SetConnectionPayload(username);

        // Start host in NetworkManager
        NetworkManager.Singleton.StartHost();

        UpdateUIOnConnected(true);
        SetStatus($"Hosting Game (Lobby ID: {currentLobby.Id})");
    }

    /// <summary>
    /// Set the player's username as connection data payload so the server can track/display names.
    /// </summary>
   private void SetConnectionPayload(string username)
{
    if (string.IsNullOrWhiteSpace(username))
    {
        username = "Player_" + UnityEngine.Random.Range(1000, 9999);
    }

    // ดึงค่า Index จาก Dropdown เมนูเลือกตัวละคร (0=Shiba, 1=Pug, 2=Duchun, 3=Husky, 4=Robber)
    int charId = characterDropdown != null ? characterDropdown.value : 0;
    
    // รวมร่างข้อมูลเป็นฟอร์แมต Username|CharacterID ตัวอย่างเช่น "MOOK|1"
    string payloadString = $"{username}|{charId}";
    
    Debug.Log($"[RelayConnection] Setting payload: {payloadString}");
    
    // ส่งข้อมูลเข้าสู่ระบบ NetworkConfig เพื่อให้ Server แกะอ่านตอนขอเข้าห้อง
    NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payloadString);
}

    /// <summary>
    /// Sends a ping every 15 seconds to keep the Unity Lobby active.
    /// </summary>
    private void StartLobbyHeartbeat()
    {
        if (lobbyHeartbeatCoroutine != null) StopCoroutine(lobbyHeartbeatCoroutine);
        lobbyHeartbeatCoroutine = StartCoroutine(LobbyHeartbeatRoutine());
    }

    private IEnumerator LobbyHeartbeatRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(15f);
        while (currentLobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            yield return wait;
        }
    }

    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            UpdateUIOnConnected(true);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UpdateUIOnConnected(true);
            SetStatus("Connected to Game");
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            UpdateUIOnConnected(false);
            
            string reason = NetworkManager.Singleton.DisconnectReason;
            if (!string.IsNullOrEmpty(reason))
            {
                SetStatus($"Disconnected: {reason}");
            }
            else
            {
                SetStatus("Disconnected from server.");
            }
        }
    }

    /// <summary>
    /// Call this function to disconnect and clean up the current session.
    /// </summary>
    public async void LeaveSession()
    {
        SetStatus("Disconnecting...");

        try
        {
            // Stop heartbeat pings
            if (lobbyHeartbeatCoroutine != null)
            {
                StopCoroutine(lobbyHeartbeatCoroutine);
                lobbyHeartbeatCoroutine = null;
            }

            if (currentLobby != null)
            {
                string playerId = AuthenticationService.Instance.PlayerId;

                if (currentLobby.HostId == playerId)
                {
                    // Host deletes the lobby
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                    Debug.Log("[RelayConnection] Lobby deleted by Host.");
                }
                else
                {
                    // Client leaves the lobby
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
                    Debug.Log("[RelayConnection] Client left Lobby.");
                }
                currentLobby = null;
            }

            // Shutdown Netcode connection
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            ResetUIState();
            SetStatus("Disconnected successfully");
        }
        catch (Exception e)
        {
            SetStatus($"Leave failed: {e.Message}");
            ResetUIState();
        }
    }

    private void UpdateUIOnConnected(bool isConnected)
    {
         if (loginPanel != null) loginPanel.SetActive(!isConnected); 
        if (leaveButton != null) leaveButton.gameObject.SetActive(isConnected);
        if (startButton != null) startButton.gameObject.SetActive(!isConnected);
        if (usernameInput != null) usernameInput.interactable = !isConnected;
    }

    private void ResetUIState()
    {
        if (loginPanel != null) loginPanel.SetActive(true); 
        if (leaveButton != null) leaveButton.gameObject.SetActive(false);
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = true;
        }
        if (usernameInput != null) usernameInput.interactable = true;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log($"[RelayConnection] {message}");
    }
}
