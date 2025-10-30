using UnityEngine;

public class SurveillanceCamera : MonoBehaviour
{

    public CameraData cameraData;

    [Header("Detection Settings")]
    public LayerMask targetMask; 
    public LayerMask obstructionMask; 

    // Referencia al objetivo (e.g., el jugador)
    private Transform target;

    
    void Start()
    {
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }

    void Update()
    {
        if (target != null)
        {
            CheckForTarget();
        }
    }

    private void CheckForTarget()
    {
        if (target == null)
        {
            return;
        }
       
        Vector3 directionToTarget = target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > cameraData.ViewRange)
        {
            
            return;
        }
        
        Vector3 normalizedDirection = directionToTarget.normalized;
        Vector3 forward = transform.forward;
        float dotProduct = Vector3.Dot(forward, normalizedDirection);
        float halfViewAngle = cameraData.ViewAngle / 2f;
        float cosOfHalfAngle = Mathf.Cos(halfViewAngle * Mathf.Deg2Rad);

        if (dotProduct < cosOfHalfAngle)
        {
            
            return;
        }

       
        if (Physics.Raycast(transform.position, normalizedDirection, out RaycastHit hit, cameraData.ViewRange, obstructionMask))
        {
            Debug.Log("Obstructed by: " + hit.collider.name);
            return;
        }

        
        Debug.Log("Objective Detected!");


    }

    
    private void OnDrawGizmosSelected()
    {
        if (cameraData == null)
        {
            return;
        }
        if (transform == null) return;

        
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(transform.position, cameraData.ViewRange);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        float halfAngleRad = cameraData.ViewAngle / 2f * Mathf.Deg2Rad;

        Vector3 forwardVector = Vector3.forward;
        Vector3 rightRayDirection = Quaternion.Euler(0, halfAngleRad * Mathf.Rad2Deg, 0) * forwardVector;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(Vector3.zero, rightRayDirection * cameraData.ViewRange);
        Vector3 leftRayDirection = Quaternion.Euler(0, -halfAngleRad * Mathf.Deg2Rad, 0) * forwardVector;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(Vector3.zero, leftRayDirection * cameraData.ViewRange);

        
        if (target != null)
        {
            
            Vector3 targetLocalPos = transform.InverseTransformPoint(target.position);

            if (targetLocalPos.magnitude <= cameraData.ViewRange)
            {
                
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(Vector3.zero, targetLocalPos);
            }
        }

       
        Gizmos.matrix = Matrix4x4.identity;
    }
}
