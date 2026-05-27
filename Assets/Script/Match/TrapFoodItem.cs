using UnityEngine;
using Unity.Netcode;

namespace Match
{
    public class TrapFoodItem : RobberItem
    {
        public GameObject trapFoodPrefab;

        [Rpc(SendTo.Server)]
        protected override void UseItemServerRpc()
        {
            // Spawn TrapFood
            GameObject trap = Instantiate(trapFoodPrefab, transform.position, Quaternion.identity);
            trap.GetComponent<NetworkObject>().Spawn();
        }
    }
}
