using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public PlayerData playerData;
    public Transform playerCamera;

    [Header("Respawn Settings")]
    public Transform initialPosition; // Optional spawn point in Inspector

    [Header("Runtime Values")]
    private int health;
    private float stamina;
    private float originalHeight;
    private bool isCrouching = false;
    private bool isDead = false;
    public int CurrentHealth => health;
    public float CurrentStamina => stamina;

    [Header("Input")]
    public PlayerInput playerInput;
    private InputAction sprintAction;
    private InputAction moveAction;

    private CharacterController controller;

    public enum StaminaState { Idle, Draining, Recovering }
    private StaminaState staminaState = StaminaState.Idle;
    private StaminaState lastStaminaState = StaminaState.Idle;
    private bool regenPaused = false;
    private bool canSprint = true;

    private float gravity = -9.81f;
    private float verticalVelocity;

    // 🔹 Respawn data
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!playerInput)
            playerInput = FindFirstObjectByType<PlayerInput>();

        sprintAction = playerInput.actions["Sprint"];
        moveAction = playerInput.actions["Move"];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        originalHeight = controller.height;

        health = playerData.maxHealth;
        stamina = playerData.maxStamina;

  

        // Store spawn position/rotation
        if (initialPosition != null)
        {
            spawnPosition = initialPosition.position;
            spawnRotation = initialPosition.rotation;
        }
        else
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }
    }

    void Update()
    {
        // Always listen for F1/F2
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            Respawn();
        }
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Skip gameplay logic if dead
        if (health <= 0 || controller == null || !controller.enabled)
            return;

        // Normal gameplay logic
        HandleCrouchInput();
        //HandleStamina();
        HandleMovement();

        // Death check
        if (health <= 0)
        {
            Die();
        }
    }



    // ------------------------------- Crouch -------------------------------
    private void HandleCrouchInput()
    {
        bool crouchPressed = Keyboard.current.cKey.wasPressedThisFrame ||
                             Keyboard.current.leftCtrlKey.wasPressedThisFrame;

        if (crouchPressed)
        {
            isCrouching = !isCrouching;
            controller.height = isCrouching ? playerData.crouchHeight : originalHeight;
        }
    }

    // ------------------------------- Stamina -------------------------------
    /*private void HandleStamina()
    {
        float delta = Time.deltaTime;
        float totalDrain = 0f;
        bool isNearEnemy = false;
        bool isSprinting = false;

        if (sprintAction.ReadValue<float>() > 0f && stamina > 0f && canSprint && !isCrouching)
        {
            totalDrain += playerData.sprintDrainRate;
            isSprinting = true;
        }

        if (totalDrain > 0f)
        {
            stamina -= totalDrain * delta;
            if (stamina <= 0f)
            {
                stamina = 0f;
                canSprint = false;
            }
            staminaState = StaminaState.Draining;
            UpdateStaminaState(staminaState, stamina);
        }
        else
        {
            if (!regenPaused && !isNearEnemy && !isSprinting && stamina < playerData.maxStamina)
            {
                stamina += playerData.recoveryRate * delta;
                if (stamina > playerData.maxStamina) stamina = playerData.maxStamina;
                if (stamina > 1f) canSprint = true;

                staminaState = StaminaState.Recovering;
                UpdateStaminaState(staminaState, stamina);
            }
            else
            {
                staminaState = StaminaState.Idle;
                UpdateStaminaState(staminaState, stamina);
            }
        }
    }*/

    private void UpdateStaminaState(StaminaState newState, float value)
    {
        if (newState != lastStaminaState)
        {
            // Optional debug
            // Debug.Log($"[Stamina] State: {newState}, Value: {Mathf.RoundToInt(value)}");
            lastStaminaState = newState;
        }
    }

    public void ModifyStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina + amount, 0f, playerData.maxStamina);
        if (stamina > 1f) canSprint = true;
    }

    public void PauseRecovery(bool pause)
    {
        regenPaused = pause;
    }

    // ------------------------------- Movement -------------------------------
    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 camForward = playerCamera.forward;
        Vector3 camRight = playerCamera.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * input.x + camForward * input.y;

        float currentSpeed = playerData.walkSpeed;

        if (isCrouching)
            currentSpeed = playerData.crouchSpeed;
        else if (sprintAction.ReadValue<float>() > 0f && stamina > 0f && canSprint)
            currentSpeed = playerData.sprintSpeed;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            targetRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, playerData.rotationSpeed * Time.deltaTime);
        }

        move = move.normalized * currentSpeed;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    // ------------------------------- Damage & Respawn -------------------------------
    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0 && gameObject.activeSelf)
        {
            health = 0;
            Die();
        }
        else
        {
            Debug.Log($"Player took {amount} damage. Remaining HP: {health}");
        }
    }

    public int GetMaxHealth()
    { 
        // Retorno el maximo de vida. El metodo retorna un "int" 
        return playerData.maxHealth;
    
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player died!");

        // Disable movement
        if (controller != null)
            controller.enabled = false;

        // Optional: visually hide player (like mesh or renderer)
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
    }

    private void Respawn()
    {
        Debug.Log("Respawning player...");

        // Enable movement
        if (controller != null)
            controller.enabled = true;

        // Reset visuals
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = true;

        // Reset position & rotation
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // Reset health & stamina
        health = playerData.maxHealth;

        stamina = playerData.maxStamina;

        isDead = false;
    }
}









