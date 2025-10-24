using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI ammoText;

    private PlayerStats playerStats;
    private Pistol pistol;

    void Start()
    {
        // Find the player and pistol in the scene
        playerStats = FindFirstObjectByType<PlayerStats>();
        pistol = FindFirstObjectByType<Pistol>();

        if (playerStats == null)
            Debug.LogWarning("[PlayerUI] PlayerStats not found!");
        if (pistol == null)
            Debug.LogWarning("[PlayerUI] Pistol not found!");
    }

    void Update()
    {
        if (playerStats != null)
            hpText.text = $"HP: {playerStats.CurrentHealth}";

        if (pistol != null)
            ammoText.text = $"Ammo: {pistol.CurrentAmmo} / {pistol.MagazineSize}";
    }


    private int playerStatsHealth()
    {
        // If you want current health exposed directly, you can add a getter in PlayerStats
        var field = typeof(PlayerStats).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)field.GetValue(playerStats);
    }

    private int pistolAmmo()
    {
        var field = typeof(Pistol).GetField("currentAmmo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)field.GetValue(pistol);
    }

    private int pistolMagazine()
    {
        return pistol.magazineSize;
    }
}

