using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;

public class ConnectionManager : MonoBehaviour
{
    private readonly HashSet<string> _connectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ulong, string> _clientIdToName = new Dictionary<ulong, string>();

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private string DecodePayloadToString(ArraySegment<byte> payload)
    {
        if (payload.Array == null || payload.Count <= 0)
            return "";

        return Encoding.UTF8.GetString(payload.Array, payload.Offset, payload.Count);
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string incomingName = DecodePayloadToString(request.Payload);
        Debug.Log($"[ConnectionManager] Approval Check for ClientId: {request.ClientNetworkId}, Name: '{incomingName}'");

        // Reject connection if the name is already in use
        if (string.IsNullOrWhiteSpace(incomingName) || _connectedNames.Contains(incomingName))
        {
            response.Approved = false;
            response.Reason = "Name already in use or invalid.";
            response.Pending = false;
            return;
        }

        // Approve the connection
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.PlayerPrefabHash = null; // Uses the default player prefab configured in NetworkManager
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;
        response.Pending = false;

        // Track name on Server
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

    private void HandleClientDisconnected(ulong clientId)
    {
        // Server untracks the client on disconnect
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            UntrackNameOnServer(clientId);
        }
    }

    private void PrintConnectedClients()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("========== SERVER CONNECTED CLIENTS ==========");
        if (_clientIdToName.Count == 0)
        {
            Debug.Log("No connected clients.");
        }
        else
        {
            foreach (var kvp in _clientIdToName)
            {
                Debug.Log($"ClientID: {kvp.Key} | Username: {kvp.Value}");
            }
        }
        Debug.Log("===============================================");
    }

    /// <summary>
    /// Public helper to get the username of a connected client by ID (useful for other team members).
    /// </summary>
    public string GetUsername(ulong clientId)
    {
        if (_clientIdToName.TryGetValue(clientId, out string name))
        {
            return name;
        }
        return "Unknown";
    }
}
