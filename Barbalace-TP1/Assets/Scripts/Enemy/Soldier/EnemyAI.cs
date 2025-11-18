using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;
// using UnityEditor; // Comentado si no estás usando la librería de UnityEditor

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Patrol, Alert, Chase, Damage, Dead }
    public event System.Action OnEnemyDied;
    public event System.Action OnEnemyRespawned;

    [Header("References")]
    public Transform player;
    public Soldier enemyData;
    private NavMeshAgent agent;
    private EnemyWeapon weapon;
    private PlayerStats playerStats;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolPointTolerance = 1.5f;
    private int currentPatrolIndex = 0;

    [Header("Alert & Chase Settings")]
    public float alertDelay = 0.4f;
    // 🎯 NUEVO: Tiempo límite para pasar a Alerta si sobrevive al daño (3.0s)
    public float timeToAlertAfterDamage = 3.0f;
    public float chaseLostDelay = 3f;
    public float alertJumpHeight = 0.4f;
    public float alertJumpDuration = 0.25f;
    private Coroutine stateTimerCoroutine;
    // 🎯 NUEVO: Coroutine específico para el temporizador de alerta por daño
    private Coroutine damageAlertTimerCoroutine;
    private float timeSincePlayerLost = 0f;

    [Header("Runtime Info")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;
    private int currentHealth;
    private EnemyState _previousState;
    public float damageStateDuration = 0.5f;
    private bool isDead = false;

    private bool _playerInVision = false;
    private RaycastHit _visionHit;
    private float lastAttackTime;
    private bool wasShotByPlayer = false;

    // -----------------------------------------------------------------
    //  LIFECYCLE
    // -----------------------------------------------------------------

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
            agent.speed = enemyData.moveSpeed;
            agent.stoppingDistance = enemyData.attackRange * 0.9f;
        }
        if (!player) player = FindAnyObjectByType<PlayerStats>()?.transform;
        if (player) playerStats = player.GetComponent<PlayerStats>();
        weapon = GetComponentInChildren<EnemyWeapon>();
        agent.enabled = true;
    }

    void Start()
    {
        if (alertManager.Instance != null)
        {
            alertManager.Instance.OnGlobalAlertTriggered += ReactToGlobalAlert;
        }
        SetState((patrolPoints != null && patrolPoints.Length > 0) ? EnemyState.Patrol : EnemyState.Idle);
    }

    void Update()
    {
        if (isDead || player == null || playerStats == null || playerStats.CurrentHealth <= 0)
        {
            if (agent.enabled && agent.hasPath) agent.isStopped = true;
            return;
        }

        bool canSeePlayer = PlayerInConeOfVision();

        // 🛑 NO necesitamos esta línea si el enemigo persigue indefinidamente.
        // timeSincePlayerLost = canSeePlayer ? 0f : timeSincePlayerLost + Time.deltaTime; 

        if (currentState != EnemyState.Damage && currentState != EnemyState.Alert)
        {
            if ((currentState == EnemyState.Idle || currentState == EnemyState.Patrol))
            {
                if (wasShotByPlayer || (alertManager.Instance != null && alertManager.Instance.IsGlobalAlert))
                {
                    SetState(EnemyState.Alert, triggerJump: false);
                }
                else if (canSeePlayer)
                {
                    SetState(EnemyState.Alert, triggerJump: true);
                }
            }
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                if (agent.enabled) agent.isStopped = true;
                break;
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Alert:
                LookAtPlayerFlat();
                break;
            case EnemyState.Chase:
               

                if (currentState == EnemyState.Chase) ChasePlayer();
                break;
        }
    }

    // -----------------------------------------------------------------
    //  STATE LOGIC
    // -----------------------------------------------------------------

    private void LookAtPlayerFlat()
    {
        if (player == null) return;
        Vector3 direction = (player.position - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 5f * Time.deltaTime);
    }

    private bool PlayerInConeOfVision()
    {
        _playerInVision = false;
        if (!player || enemyData == null) return false;

        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
        Vector3 toPlayer = player.position - eyePosition;
        float distance = toPlayer.magnitude;

        if (distance > enemyData.viewDistance || Vector3.Angle(transform.forward, toPlayer.normalized) > enemyData.viewAngle / 2f) return false;

        Ray ray = new Ray(eyePosition, toPlayer.normalized);
        if (Physics.Raycast(ray, out _visionHit, enemyData.viewDistance))
        {
            _playerInVision = (_visionHit.transform == player);
            return _visionHit.transform == player;
        }
        return false;
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) { SetState(EnemyState.Idle); return; }

        if (!agent.enabled) agent.enabled = true;
        agent.isStopped = false;

        Transform targetPoint = patrolPoints[currentPatrolIndex];

        if (agent.destination != targetPoint.position)
            agent.SetDestination(targetPoint.position);

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void ChasePlayer()
    {
        if (!agent.enabled) return;
        agent.isStopped = false;

        agent.SetDestination(player.position);
        playerStats?.ModifyStamina(-enemyData.drainRate * Time.deltaTime);
        playerStats?.PauseRecovery(true);

        float attackRange = weapon != null ? weapon.attackRange : enemyData.attackRange;

        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            if (!agent.isStopped) agent.isStopped = true;
            if (weapon != null) weapon.TryAttack(playerStats);
            else TryAttackPlayer();
        }
        else
        {
            if (agent.isStopped) agent.isStopped = false;
        }
    }

    private void TryAttackPlayer()
    {
        if (Time.time < lastAttackTime + enemyData.attackCooldown || playerStats == null || playerStats.CurrentHealth <= 0) return;
        playerStats.TakeDamage(enemyData.attackDamage);
        lastAttackTime = Time.time;
    }

    // -----------------------------------------------------------------
    //  STATES
    // -----------------------------------------------------------------

    public void TakeDamage(int amount, bool shotByPlayer = true)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        wasShotByPlayer = shotByPlayer;

        if (currentHealth <= 0)
        {
            // Si muere, detener cualquier temporizador de alerta pendiente
            if (damageAlertTimerCoroutine != null) StopCoroutine(damageAlertTimerCoroutine);
            Die();
        }
        else
        {
            // 🎯 IMPLEMENTACIÓN DEL REQUERIMIENTO:
            // Si el daño fue causado por el jugador, activa la alerta global inmediatamente.
            if (shotByPlayer && alertManager.Instance != null)
            {
                // Pasa 'this' (esta instancia del enemigo) como el desencadenante de la alerta.
                alertManager.Instance.TriggerGlobalAlert(this);
            }

            SetState(EnemyState.Damage);

            // Inicia el temporizador de alerta (los 3 segundos) solo si no murió
            if (damageAlertTimerCoroutine != null) StopCoroutine(damageAlertTimerCoroutine);
            damageAlertTimerCoroutine = StartCoroutine(DamageAlertTimerCoroutine());
        }
    }

    private void Die()
    {
        SetState(EnemyState.Dead);
        isDead = true;
        playerStats?.PauseRecovery(false);
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
        OnEnemyDied?.Invoke();
        gameObject.SetActive(false);
    }

    private void ReactToGlobalAlert(EnemyAI triggeringEnemy)
    {
        if (this != triggeringEnemy) ForceAlert(true);
    }

    public void ForceAlert(bool triggerJump = true)
    {
        if (isDead || currentState == EnemyState.Damage) return;
        SetState(EnemyState.Alert, triggerJump);
    }

    private void SetState(EnemyState newState, bool triggerJump = false)
    {
        if (currentState == EnemyState.Damage && newState == EnemyState.Damage)
        {
            if (stateTimerCoroutine != null) StopCoroutine(stateTimerCoroutine);
            stateTimerCoroutine = StartCoroutine(DamageTimerCoroutine());
            return;
        }

        if (currentState == newState) return;

        Debug.Log($"Enemy State: {currentState} → **{newState}**", this);

        if (stateTimerCoroutine != null) StopCoroutine(stateTimerCoroutine);
        if (currentState == EnemyState.Chase) playerStats?.PauseRecovery(false);
        if (newState == EnemyState.Damage) _previousState = currentState;

        currentState = newState;

        switch (newState)
        {
            case EnemyState.Idle:
                if (agent.enabled) agent.isStopped = true;
                wasShotByPlayer = false;
                break;
            case EnemyState.Patrol:
                wasShotByPlayer = false;
                break;
            case EnemyState.Alert:
                // La lógica de alerta global se activa aquí si detecta al jugador.
                if (alertManager.Instance != null && _playerInVision && !wasShotByPlayer)
                {
                    alertManager.Instance.TriggerGlobalAlert(this);
                }

                if (agent.enabled && agent.isOnNavMesh && triggerJump)
                {
                    StartCoroutine(JumpCoroutine());
                }
                else if (agent.enabled)
                {
                    agent.isStopped = true;
                }

                stateTimerCoroutine = StartCoroutine(AlertDelayCoroutine());
                break;
            case EnemyState.Chase:
                wasShotByPlayer = true;
                break;
            case EnemyState.Damage:
                if (agent.enabled) agent.isStopped = true;
                stateTimerCoroutine = StartCoroutine(DamageTimerCoroutine());
                break;
            case EnemyState.Dead:
                isDead = true;
                if (agent.enabled) agent.isStopped = true;
                break;
        }
    }

    // -----------------------------------------------------------------
    //  TIMERS
    // -----------------------------------------------------------------

    private IEnumerator JumpCoroutine()
    {
        float timer = 0f;
        float halfDuration = alertJumpDuration / 2f;
        float initialOffset = agent.baseOffset;

        if (agent.enabled) agent.isStopped = true;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            agent.baseOffset = Mathf.Lerp(initialOffset, initialOffset + alertJumpHeight, t);
            yield return null;
        }

        timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            agent.baseOffset = Mathf.Lerp(initialOffset + alertJumpHeight, initialOffset, t);
            yield return null;
        }

        agent.baseOffset = initialOffset;

        if (agent.enabled && currentState == EnemyState.Alert) agent.isStopped = false;
    }

    private IEnumerator AlertDelayCoroutine()
    {
        yield return new WaitForSeconds(alertDelay);

        if (alertManager.Instance != null && alertManager.Instance.IsGlobalAlert)
        {
            SetState(EnemyState.Chase);
        }
        else if (PlayerInConeOfVision() || wasShotByPlayer)
        {
            SetState(EnemyState.Chase);
        }
        else
        {
            SetState(
                (patrolPoints != null && patrolPoints.Length > 0)
                ? EnemyState.Patrol
                : EnemyState.Idle
            );
        }
        stateTimerCoroutine = null;
    }

    private IEnumerator DamageTimerCoroutine()
    {
        // Duración del estado de 'stagger' (Daño)
        yield return new WaitForSeconds(damageStateDuration);

        // Al salir del estado de Daño, vuelve al estado anterior, o pasa a Alerta/Chase
        if (currentState == EnemyState.Damage)
        {
            SetState(_previousState == EnemyState.Chase ? EnemyState.Chase : EnemyState.Alert);
        }
        stateTimerCoroutine = null;
    }

    // 🎯 NUEVO: TEMPORIZADOR DE ALERTA TRAS RECIBIR DAÑO (3 segundos)
    private IEnumerator DamageAlertTimerCoroutine()
    {
        // Espera los segundos definidos
        yield return new WaitForSeconds(timeToAlertAfterDamage);

        // Si sobrevivió el tiempo y sigue vivo
        if (currentHealth > 0)
        {
            Debug.Log($"El enemigo sobrevivió al disparo por más de {timeToAlertAfterDamage}s. Pasando a Alerta.", this);

            // Forzamos el estado de Alerta (sin el salto de exclamación, ya que esto fue solo un tiempo de espera)
            ForceAlert(triggerJump: false);
        }

        damageAlertTimerCoroutine = null;
    }

    // -----------------------------------------------------------------
    //  RESETEO & GIZMOS
    // -----------------------------------------------------------------

    public void ResetEnemy(int healthToSet, Vector3 respawnPosition, Quaternion respawnRotation)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (agent != null)
        {
            agent.enabled = false;
            transform.SetPositionAndRotation(respawnPosition, respawnRotation);
            agent.enabled = true;
            agent.isStopped = true;
            agent.ResetPath();
        }
        else
        {
            transform.SetPositionAndRotation(respawnPosition, respawnRotation);
        }

        currentHealth = healthToSet;
        wasShotByPlayer = false;
        isDead = false;

        SetState((patrolPoints != null && patrolPoints.Length > 0) ? EnemyState.Patrol : EnemyState.Idle);
        OnEnemyRespawned?.Invoke();
    }

    public string CurrentStateName()
    {
        return currentState.ToString();
    }

    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (enemyData == null) return;
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eyePosition, enemyData.viewDistance);

        // UnityEditor.Handles.color = Color.blue; // Requiere 'using UnityEditor'
        // Vector3 forward = transform.forward;
        // float halfAngle = enemyData.viewAngle / 2f;
        // Vector3 leftRayDirection = Quaternion.Euler(0, -halfAngle, 0) * forward;
        // UnityEditor.Handles.DrawWireArc(eyePosition, Vector3.up, leftRayDirection, enemyData.viewAngle, enemyData.viewDistance);

        if (player != null && _playerInVision)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(eyePosition, player.position);
        }
        else if (player != null && Vector3.Distance(eyePosition, player.position) <= enemyData.viewDistance)
        {
            if (_visionHit.collider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(eyePosition, _visionHit.point);
                Gizmos.DrawLine(_visionHit.point, player.position);
                Gizmos.DrawSphere(_visionHit.point, 0.1f);
            }
        }
#endif
    }
}