using UnityEngine;

namespace NeuroReachVR.Feedback
{
    /// <summary>
    /// Manages audio feedback: success/error sounds, guidance cues, background music
    /// </summary>
    public class AudioFeedbackManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        
        [Header("Sound Effects")]
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip errorClip;
        [SerializeField] private AudioClip warningClip;
        [SerializeField] private AudioClip guidanceClip;
        
        [Header("Settings")]
        [SerializeField] private float sfxVolume = 0.7f;
        [SerializeField] private float musicVolume = 0.3f;
        [SerializeField] private bool musicEnabled = true;
        
        private void Awake()
        {
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();
            
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }
        }
        
        public void PlaySuccess()
        {
            PlaySFX(successClip);
        }
        
        public void PlayError()
        {
            PlaySFX(errorClip);
        }
        
        public void PlayWarning()
        {
            PlaySFX(warningClip);
        }
        
        public void PlayGuidance()
        {
            PlaySFX(guidanceClip);
        }
        
        public void PlayCustom(AudioClip clip)
        {
            PlaySFX(clip);
        }
        
        public void PlayBackgroundMusic(AudioClip music)
        {
            if (!musicEnabled || musicSource == null || music == null) return;
            
            musicSource.clip = music;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        
        public void StopBackgroundMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }
        
        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            if (!enabled && musicSource != null)
                musicSource.Stop();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }
        
        private void PlaySFX(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
                sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}

