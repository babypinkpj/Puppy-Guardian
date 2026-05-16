using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerRpcDemo : NetworkBehaviour
{
    public void PingServer(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!context.performed) return;
        Debug.Log($"[Local] Interact pressed by client: {NetworkManager.Singleton.LocalClientId}");
        SendPingServerRpc();
    }

    [ServerRpc]
    private void SendPingServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[Server] Received request from clientId: {senderId}");
    }
}

