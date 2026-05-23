using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ConnectionManager : MonoBehaviour
{
    [Header("Character Prefabs")]
    [Tooltip("ใส่ Prefab ตัวละครเรียงตามลำดับ Dropdown: 0=Shiba, 1=Pug, 2=Duchun, 3=Husky, 4=Robber")]
    [SerializeField] private NetworkObject[] characterPrefabs;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] dogSpawnPoints;
    [SerializeField] private Transform robberSpawnPoint;

    [Header("UI Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject leaveButton;
    [SerializeField] private TMP_Text errorText;

    private readonly HashSet<string> _connectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ulong, string> _clientIdToName = new Dictionary<ulong, string>();
    private int _dogSpawnIndex = 0;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        }
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

    private string DecodePayloadToString(ArraySegment<byte> payload)
    {
        if (payload.Array == null || payload.Count <= 0) return "";
        return Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string rawPayload = DecodePayloadToString(request.Payload);
        Debug.Log($"[ApprovalCheck] Raw Payload received: {rawPayload}");

        // แยกข้อความด้วยเครื่องหมาย |
        string[] parts = rawPayload.Split('|');
        string incomingName = parts[0].Trim();
        int charId = (parts.Length > 1 && int.TryParse(parts[1], out int id)) ? id : 0;

        if (string.IsNullOrWhiteSpace(incomingName) || _connectedNames.Contains(incomingName))
        {
            response.Approved = false;
            response.Reason = "Name already in use or invalid.";
            response.Pending = false;
            return;
        }

        // อนุมัติให้ผ่านเข้าเกม
        response.Approved = true;
        response.CreatePlayerObject = true;

        // เลือกจุดเกิดตามประเภทตัวละคร (Robber แยกตัวออกไป)
        if (charId == 4 && robberSpawnPoint != null)
        {
            response.Position = robberSpawnPoint.position;
            response.Rotation = robberSpawnPoint.rotation;
        }
        else if (dogSpawnPoints != null && dogSpawnPoints.Length > 0)
        {
            int idx = _dogSpawnIndex % dogSpawnPoints.Length;
            response.Position = dogSpawnPoints[idx].position;
            response.Rotation = dogSpawnPoints[idx].rotation;
            _dogSpawnIndex++;
        }
        else
        {
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;
        }

        // นำพรีแฟบตัวละครที่เลือกมาสปอว์นแทนค่าว่างเปล่า
        if (characterPrefabs != null && charId >= 0 && charId < characterPrefabs.Length)
        {
            response.PlayerPrefabHash = characterPrefabs[charId].PrefabIdHash;
        }
        else
        {
            response.PlayerPrefabHash = null; // ป้องกันเกมพังถ้าลืมใส่ข้อมูล ให้ใช้ตัวตั้งต้น
            Debug.LogWarning("Character prefab index out of bounds! Using default network prefab.");
        }

        response.Reason = ""; // เคลียร์ข้อความเหตุผลการปฏิเสธให้ว่าง
        response.Pending = false;

        TrackNameOnServer(request.ClientNetworkId, incomingName);
    }

    private void TrackNameOnServer(ulong clientId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        if (_clientIdToName.TryGetValue(clientId, out string existing))
        {
            if (!string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
            {
                _connectedNames.Remove(existing);
                _clientIdToName[clientId] = name;
                _connectedNames.Add(name);
            }
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

    private void HandleServerStarted()
    {
        if (NetworkManager.Singleton.IsHost) SetUIConnected(true);
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId) SetUIConnected(true);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetUIConnected(false);
            string reason = NetworkManager.Singleton.DisconnectReason;
            if (!string.IsNullOrEmpty(reason)) SetError(reason);
        }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            UntrackNameOnServer(clientId);
        }
    }

    private void SetUIConnected(bool connected)
    {
        if (loginPanel != null) loginPanel.SetActive(!connected);
        if (leaveButton != null) leaveButton.SetActive(connected);
        if (connected) ClearError();
    }

    private void SetError(string message)
    {
        if (errorText != null) errorText.text = message;
        Debug.LogWarning(message);
    }

    private void ClearError()
    {
        if (errorText != null) errorText.text = "";
    }

    public void OnLeaveButtonClick()
    {
        ClearError();
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.Shutdown();
        SetUIConnected(false);
    }

    private void PrintConnectedClients()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log("========== SERVER CONNECTED CLIENTS ==========");
        foreach (var kvp in _clientIdToName)
        {
            Debug.Log($"ClientID: {kvp.Key} | Username: {kvp.Value}");
        }
        Debug.Log("===============================================");
    }
}