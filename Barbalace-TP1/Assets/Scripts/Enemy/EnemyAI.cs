using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Normal, Chase, Damage, Dead }

    public event System.Action OnEnemyDied;
    public event System.Action OnEnemyRespawned;
    public event System.Action OnPlayerDied;

    [Header("References")]
    public Transform player;
    public Soldier enemyData;
    private CharacterController controller;
    private PlayerStats playerStats;

    private NavMeshAgent agent;
    private EnemyWeapon weapon;

    public bool IsDead => currentState == EnemyState.Dead;
    private float gravity = -9.81f;
    private float verticalVelocity;


    [Header("Combat Settings")]
    public int attackDamage = 20;          // fallback damage if weapon fails
    public float attackRange = 2f;         // fallback range if weapon fails
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Movement & Vision")]
    private float moveSpeed;
    private float normalSpeed = 2f;
    private float chaseSpeed = 3.5f;
    private float currentSpeed;

    private float viewDistance;
    private float viewAngle;
    private float drainRate;

    private bool wasShotByPlayer = false;
    private float chaseTimer = 0f;
    public float chaseDurationAfterShot = 5f;

    [Header("Runtime Info")]
    [SerializeField] private EnemyState currentState = EnemyState.Normal;
    private int currentHealth;

    private float damageStateTimer = 0f;
    public float damageStateDuration = 0.5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isDead = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
            moveSpeed = enemyData.moveSpeed;
            viewDistance = enemyData.viewDistance;
            viewAngle = enemyData.viewAngle;
            drainRate = enemyData.drainRate;
        }

        if (!player)
            player = FindFirstObjectByType<PlayerStats>()?.transform;

        if (player)
            playerStats = player.GetComponent<PlayerStats>();
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentSpeed = normalSpeed;

        weapon = GetComponentInChildren<EnemyWeapon>();
        if (weapon == null)
            Debug.LogWarning($"{gameObject.name} has no EnemyWeapon assigned!");
    }

    void Update()
    {
        if (currentState == EnemyState.Dead || !player) return;

        HandleDamageState();

        bool canSeePlayer = PlayerInConeOfVision();

        // Start chasing if player seen or recently shot
        if (canSeePlayer || wasShotByPlayer)
        {
            ForceChase();
        }

        // Movement & attack logic
        switch (currentState)
        {
            case EnemyState.Normal:
                Patrol();
                break;

            case EnemyState.Chase:
                ChasePlayer();
                break;
        }


    }

    private void HandleDamageState()
    {
        if (currentState != EnemyState.Damage) return;

        damageStateTimer -= Time.deltaTime;
        if (damageStateTimer <= 0f)
        {
            SetState(PlayerInConeOfVision() ? EnemyState.Chase : EnemyState.Normal);
        }
    }

    private bool PlayerInConeOfVision()
    {
        if (!player) return false;

        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > viewDistance) return false;

        Vector3 dir = toPlayer.normalized;
        dir.y = 0f;

        if (Vector3.Angle(transform.forward, dir) > viewAngle) return false;

        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, (player.position - (transform.position + Vector3.up * 1.5f)).normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, viewDistance))
        {
            return hit.transform == player;
        }

        return false;
    }

    void Patrol()
    {
        // Optional patrol logic
    }

    private void ChasePlayer()
    {
        if (player == null) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        if (playerStats == null || playerStats.CurrentHealth <= 0)
            return; // Player is dead, do nothing

        // Movement
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        controller.Move(direction * moveSpeed * Time.deltaTime);

        // Rotation
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Drain stamina
        playerStats?.ModifyStamina(-drainRate * Time.deltaTime);
        playerStats?.PauseRecovery(true);

        // Attack if in range
        float distance = Vector3.Distance(transform.position, player.position);
        if (weapon != null && distance <= weapon.attackRange)
        {
            weapon.TryAttack(playerStats);
        }
        else if (distance <= attackRange)
        {
            TryAttackPlayer();
        }
    }

    private void TryAttackPlayer()
    {
        if (Time.time < lastAttackTime + attackCooldown || playerStats == null) return;

        playerStats.TakeDamage(attackDamage);
        lastAttackTime = Time.time;
        Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage!");
    }

    public void TakeDamage(int amount)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}");

        if (currentHealth > 0)
        {
            SetState(EnemyState.Chase);
            damageStateTimer = damageStateDuration;
            controller.Move(Vector3.zero);
            wasShotByPlayer = true;
            chaseTimer = chaseDurationAfterShot;
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        SetState(EnemyState.Dead);
        Debug.Log($"{gameObject.name} died!");
        playerStats?.PauseRecovery(false);

        OnEnemyDied?.Invoke();
        gameObject.SetActive(false);

        if (agent != null) agent.isStopped = true;

        Debug.Log("Player died!");
        gameObject.SetActive(false);
        if (controller != null) controller.enabled = false;

        OnPlayerDied?.Invoke(); // Notify enemies or other systems
    }

    public void ResetEnemy(int healthToSet, Vector3 respawnPosition, Quaternion respawnRotation)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        damageStateTimer = 0f;

        if (controller != null)
        {
            controller.enabled = false;
            transform.position = respawnPosition;
            transform.rotation = respawnRotation;
            controller.enabled = true;
        }

        currentHealth = healthToSet;
        SetState(EnemyState.Normal);
        isDead = false;
       

        OnEnemyRespawned?.Invoke();
    }

    private void SetState(EnemyState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            Debug.Log($"Enemy state: {currentState}");
        }
    }

    public void ForceChase()
    {
        if (currentState == EnemyState.Dead) return;

        SetState(EnemyState.Chase);
        wasShotByPlayer = true;
        chaseTimer = chaseDurationAfterShot;
        //Debug.Log($"{gameObject.name} is now chasing the player!");
    }

    public string CurrentStateName()
    {
        return currentState.ToString();
    }

}





