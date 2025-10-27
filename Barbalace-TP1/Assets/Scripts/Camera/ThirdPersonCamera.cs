using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target;

    [Header("Camera Settings")]
    public float height = 10f;        // how high the camera is above the player
    public float distance = 6f;       // how far behind the player it is
    public float angle = 60f;         // tilt angle downward
    public float followSpeed = 5f;    // smooth follow speed
    public float lookSmooth = 10f;    // how smoothly camera aligns with player forward

    private Vector3 refVelocity;

    void LateUpdate()
    {
        if (!target) return;

        // Calculate desired position (slightly behind and above the player)
        Vector3 offset = Quaternion.Euler(angle, 0f, 0f) * Vector3.back * distance;
        Vector3 desiredPosition = target.position + Vector3.up * height + offset;

        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref refVelocity, 1f / followSpeed);

        // Look at the player smoothly
        Vector3 lookPoint = target.position + Vector3.up * 1.5f; // a bit above the player’s head
        Quaternion targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lookSmooth);
    }
}

