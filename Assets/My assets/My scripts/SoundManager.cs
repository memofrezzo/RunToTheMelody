using System;
using UnityEngine;
using UnityEngine.Audio;

namespace SmallHedge.SoundManager
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundsSO SO; // Tu ScriptableObject que contiene la configuración de cada sonido.
        private static SoundManager instance = null;
        private AudioSource audioSource; // AudioSource principal para sonidos que no sean de footsteps.

        // Variables para gestionar el volumen de Footstep desde el Animator.
        // Por defecto, el override es 1 (volumen completo) y no está muteado.
        public static float footstepVolumeOverride = 1f;
        public static bool footstepMuted = false;
        private static bool globalMuted = false;
        private static float previousMasterVolume = 1f;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                audioSource = GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Reproduce un sonido del tipo especificado.
        /// El parámetro 'volume' se multiplica por el valor asignado en el SoundList del SO.
        /// Para Footstep, se aplica también el footstepVolumeOverride.
        /// </summary>
        public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
        {
            if (instance == null) return;

            SoundList soundList = instance.SO.sounds[(int)sound];
            AudioClip[] clips = soundList.sounds;
            if (clips == null || clips.Length == 0) return;

            AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];

            if (sound == SoundType.Footstep)
            {
                volume *= footstepVolumeOverride;
            }

            if (source != null)
            {
                source.outputAudioMixerGroup = soundList.mixer;
                source.clip = randomClip;
                source.volume = volume * soundList.volume;
                source.Play();
            }
            else
            {
                instance.audioSource.outputAudioMixerGroup = soundList.mixer;
                instance.audioSource.PlayOneShot(randomClip, volume * soundList.volume);
            }
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                globalMuted = !globalMuted;
                if (globalMuted)
                {
                    previousMasterVolume = AudioListener.volume;
                    AudioListener.volume = 0f;
                }
                else
                {
                    AudioListener.volume = previousMasterVolume;
                }
            }
        }


        /// <summary>
        /// Ajusta el volumen del grupo de mezcla asociado a un tipo de sonido.
        /// El volumen se define en el rango 0 (silencio) a 1 (volumen completo).
        /// </summary>
        public static void SetGroupVolume(SoundType sound, float volume01)
        {
            if (instance == null) return;

            SoundList soundList = instance.SO.sounds[(int)sound];

            if (soundList.mixer == null || string.IsNullOrEmpty(soundList.exposedParamName))
            {
                Debug.LogWarning($"SoundManager: No se puede ajustar el volumen de {sound} porque falta el mixer o el nombre del parámetro expuesto.");
                return;
            }

            float volumeDb = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(volume01));
            soundList.mixer.audioMixer.SetFloat(soundList.exposedParamName, volumeDb);
        }
    }

    [Serializable]
    public struct SoundList
    {
        [HideInInspector] public string name;
        [Range(0, 1)] public float volume;
        public AudioMixerGroup mixer;
        public AudioClip[] sounds;
        [Tooltip("Nombre del parámetro expuesto en el AudioMixer para ajustar volumen en tiempo real")] public string exposedParamName;
    }
}
