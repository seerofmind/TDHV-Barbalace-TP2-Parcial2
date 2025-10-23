using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("References")]
    public Transform playerCamera; // used for camera-relative movement


    [Header("Sprint Settings")]
    public float sprintMultiplier = 2f; // multiplies base speed when sprinting
    private InputAction sprintAction;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
   

    private float xRotation = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    void Update()
    {
        HandleMovement();
        
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        // Movement input
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Use camera’s forward/right but ignore vertical tilt
        Vector3 camForward = playerCamera.forward;
        Vector3 camRight = playerCamera.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * input.x + camForward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Jump
        if (jumpAction.triggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        
    }
}


