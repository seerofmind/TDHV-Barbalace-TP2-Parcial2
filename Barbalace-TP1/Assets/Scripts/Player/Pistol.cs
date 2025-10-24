using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Pistol : MonoBehaviour
{
    [Header("Pistol Settings")]
    public int damage = 25;
    public float range = 500f;
    public float fireRate = 1f;

    [Header("Ammo Settings")]
    public int magazineSize = 15;   // bullets per magazine
    private int currentAmmo;        // current bullets left
    private bool isRecharging = false;

    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;


    [Header("References")]
    public Transform firePoint;
    public LayerMask enemyLayer;

    private float nextTimeToFire = 0f;
    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction rechargeAction;

    void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
            Debug.LogError("No PlayerInput found on player!");

        attackAction = playerInput.actions["Attack"];
        rechargeAction = playerInput.actions["Recharge"]; // 🔋 renamed action

        if (attackAction == null)
            Debug.LogError("No 'Attack' action found in InputActions!");
        if (rechargeAction == null)
            Debug.LogError("No 'Recharge' action found in InputActions!");

        currentAmmo = magazineSize; // start full
    }

    void Update()
    {
        // 🔫 Handle shooting
        if (!isRecharging && attackAction != null && attackAction.triggered && Time.time >= nextTimeToFire)
        {
            if (currentAmmo > 0)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                Shoot();
            }
            else
            {
                Debug.Log("No ammo! Press R to recharge.");
            }
        }

        // 🔋 Handle recharging
        if (rechargeAction != null && rechargeAction.triggered)
        {
            TryRecharge();
        }
    }

    void Shoot()
    {
        currentAmmo--;
        Debug.Log($"Shot fired! Ammo: {currentAmmo}/{magazineSize}");

        // Raycast from the middle of the camera
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);
            targetPoint = hit.point;

            var enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
                enemy.TakeDamage(damage);
        }
        else
        {
            targetPoint = ray.GetPoint(range);
            Debug.Log("Shot missed!");
        }

        StartCoroutine(ShowTracer(firePoint.position, targetPoint));
    }

    void TryRecharge()
    {
        if (isRecharging)
        {
            Debug.Log("Already recharging...");
            return;
        }

        if (currentAmmo == magazineSize)
        {
            Debug.Log("Magazine already full!");
            return;
        }

        StartCoroutine(Recharge());
    }

    IEnumerator Recharge()
    {
        isRecharging = true;
        Debug.Log("Recharging...");

        // Optional: add recharge animation or sound here
        yield return new WaitForSeconds(1.5f); // recharge delay

        currentAmmo = magazineSize;
        isRecharging = false;
        Debug.Log("Recharged!");
    }

    IEnumerator ShowTracer(Vector3 start, Vector3 end, float duration = 0.3f)
    {
        LineRenderer line = new GameObject("Tracer").AddComponent<LineRenderer>();
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = Color.blue;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        yield return new WaitForSeconds(duration);
        Destroy(line.gameObject);
    }
}





