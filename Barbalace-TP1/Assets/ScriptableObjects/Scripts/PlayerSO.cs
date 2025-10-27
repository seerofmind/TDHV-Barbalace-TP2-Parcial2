using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSO", menuName = "Scriptable Objects/PlayerSO")]
public class Soldier : ScriptableObject
{
    [Header("Health Settings")]
    public int health = 100;

    [Header("Stamina Settings")]
    public float maxStamina = 10f;
    public float recoveryRate = 1f;
    public float sprintDrainRate = 2f;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Crouch Settings")]
    public float crouchSpeed = 2f;
    public float crouchHeight = 1f;
}
