using UnityEngine;
using RhythmSystem;

namespace RhythmSystem.Play
{
    public enum NoteState
    {
        Idle,
        Holding,
        Completed,
        Missed
    }

    public class NoteObject : MonoBehaviour
    {
        private NoteData noteData;
        private LaneController parentLane;
        private bool isInitialized = false;
        private Color holdBarColor = Color.white;

        public SpriteRenderer holdBody;
        public NoteState State { get; private set; } = NoteState.Idle;

        public bool IsJudged { get; set; } = false;
        public NoteData Data => noteData;

        public void Initialize(NoteData data, LaneController lane, float startTimeMs, MergeObjectData mergeData = null)
        {
            noteData = data;
            parentLane = lane;
            isInitialized = true;
            State = NoteState.Idle;
            IsJudged = false;
            
            if (parentLane == null) gameObject.SetActive(false);

            SpriteRenderer headRenderer = GetComponent<SpriteRenderer>();
            if (headRenderer != null && mergeData != null)
            {
                if (data.mergeType >= 0 && data.mergeType < mergeData.MergeData.Length)
                {
                    var category = mergeData.MergeData[data.mergeType];
                    holdBarColor = category.HoldBodyColor;

                    if (data.objectIndex >= 0 && data.objectIndex < category.MergeDataList.Length)
                    {
                        var obj = category.MergeDataList[data.objectIndex];
                        if (obj.sprite != null)
                        {
                            headRenderer.sprite = obj.sprite;
                        }
                    }
                }
            }
            
            UpdatePosition(startTimeMs);
            UpdateHoldBody(startTimeMs);
        }

        public void SetLane(LaneController lane)
        {
            parentLane = lane;
            if (parentLane != null && !IsJudged)
            {
                gameObject.SetActive(true);
            }
            else if (IsJudged)
            {
                gameObject.SetActive(false);
            }
        }

        public void UpdatePosition(float currentTimeMs, float worldSpeed = -1f)
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
            
            float baseSpeed = EditorTestSession.IsTestMode ? 
                EditorTestSession.ScrollSpeed : 
                Core.GameSettingsManager.Instance.Settings.rhythm.scrollSpeed;
            float effectiveSpeed = worldSpeed > 0 ? worldSpeed : baseSpeed;

            // If we are holding, the "head" of the note stays at the judgment line
            float xPos;
            if (State == NoteState.Holding)
            {
                xPos = judgmentPos.x;
            }
            else
            {
                xPos = judgmentPos.x - (timeRemaining * effectiveSpeed);
            }

            transform.position = new Vector3(xPos, judgmentPos.y, 0);

            UpdateHoldBody(currentTimeMs, worldSpeed);
        }

        private void UpdateHoldBody(float currentTimeMs, float worldSpeed = -1f)
        {
            if (holdBody == null) return;

            if (noteData.type == NoteType.Hold)
            {
                holdBody.gameObject.SetActive(true);
                
                float totalLengthSeconds = noteData.length / 1000f;
                float currentElapsedInHold = (currentTimeMs - noteData.time) / 1000f;
                
                float remainingLengthSeconds;
                if (State == NoteState.Holding)
                {
                    remainingLengthSeconds = totalLengthSeconds - currentElapsedInHold;
                }
                else
                {
                    remainingLengthSeconds = totalLengthSeconds;
                }

                remainingLengthSeconds = Mathf.Max(0, remainingLengthSeconds);
                
                float baseSpeed = EditorTestSession.IsTestMode ? 
                    EditorTestSession.ScrollSpeed : 
                    Core.GameSettingsManager.Instance.Settings.rhythm.scrollSpeed;
                float effectiveSpeed = worldSpeed > 0 ? worldSpeed : baseSpeed;
                float visualLength = remainingLengthSeconds * effectiveSpeed;
                
                // Adjust size and position of the body using Tiled mode
                holdBody.drawMode = SpriteDrawMode.Tiled;
                holdBody.size = new Vector2(visualLength, holdBody.size.y);
                holdBody.color = holdBarColor; // Apply the category color
                holdBody.transform.localScale = Vector3.one; 
                holdBody.transform.localPosition = new Vector3(-visualLength / 2f, 0, 0); 
            }
            else
            {
                holdBody.gameObject.SetActive(false);
            }
        }

        public float GetNoteTime() => noteData.time;

        public void StartHolding()
        {
            if (noteData.type == NoteType.Hold)
            {
                State = NoteState.Holding;
            }
        }

        public void CompleteHold()
        {
            State = NoteState.Completed;
            OnJudged();
        }

        public void OnJudged()
        {
            IsJudged = true;
            gameObject.SetActive(false); 
        }

        public void SetMissed()
        {
            State = NoteState.Missed;
            OnJudged();
        }
    }
}
