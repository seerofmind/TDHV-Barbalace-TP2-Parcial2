using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // Singleton for easy access
    public static GameManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        CheckSceneReset();
    }

    private void CheckSceneReset()
    {
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }

    // Optional: can handle other global events here
    public void RespawnEnemy(Transform enemy, Vector3 startPos, Quaternion startRot)
    {
        enemy.position = startPos;
        enemy.rotation = startRot;
        enemy.gameObject.SetActive(true);
    }
}
