using UnityEngine;

[CreateAssetMenu(fileName = "EnemySO", menuName = "Scriptable Objects/EnemySO")]
public class Soldier : ScriptableObject
{
    [Header("Base Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 3f;

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 45f;

    [Header("Proximity Stamina Drain")]
    public float drainDistance = 5f;
    public float drainRate = 1f;
}
