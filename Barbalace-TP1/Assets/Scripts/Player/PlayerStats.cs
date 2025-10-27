using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public Soldier playerData;
    public Transform playerCamera;

    [Header("Runtime Values")]
     private int health;
     private float stamina;
     private float maxStamina;
     private float recoveryRate;
    private float sprintDrainRate;
    public int CurrentHealth => health;

    private float walkSpeed;
     private float sprintSpeed;
     private float rotationSpeed;

    [Header("Crouch Settings")]
     private float crouchHeight;
    private float crouchSpeed;

    private float originalHeight;
    private bool isCrouching = false;


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

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!playerInput)
            playerInput = FindFirstObjectByType<PlayerInput>();

        sprintAction = playerInput.actions["Sprint"];
        moveAction = playerInput.actions["Move"];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        originalHeight = controller.height; // store the normal height

        // 🔹 Load values from ScriptableObject
        if (playerData != null)
        {
            health = playerData.health;
            stamina = playerData.maxStamina;
            maxStamina = playerData.maxStamina;
            recoveryRate = playerData.recoveryRate;
            sprintDrainRate = playerData.sprintDrainRate;

            walkSpeed = playerData.walkSpeed;
            sprintSpeed = playerData.sprintSpeed;
            rotationSpeed = playerData.rotationSpeed;
            crouchHeight = playerData.crouchHeight;
            crouchSpeed = playerData.crouchSpeed;

        }
        else
        {
            Debug.LogWarning("[PlayerStats] No PlayerData assigned!");
        }
    }

    void Update()
    {
        HandleCrouchInput();
        HandleStamina();
        HandleMovement();
    }

    // -------------------------------- Crouch --------------------------------
    private void HandleCrouchInput()
    {
        bool crouchPressed = Keyboard.current.cKey.wasPressedThisFrame ||
                             Keyboard.current.leftCtrlKey.wasPressedThisFrame;

        if (crouchPressed)
        {
            isCrouching = !isCrouching; // toggle crouch

            if (isCrouching)
            {
                controller.height = crouchHeight;
            }
            else
            {
                controller.height = originalHeight;
            }
        }
    }

    // -------------------------------- Stamina --------------------------------
    private void HandleStamina()
    {
        float delta = Time.deltaTime;
        float totalDrain = 0f;
        bool isNearEnemy = false;
        bool isSprinting = false;

        // Sprint drain
        if (sprintAction.ReadValue<float>() > 0f && stamina > 0f && canSprint && !isCrouching)
        {
            totalDrain += sprintDrainRate;
            isSprinting = true;
        }

        // Apply drain
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
            // Recover if not paused, not near enemy, not sprinting
            if (!regenPaused && !isNearEnemy && !isSprinting && stamina < maxStamina)
            {
                stamina += recoveryRate * delta;
                if (stamina > maxStamina) stamina = maxStamina;

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
    }

    private void UpdateStaminaState(StaminaState newState, float value)
    {
        if (newState != lastStaminaState)
        {
            Debug.Log($"[Stamina] State: {newState}, Value: {Mathf.RoundToInt(value)}");
            lastStaminaState = newState;
        }
    }

    // -------------------------------- Movement --------------------------------
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

        float currentSpeed = walkSpeed;

        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (sprintAction.ReadValue<float>() > 0f && stamina > 0f && canSprint)
            currentSpeed = sprintSpeed;

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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        move = move.normalized * currentSpeed;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    // --------------------------- Stamina utilities ---------------------------
    public void UseStamina(float amount)
    {
        stamina -= amount;
        stamina = Mathf.Max(stamina, 0);
        Debug.Log("Stamina used: " + Mathf.RoundToInt(stamina));
    }

    public void ModifyStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina + amount, 0f, maxStamina);
    }

    public void PauseRecovery(bool pause)
    {
        regenPaused = pause;
    }
}






