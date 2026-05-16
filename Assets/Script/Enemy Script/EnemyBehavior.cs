using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : NetworkBehaviour
{
    public NetworkVariable<int> currentHP = new NetworkVariable<int>(100);
    public int maxHP = 100;
    [SerializeField] private Image healthBarFill;
    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHP.Value -= damage;

        if (currentHP.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died");

        // Destroy on server → sync to all clients
        NetworkObject.Despawn();
    }

    private void OnEnable()
    {
        currentHP.OnValueChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        currentHP.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        UpdateHealthUI();
    }
    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = (float)currentHP.Value / maxHP;
        }
    }
}