using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

public class LevelCommands : MonoBehaviour
{
    [Command("add-level")]
    public static void AddLevel(ulong networkObjectId, int amount = 1)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            Debug.LogWarning($"NetworkObject {networkObjectId} not found");
            return;
        }

        var player = netObj.GetComponent<PlayerStateSync>();

        if (player != null)
        {
            player.PlayerLevel.Value += amount;
            Debug.Log($"Added {amount} level to {networkObjectId}");
        }
    }
}
