using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyRespawnManager : MonoBehaviour
{
    [Header("Enemy Reference")]
    public EnemyAI enemyInScene; // assign in inspector

    [Header("Input")]
    public InputActionReference respawnInputReference; // optional, can be null

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private int originalHealth;
    private bool hasSavedOriginalConfig = false;
    public static EnemyRespawnManager Instance { get; private set; }


    void Start()
    {
        if (enemyInScene != null && !hasSavedOriginalConfig)
            SaveEnemyOriginalConfiguration();
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void SaveEnemyOriginalConfiguration()
    {
        if (enemyInScene == null) return;
        originalPosition = enemyInScene.transform.position;
        originalRotation = enemyInScene.transform.rotation;
        originalHealth = enemyInScene.maxHealth;
        hasSavedOriginalConfig = true;
        Debug.Log($"Saved enemy original config: pos={originalPosition}, health={originalHealth}");
    }

    void OnEnable()
    {
        if (respawnInputReference != null && respawnInputReference.action != null)
        {
            respawnInputReference.action.Enable();
            respawnInputReference.action.performed += OnRespawnInput;
        }
    }

    void OnDisable()
    {
        if (respawnInputReference != null && respawnInputReference.action != null)
        {
            respawnInputReference.action.performed -= OnRespawnInput;
            respawnInputReference.action.Disable();
        }
    }

    void OnRespawnInput(InputAction.CallbackContext ctx)
    {
        Debug.Log("Respawn Input action performed");
        RespawnEnemy();
    }

    void Update()
    {
        // Fallback keyboard check — useful for debugging if InputActionReference isn't firing
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
        {
            Debug.Log("Keyboard.f3 detected in manager");
            RespawnEnemy();
        }
    }

    public void RespawnEnemy()
    {
        if (!hasSavedOriginalConfig)
        {
            SaveEnemyOriginalConfiguration();
        }

        if (enemyInScene == null)
        {
            Debug.LogError("Respawn failed: enemyInScene is null.");
            return;
        }

        Debug.Log("RespawnEnemy called — attempting reset...");

        // Ensure GameObject is active before resetting
        if (!enemyInScene.gameObject.activeSelf)
            enemyInScene.gameObject.SetActive(true);

        // Call the robust reset method
        enemyInScene.ResetEnemy(originalHealth, originalPosition, originalRotation);

        Debug.Log($"Enemy respawned to {originalPosition}");
    }

    public void NotifyEnemyDeath(EnemyAI deadEnemy)
    {
        if (deadEnemy == enemyInScene)
        {
            Debug.Log("Enemy has died. Press F3 to respawn.");
        }
    }

}


