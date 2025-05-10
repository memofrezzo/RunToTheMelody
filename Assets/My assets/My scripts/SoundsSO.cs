using UnityEngine;

namespace SmallHedge.SoundManager
{
    [CreateAssetMenu(menuName = "Sound SO", fileName = "Sound So")]
    public class SoundsSO : ScriptableObject
    {
        // En el Inspector, vas a poder agregar tantos elementos como desees.
        // Cada elemento (SoundList) tendrá:
        //    - Volume: valor entre 0 y 1 (por defecto, asigná 1 para Footstep si querés que suene fuerte).
        //    - Mixer: acá arrastras el AudioMixerGroup correspondiente (por ejemplo, "Player/Footsteps" para Footstep, o "Player/Actions" para Jump, Slide, etc.)
        public SoundList[] sounds;
    }
}
