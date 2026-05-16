using UnityEngine;
using Unity.Netcode;

public class AttackHitbox : NetworkBehaviour
{
    public int damage = 10;
    public LayerMask enemyLayer;

    private bool isActive = false;
    private ulong ownerId;
    private Collider hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.enabled = false; // ❌ start disabled
    }
    private void OnTriggerEnter(Collider other)
    {

        if (!IsServer) return;
        if (!isActive) return;

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            Debug.Log("HIT ENEMY!");
            enemy.TakeDamage(damage);
        }
    }
    public void Activate(ulong ownerClientId, int dmg)
    {
        ownerId = ownerClientId;
        damage = dmg;
        isActive = true;

        hitboxCollider.enabled = true;
    }
    public void Deactivate()
    {
        isActive = false;

        hitboxCollider.enabled = false;
    }
}