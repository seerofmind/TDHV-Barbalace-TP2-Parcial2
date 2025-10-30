using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas (e.g., el menú principal)

public class PauseMenu : MonoBehaviour
{
    // Referencia al Panel del Menú de Pausa en el Canvas
    public GameObject pauseMenuUI;

    // Variable estática para rastrear el estado de la pausa
    public static bool GameIsPaused = false;

    void Update()
    {
        // Detecta la pulsación de la tecla 'Escape' (o cualquier otra tecla de pausa)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // ⏯️ Reanudar el Juego
    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Oculta el panel de la UI
        Time.timeScale = 1f;          // Restaura la escala de tiempo normal (movimiento)
        GameIsPaused = false;         // Actualiza el estado
    }

    // ⏸️ Pausar el Juego
    public void Pause()
    {
        pauseMenuUI.SetActive(true);  // Muestra el panel de la UI
        Time.timeScale = 0f;          // Detiene la escala de tiempo (congela el movimiento)
        GameIsPaused = true;          // Actualiza el estado
    }
    
}
