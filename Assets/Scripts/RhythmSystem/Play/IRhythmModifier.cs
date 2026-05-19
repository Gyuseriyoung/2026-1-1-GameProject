namespace RhythmSystem.Play
{
    public interface IRhythmModifier
    {
        void Initialize(RhythmState state);
        void OnNoteHit(NoteHitEventArgs args, RhythmState state);
        void OnNoteMiss(NoteMissEventArgs args, RhythmState state);
        void OnUpdate(float deltaTime, RhythmState state);
    }

    /// <summary>
    /// Example modifier that increases scroll speed on miss.
    /// </summary>
    public class SpeedIncrementModifier : IRhythmModifier
    {
        private float incrementAmount = 0.1f;
        private float maxMultiplier = 2.0f;

        public void Initialize(RhythmState state) { }

        public void OnNoteHit(NoteHitEventArgs args, RhythmState state) { }

        public void OnNoteMiss(NoteMissEventArgs args, RhythmState state)
        {
            state.scrollSpeedMultiplier = UnityEngine.Mathf.Min(state.scrollSpeedMultiplier + incrementAmount, maxMultiplier);
        }

        public void OnUpdate(float deltaTime, RhythmState state) { }
    }
}
