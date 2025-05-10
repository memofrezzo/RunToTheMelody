using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Finish : MonoBehaviour
{
    [Header("Referencias de Detección")]
    [SerializeField] private BoxCollider triggerZone;
    [SerializeField] private string playerTag = "Player";

    [Header("Fade & UI")]
    [SerializeField] private float fadeDuration = 0.5f;  // Duración del fade-out (reducida para aparecer más rápido)
    [SerializeField] private CanvasGroup victoryCanvasGroup; // CanvasGroup del Canvas de victoria
    [SerializeField] private Image fadeOverlay; // Imagen que cubrirá la pantalla para fundir a negro

    [Header("Audio")]
    [SerializeField] private AudioSource gameplayMusic;  // Música "Risk"
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip buttonSound;
    [SerializeField] private AudioClip buttonHoverSound;

    [Header("Estadísticas")]
    [SerializeField] private int totalCoinsInLevel = 3;
    [SerializeField] private int currentLevelIndex;

    [Header("UI Personalización")]
    [SerializeField] private float cornerRadius = 10f; // Radio para esquinas redondeadas
    [SerializeField] private Color coinCollectedColor = new Color(1f, 0.8f, 0.2f, 1f); // Amarillo dorado
    [SerializeField] private Color coinMissedColor = new Color(1f, 1f, 1f, 0.3f); // Blanco semitransparente

    // Variables internas
    private bool levelCompleted = false;
    private AudioSource audioSource;
    private GameObject winScreen;
    private int attempts = 1;
    private int collectedCoins = 0;

    // Referencias UI
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI attemptsText;
    private TextMeshProUGUI coinsText;
    private List<Image> coinImages = new List<Image>();
    private Button restartButton;
    private Button menuButton;
    private Button exitButton;

    private void Awake()
    {
        // Configurar collider como trigger
        if (triggerZone == null)
        {
            triggerZone = gameObject.AddComponent<BoxCollider>();
            triggerZone.size = new Vector3(5f, 3f, 0.5f);
            triggerZone.isTrigger = true;
        }
        else
        {
            triggerZone.isTrigger = true;
        }

        // Obtener AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        LoadPlayerData();
    }

    private void Start()
    {
        // Crear la interfaz de victoria y la superposición de fade
        CreateVictoryUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !levelCompleted)
        {
            levelCompleted = true;
            DisablePlayerControls(other.gameObject);
            StartCoroutine(PlayVictorySequence());
            Debug.Log("¡Jugador ha cruzado la meta!");
        }
    }

    private IEnumerator PlayVictorySequence()
    {
        // 1. Fade out la música de gameplay ("Risk") en 1 segundo
        if (gameplayMusic != null)
        {
            float elapsed = 0f;
            float startVol = gameplayMusic.volume;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                gameplayMusic.volume = Mathf.Lerp(startVol, 0f, elapsed / 1f);
                yield return null;
            }
            gameplayMusic.Stop();
        }

        // 2. Reproducir la música de victoria (si se desea)
        if (victoryMusic != null)
        {
            audioSource.clip = victoryMusic;
            audioSource.loop = false;
            audioSource.Play();
        }

        // 3. Configurar el cursor para la interfaz
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Realizar el fade-out de pantalla en fadeOverlay
        if (fadeOverlay != null)
        {
            // Asegurarse de que el overlay NO bloquee clicks
            fadeOverlay.raycastTarget = false;
            float elapsed = 0f;
            Color startColor = fadeOverlay.color; // Asumido transparente al inicio
            Color targetColor = new Color(0f, 0f, 0f, 0.8f); // Negro semitransparente
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.color = Color.Lerp(startColor, targetColor, elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.color = targetColor;
        }

        // 5. Mostrar la pantalla de victoria
        ShowVictoryScreen();

        // 6. Guardar progreso
        SavePlayerData();

        // 7. Iniciar la aparición de monedas (0.5 por segundo)
        StartCoroutine(RevealCoins());
    }

    private void DisablePlayerControls(GameObject player)
    {
        // Desactivar scripts de control que contengan "Control" en su nombre
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour comp in components)
        {
            if (comp != null && comp.GetType().Name.Contains("Control"))
            {
                comp.enabled = false;
            }
        }
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void CreateVictoryUI()
    {
        // Crear el Canvas principal si no existe
        GameObject canvasObject = new GameObject("VictoryCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // Agregar CanvasGroup para control de transparencia
        victoryCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        victoryCanvasGroup.alpha = 0f; // Inicia invisible
        victoryCanvasGroup.interactable = false;
        victoryCanvasGroup.blocksRaycasts = false;

        // Guardar referencia para después
        winScreen = canvasObject;

        // Crear la imagen de fade-out overlay
        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(canvasObject.transform, false);
        fadeOverlay = overlayObj.AddComponent<Image>();

        // Configurar para que cubra toda la pantalla:
        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Color negro con alfa 0 al inicio
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        // Asegurarse de que el overlay NO intercepte clicks
        fadeOverlay.raycastTarget = false;

        // Crear el panel principal con bordes redondeados
        GameObject panel = new GameObject("VictoryPanel");
        panel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Aplicar bordes redondeados al panel
        if (cornerRadius > 0)
        {
            panelImage.sprite = CreateRoundedRectSprite(500, 600, cornerRadius);
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 600f);
        panelRect.anchoredPosition = Vector2.zero;
        // Asegurar que el panel esté por encima del overlay
        panel.transform.SetAsLastSibling();

        // Crear título
        GameObject titleObj = CreateTextObject("TitleText", panel.transform, "¡NIVEL COMPLETADO!", 48);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 220);
        titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.color = new Color(1f, 0.8f, 0.2f); // Dorado

        // Crear texto de intentos
        GameObject attemptsObj = CreateTextObject("AttemptsText", panel.transform, "Intentos: " + attempts, 28);
        RectTransform attemptsRect = attemptsObj.GetComponent<RectTransform>();
        attemptsRect.anchoredPosition = new Vector2(0, 150);
        attemptsText = attemptsObj.GetComponent<TextMeshProUGUI>();

        // Crear texto de monedas
        GameObject coinsTextObj = CreateTextObject("CoinsText", panel.transform, "Monedas: " + collectedCoins + "/" + totalCoinsInLevel, 28);
        RectTransform coinsTextRect = coinsTextObj.GetComponent<RectTransform>();
        coinsTextRect.anchoredPosition = new Vector2(0, 100);
        coinsText = coinsTextObj.GetComponent<TextMeshProUGUI>();

        // Crear contenedor para iconos de monedas
        GameObject coinsContainer = new GameObject("CoinsContainer");
        coinsContainer.transform.SetParent(panel.transform, false);
        RectTransform coinsContainerRect = coinsContainer.AddComponent<RectTransform>();
        coinsContainerRect.anchoredPosition = new Vector2(0, 50);
        coinsContainerRect.sizeDelta = new Vector2(300, 50);

        // Crear imágenes de monedas redondas
        float coinSize = 40f;
        float spacing = 20f;
        float startX = -((totalCoinsInLevel - 1) * (coinSize + spacing)) / 2;

        for (int i = 0; i < totalCoinsInLevel; i++)
        {
            GameObject coinObj = new GameObject("Coin_" + i);
            coinObj.transform.SetParent(coinsContainer.transform, false);
            Image coinImage = coinObj.AddComponent<Image>();

            // Hacer que las monedas sean redondas
            coinImage.sprite = CreateCircleSprite(32);

            // Colorear según si se recolectó o no
            coinImage.color = i < collectedCoins ? coinCollectedColor : coinMissedColor;
            coinImage.preserveAspect = true;

            RectTransform coinRect = coinObj.GetComponent<RectTransform>();
            coinRect.sizeDelta = new Vector2(coinSize, coinSize);
            coinRect.anchoredPosition = new Vector2(startX + i * (coinSize + spacing), 0);

            // Agregar a la lista
            coinImages.Add(coinImage);
        }

        // Crear botones con bordes redondeados
        GameObject restartObj = CreateRoundedButton("RestartButton", panel.transform, "REINTENTAR", new Vector2(0, -100));
        restartButton = restartObj.GetComponent<Button>();
        restartButton.onClick.AddListener(RestartLevel);
        ConfigureButtonFeedback(restartButton);

        GameObject menuObj = CreateRoundedButton("MenuButton", panel.transform, "MENÚ PRINCIPAL", new Vector2(0, -170));
        menuButton = menuObj.GetComponent<Button>();
        menuButton.onClick.AddListener(GoToMainMenu);
        ConfigureButtonFeedback(menuButton);

        GameObject exitObj = CreateRoundedButton("ExitButton", panel.transform, "SALIR", new Vector2(0, -240));
        exitButton = exitObj.GetComponent<Button>();
        exitButton.onClick.AddListener(ExitGame);
        ConfigureButtonFeedback(exitButton);

        // Inicialmente ocultamos la pantalla de victoria pero mantenemos activo el GameObject
        victoryCanvasGroup.alpha = 0;
        victoryCanvasGroup.interactable = false;
        victoryCanvasGroup.blocksRaycasts = false;
    }

    private void ConfigureButtonFeedback(Button button)
    {
        // Obtener la imagen del botón para animaciones
        Image buttonImage = button.GetComponent<Image>();

        // Configurar colores y transparencia para el botón
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);  // 50% de transparencia en estado normal
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1.2f); // 0% de transparencia al pasar el cursor
        colors.pressedColor = new Color(1f, 0.3f, 0.3f, 1f); // Color rojizo al presionar
        colors.selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.fadeDuration = 0.1f; // Transición rápida entre estados
        button.colors = colors;

        // Agregar eventos para efectos de sonido con EventTrigger
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        // Evento de hover (pasar el cursor por encima)
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            if (buttonHoverSound != null)
                audioSource.PlayOneShot(buttonHoverSound);
        });
        trigger.triggers.Add(pointerEnter);
    }

    private Sprite CreateCircleSprite(int resolution)
    {
        // Crear textura circular para monedas
        Texture2D texture = new Texture2D(resolution, resolution);
        float centerX = resolution / 2f;
        float centerY = resolution / 2f;
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = centerX - x;
                float dy = centerY - y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance < radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();

        // Crear sprite a partir de la textura
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateRoundedRectSprite(int width, int height, float radius)
    {
        // Crear textura con rectángulo redondeado para paneles y botones
        Texture2D texture = new Texture2D(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Verificar si estamos en una esquina
                bool inCorner = false;
                float cornerDistance = 0f;

                // Esquina superior izquierda
                if (x < radius && y < radius)
                {
                    cornerDistance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    inCorner = true;
                }
                // Esquina superior derecha
                else if (x > width - radius && y < radius)
                {
                    cornerDistance = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, radius));
                    inCorner = true;
                }
                // Esquina inferior izquierda
                else if (x < radius && y > height - radius)
                {
                    cornerDistance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius));
                    inCorner = true;
                }
                // Esquina inferior derecha
                else if (x > width - radius && y > height - radius)
                {
                    cornerDistance = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, height - radius));
                    inCorner = true;
                }

                if (inCorner && cornerDistance > radius)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();

        // Crear sprite a partir de la textura
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private GameObject CreateTextObject(string name, Transform parent, string text, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(450, 50);
        return textObj;
    }

    private GameObject CreateRoundedButton(string name, Transform parent, string text, Vector2 position)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        // Imagen de fondo del botón con bordes redondeados
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.sprite = CreateRoundedRectSprite(300, 50, cornerRadius);
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        Button button = buttonObj.AddComponent<Button>();
        EventTrigger eventTrigger = buttonObj.AddComponent<EventTrigger>();

        // Crear texto para el botón
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(300, 50);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonObj;
    }

    private void ShowVictoryScreen()
    {
        UpdateUI();
        // Activar la interactividad del CanvasGroup de la UI de victoria
        victoryCanvasGroup.interactable = true;
        victoryCanvasGroup.blocksRaycasts = true;
        StartCoroutine(FadeInCanvas(victoryCanvasGroup, 0.5f));
    }

    private void UpdateUI()
    {
        if (attemptsText != null)
        {
            attemptsText.text = "Intentos: " + attempts;
        }
        if (coinsText != null)
        {
            coinsText.text = "Monedas: " + collectedCoins + "/" + totalCoinsInLevel;
        }

        // Actualizar visibilidad y color de las monedas según las conseguidas
        for (int i = 0; i < coinImages.Count; i++)
        {
            if (i < collectedCoins)
            {
                coinImages[i].color = coinCollectedColor;
            }
            else
            {
                coinImages[i].color = coinMissedColor;
            }
            coinImages[i].gameObject.SetActive(true);
        }
    }

    private IEnumerator FadeInCanvas(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }

    private IEnumerator RevealCoins()
    {
        // Las monedas aparecen 0.5 segundos entre cada una
        for (int i = 0; i < totalCoinsInLevel && i < coinImages.Count; i++)
        {
            // Si la moneda fue recolectada, mostrarla con color completo
            if (i < collectedCoins)
            {
                coinImages[i].color = coinCollectedColor;
                if (coinSound != null)
                {
                    audioSource.PlayOneShot(coinSound);
                }
            }
            // Si no fue recolectada, mostrarla semitransparente
            else
            {
                coinImages[i].color = coinMissedColor;
            }
            coinImages[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void RestartLevel()
    {
        if (buttonSound != null)
        {
            audioSource.PlayOneShot(buttonSound);
        }
        StartCoroutine(DelayedSceneLoad("Lvl1", 0.2f));
    }

    public void GoToMainMenu()
    {
        if (buttonSound != null)
        {
            audioSource.PlayOneShot(buttonSound);
        }
        try
        {
            StartCoroutine(DelayedSceneLoad("Menu Principal", 0.2f));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo cargar la escena del menú principal. Error: " + e.Message);
        }
    }

    public void ExitGame()
    {
        if (buttonSound != null)
        {
            audioSource.PlayOneShot(buttonSound);
        }
        Debug.Log("Saliendo del juego...");
        StartCoroutine(DelayedExit(0.5f));
    }

    private IEnumerator DelayedSceneLoad(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Intentar cargar por nombre de escena
        try
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo cargar la escena '" + sceneName + "'. Error: " + e.Message);
        }
    }

    private IEnumerator DelayedExit(float delay)
    {
        yield return new WaitForSeconds(delay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadPlayerData()
    {
        currentLevelIndex = SceneManager.GetActiveScene().buildIndex;
        attempts = PlayerPrefs.GetInt("Attempts_" + currentLevelIndex, 1);
        collectedCoins = PlayerPrefs.GetInt("Coins_" + currentLevelIndex, 0);
        TryFindCoinManager();
    }

    private void TryFindCoinManager()
    {
        if (collectedCoins == 0)
        {
            System.Random rand = new System.Random();
            collectedCoins = rand.Next(0, totalCoinsInLevel + 1);
        }
        Debug.Log("Monedas cargadas/generadas: " + collectedCoins);
    }

    private void SavePlayerData()
    {
        PlayerPrefs.SetInt("Attempts_" + currentLevelIndex, attempts);
        int previousCoins = PlayerPrefs.GetInt("Coins_" + currentLevelIndex, 0);
        if (collectedCoins > previousCoins)
        {
            PlayerPrefs.SetInt("Coins_" + currentLevelIndex, collectedCoins);
        }
        PlayerPrefs.SetInt("Completed_" + currentLevelIndex, 1);
        PlayerPrefs.Save();
    }

    public void SetCollectedCoins(int coins)
    {
        collectedCoins = Mathf.Min(coins, totalCoinsInLevel);
    }

    public void SetAttempts(int newAttempts)
    {
        attempts = newAttempts;
    }
}
