using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealScript : MonoBehaviour
{
    public Transform randomspwn;
    public int heal;

    private void Start()
    {
        transform.position = randomspwn.position;
    }
    public void despawn()
    {
        Debug.Log("Reach time");
        Destroy(gameObject);
    }
   private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player")) // สมมติหมามี tag Player
    {
        PlayerMovement dog = other.GetComponent<PlayerMovement>();
        GameManager screen = Object.FindFirstObjectByType<GameManager>();
        AudioGameplay sfxheal = Object.FindFirstObjectByType<AudioGameplay>();
        if (dog != null && screen != null)
        {
            dog.healPlayer(heal);
                screen.Screen();
            Debug.Log("Healing");
            sfxheal.SFXSource.PlayOneShot(sfxheal.sfxHeal);
            Destroy(gameObject); // ทำลาย Food เอง
        }
    }
}


}
