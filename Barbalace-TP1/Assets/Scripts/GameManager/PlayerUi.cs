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
            hpText.text = $"HP: {Mathf.CeilToInt(playerStats.CurrentHealth)} / {Mathf.CeilToInt(playerStats.maxHealth)}";

        if (pistol != null)
            ammoText.text = $"Ammo: {pistol.CurrentAmmo} / {pistol.MagazineSize}";

       
            if (playerStats != null && playerStats.gameObject.activeSelf)
                hpText.text = $"HP: {playerStats.CurrentHealth}";
            else
                hpText.text = "HP: 0";

            if (pistol != null && pistol.gameObject.activeSelf)
                ammoText.text = $"Ammo: {pistol.CurrentAmmo} / {pistol.MagazineSize}";
        

    }
}


