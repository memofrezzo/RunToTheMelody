using UnityEngine;
using System.Collections;
using StarterAssets;

public class HitPauseTimerBehaviour : StateMachineBehaviour
{
    [Tooltip("Tiempo en segundos después de comenzar la animación de HitLegs para pausar el juego.")]
    [SerializeField] private float pauseDelay = 2.8f;

    [Tooltip("Tiempo en segundos antes de desactivar la velocidad de movimiento hacia adelante.")]
    [SerializeField] private float movementDisableDelay = 0.15f;

    private float originalMoveSpeed;

    // Se ejecuta al entrar en el estado HitLegs
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ThirdPersonController controller = animator.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            // Guardamos la velocidad original para poder restaurarla cuando sea necesario
            originalMoveSpeed = controller.MoveSpeed;

            // Iniciamos corrutina para desactivar solo el movimiento hacia adelante
            animator.GetComponent<MonoBehaviour>().StartCoroutine(DisableForwardMovementAfterDelay(controller));

            // Iniciamos corrutina para pausar el juego después
            animator.GetComponent<MonoBehaviour>().StartCoroutine(PauseGameAfterDelay());
        }
    }

    private IEnumerator DisableForwardMovementAfterDelay(ThirdPersonController controller)
    {
        yield return new WaitForSeconds(movementDisableDelay);

        // Solo modificamos la velocidad de movimiento a 0, 
        // sin llamar a StopMovement() que podría tener efectos adicionales
        controller.MoveSpeed = 0f;
        Debug.Log("Movimiento hacia adelante desactivado");
    }

    private IEnumerator PauseGameAfterDelay()
    {
        // Esperamos el tiempo especificado para pausar
        yield return new WaitForSeconds(pauseDelay);

        if (GameMenu.Instance != null)
        {
            GameMenu.Instance.PauseGame();
            Debug.Log("Juego pausado por HitPauseTimerBehaviour");
        }
    }

    // Cuando salimos del estado, aseguramos que la velocidad se restaure
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ThirdPersonController controller = animator.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            // Restauramos la velocidad original
            controller.MoveSpeed = originalMoveSpeed;
            Debug.Log("Velocidad restaurada a " + originalMoveSpeed);
        }
    }
}