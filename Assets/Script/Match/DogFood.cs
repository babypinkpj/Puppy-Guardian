using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Match
{
    public class DogFood : NetworkBehaviour
    {
        private Coroutine _eatingRoutine;
        private DogController _eatingDog;

        public void StartEating(DogController dog)
        {
            if (_eatingRoutine == null)
            {
                _eatingDog = dog;
                _eatingRoutine = StartCoroutine(EatRoutine(dog));
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsServer || _eatingRoutine != null) return;
            
            DogController dog = other.GetComponent<DogController>();
            if (dog != null && dog.GetComponent<CharacterController>().velocity.magnitude <= 0.1f)
            {
                StartEating(dog);
            }
        }

        private IEnumerator EatRoutine(DogController dog)
        {
            // Heal 10% every 1s for 7s
            for (int i = 0; i < 7; i++)
            {
                yield return new WaitForSeconds(1f);
                if (dog != null && dog.HP.Value > 0)
                {
                    // Negative damage to heal
                    dog.TakeDamageServerRpc(-dog.maxHP * 0.1f);
                }
                else
                {
                    break;
                }
            }
            if (IsServer) NetworkObject.Despawn();
        }

        // Interrupt eating if dog takes damage
        private void Update()
        {
            if (_eatingDog != null && _eatingRoutine != null)
            {
                // Simple interrupt check: if dog moves or is stunned, interrupt. 
                // Actual implementation might use an event from TakeDamage.
                if (_eatingDog.GetComponent<CharacterController>().velocity.magnitude > 0.1f)
                {
                    StopCoroutine(_eatingRoutine);
                    _eatingRoutine = null;
                    _eatingDog = null;
                    if (IsServer) NetworkObject.Despawn(); // Food disappears
                }
            }
        }
    }
}
