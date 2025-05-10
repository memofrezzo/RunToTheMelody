using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    [Header("Referencias")]
    private ThirdPersonController controller;
    private StarterAssetsInputs inputScript;

    [Header("Estado")]
    public bool isPaused = false;

    // Singleton para acceso global
    public static GameMenu Instance;

    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        controller = FindObjectOfType<ThirdPersonController>();
        inputScript = FindObjectOfType<StarterAssetsInputs>();
    }

    private void Start()
    {
        // Aseguramos que el juego comience sin pausa
        //ResumeGame();
    }

    private void Update()
    {
        // Escuchar entrada para pausa
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Desbloquear cursor para UI
        inputScript.cursorLocked = false;
        inputScript.cursorInputForLook = false;
        Cursor.lockState = CursorLockMode.None;

        // Notificar al controlador
        if (controller != null)
            controller.OnGamePaused();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Bloquear cursor para el juego
        inputScript.cursorLocked = true;
        inputScript.cursorInputForLook = true;
        Cursor.lockState = CursorLockMode.Locked;

        // Notificar al controlador
        if (controller != null)
            controller.OnGameResumed();
    }

    public void RestartLevel()
    {
        ResumeGame(); // Asegurar que el tiempo está corriendo antes de cargar
        SceneManager.LoadScene("Lvl1");
    }

    public void GoToLastCheckpoint()
    {
        if (controller != null)
        {
            controller.LastCheckPoint();
            Debug.Log("Volviendo al último checkpoint");
        }
    }

    // NUEVO: Método para salir del juego
    public void ExitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    private void OnGUI()
    {
        if (isPaused)
        {
            // Definición del estilo del botón
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.yellow;
            buttonStyle.active.textColor = new Color(1f, 0.5f, 0.5f);

            // Estilo para el título
            GUIStyle titleStyle = new GUIStyle(GUI.skin.box);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            Rect menuRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 120, 300, 300);
            GUI.Box(menuRect, "");

            // Título
            GUI.Box(new Rect(menuRect.x, menuRect.y + 10, 300, 30), "JUEGO PAUSADO", titleStyle);

            // Botones
            if (GUI.Button(new Rect(menuRect.x + 50, menuRect.y + 40, 200, 40), "Continuar", buttonStyle))
            {
                if (controller != null && controller.IsDead)
                {
                    RestartLevel();
                }
                else
                {
                    ResumeGame(); // Reanuda el juego
                }
            }

            if (GUI.Button(new Rect(menuRect.x + 50, menuRect.y + 110, 200, 40), "Reiniciar Nivel", buttonStyle))
            {
                RestartLevel();
            }

            if (GUI.Button(new Rect(menuRect.x + 50, menuRect.y + 160, 200, 40), "Último Checkpoint", buttonStyle))
            {
                GoToLastCheckpoint();
            }

            // NUEVO: Botón de salir
            if (GUI.Button(new Rect(menuRect.x + 50, menuRect.y + 210, 200, 40), "Salir", buttonStyle))
            {
                ExitGame();
            }
        }
    }
}
