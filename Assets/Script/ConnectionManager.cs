using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public struct LobbyPlayerState : INetworkSerializable, IEquatable<LobbyPlayerState>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public int CharacterId;
    public int DogHatId;
    public int RobberHatId;
    public bool IsReady;
    public bool IsHost;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref CharacterId);
        serializer.SerializeValue(ref DogHatId);
        serializer.SerializeValue(ref RobberHatId);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref IsHost);
    }

    public bool Equals(LobbyPlayerState other)
    {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               CharacterId == other.CharacterId &&
               DogHatId == other.DogHatId &&
               RobberHatId == other.RobberHatId &&
               IsReady == other.IsReady &&
               IsHost == other.IsHost;
    }
}

public class ConnectionManager : NetworkBehaviour
{
    [Header("Character Prefabs")]
    [Tooltip("ใส่ Prefab ตัวละครเรียงตามลำดับ Dropdown: 0=Shiba, 1=Pug, 2=Duchun, 3=Husky, 4=Robber")]
    [SerializeField] private NetworkObject[] characterPrefabs;

    public NetworkList<LobbyPlayerState> ConnectedPlayers;
    public NetworkVariable<float> CountdownTimer = new NetworkVariable<float>(-1f);

    private readonly HashSet<string> _connectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ulong, string> _clientIdToName = new Dictionary<ulong, string>();
    private readonly Dictionary<ulong, int> _clientIdToHat = new Dictionary<ulong, int>();

    private struct TempClientData
    {
        public string Name;
        public int CharId;
        public int DogHat;
        public int RobberHat;
    }
    private Dictionary<ulong, TempClientData> _tempClientData = new Dictionary<ulong, TempClientData>();

    [HideInInspector] public string RoomPassword = "";

    public void SetHostData(string hostName, int charId, int dogHatId, int robberHatId)
    {
        _tempClientData[0] = new TempClientData 
        { 
            Name = hostName, 
            CharId = charId, 
            DogHat = dogHatId, 
            RobberHat = robberHatId 
        };
    }

    private void Awake()
    {
        ConnectedPlayers = new NetworkList<LobbyPlayerState>();
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    public int GetHatIdForClient(ulong clientId)
    {
        if (_clientIdToHat.TryGetValue(clientId, out int hatId))
            return hatId;
        return 0;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private string DecodePayloadToString(ArraySegment<byte> payload)
    {
        if (payload.Array == null || payload.Count <= 0) return "";
        return Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string rawPayload = DecodePayloadToString(request.Payload);
        Debug.Log($"[ApprovalCheck] Raw Payload received: {rawPayload}");

        string[] parts = rawPayload.Split('|');
        string incomingName = parts[0].Trim();
        int charId = (parts.Length > 1 && int.TryParse(parts[1], out int id)) ? id : 0;
        int dogHatId = (parts.Length > 2 && int.TryParse(parts[2], out int dHat)) ? dHat : 0;
        int robberHatId = (parts.Length > 3 && int.TryParse(parts[3], out int rHat)) ? rHat : 0;
        string incomingPassword = (parts.Length > 4) ? parts[4] : "";

        // Server-side Password Verification
        if (!string.IsNullOrEmpty(RoomPassword))
        {
            if (incomingPassword != RoomPassword)
            {
                response.Approved = false;
                response.Reason = "Incorrect Password!";
                response.Pending = false;
                return;
            }
        }

        OnlineSessionManager sessionMgr = FindAnyObjectByType<OnlineSessionManager>();
        int maxPlayers = (sessionMgr != null) ? sessionMgr.maxPlayers : 5;
        if (ConnectedPlayers.Count >= maxPlayers)
        {
            response.Approved = false;
            response.Reason = "Server is Full!";
            response.Pending = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(incomingName) || _connectedNames.Contains(incomingName))
        {
            response.Approved = false;
            response.Reason = "Name already in use or invalid.";
            response.Pending = false;
            return;
        }

        _tempClientData[request.ClientNetworkId] = new TempClientData 
        { 
            Name = incomingName, 
            CharId = charId, 
            DogHat = dogHatId, 
            RobberHat = robberHatId 
        };

        // อนุมัติให้ผ่านเข้าเกม
        response.Approved = true;
        response.CreatePlayerObject = false; // NO PLAYER OBJECT IN LOBBY

        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;
        response.Reason = ""; 
        response.Pending = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (_tempClientData.TryGetValue(NetworkManager.ServerClientId, out TempClientData hostData))
            {
                ConnectedPlayers.Add(new LobbyPlayerState
                {
                    ClientId = NetworkManager.ServerClientId,
                    PlayerName = new FixedString32Bytes(hostData.Name),
                    CharacterId = hostData.CharId,
                    DogHatId = hostData.DogHat,
                    RobberHatId = hostData.RobberHat,
                    IsReady = false,
                    IsHost = true
                });
                _clientIdToName[NetworkManager.ServerClientId] = hostData.Name;
                _connectedNames.Add(hostData.Name);
                _clientIdToHat[NetworkManager.ServerClientId] = hostData.DogHat;
                _tempClientData.Remove(NetworkManager.ServerClientId);
            }
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (IsServer && IsSpawned)
        {
            if (_tempClientData.TryGetValue(clientId, out TempClientData data))
            {
                ConnectedPlayers.Add(new LobbyPlayerState
                {
                    ClientId = clientId,
                    PlayerName = data.Name,
                    CharacterId = data.CharId,
                    DogHatId = data.DogHat,
                    RobberHatId = data.RobberHat,
                    IsReady = false,
                    IsHost = (clientId == NetworkManager.ServerClientId)
                });
                _tempClientData.Remove(clientId);
                _clientIdToHat[clientId] = (data.CharId == 4) ? data.RobberHat : data.DogHat;
                _clientIdToName[clientId] = data.Name;
                _connectedNames.Add(data.Name);
            }
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            string reason = NetworkManager.Singleton.DisconnectReason;
            if (!string.IsNullOrEmpty(reason)) 
            {
                OnlineSessionManager sessionMgr = FindAnyObjectByType<OnlineSessionManager>();
                if (sessionMgr != null) sessionMgr.SetErrorText(reason);
            }
        }

        if (IsServer)
        {
            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                if (ConnectedPlayers[i].ClientId == clientId)
                {
                    ConnectedPlayers.RemoveAt(i);
                    break;
                }
            }
            if (_clientIdToName.TryGetValue(clientId, out string name))
            {
                _clientIdToName.Remove(clientId);
                _connectedNames.Remove(name);
            }
            _tempClientData.Remove(clientId);
            _clientIdToHat.Remove(clientId);
            
            CheckCountdown();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ToggleReadyServerRpc(RpcParams rpcParams = default)
    {
        if (_isStartingGame) return;
        
        ulong clientId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < ConnectedPlayers.Count; i++)
        {
            if (ConnectedPlayers[i].ClientId == clientId)
            {
                var p = ConnectedPlayers[i];
                p.IsReady = !p.IsReady;
                ConnectedPlayers[i] = p;
                CheckCountdown();
                break;
            }
        }
    }

    private void CheckCountdown()
    {
        if (_isStartingGame) return;
        
        if (ConnectedPlayers.Count < 2) 
        {
            CountdownTimer.Value = -1f;
            return;
        }
        foreach (var p in ConnectedPlayers)
        {
            if (!p.IsReady)
            {
                CountdownTimer.Value = -1f;
                return;
            }
        }
        if (CountdownTimer.Value < 0f)
        {
            CountdownTimer.Value = 5f;
        }
    }

    private bool _isStartingGame = false;

    private void Update()
    {
        if (IsServer && CountdownTimer.Value > 0f && !_isStartingGame)
        {
            CountdownTimer.Value -= Time.deltaTime;
            if (CountdownTimer.Value <= 0f)
            {
                CountdownTimer.Value = 0f;
                _isStartingGame = true;
                StartCoroutine(DelayedStartGame());
            }
        }
    }

    private System.Collections.IEnumerator DelayedStartGame()
    {
        yield return new WaitForSeconds(2.0f);
        StartGame();
    }

    private void StartGame()
    {
        string sceneName = FindAnyObjectByType<OnlineSessionManager>()?.GameplaySceneName ?? "GameScene";
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}