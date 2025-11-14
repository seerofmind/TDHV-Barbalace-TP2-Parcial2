using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Scriptable Objects/EnemySO")]
public class Soldier : ScriptableObject
{
    [Header("Base Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 3f;
    public float chaseSpeed = 3.5f;

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 45f;

    [Header("Proximity Stamina Drain")]
    public float drainDistance = 5f;
    public float drainRate = 1f;

    [Header("Combat Settings")]
    public int attackDamage = 20;          // fallback damage if weapon fails
    public float attackRange = 2f;         // fallback range if weapon fails
    public float attackCooldown = 1.5f;


}
