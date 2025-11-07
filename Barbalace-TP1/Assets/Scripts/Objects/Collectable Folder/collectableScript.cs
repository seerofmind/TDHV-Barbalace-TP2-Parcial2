using UnityEngine;

public class collectableScript : MonoBehaviour
{
    [Header("Collectable Data")]
    public CollectableSO collectableData; // 🔹 reference your SO

    // Optional runtime getter
    public int Value => collectableData != null ? collectableData.value : 1;

    // Dentro de MagazineItem.cs (o el script que maneja OnTriggerEnter)
    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificar si el objeto que entró es el jugador y tiene el componente PlayerStats
        if (other.CompareTag("Player"))
        {
            PlayerStats playerStats = other.GetComponent<PlayerStats>();

            // 🎯 VERIFICACIÓN 1: ¿Existe PlayerStats?
            if (playerStats == null)
            {
                Debug.LogError("El objeto 'Player' no tiene el componente PlayerStats.");
                return;
            }

            // 🎯 VERIFICACIÓN 2: ¿Tiene PlayerStats asignada la Pistola?
            if (playerStats.playerPistol == null)
            {
                Debug.LogError("PlayerStats tiene una referencia 'playerPistol' en NULL. ¡Asígnala en el Inspector del jugador!");
                return;
            }

            // 🎯 VERIFICACIÓN 3 (Si usas un ScriptableObject): ¿El ítem tiene datos válidos?
            // Asumiendo que 'collectableData' es la fuente de 'value'
            /*
            if (collectableData == null) 
            {
                Debug.LogError("El item no tiene asignado un CollectableData Scriptable Object.");
                return;
            }
            */

            // Si todas las verificaciones pasan, la línea es segura:
            // Asumiendo que tu variable para el valor es 'magazinesToGive' como en la respuesta anterior:
            int value = collectableData.value; // O collectableData.value si usas Scriptable Objects

            bool magazinesAdded = playerStats.playerPistol.AddReserveMagazines(value);

            if (magazinesAdded)
            {
                Destroy(gameObject);
            }
        }
    }

}

