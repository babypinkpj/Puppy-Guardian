using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;

    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log(gameObject.name + " HP : " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " Died");

        // ตาย
        gameObject.SetActive(false);
    }
}