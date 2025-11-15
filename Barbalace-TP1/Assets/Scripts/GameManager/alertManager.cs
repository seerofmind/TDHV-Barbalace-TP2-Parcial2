using UnityEngine;
using System;
using System.Collections;

public class alertManager : MonoBehaviour
{
    public static alertManager Instance { get; private set; }

    public event Action<EnemyAI> OnGlobalAlertTriggered;

    public bool IsGlobalAlert = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void TriggerGlobalAlert(EnemyAI triggeringEnemy)
    {
        if (IsGlobalAlert) return;

        IsGlobalAlert = true;
        Debug.Log("🚨 ALERTA GLOBAL ACTIVADA por: " + (triggeringEnemy != null ? triggeringEnemy.name : "Sistema Externo (Cámara)"));

        OnGlobalAlertTriggered?.Invoke(triggeringEnemy);

        StartCoroutine(EndAlertAfterTime(30f));
    }

    public void TriggerGlobalAlert()
    {
        TriggerGlobalAlert(null);
    }

    public event Action OnGlobalAlertDisabled;

    private IEnumerator EndAlertAfterTime(float duration)
    {
        yield return new WaitForSeconds(duration);

        IsGlobalAlert = false;
        Debug.Log("Alerta Global Desactivada. Los enemigos vuelven a la Patrulla.");
        OnGlobalAlertDisabled?.Invoke();
    }
}