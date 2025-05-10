using UnityEngine;

public class TriggerDetector : MonoBehaviour
{
    [Tooltip("Tag que identifica al jugador en tu escena")]
    public string playerTag = "Player";

    // Se llama cuando cualquier Collider entra en este trigger
    private void OnTriggerEnter(Collider other)
    {
        // Comprobamos que quien entr� lleve el tag del jugador
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Jugador entr� en el trigger �{gameObject.name}�");

            // Aqu� pon�s la l�gica que necesites, por ejemplo:
            // GameMenu.Instance.PauseGame();
            // controller.LastCheckPoint();
            // etc.
        }
    }

    // Opcional: detectar cu�ndo sale
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Jugador sali� del trigger �{gameObject.name}�");
        }
    }
}
