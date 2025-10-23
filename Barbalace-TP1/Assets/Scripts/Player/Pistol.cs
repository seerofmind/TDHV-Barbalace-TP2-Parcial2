using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Pistol : MonoBehaviour
{
    [Header("Pistol Settings")]
    public int damage = 25;
    public float range = 500f;
    public float fireRate = 1f;

    [Header("References")]
    public Transform firePoint;         // drag FirePoint here in inspector
    public LayerMask enemyLayer;        // assign your Enemy layer here

    private float nextTimeToFire = 0f;
    private PlayerInput playerInput;
    private InputAction AttackAction;

    void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
            Debug.LogError("No PlayerInput found on player!");

        AttackAction = playerInput.actions["Attack"];
        if (AttackAction == null)
            Debug.LogError("No 'Attack' action found in InputActions!");
    }

    void Update()
    {
        if (AttackAction != null && AttackAction.triggered && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        // Raycast from the middle of the camera (crosshair position)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log("Hit: " + hit.collider.name);
            targetPoint = hit.point;

            var enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
        else
        {
            // If we hit nothing, set a point far away
            targetPoint = ray.GetPoint(range);
            Debug.Log("Shot missed!");
        }

        // Always draw tracer from the firePoint (the gun) to where we aimed
        StartCoroutine(ShowTracer(firePoint.position, targetPoint));
    }






    IEnumerator ShowTracer(Vector3 start, Vector3 end, float duration = 0.3f) // 👈 default 1 second
    {
        LineRenderer line = new GameObject("Tracer").AddComponent<LineRenderer>();
        line.startWidth = 0.5f;
        line.endWidth = 0.5f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = Color.blue;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        yield return new WaitForSeconds(duration); // 👈 tracer lifetime
        Destroy(line.gameObject);
    }

}





