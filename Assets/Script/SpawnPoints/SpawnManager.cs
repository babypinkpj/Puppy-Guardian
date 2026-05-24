using System;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] robberSpawnPoints;  // to Robber Spawner 
    [SerializeField] private Transform[] playerSpawnPoints;  // to Player Spawner(1)-(4)

    private bool[] _robberSlotUsed;
    private bool[] _playerSlotUsed;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        _robberSlotUsed = new bool[robberSpawnPoints.Length];
        _playerSlotUsed = new bool[playerSpawnPoints.Length];
    }

    public Vector3 GetSpawnPosition(bool isRobber)
    {
        if (!IsServer) return Vector3.zero;

        Transform[] slots = isRobber ? robberSpawnPoints : playerSpawnPoints;
        bool[] used = isRobber ? _robberSlotUsed : _playerSlotUsed;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!used[i])
            {
                used[i] = true;
                return slots[i].position;
            }
        }

        string role = isRobber ? "Robber" : "Player";
        Debug.LogWarning($"[SpawnManager] {role} spawn slots are all full!");
        return slots[0].position; // fallback
    }

    public void ReleaseSpawnSlot(bool isRobber, Vector3 pos)
    {
        if (!IsServer) return;

        Transform[] slots = isRobber ? robberSpawnPoints : playerSpawnPoints;
        bool[] used = isRobber ? _robberSlotUsed : _playerSlotUsed;

        for (int i = 0; i < slots.Length; i++)
        {
            if (Vector3.Distance(slots[i].position, pos) < 0.1f)
            {
                used[i] = false;
                return;
            }
        }
    }
}