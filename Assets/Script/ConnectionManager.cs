using QFSW.QC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionManager : MonoBehaviour
{
    [Header("Text UI")]
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_Text NameText;
    [SerializeField] private TMP_Text errorText; // optional (can be null)

    [Header("Game UI")]
    [SerializeField] GameObject GameUIPanel;
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject leaveButton;
    [SerializeField] GameObject changeNamePanel;
    [SerializeField] GameObject cosmeticPanel;

    // Buttons
    [Header("Buttons")]
    [SerializeField] Button changeNameButton;
    [SerializeField] Button cosmaticButton;
    [SerializeField] Button startHostButton;
    [SerializeField] Button startClientButton;
    [SerializeField] Button startGameButton;

    [Header("Audio")]
    [SerializeField] private AudioSource buttonAudioSource;

    [Header("Podium Spawn")]
    [SerializeField] private Transform hostPodium;
    [SerializeField] private Transform[] clientPodiums;
    private Dictionary<ulong, int> clientPodiumMap = new Dictionary<ulong, int>();
    private bool[] podiumTaken;

    [Header("Skin")]
    private int selectedSkinIndex = 0;

    [Header("Level")]
    public int playerLevel = 1;

    [Header("Player Count UI")]
    [SerializeField] private TMP_Text playerCountText;

    [Header("Arena Settings")]
    [SerializeField] private Transform[] arenaPlayerPodiums; // Assign the 4 player side podiums here
    [SerializeField] private Transform arenaEnemyPodium;     // Assign the single enemy podium here

    private void Awake()
    {
        NameText.text = PlayerPrefs.GetString(
            "PlayerName",
            "Player" + UnityEngine.Random.Range(1000, 9999));

        podiumTaken = new bool[clientPodiums.Length];
        Instance = this;
    }

    private void SetConnectionData(string username, int characterID)
    {
        string payload = $"{username}:{characterID}:{playerLevel}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            Encoding.UTF8.GetBytes(payload);
    }
    public void PlayButtonSound()
{
    if (buttonAudioSource != null)
    {
        buttonAudioSource.Play();
    }
}

    public void StartHostWithUsername()
    {
       
        string userName = NameText.text;
        LocalUsername = userName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            SetError("Name cannot be empty");
            return;
        }
     PlayButtonSound();
        SetConnectionData(userName, selectedSkinIndex);
        NetworkManager.Singleton.StartHost();

        // Fix: update UI immediately
        SetUIConnected(true);
    }

    public void StartClientWithUsername()
    {
        
        if (usernameInput == null)
        {
            Debug.LogError("UsernameInput is not assigned!");
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager is missing!");
            return;
        }
    PlayButtonSound();
        string userName = NameText.text;
        LocalUsername = userName;
        SetConnectionData(userName, selectedSkinIndex);
        NetworkManager.Singleton.StartClient();

        // Fix: update UI immediately
        SetUIConnected(true);
    }

    private bool isApproveConnection = false;

    [Command("set-approve")]
    public bool SetIsApproveConnection()
    {
        isApproveConnection = !isApproveConnection;
        return isApproveConnection;
    }

    private void Start()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        buttonAudioSource = GetComponent<AudioSource>();
    }
    
    private string DecodePayloadToString(ArraySegment<byte> payload)
    {
        if (payload.Array == null || payload.Count <= 0)
            return "";

        return Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
    }

    private readonly HashSet<string> _connectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ulong, string> _clientIdToName = new Dictionary<ulong, string>();
    
    private void PrintConnectedClients()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        Debug.Log("========== SERVER CONNECTED CLIENTS ==========");

        if (_clientIdToName.Count == 0)
        {
            Debug.Log("No connected clients.");
            return;
        }

        foreach (var kvp in _clientIdToName)
        {
            Debug.Log($"ClientID: {kvp.Key} | Username: {kvp.Value}");
        }

        Debug.Log("===============================================");
    }
    
    private void TrackNameOnServer(ulong clientId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        // Prevent double-tracking
        if (_clientIdToName.TryGetValue(clientId, out string existing))
        {
            if (!string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
            {
                _connectedNames.Remove(existing);
                _clientIdToName[clientId] = name;
                _connectedNames.Add(name);
            }
            return;
        }
        else
        {
            _clientIdToName.Add(clientId, name);
            _connectedNames.Add(name);
        }
        PrintConnectedClients();
    }
    
    private void UntrackNameOnServer(ulong clientId)
    {
        if (_clientIdToName.TryGetValue(clientId, out string name))
        {
            _clientIdToName.Remove(clientId);
            _connectedNames.Remove(name);
        }

        PrintConnectedClients();
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (!TryParseConnectionPayload(request.Payload, out string incomingName, out int characterID, out int level))
        {
            response.Approved = false;
            return;
        }

        Transform spawn = GetSpawnPoint(request.ClientNetworkId);

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.PlayerPrefabHash = GetPrefabHashForCharacterID(characterID);

        // Set the initial position data
        response.Position = Vector3.zero;
        response.Rotation = spawn != null ? spawn.rotation : Quaternion.identity;

        response.Pending = false;

        TrackNameOnServer(request.ClientNetworkId, incomingName);
        StartCoroutine(SetPlayerLevel(request.ClientNetworkId, level));
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
        
        NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            SetUIConnected(true);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        UpdatePlayerCountUI();
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetUIConnected(true);

            startHostButton.interactable = NetworkManager.Singleton.IsHost;
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        UpdatePlayerCountUI();
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetUIConnected(false);

            string reason = NetworkManager.Singleton.DisconnectReason;

            if (!string.IsNullOrEmpty(reason))
                SetError(reason);
        }

        if (NetworkManager.Singleton.IsServer)
        {
            if (clientPodiumMap.TryGetValue(clientId, out int index))
            {
                podiumTaken[index] = false;
                clientPodiumMap.Remove(clientId);

                Debug.Log($"Freed podium {index}");
            }

            UntrackNameOnServer(clientId);
        }
    }

    private void SetUIConnected(bool connected)
    {
        loginPanel.SetActive(!connected);
        leaveButton.SetActive(connected);
        changeNameButton.interactable = !connected;
        GameUIPanel.SetActive(!connected);

        if (connected)
            ClearError();
    }
    
    private void SetError(string message)
    {
        if (errorText != null)
            errorText.text = message;

        Debug.LogWarning(message);
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = "";
    }

    public void OnLeaveButtonClick()
    {
        ClearError();

        if (NetworkManager.Singleton != null)
        {
            // Clear all tracking data so the next session starts fresh
            _connectedNames.Clear();
            _clientIdToName.Clear();
            clientPodiumMap.Clear();

            if (podiumTaken != null)
            {
                for (int i = 0; i < podiumTaken.Length; i++) podiumTaken[i] = false;
            }

            NetworkManager.Singleton.Shutdown();
        }

        SetUIConnected(false);
    }

    public void OpenChangeName()
    {
        changeNamePanel.GetComponentInChildren<TMP_InputField>().text = "";
        changeNamePanel.SetActive(true);
        changeNameButton.interactable = false; cosmaticButton.interactable = false; startHostButton.interactable = false; startClientButton.interactable = false;
    }
    public void AcceptChangeName()
    {
        string newName = changeNamePanel.GetComponentInChildren<TMP_InputField>().text;
        if (string.IsNullOrWhiteSpace(newName))
        {
            SetError("Name cannot be empty");
            return;
        }
        NameText.text = newName;
        PlayerPrefs.SetString("PlayerName", newName);
        PlayerPrefs.Save();
        SetConnectionData(newName, selectedSkinIndex);
        changeNamePanel.SetActive(false);
        changeNameButton.interactable = true; cosmaticButton.interactable = true; startHostButton.interactable = true; startClientButton.interactable = true;
    }
    public void CancelChangeName()
    {
        changeNamePanel.SetActive(false);
        changeNameButton.interactable = true; cosmaticButton.interactable = true; startHostButton.interactable = true; startClientButton.interactable = true;
    }

    private Transform GetSpawnPoint(ulong clientId)
    {
        // The Host is ALWAYS ServerClientId (0)
        if (clientId == NetworkManager.ServerClientId)
        {
            if (hostPodium != null) return hostPodium;

            Debug.LogError("Host Podium is not assigned in the Inspector!");
            return this.transform; // Fallback to avoid (0,0,0)
        }

        // Client logic
        if (clientPodiumMap.TryGetValue(clientId, out int assignedIndex))
        {
            return clientPodiums[assignedIndex];
        }

        for (int i = 0; i < clientPodiums.Length; i++)
        {
            if (!podiumTaken[i])
            {
                podiumTaken[i] = true;
                clientPodiumMap[clientId] = i;
                return clientPodiums[i];
            }
        }

        return (clientPodiums.Length > 0) ? clientPodiums[0] : this.transform;
    }

    private void PrintPodiums()
    {
        for (int i = 0; i < podiumTaken.Length; i++)
        {
            Debug.Log($"Podium {i}: {(podiumTaken[i] ? "Taken" : "Free")}");
        }
    }
    public void OpenCosmetic()
    {
        PlayButtonSound();
        cosmeticPanel.SetActive(true);
        changeNameButton.interactable = false; cosmaticButton.interactable = false; startHostButton.interactable = false; startClientButton.interactable = false;
    }
    public void skin1() { PlayButtonSound(); selectedSkinIndex = 0; CloseCosmetic(); }
    public void skin2() { PlayButtonSound(); selectedSkinIndex = 1; CloseCosmetic(); }
    public void skin3() { PlayButtonSound(); selectedSkinIndex = 2; CloseCosmetic(); }
    public void skin4() { PlayButtonSound(); selectedSkinIndex = 3; CloseCosmetic(); }
    public void enemy() { PlayButtonSound(); selectedSkinIndex = 4; CloseCosmetic(); }

    private void CloseCosmetic()
    {
        SetConnectionData(NameText.text, selectedSkinIndex);
        cosmeticPanel.SetActive(false);
        changeNameButton.interactable = true; cosmaticButton.interactable = true; startHostButton.interactable = true; startClientButton.interactable = true;
    }
    private bool TryParseConnectionPayload(ArraySegment<byte> payload, out string username, out int characterID, out int level)
    {
        username = "";
        characterID = 0;
        level = 1;

        string decoded = DecodePayloadToString(payload);

        if (string.IsNullOrWhiteSpace(decoded))
            return false;

        string[] parts = decoded.Split(':');

        if (parts.Length >= 1)
            username = parts[0].Trim();

        if (parts.Length >= 2)
            int.TryParse(parts[1].Trim(), out characterID);

        if (parts.Length >= 3)
            int.TryParse(parts[2].Trim(), out level);

        return true;
    }

    [SerializeField] private List<uint> alternatePlayerPrefabHashes = new();

    private uint? GetPrefabHashForCharacterID(int characterID)
    {
        if (alternatePlayerPrefabHashes == null || alternatePlayerPrefabHashes.Count == 0)
            return null;
        if (characterID < 0 || characterID >= alternatePlayerPrefabHashes.Count)
            return alternatePlayerPrefabHashes[0];
        return alternatePlayerPrefabHashes[characterID];
    }

    public static ConnectionManager Instance { get; private set; }
    public string LocalUsername { get; private set; } = "";
    private IEnumerator SetPlayerLevel(ulong clientId, int level)
    {
        // Wait 1 frame to ensure the NetworkObject is fully initialized
        yield return null;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var player = client.PlayerObject;
            if (player != null)
            {
                Transform spawn = GetSpawnPoint(clientId);

                if (spawn != null)
                {
                    TeleportOwnerClientRpc(
                        spawn.position,
                        spawn.rotation,
                        clientId,
                        new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new[] { clientId }
                            }
                        }
                    );
                }

                // 2. Sync the level data
                var state = player.GetComponent<PlayerStateSync>();
                if (state != null)
                {
                    state.PlayerLevel.Value = level;
                }

                // 3. FINALLY: Reveal the player after they are in the right spot
                // Assuming your character model is in a child object named "Visuals"
                Transform visuals = player.transform.Find("Visuals");
                if (visuals != null) visuals.gameObject.SetActive(true);
            }
        }
    }
    private void Update()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
        {
            UpdatePlayerCountUI();
        }
    }
    private void UpdatePlayerCountUI()
    {
        // The Singleton.ConnectedClients list is the "Source of Truth"
        int count = NetworkManager.Singleton.ConnectedClients.Count;

        if (playerCountText != null)
        {
            playerCountText.text = $"{count}/5 Players";
        }
    }
    // Link this method to your "Start Game" UI Button's OnClick event
    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only host can start the game!");
            return;
        }

        leaveButton.SetActive(false);

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            var player = client.Value.PlayerObject;

            var state = player.GetComponent<PlayerStateSync>();
            if (state != null)
            {
                if (clientPodiumMap.TryGetValue(clientId, out int index))
                {
                    state.ArenaIndex.Value = index;
                }

                state.IsInArena.Value = true;
            }
        }
    }
    public void TeleportLocalPlayerToArena()
    {
        var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer == null) return;

        var state = localPlayer.GetComponent<PlayerStateSync>();
        Transform targetPodium = null;

        if (localPlayer.TryGetComponent<MainEnemyScript>(out var enemy))
        {
            targetPodium = arenaEnemyPodium;
        }
        else
        {
            int index = state.ArenaIndex.Value;

            if (index >= 0 && index < arenaPlayerPodiums.Length)
            {
                targetPodium = arenaPlayerPodiums[index];
            }
        }

        if (targetPodium != null &&
            localPlayer.TryGetComponent<NetworkTransform>(out var nt))
        {
            nt.Teleport(targetPodium.position, targetPodium.rotation, localPlayer.transform.localScale);
        }
    }
    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 position, Quaternion rotation, ulong targetClientId, ClientRpcParams rpcParams = default)
    {
        // Only the target client runs this
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        var player = NetworkManager.Singleton.LocalClient.PlayerObject;

        if (player != null && player.TryGetComponent<NetworkTransform>(out var nt))
        {
            nt.Teleport(position, rotation, player.transform.localScale);
        }
    }
}

