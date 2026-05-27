using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Match
{
    public class TrapFoodInstance : NetworkBehaviour
    {
        private Coroutine _eatingRoutine;
        
        public void StartEating(DogController dog)
        {
            if (_eatingRoutine == null)
            {
                _eatingRoutine = StartCoroutine(EatRoutine(dog));
            }
        }

        private IEnumerator EatRoutine(DogController dog)
        {
            // Dog takes 3% damage per second for 5s
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(1f);
                if (dog != null && dog.HP.Value > 0)
                {
                    dog.TakeDamageServerRpc(dog.maxHP * 0.03f); // Positive value deals damage
                }
            }

            TrapEffectServerRpc(dog.GetComponent<NetworkObject>().NetworkObjectId);

            if (IsServer) NetworkObject.Despawn();
        }

        [Rpc(SendTo.Server)]
        private void TrapEffectServerRpc(ulong dogNetId)
        {
            TrapEffectClientRpc(dogNetId);
        }

        [Rpc(SendTo.Everyone)]
        private void TrapEffectClientRpc(ulong dogNetId)
        {
            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (localId == MatchManager.Instance.RobberClientId.Value)
            {
                MatchUIManager.Instance?.ShowAnnouncementRpc("A dog ate the Trap Food!");
                
                RobberController robber = FindAnyObjectByType<RobberController>();
                if (robber != null && robber.IsOwner) robber.ApplySpeedUp(1, 5f);
            }
        }
    }
}
