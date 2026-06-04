using UnityEngine;

namespace RhythmSystem.Play
{
    public class RhythmClock : MonoBehaviour
    {
        private RhythmState state;
        private AudioSource audioSource;
        private float musicOffset; // in ms
        private double dspStartTime;
        private bool isMusicStarted;

        public void Initialize(RhythmState state, AudioSource audioSource, float musicOffset)
        {
            this.state = state;
            this.audioSource = audioSource;
            this.musicOffset = musicOffset;
        }

        public void StartMusic(float initialTimeMs)
        {
            if (audioSource == null || audioSource.clip == null) return;

            // Calculate when the music SHOULD start in DSP time
            // If initialTimeMs is -2000, we start 2 seconds from now.
            // If initialTimeMs is 0, we start now.
            // If initialTimeMs is 1000, we should have started 1s ago (seek needed).
            
            double now = AudioSettings.dspTime;
            dspStartTime = now - (initialTimeMs / 1000.0);

            if (initialTimeMs < 0)
            {
                // Future start
                audioSource.time = musicOffset / 1000f;
                audioSource.PlayScheduled(dspStartTime);
            }
            else
            {
                // Immediate start with seek
                audioSource.time = (initialTimeMs + musicOffset) / 1000f;
                audioSource.Play();
            }
            
            isMusicStarted = true;
        }

        public void SetPaused(bool paused)
        {
            if (!isMusicStarted || audioSource == null) return;

            if (paused)
            {
                audioSource.Pause();
            }
            else
            {
                // When unpausing, we must recalculate dspStartTime to maintain sync
                audioSource.UnPause();
                dspStartTime = AudioSettings.dspTime - (audioSource.time * 1000.0 - musicOffset) / 1000.0;
            }
        }

        public float CalculateGlobalTimeMs()
        {
            if (!isMusicStarted || state.isPaused) return state.currentTimeMs;

            // Absolute master time from DSP clock
            return (float)((AudioSettings.dspTime - dspStartTime) * 1000.0);
        }

        public void StopMusic()
        {
            if (audioSource != null) audioSource.Stop();
            isMusicStarted = false;
        }

        public void Seek(float timeMs)
        {
            if (audioSource == null || audioSource.clip == null) return;
            
            state.currentTimeMs = timeMs;
            audioSource.time = (timeMs + musicOffset) / 1000f;
            dspStartTime = AudioSettings.dspTime - (timeMs / 1000.0);
        }
    }
}
