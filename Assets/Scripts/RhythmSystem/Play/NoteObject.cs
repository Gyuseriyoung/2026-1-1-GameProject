using UnityEngine;
using RhythmSystem;

namespace RhythmSystem.Play
{
    public class NoteObject : MonoBehaviour
    {
        private NoteData noteData;
        private LaneController parentLane;
        private float scrollSpeed;
        private bool isInitialized = false;

        public bool IsJudged { get; set; } = false;
        public NoteData Data => noteData;

        public void Initialize(NoteData data, LaneController lane, float speed, float startTimeMs)
        {
            noteData = data;
            parentLane = lane;
            scrollSpeed = speed;
            isInitialized = true;
            
            if (parentLane == null) gameObject.SetActive(false);
            
            UpdatePosition(startTimeMs);
        }

        public void SetLane(LaneController lane)
        {
            parentLane = lane;
            if (parentLane != null && !IsJudged)
            {
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void UpdatePosition(float currentTimeMs)
        {
            if (!isInitialized || IsJudged || parentLane == null) return;

            bool isLaneActive = parentLane.gameObject.activeSelf;
            if (gameObject.activeSelf != isLaneActive)
            {
                gameObject.SetActive(isLaneActive);
            }

            if (!isLaneActive) return;

            Vector2 judgmentPos = parentLane.GetJudgmentPosition();

            float timeRemaining = (noteData.time - currentTimeMs) / 1000f;
            float xPos = judgmentPos.x - (timeRemaining * scrollSpeed);

            transform.position = new Vector3(xPos, judgmentPos.y, 0);

            if (timeRemaining < -0.2f) { /* Auto-miss handled by manager */ }
        }

        public float GetNoteTime() => noteData.time;

        public void OnJudged()
        {
            IsJudged = true;
            gameObject.SetActive(false); // Placeholder: add animation/particle later
        }
    }
}
