using UnityEngine;

namespace RhythmSystem.Play
{
    /// <summary>
    /// Handles visual and logic for a single lane.
    /// Prepared for performance elements (visual effects, animations).
    /// </summary>
    public class LaneController : MonoBehaviour
    {
        public int laneIndex;
        public Transform visualJudgmentLine;
        
        private Vector2 currentJudgmentPos;

        public void Initialize(int index, Vector2 pos)
        {
            laneIndex = index;
            UpdateJudgmentPosition(pos);
        }

        public void UpdateJudgmentPosition(Vector2 pos)
        {
            currentJudgmentPos = pos;
            if (visualJudgmentLine != null)
            {
                visualJudgmentLine.position = new Vector3(pos.x, pos.y, visualJudgmentLine.position.z);
            }
        }

        public void UpdateX(float newX)
        {
            currentJudgmentPos.x = newX;
            UpdateJudgmentPosition(currentJudgmentPos);
        }

        public void UpdateY(float newY)
        {
            currentJudgmentPos.y = newY;
            UpdateJudgmentPosition(currentJudgmentPos);
        }

        public Vector2 GetJudgmentPosition() => currentJudgmentPos;

        /// <summary>
        /// Called when the player presses the key for this lane.
        /// </summary>
        public void OnPress()
        {
            // Debug.Log($"Lane {laneIndex} Pressed");
        }

        /// <summary>
        /// Called when a note is successfully hit in this lane.
        /// </summary>
        public void OnHit(string rating)
        {
            // Debug.Log($"Lane {laneIndex} Hit: {rating}");
        }

        /// <summary>
        /// Called when a note is missed in this lane.
        /// </summary>
        public void OnMiss()
        {
            
        }
    }
}
