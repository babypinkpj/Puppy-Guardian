using UnityEngine;
using Unity.Netcode;

public class CosmeticNetworkSync : NetworkBehaviour
{
    [Tooltip("Hat prefabs that can be equipped. This array must exactly match the arrays in CosmeticsManager!")]
    public GameObject[] hatPrefabs;

    private NetworkVariable<int> currentHatId = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private GameObject spawnedHat;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // The Server asks ConnectionManager for the hat ID requested by this client
            ConnectionManager connManager = FindAnyObjectByType<ConnectionManager>();
            if (connManager != null)
            {
                currentHatId.Value = connManager.GetHatIdForClient(OwnerClientId);
            }
        }

        // Listen for changes
        currentHatId.OnValueChanged += OnHatIdChanged;

        // Equip immediately for late joiners or instant spawn
        EquipHat(currentHatId.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentHatId.OnValueChanged -= OnHatIdChanged;
    }

    private void OnHatIdChanged(int oldHat, int newHat)
    {
        EquipHat(newHat);
    }

    private void EquipHat(int hatId)
    {
        if (spawnedHat != null)
        {
            Destroy(spawnedHat);
        }

        if (hatId <= 0 || hatPrefabs == null || hatId >= hatPrefabs.Length || hatPrefabs[hatId] == null)
            return;

        Transform headPoint = FindChildRecursive(transform, "HeadPoint");
        if (headPoint == null) headPoint = FindChildRecursive(transform, "Head");
        if (headPoint == null) headPoint = transform;

        spawnedHat = Instantiate(hatPrefabs[hatId], headPoint);
        spawnedHat.transform.localPosition = Vector3.zero;
        spawnedHat.transform.localRotation = Quaternion.identity;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
