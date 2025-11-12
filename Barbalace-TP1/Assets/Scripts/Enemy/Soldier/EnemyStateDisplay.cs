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
        if (!enemy || !textMesh) return;

        // Keep the text above the enemy
        Vector3 worldPos = enemy.transform.position + offset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        transform.position = screenPos;

        // Update text content and color
        string state = enemy.CurrentStateName();
        textMesh.text = state;

        switch (state)
        {
            case "Normal": textMesh.color = Color.white; break;
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


