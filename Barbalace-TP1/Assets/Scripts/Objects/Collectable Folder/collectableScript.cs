using UnityEngine;

public class collectableScript : MonoBehaviour
{
    public int value = 1; // valor de la moneda (puede variar si querés monedas especiales)

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that touched us has a CharacterController (the player)
        if (other.GetComponent<CharacterController>())
        {
            // You can also increment a score or coins here
            // Example:
            // GameManager.instance.AddCoin(1);

            // Destroy collectible
            Destroy(gameObject);
        }
    }
}
