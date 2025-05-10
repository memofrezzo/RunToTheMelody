using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using System.Collections;
using UnityEngine.Audio;

public class GameStarter : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Text startText;
    [SerializeField] private GameObject startTextContainer;

    [Header("Opciones")]
    [SerializeField] private KeyCode startKey = KeyCode.W;
    [SerializeField] private float fadeOutTime = 1.0f;
    [SerializeField] private Color textColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private int textSize = 48;

    // Referencia a componentes
    private ThirdPersonController playerController;
    private AudioSource[] audioSources;

    // Estado
    private bool gameStarted = false;

    private void Awake()
    {
        // Configurar el texto si no fue asignado en el inspector
        if (startText == null)
        {
            InitializeUI();
        }
        else
        {
            // Configurar propiedades del texto existente
            ConfigureText();
        }

        // Obtener referencia al controlador del jugador
        playerController = FindObjectOfType<ThirdPersonController>();

        // Obtener todas las fuentes de audio de la escena para pausarlas
        audioSources = FindObjectsOfType<AudioSource>();
    }

    private void Start()
    {
        // Pausar el juego y la música
        PauseGame();
    }

    private void Update()
    {
        if (!gameStarted && Input.GetKeyDown(startKey))
        {
            StartGame();
        }
    }

    private void InitializeUI()
    {
        // Crear un nuevo GameObject para contener el texto
        startTextContainer = new GameObject("StartTextContainer");
        startTextContainer.transform.SetParent(transform);

        // Configurar Canvas
        Canvas canvas = startTextContainer.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Asegurar que esté por encima de otros elementos UI

        CanvasScaler scaler = startTextContainer.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        startTextContainer.AddComponent<GraphicRaycaster>();

        // Crear GameObject para el texto
        GameObject textObj = new GameObject("StartText");
        textObj.transform.SetParent(startTextContainer.transform);

        // Configurar RectTransform para centrar
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(600, 100);

        // Añadir y configurar el texto
        startText = textObj.AddComponent<Text>();
        ConfigureText();
    }

    private void ConfigureText()
    {
        startText.text = "PRESS 'W' TO START";
        startText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        startText.fontSize = textSize;
        startText.alignment = TextAnchor.MiddleCenter;
        startText.color = textColor;
        startText.fontStyle = FontStyle.Bold;
    }

    private void PauseGame()
    {
        // Pausar el tiempo del juego
        Time.timeScale = 0f;

        // Silenciar todas las fuentes de audio
        foreach (AudioSource source in audioSources)
        {
            source.Pause();
        }

        // Si encontramos el controlador del jugador, desactivamos movimiento
        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }

    private void StartGame()
    {
        gameStarted = true;

        // Iniciar animación de desvanecimiento del texto
        StartCoroutine(FadeOutText());

        // Reanudar tiempo de juego
        Time.timeScale = 1f;

        // Reanudar audio
        foreach (AudioSource source in audioSources)
        {
            source.Play();
        }

        // Activar el movimiento del jugador
        if (playerController != null)
        {
            playerController.enabled = true;

            // Llamar al método StartMovement() si está disponible
            playerController.StartMovement();
        }
    }

    private IEnumerator FadeOutText()
    {
        float elapsedTime = 0f;
        Color initialColor = startText.color;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.unscaledDeltaTime; // Usamos unscaledDeltaTime porque Time.timeScale puede ser 0
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutTime);
            startText.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            yield return null;
        }

        // Desactivar el GameObject del texto cuando termine la animación
        startTextContainer.SetActive(false);
    }
}