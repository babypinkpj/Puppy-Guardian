using UnityEngine;
using Unity.Netcode;

namespace Match
{
    public class RadarItem : RobberItem
    {
        [Rpc(SendTo.Server)]
        protected override void UseItemServerRpc()
        {
            RadarEffectClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void RadarEffectClientRpc()
        {
            // Only robber sees the glow
            if (NetworkManager.Singleton.LocalClientId == MatchManager.Instance.RobberClientId.Value)
            {
                MatchUIManager.Instance?.ShowAnnouncementRpc("Radar used: Dogs revealed for 5s");
                // TODO: Enable Outline/Glow on Dog prefabs for 5s
            }
        }
    }
}
