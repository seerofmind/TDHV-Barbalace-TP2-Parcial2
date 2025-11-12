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
    [SerializeField] private int reserveMagazines = 2; // Cargadores de reserva (2 al inicio)
    public const int MAX_MAGAZINES = 2; // Máximo de cargadores, para referencia al reaparecer
    public int CurrentAmmo => currentAmmo;
    public int MagazineSize => magazineSize;
    public int ReserveMagazines => reserveMagazines;

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
        ResetAmmoOnStart();
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
            else if (reserveMagazines > 0) // Si no hay balas, pero sí cargadores
            {
                Debug.Log("Magazine empty. Need to recharge.");
            }
            else // 🎯 SIN BALAS Y SIN CARGADORES
            {
                Debug.Log("No magazines left.");
            }
        }

        // 🔋 Handle recharging
        if (rechargeAction != null && rechargeAction.triggered)
        {
            TryRecharge();
        }
    }
    // ... (El método Shoot() sigue igual)

    public void ResetAmmoOnStart()
    {
        currentAmmo = magazineSize;
        reserveMagazines = MAX_MAGAZINES;
        Debug.Log($"Munición inicializada: {reserveMagazines} cargadores de reserva y {currentAmmo} balas en el cargador.");
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

            var enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // 🧠 Enemy takes damage
                enemy.TakeDamage(damage);

                // 🏃‍♂️ Force it to chase the player if not dead
                if (!enemy.IsDead)
                {
                    enemy.ForceChase();
                }

            }
            var surveillanceCamera = hit.collider.GetComponent<SurveillanceCamera>();
            if (surveillanceCamera != null)
            {
                surveillanceCamera.TakeDamage(damage);
            }
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

        // 🎯 VERIFICACIÓN CLAVE: Solo permite recargar si hay cargadores de reserva
        if (reserveMagazines <= 0)
        {
            Debug.Log("No reserve magazines left.");
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

        // 🎯 CONSUMIR UN CARGADOR DE RESERVA
        reserveMagazines--;

        // Rellenar el cargador actual
        currentAmmo = magazineSize;

        isRecharging = false;
        Debug.Log($"Recharged! {reserveMagazines} magazines left.");
    }


    public bool AddReserveMagazines(int amount)
    {
        if (reserveMagazines >= MAX_MAGAZINES)
        {
            Debug.Log("Cargadores llenos. No se puede recoger más munición.");
            return false;
        }

        // Calcular el nuevo total, limitándolo al máximo
        int oldMagazines = reserveMagazines;
        reserveMagazines = Mathf.Min(reserveMagazines + amount, MAX_MAGAZINES);
        int addedAmount = reserveMagazines - oldMagazines;

        if (addedAmount > 0)
        {
            Debug.Log($"Recogido: +{addedAmount} cargadores. Total: {reserveMagazines}/{MAX_MAGAZINES}.");
            return true;
        }
        return false;
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





