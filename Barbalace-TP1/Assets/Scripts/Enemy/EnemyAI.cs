using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Normal, Chase, Damage, Dead }

    [Header("References")]
    public Transform player;
    private CharacterController controller;
    private PlayerStats playerStats;

    [Header("Enemy Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 3f;

    [Header("Vision Settings")]
    public float viewDistance = 10f;   // Max distance
    [Range(0f, 180f)]
    public float viewAngle = 45f;      // Half-angle of vision cone

    [Header("Proximity Stamina Drain")]
    [SerializeField] private Transform enemy;
    [SerializeField] private float drainDistance = 5f;
    [SerializeField] private float drainRate = 1f;


    [Header("Runtime Info (Debug)")]
    [SerializeField] private EnemyState currentState = EnemyState.Normal;
    [SerializeField] private int currentHealth;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // Damage handling
    private float damageStateTimer = 0f;
    public float damageStateDuration = 0.5f; // stun time

    private bool isDead = false;     // tracks if the enemy is dead
    private bool drainEnabled = true; // controls stamina drain


    public EnemyRespawnManager respawnManager;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (!player)
            player = FindFirstObjectByType<PlayerStats>()?.transform;

        if (player)
            playerStats = player.GetComponent<PlayerStats>();

        Debug.Log("Enemy state: " + currentState);
    }

    void Update()
    {
        if (currentState == EnemyState.Dead || !player) return;

        // Handle damage "stun"
        if (currentState == EnemyState.Damage)
        {
            damageStateTimer -= Time.deltaTime;
            if (damageStateTimer <= 0f)
            {
                if (PlayerInConeOfVision())
                    SetState(EnemyState.Chase);
                else
                    SetState(EnemyState.Normal);
            }
            return;
        }

        if (PlayerInConeOfVision())
        {
            SetState(EnemyState.Chase);
            ChasePlayer();

            // Drain stamina if we have a PlayerStats ref
            if (playerStats != null)
            {
                playerStats.ModifyStamina(-1f * Time.deltaTime); // drain 1 per second
                playerStats.PauseRecovery(true); // stop regen
            }
        }
        else
        {
            SetState(EnemyState.Normal);
            controller.Move(Vector3.zero);

            if (playerStats != null)
                playerStats.PauseRecovery(false); // allow regen again
        }
    }

    private bool PlayerInConeOfVision()
    {
        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > viewDistance) return false;

        toPlayer.y = 0f;
        Vector3 forward = transform.forward;
        float angleToPlayer = Vector3.Angle(forward, toPlayer);

        return angleToPlayer <= viewAngle;
    }

    private void ChasePlayer()
    {
        if (currentState != EnemyState.Chase) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        controller.Move(direction * moveSpeed * Time.deltaTime);

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log(gameObject.name + " took " + amount + " damage. Health: " + currentHealth);

        if (currentHealth > 0)
        {
            SetState(EnemyState.Damage);
            damageStateTimer = damageStateDuration;
            controller.Move(Vector3.zero);
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        SetState(EnemyState.Dead);
        Debug.Log(gameObject.name + " died!");

        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            // Disable draining from this enemy when it dies
            playerStats.PauseRecovery(false);
        }

        // Stop movement and disable the controller


        // Notify the respawn manager (singleton)
        if (EnemyRespawnManager.Instance != null)
            EnemyRespawnManager.Instance.NotifyEnemyDeath(this);

        // Optional: visually hide enemy instead of destroying
        gameObject.SetActive(false);
    }


    /// <summary>
    /// Robust respawn/reset method compatible with EnemyRespawnManager.
    /// Restores transform, health, state and re-enables movement components (NavMeshAgent or CharacterController).
    /// </summary>
    public void ResetEnemy(int healthToSet, Vector3 respawnPosition, Quaternion respawnRotation)
    {
        // Ensure the object is active (manager may call this after re-activating, but safe to do here)
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Stop any damage stun
        damageStateTimer = 0f;

        // Restore transform safely depending on movement component
        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            // Ensure agent enabled, warp to position, and clear path
            agent.enabled = true;
            agent.Warp(respawnPosition);
            agent.ResetPath();
            transform.rotation = respawnRotation;
        }
        else if (controller != null)
        {
            // CharacterController: disable while teleporting to avoid collisions/movement interference
            bool wasEnabled = controller.enabled;
            if (wasEnabled) controller.enabled = false;
            transform.position = respawnPosition;
            transform.rotation = respawnRotation;
            if (wasEnabled) controller.enabled = true;
        }
        else
        {
            // fallback if neither component is present
            transform.position = respawnPosition;
            transform.rotation = respawnRotation;
        }

        // Reset health & state
        currentHealth = healthToSet;
        SetState(EnemyState.Normal);

        // Re-enable any colliders or other components you may have disabled on death
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;



        // Restore player's reference to this enemy (so proximity-stamina works again)
        // Reset the enemy’s own state on respawn
        controller.enabled = true;
        isDead = false;

        // Make sure drain logic is re-enabled (enemy handles its own stamina drain checks now)
        drainEnabled = true; 

        Debug.Log($"{gameObject.name} ResetEnemy: pos={respawnPosition}, health={currentHealth}");

    }



    private void SetState(EnemyState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            Debug.Log("Enemy state: " + currentState);
        }
    }

    // --------------------------- Enemy management
    public void ClearEnemyReference(Transform enemyTransform)
    {
        if (enemy == enemyTransform)
            enemy = null;
    }

    public void SetEnemyReference(Transform enemyTransform)
    {
        enemy = enemyTransform;
    }






    // Optional: Draw cone in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 forward = transform.forward * viewDistance;
        Quaternion leftRot = Quaternion.Euler(0, -viewAngle, 0);
        Quaternion rightRot = Quaternion.Euler(0, viewAngle, 0);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftRot * forward);
        Gizmos.DrawLine(transform.position, transform.position + rightRot * forward);
    }

   

}




