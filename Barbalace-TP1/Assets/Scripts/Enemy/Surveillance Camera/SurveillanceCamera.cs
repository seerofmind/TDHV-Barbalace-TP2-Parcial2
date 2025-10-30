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
        
        Vector3 directionToTarget = target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > cameraData.ViewRange)
        {
            
            return;
        }

        // **2. Verificación de Ángulo (Ángulo de Visión)**
        // Vector de dirección normalizado desde la cámara al objetivo.
        Vector3 normalizedDirection = directionToTarget.normalized;

        // Vector que representa la dirección de "mirada" de la cámara.
        Vector3 forward = transform.forward;

        // El Dot Product entre dos vectores normalizados es igual al coseno del ángulo entre ellos.
        // Dot(A, B) = |A| * |B| * cos(theta). Si |A|=|B|=1, Dot(A, B) = cos(theta).
        float dotProduct = Vector3.Dot(forward, normalizedDirection);

        // El ángulo máximo permitido se debe convertir de grados a coseno.
        // Nota: El ángulo de la cámara (60°) es el ángulo de apertura TOTAL. 
        // Necesitamos la mitad del ángulo (30°) para el cálculo del producto escalar.
        float halfViewAngle = cameraData.ViewAngle / 2f;
        float cosOfHalfAngle = Mathf.Cos(halfViewAngle * Mathf.Deg2Rad);

        if (dotProduct < cosOfHalfAngle)
        {
            // El objetivo está fuera del cono de visión angular
            return;
        }

        // **3. Verificación de Obstrucción (Raycast)**
        // Se asegura de que no haya paredes u otros objetos entre la cámara y el objetivo.
        if (Physics.Raycast(transform.position, normalizedDirection, out RaycastHit hit, cameraData.ViewRange, obstructionMask))
        {
            // Si el Raycast impacta algo antes de llegar al objetivo, significa que hay una obstrucción.
            // Para ser más precisos, si el hit.collider NO es el objetivo, está obstruido.
            // Una implementación simple es chequear si impactó en la capa de obstrucción.
            Debug.Log("Obstruido por: " + hit.collider.name);
            return;
        }

        // Si pasa las 3 pruebas: ¡El objetivo ha sido detectado!
        Debug.Log("¡Objetivo Detectado!");


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
