using UnityEngine;

// Requiere un Collider configurado como Trigger
[RequireComponent(typeof(Collider))]
public class MedkitItem : MonoBehaviour
{
    [Header("Item Settings")]
    public int healthToRestore = 50; // Cuánta vida restaura este botiquín

    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificar si el objeto que entró es el jugador
        if (other.CompareTag("Player"))
        {
            // 2. Buscar el script del jugador (PlayerStats)
            PlayerStats playerStats = other.GetComponent<PlayerStats>();

            if (playerStats != null)
            {
                // 3. Llamar al método de curación del jugador
                bool healthRestored = playerStats.Heal(healthToRestore);

                // 4. Si se curó alguna cantidad de vida, destruir el item
                if (healthRestored)
                {
                    // Opcional: Sonido/Efecto de recogida aquí
                    Destroy(gameObject);
                }
            }
        }
    }
}
