using UnityEngine;
using System;
using System.Collections;

public class alertManager : MonoBehaviour
{
    public static alertManager Instance { get; private set; }

    // Evento que otros scripts (EnemyAI) escucharán.
    public event Action OnGlobalAlertTriggered;

    // Estado actual de alerta.
    public bool IsGlobalAlert = false;

    void Awake()
    {
        // Implementación del Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Opcional: DontDestroyOnLoad(gameObject); si necesitas persistencia
        }
    }

    /// <summary>
    /// Activa la alerta global, haciendo que todos los enemigos persigan al jugador.
    /// </summary>
    public void TriggerGlobalAlert()
    {
        if (IsGlobalAlert) return; // Si ya estamos en alerta, salimos.

        IsGlobalAlert = true;
        Debug.Log("🚨 ALERTA GLOBAL ACTIVADA: Todos los enemigos en estado Chase.");

        // Dispara el evento para todos los suscriptores (EnemyAI)
        StartCoroutine(EndAlertAfterTime(30f));
        OnGlobalAlertTriggered?.Invoke();
    }

    public event Action OnGlobalAlertDisabled; // Nuevo evento para notificar

    private IEnumerator EndAlertAfterTime(float duration)
    {
        yield return new WaitForSeconds(duration);

        IsGlobalAlert = false;
        Debug.Log("Alerta Global Desactivada. Los enemigos vuelven a la Patrulla.");
        OnGlobalAlertDisabled?.Invoke();
    }
}