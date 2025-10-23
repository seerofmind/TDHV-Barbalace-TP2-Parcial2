using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target;

    [Header("Camera settings")]
    public float sensitivity = 1f;      // mouse sensitivity
    public float distance = 4f;         // distance behind player
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public float height = 2f;           // height offset above player

    [Header("Input")]
    public PlayerInput playerInput;
    private InputAction lookAction;

    private float yaw;
    private float pitch;

    void Awake()
    {
        if (!playerInput)
            playerInput = FindFirstObjectByType<PlayerInput>();

        lookAction = playerInput.actions["Look"];

        if (target)
        {
            // 👇 Sync yaw with target's starting facing direction
            yaw = target.eulerAngles.y;
        }
    }

    void Start()
    {
        if (target != null)
        {
            Vector3 dir = (target.position + Vector3.up * height) - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            yaw = lookRotation.eulerAngles.y;
            pitch = lookRotation.eulerAngles.x;
        }
    }



    void LateUpdate()
    {
        if (!target) return;

        // Read input
        Vector2 lookInput = lookAction.ReadValue<Vector2>() * sensitivity;

        // Flip X axis so it's not inverted
        yaw += lookInput.x;        // <-- keep positive
        pitch -= lookInput.y;      // Y usually feels natural inverted (like FPS games)

        // Clamp vertical look
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Orbit around target
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 position = target.position - rotation * Vector3.forward * distance + Vector3.up * height;

        transform.position = position;
        transform.LookAt(target.position + Vector3.up * height);
    }
}

