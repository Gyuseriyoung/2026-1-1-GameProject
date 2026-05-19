using UnityEngine;

namespace RhythmSystem.Play
{
    public class RhythmClock : MonoBehaviour
    {
        private RhythmState state;
        private AudioSource audioSource;
        private float musicOffset;

        public void Initialize(RhythmState state, AudioSource audioSource, float musicOffset)
        {
            this.state = state;
            this.audioSource = audioSource;
            this.musicOffset = musicOffset;
        }

        public void SyncUpdate(float deltaTime)
        {
            if (!state.isPlaying || state.isPaused) return;

            state.currentTimeMs += deltaTime * 1000f;

            if (audioSource != null && audioSource.clip != null)
            {
                float targetAudioTime = (state.currentTimeMs + musicOffset) / 1000f;

                if (targetAudioTime >= 0 && targetAudioTime < audioSource.clip.length)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.time = targetAudioTime;
                        audioSource.Play();
                    }
                    else
                    {
                        float drift = Mathf.Abs(audioSource.time - targetAudioTime);
                        if (drift > 0.05f) audioSource.time = targetAudioTime;
                    }
                }
                else if (targetAudioTime >= audioSource.clip.length)
                {
                    if (audioSource.isPlaying) audioSource.Stop();
                }
            }
        }

        public void Seek(float timeMs)
        {
            state.currentTimeMs = timeMs;
            if (audioSource != null && audioSource.clip != null)
            {
                float targetAudioTime = (timeMs + musicOffset) / 1000f;
                if (targetAudioTime >= 0 && targetAudioTime < audioSource.clip.length)
                {
                    audioSource.time = targetAudioTime;
                }
            }
        }
    }
}
