using SmartARMeasure.Models;
using UnityEngine;

namespace SmartARMeasure.Utilities
{
    /// <summary>
    /// Provides procedural audio feedback for UI clicks, marker placement, and reset actions.
    /// Fully responsive to AppSettings.SoundFX toggle.
    /// </summary>
    public static class AudioFeedback
    {
        private static AudioSource audioSource;
        private static AudioClip clickClip;
        private static AudioClip markerPlacedClip;
        private static AudioClip warningClip;

        private static void EnsureInitialized()
        {
            if (audioSource == null)
            {
                GameObject go = new GameObject("[AudioFeedback]");
                Object.DontDestroyOnLoad(go);
                audioSource = go.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D Sound
                audioSource.volume = 0.5f;

                clickClip = CreateToneClip("ClickTone", 0.035f, 1000f, 600f);
                markerPlacedClip = CreateToneClip("MarkerTone", 0.08f, 750f, 1200f);
                warningClip = CreateToneClip("WarningTone", 0.12f, 400f, 250f);
            }
        }

        /// <summary>
        /// Plays a crisp UI click sound when sound effects are enabled.
        /// </summary>
        public static void PlayClick()
        {
            if (AppSettings.Instance != null && !AppSettings.Instance.SoundFX) return;
            EnsureInitialized();
            if (audioSource != null && clickClip != null)
            {
                audioSource.PlayOneShot(clickClip, 0.4f);
            }
        }

        /// <summary>
        /// Plays a positive chime when a measurement marker is placed.
        /// </summary>
        public static void PlayMarkerPlaced()
        {
            if (AppSettings.Instance != null && !AppSettings.Instance.SoundFX) return;
            EnsureInitialized();
            if (audioSource != null && markerPlacedClip != null)
            {
                audioSource.PlayOneShot(markerPlacedClip, 0.6f);
            }
        }

        /// <summary>
        /// Plays a low warning / reset sound.
        /// </summary>
        public static void PlayWarning()
        {
            if (AppSettings.Instance != null && !AppSettings.Instance.SoundFX) return;
            EnsureInitialized();
            if (audioSource != null && warningClip != null)
            {
                audioSource.PlayOneShot(warningClip, 0.5f);
            }
        }

        /// <summary>
        /// Procedurally generates a short smooth audio tone.
        /// </summary>
        private static AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;

                // Smooth attack and decay envelope to eliminate pops
                float envelope = Mathf.Sin(t * Mathf.PI);
                samples[i] = Mathf.Sin(phase) * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
