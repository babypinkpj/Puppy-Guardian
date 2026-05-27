using UnityEngine;
using Unity.Netcode;

namespace Match
{
    public class PoliceCarEscape : NetworkBehaviour
    {
        [Header("Audio")]
        public AudioClip sirenClip;
        public AudioSource audioSource;

        public override void OnNetworkSpawn()
        {
            if (audioSource && sirenClip)
            {
                audioSource.clip = sirenClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            if (MatchManager.Instance.CurrentState.Value == MatchManager.MatchState.Escape)
            {
                DogController dog = other.GetComponent<DogController>();
                if (dog != null)
                {
                    // Optionally notify manager here instead of DogController doing it
                    // DogController already has logic for this, so this can just serve as an effect trigger
                }
            }
        }
    }
}
