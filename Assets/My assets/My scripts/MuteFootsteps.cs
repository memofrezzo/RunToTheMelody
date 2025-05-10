using StarterAssets;
using System.Collections;
using UnityEngine;

namespace SmallHedge.SoundManager
{
    public class MuteFootSteps : StateMachineBehaviour
    {
        [Header("Configuración del Mute de Footsteps")]
        [Tooltip("Volumen a aplicar cuando se mutea (por ejemplo, 0.05 para casi silencio).")]
        [SerializeField, Range(0f, 1f)]
        private float mutedVolume = 0.05f;

        [Tooltip("Duración en segundos durante la cual se mantiene el estado muted por defecto.")]
        [SerializeField]
        private float muteDuration = 0.5f;

        [Tooltip("Volumen normal a restaurar (por ejemplo, 1 para volumen completo).")]
        [SerializeField, Range(0f, 1f)]
        private float normalVolume = 1f;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Aplicar mute inmediatamente
            SoundManager.footstepVolumeOverride = mutedVolume;
            SoundManager.footstepMuted = true;

            // Obtener el controlador para arrancar la corrutina
            ThirdPersonController controller = animator.GetComponent<ThirdPersonController>();
            if (controller != null)
            {
                // Lanza la corrutina pasándole el controlador y la duración
                controller.StartCoroutine(UnmuteAfterDelay(controller, muteDuration));
            }
        }

        private IEnumerator UnmuteAfterDelay(ThirdPersonController controller, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Solo desmutear si el jugador NO está en estado de hit (HitLegs o HitShelf)
            if (!controller.IsDead)
            {
                SoundManager.footstepVolumeOverride = normalVolume;
                SoundManager.footstepMuted = false;
            }
        }
    }
}
