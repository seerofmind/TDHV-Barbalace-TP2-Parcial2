using UnityEngine;

public class collectableScript : MonoBehaviour
{
    [Header("Collectable Data")]
    public CollectableSO collectableData; // 🔹 reference your SO

    // Optional runtime getter
    public int Value => collectableData != null ? collectableData.value : 1;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Something collided: {other.name}");

        if (other.GetComponentInParent<CharacterController>())

        {
            int coinValue = Value;
            Debug.Log($"Collected coin with value: {coinValue}");
            Destroy(gameObject);
        }
    }

    // On the PlayerStats script
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var collectable = hit.collider.GetComponent<collectableScript>();
        if (collectable != null)
        {
            Debug.Log($"Collected coin with value: {collectable.Value}");
            Destroy(collectable.gameObject);
        }
    }

}

