using UnityEngine;
using UnityEngine.AI;
using System.Collections;




[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Normal, Patrol, Alert, Chase, Damage, Dead }

    public event System.Action OnEnemyDied;
    public event System.Action OnEnemyRespawned;
    public event System.Action OnPlayerDied;

    [Header("References")]
    public Transform player;
    public Soldier enemyData;
    private CharacterController controller;
    private PlayerStats playerStats;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints; // 🎯 NUEVO: Puntos de Patrulla
    public float patrolPointTolerance = 1.5f; // Distancia para considerar "llegado"
    private int currentPatrolIndex = 0; // Índice del punto de patrulla actual

  
    [Header("Alert Settings")]
    public float alertTimeLimit = 3f; // Tiempo límite para pasar a alerta (3 segundos)
    private Coroutine alertTimerCoroutine; // Referencia para controlar el temporizador
    
    
    private NavMeshAgent agent;
    private EnemyWeapon weapon;

    public bool IsDead => currentState == EnemyState.Dead;
    private float gravity = -9.81f;
    private float verticalVelocity;


  
    private float lastAttackTime;

    
    private float moveSpeed;
    private float normalSpeed = 2f;
  
    private float currentSpeed;

   
    
    private float drainRate;

    private bool wasShotByPlayer = false;
    

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

        currentHealth = enemyData.maxHealth;
        moveSpeed = enemyData.moveSpeed;
        drainRate = enemyData.drainRate;

       

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

        if (alertManager.Instance != null)
        {
            alertManager.Instance.OnGlobalAlertTriggered += ForceAlert;
        }

        
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            SetState(EnemyState.Patrol); // ¡Establece el estado inicial!
            currentPatrolIndex = 0;
        }
    }

    void Update()
    {
        if (currentState == EnemyState.Dead || !player) return;

        HandleDamageState();

        bool canSeePlayer = PlayerInConeOfVision();

       
        if (canSeePlayer || wasShotByPlayer)
        {
            // 💥 Si cualquier Soldier detecta al jugador, activa la alerta global
            if (alertManager.Instance != null && !alertManager.Instance.IsGlobalAlert)
            {
                alertManager.Instance.TriggerGlobalAlert();
            }
            ForceChase();
        }

       

        switch (currentState)
        {
            case EnemyState.Normal:
                // No hace nada
                break;

            case EnemyState.Patrol:
                // Si la Alerta Global se activa MIENTRAS patrulla, debe interrumpir
                if (alertManager.Instance != null && alertManager.Instance.IsGlobalAlert)
                {
                    ForceChase();
                    break;
                }
                Patrol(); // Ejecución normal de la Patrulla
                break;

            case EnemyState.Alert:
                // Pasa a Chase si hay alerta global (como ya está definido en ForceAlert)
                ForceChase();
                break;

            case EnemyState.Chase:
                
                if (!canSeePlayer && (alertManager.Instance == null || !alertManager.Instance.IsGlobalAlert))
                {
                    if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        SetState(EnemyState.Patrol);
                    }
                    else
                    {
                        SetState(EnemyState.Normal);
                    }
                    break;
                }
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
        if (toPlayer.magnitude > enemyData.viewDistance) return false;

        Vector3 dir = toPlayer.normalized;
        dir.y = 0f;

        if (Vector3.Angle(transform.forward, dir) > enemyData.viewAngle) return false;

        Ray ray = new Ray(transform.position + Vector3.up * 1.5f, (player.position - (transform.position + Vector3.up * 1.5f)).normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, enemyData.viewDistance))
        {
            return hit.transform == player;
        }

        return false;
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 || controller == null || !controller.enabled)
        {
            SetState(EnemyState.Normal);
            return;
        }

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        Vector3 direction = targetPoint.position - transform.position;

        // Calcula la dirección y la distancia solo en el plano horizontal (XZ).
        Vector3 directionFlat = direction;
        directionFlat.y = 0f;
        float distanceFlat = directionFlat.magnitude;

        // Rotación (se mantiene igual)
        if (directionFlat.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionFlat);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // -------------------------------------------------------------
        // 🎯 LÓGICA DE MOVIMIENTO Y GRAVEDAD 🎯
        // -------------------------------------------------------------

        // Mueve en la dirección XZ
        Vector3 moveVector = directionFlat.normalized * enemyData.moveSpeed;

        // ✅ CORRECCIÓN: Fuerza de empuje constante hacia abajo (simulación de gravedad).
        // Esto asegura que CharacterController.Move() siempre vea un cambio de Y, 
        // lo cual evita que se "atasque" si está en el suelo.
        float gravityPush = -9.81f; // Usa una fuerza negativa constante.

        // Si el controlador está en el suelo, aplicamos un pequeño empuje; 
        // si no, aplicamos la gravedad completa (si ya la manejas en otra variable, úsala).
        if (controller.isGrounded)
        {
            // Empuje mínimo para asegurar el movimiento en la superficie.
            moveVector.y = -2f;
        }
        else
        {
            // Si no está en el suelo, usa la gravedad normal (o tu variable verticalVelocity).
            // moveVector.y = verticalVelocity; // Si tienes una variable de velocidad vertical
            moveVector.y = gravityPush; // Si solo quieres aplicar la fuerza.
        }

        // 🎯 DEBUG CLAVE: Muestra el vector que se está intentando mover
        /*Debug.Log($"Movimiento Patrulla: {moveVector * Time.deltaTime}. Distancia: {distanceFlat}");*/

        controller.Move(moveVector * Time.deltaTime);

        // -------------------------------------------------------------

        // Verificación de llegada
        if (distanceFlat < patrolPointTolerance)
        {
            GoToNextPatrolPoint();
        }
        /*Debug.Log($"Patrullando hacia {targetPoint.name}. Posición actual: {transform.position}");*/
        // Si esto se imprime, sabes que el script Patrol está siendo llamado, 
        // y el problema es la llamada a controller.Move().
    }
    private void GoToNextPatrolPoint()
    {
        // Avanzar al siguiente punto de forma cíclica
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        Debug.Log($"{gameObject.name} va al punto {currentPatrolIndex}");
    }

    public void ForceAlert()
    {
        if (currentState == EnemyState.Dead) return;

        // Primero cambiamos a Alert (para visualización/sonido)
        SetState(EnemyState.Alert);

        // Luego pasamos a Chase (o lo manejamos en Update, como arriba)
        ForceChase();
        // Nota: El 'AlertManager.IsGlobalAlert' mantendrá el estado en Chase
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
        else if (distance <= enemyData.attackRange)
        {
            TryAttackPlayer();
        }
    }

    private void TryAttackPlayer()
    {
        if (Time.time < lastAttackTime + enemyData.attackCooldown || playerStats == null) return;

        playerStats.TakeDamage(enemyData.attackDamage);
        lastAttackTime = Time.time;
        Debug.Log($"{gameObject.name} attacked player for {enemyData.attackDamage} damage!");
    }

    private void StartAlertTimer()
    {
        // 1. Si ya existe un temporizador (fue disparado de nuevo antes de que terminara el anterior), lo detenemos.
        if (alertTimerCoroutine != null)
        {
            StopCoroutine(alertTimerCoroutine);
        }

        // 2. Iniciamos el nuevo temporizador.
        alertTimerCoroutine = StartCoroutine(AlertCountdown());
    }

    private IEnumerator AlertCountdown()
    {
        // 1. Espera el tiempo límite
        yield return new WaitForSeconds(alertTimeLimit);

        // 2. Después del tiempo, si el enemigo no está muerto y la alerta no está activa...
        if (currentState != EnemyState.Dead && !alertManager.Instance.IsGlobalAlert)
        {
            // ... activa la alerta global.
            if (alertManager.Instance != null)
            {
                alertManager.Instance.TriggerGlobalAlert();
                // Esto forzará a este enemigo y a todos los demás al estado Chase.
            }
        }

        // 3. Limpiamos la referencia
        alertTimerCoroutine = null;
    }
    public void TakeDamage(int amount)
    {
        Debug.Log(amount);
        if (currentState == EnemyState.Dead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            // El enemigo murió, cancelamos cualquier temporizador
            if (alertTimerCoroutine != null)
            {
                StopCoroutine(alertTimerCoroutine);
            }
            Die();
        }
        else
        {
            // 1. Pasa al estado de Daño temporalmente
            SetState(EnemyState.Damage);

            // 2. Si el enemigo sobrevive, inicia (o reinicia) el contador de alerta
            StartAlertTimer();

            Debug.Log($"Enemigo herido. Temporizador de alerta iniciado/reiniciado.");
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
        
        //Debug.Log($"{gameObject.name} is now chasing the player!");
    }

    public string CurrentStateName()
    {
        return currentState.ToString();
    }

}





