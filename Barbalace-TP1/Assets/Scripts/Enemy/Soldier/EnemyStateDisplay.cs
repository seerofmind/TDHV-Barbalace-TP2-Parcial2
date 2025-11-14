using UnityEngine;
using TMPro;
using System.Collections;

public class EnemyStateDisplay : MonoBehaviour
{
    [Header("References")]
    private EnemyAI enemy;
    public TextMeshPro textMesh; 
    public Vector3 offset = new Vector3(0, 2f, 0);

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f; // How fast text fades in/out

    private Camera mainCamera;
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Start()
    {
        enemy = GetComponent<EnemyAI>();
        if (enemy == null)
        {
            Debug.LogError("EnemyStateDisplay requiere un componente EnemyAI en el objeto o en un padre.", this);
            enabled = false; // Desactiva el script si no encuentra el objetivo
            return;
        }
        mainCamera = Camera.main;

        // Ensure there's a CanvasGroup for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Start fully visible
        canvasGroup.alpha = 1f;

        // Subscribe to enemy events (we’ll set these up below)
        enemy.OnEnemyDied += HandleEnemyDied;
        enemy.OnEnemyRespawned += HandleEnemyRespawned;
    }

    void Update()
    {
        if (!enemy || !textMesh || !mainCamera) return;

        // 1. Mantener la posición del texto sobre el enemigo
        Vector3 worldPos = enemy.transform.position + offset;

        // 🎯 MEJORA: Usar WorldToScreenPoint para UI en Screen Space es correcto, 
        // pero si el Canvas es World Space, usa la rotación directa.

        // 2. Hacer que el texto mire a la cámara (requerido)
        // Usamos el vector de la cámara al objeto para determinar la rotación.
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        transform.rotation = targetRotation; // Aplicar rotación inmediata

        // Si tu Canvas es Screen Space - Overlay/Camera, tu código original de posición es correcto:
        // transform.position = mainCamera.WorldToScreenPoint(worldPos); 

        // Si tu Canvas es World Space, usa la posición 3D:
        transform.position = worldPos;


        // 3. Actualizar el contenido (Esto incluye el estado Patrol)
        string state = enemy.CurrentStateName();
        textMesh.text = state;

        // ... (El resto del switch para colores, incluyendo Patrol) ...

        switch (state)
        {
            case "Normal": textMesh.color = Color.white; break;
            case "Patrol": textMesh.color = Color.cyan; break; // 🎯 NUEVO COLOR PARA PATROL
            case "Chase": textMesh.color = Color.red; break;
            case "Damage": textMesh.color = Color.yellow; break;
            case "Dead": textMesh.color = Color.gray; break;
        }
    }

    private void HandleEnemyDied()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeText(0f)); // Fade out
    }

    private void HandleEnemyRespawned()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeText(1f)); // Fade in
    }

    private IEnumerator FadeText(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void OnDestroy()
    {
        if (enemy != null)
        {
            enemy.OnEnemyDied -= HandleEnemyDied;
            enemy.OnEnemyRespawned -= HandleEnemyRespawned;
        }
    }
}


