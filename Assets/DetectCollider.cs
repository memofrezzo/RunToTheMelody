using UnityEngine;

public class TriggerDetector : MonoBehaviour
{
    [Tooltip("Tag que identifica al jugador en tu escena")]
    public string playerTag = "Player";

    // Se llama cuando cualquier Collider entra en este trigger
    private void OnTriggerEnter(Collider other)
    {
        // Comprobamos que quien entró lleve el tag del jugador
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Jugador entró en el trigger «{gameObject.name}»");

            // Aquí ponés la lógica que necesites, por ejemplo:
            // GameMenu.Instance.PauseGame();
            // controller.LastCheckPoint();
            // etc.
        }
    }

    // Opcional: detectar cuándo sale
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Jugador salió del trigger «{gameObject.name}»");
        }
    }
}
