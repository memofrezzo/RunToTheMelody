using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionController : MonoBehaviour
{
    // Componente Image que cubra toda la pantalla (fondo negro) con alfa inicial 0.
    public Image fadeImage;

    // AudioSource de la m�sica que se va a apagar progresivamente.
    public AudioSource musicSource;

    // Nombre de la siguiente escena a cargar, por defecto "Lvl1".
    public string nextScene = "Lvl1";

    // Duraci�n del efecto de fundido (en segundos).
    public float fadeDuration = 1f;

    // Tiempo de espera antes de iniciar la transici�n (configurable desde el Inspector, por defecto 19 segundos).
    public float transitionDelay = 19f;

    // Variable para evitar llamadas m�ltiples a la transici�n.
    private bool isTransitioning = false;

    void Start()
    {
        // Bloquear y ocultar el cursor.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Asegurarse de que la imagen de fade est� inicialmente transparente.
        if (fadeImage != null)
        {
            Color tempColor = fadeImage.color;
            tempColor.a = 0f;
            fadeImage.color = tempColor;
        }

        // Iniciar la corrutina que espera "transitionDelay" segundos para ejecutar la transici�n.
        StartCoroutine(DelayedTransition());
    }

    void Update()
    {
        // Si el jugador presiona "Enter", se ejecuta la transici�n de forma inmediata.
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TransitionToNextScene();
        }
    }

    // Corrutina que espera la cantidad de segundos definida en "transitionDelay" y luego llama a la transici�n.
    IEnumerator DelayedTransition()
    {
        yield return new WaitForSeconds(transitionDelay);
        TransitionToNextScene();
    }

    // M�todo central que se encarga de asegurar que la transici�n se ejecute una sola vez.
    void TransitionToNextScene()
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(FadeAndLoadScene());
        }
    }

    // Corrutina para ejecutar el fundido a negro y disminuir el volumen de la m�sica progresivamente.
    IEnumerator FadeAndLoadScene()
    {
        float timer = 0f;
        float startVolume = (musicSource != null) ? musicSource.volume : 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);

            // Reducir el volumen de la m�sica de forma lineal.
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            }

            // Incrementar la opacidad de la imagen para lograr el efecto de fundido a negro.
            if (fadeImage != null)
            {
                Color tempColor = fadeImage.color;
                tempColor.a = progress;
                fadeImage.color = tempColor;
            }

            yield return null;
        }

        // Cargar la siguiente escena tras completar la transici�n.
        SceneManager.LoadScene(nextScene);
    }
}
