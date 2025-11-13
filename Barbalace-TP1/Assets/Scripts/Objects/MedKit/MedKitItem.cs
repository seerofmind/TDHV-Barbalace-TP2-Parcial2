using UnityEngine;

// Requiere un Collider configurado como Trigger
[RequireComponent(typeof(Collider))]
public class MedkitItem : MonoBehaviour
{
    [Header("References")]
    public MedKitSO medkitData; 

    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificar si el objeto que entró es el jugador
        if (other.CompareTag("Player"))
        {
            // 2. Buscar el script del jugador (PlayerStats)
            PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();

            if (playerStats != null)
            {
                // 3. Llamar al método de curación del jugador
                bool healthRestored = playerStats.Heal(medkitData.healthToRestore);

                // 4. Si se curó alguna cantidad de vida, destruir el item
                if (healthRestored)
                {
                    
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("Vida ya al máximo, no se recoge el Medkit.");
                }
            }
        }
    }
}
