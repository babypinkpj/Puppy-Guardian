using UnityEngine;
using Unity.Netcode;

namespace Match
{
    public class AirhornItem : RobberItem
    {
        public AudioClip airhornClip;

        [Rpc(SendTo.Server)]
        protected override void UseItemServerRpc()
        {
            AirhornEffectClientRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void AirhornEffectClientRpc()
        {
            // Play global sound
            if (airhornClip) AudioSource.PlayClipAtPoint(airhornClip, transform.position);

            // Apply to all dogs locally
            DogController[] dogs = FindObjectsByType<DogController>(FindObjectsSortMode.None);
            foreach (var dog in dogs)
            {
                dog.ApplySlowness(1, 10f); // Slowness I for 10s
                dog.ApplyBlur(5f); // Blur for 5s
            }
        }
    }
}
