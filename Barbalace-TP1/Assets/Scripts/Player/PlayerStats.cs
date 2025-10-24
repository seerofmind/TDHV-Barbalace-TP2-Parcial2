using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    public PlayerSO playerData; // 🔹 Reference to the ScriptableObject
    public Transform playerCamera;

    [Header("Runtime Values")]
    [SerializeField] private int health;
    [SerializeField] private float stamina;
    [SerializeField] private float maxStamina;
    [SerializeField] private float recoveryRate;
    [SerializeField] private float sprintDrainRate;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchHeight = 1f;
    private float originalHeight;
    private bool isCrouching = false;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float rotationSpeed;

    [Header("Input")]
    public PlayerInput playerInput;
    private InputAction sprintAction;
    private InputAction moveAction;
    private InputAction crouchAction;

    private CharacterController controller;

    public enum StaminaState { Idle, Draining, Recovering }
    [SerializeField] private StaminaState staminaState = StaminaState.Idle;

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
        crouchAction = playerInput.actions["Crouch"];
        originalHeight = controller.height;


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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
        }
        else
        {
            Debug.LogWarning("[PlayerStats] No PlayerData assigned!");
        }
    }



    void Update()
    {
        HandleStamina();
        HandleMovement();
        HandleCrouch();

    }

    private void HandleStamina()
    {
        float delta = Time.deltaTime;
        float totalDrain = 0f;
        bool isNearEnemy = false;
        bool isSprinting = false;

        // Sprint drain
        if (sprintAction.ReadValue<float>() > 0f && stamina > 0f && canSprint)
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
                canSprint = false; // 👈 lock sprint when drained
            }
            staminaState = StaminaState.Draining;
            UpdateStaminaState(staminaState, stamina);
        }
        else
        {
            // Recover ONLY if not paused, not near enemy, not sprinting
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

    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Direction relative to camera
        Vector3 camForward = playerCamera.forward;
        Vector3 camRight = playerCamera.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * input.x + camForward * input.y;

        float currentSpeed = isCrouching ? crouchSpeed : walkSpeed;


        if (sprintAction.ReadValue<float>() > 0f && stamina > 0f && canSprint)
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
            // Rotate to face movement direction (camera-relative)
            Quaternion targetRotation = Quaternion.LookRotation(move);
            targetRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        move = move.normalized * currentSpeed;
        move.y = verticalVelocity;

        // Move forward in that direction
        controller.Move(move * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (crouchAction.WasPressedThisFrame())
        {
            isCrouching = !isCrouching;

            // Adjust height and camera position
            controller.height = isCrouching ? crouchHeight : originalHeight;

            Vector3 camPos = playerCamera.localPosition;
            camPos.y = isCrouching ? crouchHeight * 0.75f : originalHeight * 0.9f;
            playerCamera.localPosition = camPos;
        }
    }

    private void RotatePlayer(Vector3 move)
    {
        if (move.sqrMagnitude < 0.01f) return;

        move.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(move);
        targetRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }



    // --------------------------- Stamina utilities

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





