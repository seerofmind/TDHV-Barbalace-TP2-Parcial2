using UnityEngine;

// EnemyWeapon.cs
public class EnemyWeapon : MonoBehaviour
{
    public int damage = 10;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    private float nextAttackTime;

    public void TryAttack(PlayerStats player)
    {
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            player.TakeDamage(damage);
            Debug.Log($"Enemy attacked player for {damage} damage!");
        }
    }
}

