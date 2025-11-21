using UnityEngine;
using NeuroReachVR.Feedback;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Legacy feedback wrapper - delegates to MultimodalFeedback
    /// Maintained for backward compatibility with existing tasks
    /// </summary>
    public class TaskFeedback : MonoBehaviour
    {
        [Header("Multimodal Feedback (Preferred)")]
        [SerializeField] private MultimodalFeedback multimodalFeedback;
        
        [Header("Legacy Settings (Fallback)")]
        [SerializeField] private ParticleSystem successParticles;
        [SerializeField] private ParticleSystem errorParticles;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip errorClip;
        [SerializeField] private float volume = 0.7f;
        
        private void Awake()
        {
            if (multimodalFeedback == null)
                multimodalFeedback = GetComponent<MultimodalFeedback>();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }
        
        public void PlaySuccess(Vector3 position)
        {
            if (multimodalFeedback != null)
            {
                multimodalFeedback.PlaySuccess(position);
                return;
            }
            
            // Fallback to legacy system
            PlayVisual(successParticles, position, Color.green);
            PlayAudio(successClip);
        }
        
        public void PlayError(Vector3 position)
        {
            if (multimodalFeedback != null)
            {
                multimodalFeedback.PlayError(position);
                return;
            }
            
            // Fallback to legacy system
            PlayVisual(errorParticles, position, Color.red);
            PlayAudio(errorClip);
        }
        
        private void PlayVisual(ParticleSystem particles, Vector3 position, Color color)
        {
            if (particles == null) return;
            
            particles.transform.position = position;
            var main = particles.main;
            main.startColor = color;
            particles.Play();
        }
        
        private void PlayAudio(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip, volume);
        }
    }
}

