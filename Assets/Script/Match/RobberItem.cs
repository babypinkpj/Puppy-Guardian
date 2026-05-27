using UnityEngine;
using Unity.Netcode;

namespace Match
{
    public enum ItemType
    {
        Airhorn,
        Radar,
        MuteShoe,
        TrapFood
    }

    public class RobberItem : NetworkBehaviour
    {
        public ItemType type;
        public Sprite icon;

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            RobberController robber = other.GetComponent<RobberController>();
            if (robber != null && robber.IsOwner)
            {
                PickupClientRpc(robber.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void PickupClientRpc(ulong robberNetId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(robberNetId, out NetworkObject robberObj))
            {
                RobberController robber = robberObj.GetComponent<RobberController>();
                if (robber != null && robber.IsOwner)
                {
                    robber.PickupItem(this);
                }
                else
                {
                    gameObject.SetActive(false); // Hide for others
                }
            }
        }

        public virtual void UseItem(RobberController robber)
        {
            UseItemServerRpc();
        }

        [Rpc(SendTo.Server)]
        protected virtual void UseItemServerRpc()
        {
        }
    }
}
