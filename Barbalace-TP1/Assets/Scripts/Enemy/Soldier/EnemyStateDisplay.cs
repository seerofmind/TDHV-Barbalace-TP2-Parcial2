using UnityEngine;
using TMPro;
using System.Collections;

public class EnemyStateDisplay : MonoBehaviour
{
    [Header("References")]
    public EnemyAI enemy;
    public TextMeshPro textMesh;
    public Vector3 offset = new Vector3(0, 2f, 0);

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (enemy == null)
        {
            Debug.LogError("La referencia 'EnemyAI' no está asignada en el Inspector para " + gameObject.name, this);
            enabled = false;
            return;
        }

        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
            if (textMesh == null)
            {
                Debug.LogError("TextMeshPro no está asignado en este objeto.", this);
                enabled = false;
                return;
            }
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        enemy.OnEnemyDied += HandleEnemyDied;
        enemy.OnEnemyRespawned += HandleEnemyRespawned;
    }

    void Update()
    {
        if (enemy == null || textMesh == null) return;

        Vector3 worldPos = enemy.transform.position + offset;
        transform.position = worldPos;

        string stateName = enemy.CurrentStateName();
        textMesh.text = stateName;

        switch (stateName)
        {
            case nameof(EnemyAI.EnemyState.Idle):
                textMesh.color = Color.white;
                break;
            case nameof(EnemyAI.EnemyState.Patrol):
                textMesh.color = Color.cyan;
                break;
            case nameof(EnemyAI.EnemyState.Alert):
                textMesh.color = new Color(1f, 0.64f, 0f);
                break;
            case nameof(EnemyAI.EnemyState.Chase):
                textMesh.color = Color.red;
                break;
            case nameof(EnemyAI.EnemyState.Damage):
                textMesh.color = Color.magenta;
                break;
            case nameof(EnemyAI.EnemyState.Dead):
                textMesh.color = Color.gray;
                break;
        }
    }

    private void HandleEnemyDied()
    {
        textMesh.text = nameof(EnemyAI.EnemyState.Dead);
        textMesh.color = Color.gray;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeText(0f));
    }

    private void HandleEnemyRespawned()
    {
        if (enemy != null)
        {
            textMesh.text = enemy.CurrentStateName();
        }

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeText(1f));
    }

    private IEnumerator FadeText(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
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