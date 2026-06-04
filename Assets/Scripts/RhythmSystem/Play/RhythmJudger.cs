using UnityEngine;
using System.Collections.Generic;

namespace RhythmSystem.Play
{
    public class RhythmJudger : MonoBehaviour
    {
        private RhythmState state;
        private PlayNoteSpawner spawner;
        private List<IRhythmModifier> modifiers = new List<IRhythmModifier>();
        private List<NoteObject> activeHoldNotes = new List<NoteObject>();

        [Header("Judgment Windows")]
        public float perfectWindow = 50f;
        public float greatWindow = 100f;
        public float goodWindow = 150f;
        public float missWindow = 200f;
        public float earlyMissWindow = 400f;

        public void Initialize(RhythmState state, PlayNoteSpawner spawner, List<IRhythmModifier> initialModifiers)
        {
            this.state = state;
            this.spawner = spawner;
            this.modifiers = initialModifiers ?? new List<IRhythmModifier>();

            RhythmEvents.OnLaneDown += OnLaneDown;
            RhythmEvents.OnLaneUp += OnLaneUp;
        }

        private void OnDestroy()
        {
            RhythmEvents.OnLaneDown -= OnLaneDown;
            RhythmEvents.OnLaneUp -= OnLaneUp;
        }

        public void UpdateLogic()
        {
            if (!state.isPlaying || state.isPaused) return;

            UpdateHoldNotes();
            CheckForMisses();

            foreach (var mod in modifiers)
            {
                mod.OnUpdate(Time.deltaTime, state);
            }
        }

        private void OnLaneDown(int laneIndex)
        {
            var activeLanes = spawner.GetActiveLanes();
            if (!activeLanes.ContainsKey(laneIndex)) return;

            activeLanes[laneIndex].OnPress();

            var spawnedNotes = spawner.GetSpawnedNotes();
            NoteObject bestNote = null;
            float minDiff = float.MaxValue;
            bool isEarly = false;

            // 1. Find the best note for judgment AND play its sound (Next Note Preview)
            NoteObject nextNoteToPlay = null;
            float earliestTime = float.MaxValue;

            foreach (var note in spawnedNotes)
            {
                if (note.IsJudged || note.Data.laneIndex != laneIndex) continue;

                // For Sound Preview: Find the absolute earliest unjudged note
                if (note.Data.time < earliestTime)
                {
                    earliestTime = note.Data.time;
                    nextNoteToPlay = note;
                }

                // For Judgment: Find the closest note within earlyMissWindow
                float rawDiff = note.GetNoteTime() - state.currentTimeMs;
                float absDiff = Mathf.Abs(rawDiff);

                if (absDiff < minDiff && absDiff <= earlyMissWindow)
                {
                    minDiff = absDiff;
                    bestNote = note;
                    isEarly = rawDiff > 0;
                }
            }

            // Play sound immediately if any upcoming note exists in this lane
            if (nextNoteToPlay != null)
            {
                var laneManager = GetComponent<LaneManager>();
                if (laneManager != null) laneManager.PlayNoteSound(nextNoteToPlay.Data);
            }

            if (bestNote != null)
            {
                ProcessHit(bestNote, minDiff, isEarly);
            }
        }

        private void OnLaneUp(int laneIndex)
        {
            for (int i = activeHoldNotes.Count - 1; i >= 0; i--)
            {
                var note = activeHoldNotes[i];
                if (note.Data.laneIndex == laneIndex)
                {
                    float endTime = note.Data.time + note.Data.length;
                    if (state.currentTimeMs < endTime - perfectWindow)
                    {
                        ProcessMiss(note, "Miss (Released Early)");
                        activeHoldNotes.RemoveAt(i);
                    }
                }
            }
        }

        private void ProcessHit(NoteObject note, float absDiff, bool isEarly)
        {
            JudgmentRating rating = JudgmentRating.Miss;

            if (absDiff <= perfectWindow) rating = JudgmentRating.Perfect;
            else if (absDiff <= greatWindow) rating = JudgmentRating.Great;
            else if (absDiff <= goodWindow) rating = JudgmentRating.Good;
            else if (absDiff <= missWindow) rating = JudgmentRating.Miss;
            else if (isEarly && absDiff <= earlyMissWindow) rating = JudgmentRating.EarlyMiss;

            if (note.Data.type == NoteType.Hold && rating != JudgmentRating.Miss && rating != JudgmentRating.EarlyMiss)
            {
                note.StartHolding();
                activeHoldNotes.Add(note);
                TriggerHitEvent(note, rating);
                spawner.GetActiveLanes()[note.Data.laneIndex].OnHit(rating.ToString());
            }
            else
            {
                if (rating == JudgmentRating.Miss || rating == JudgmentRating.EarlyMiss)
                {
                    ProcessMiss(note, rating.ToString());
                }
                else
                {
                    TriggerHitEvent(note, rating);
                    note.OnJudged();
                    
                    var activeLanes = spawner.GetActiveLanes();
                    if (activeLanes.ContainsKey(note.Data.laneIndex))
                        activeLanes[note.Data.laneIndex].OnHit(rating.ToString());
                }
            }
        }

        private void ProcessMiss(NoteObject note, string rating)
        {
            state.combo = 0;
            note.SetMissed();

            var args = new NoteMissEventArgs { note = note, laneIndex = note.Data.laneIndex, timeMs = state.currentTimeMs, combo = state.combo };
            RhythmEvents.OnNoteMiss?.Invoke(args);

            foreach (var mod in modifiers) mod.OnNoteMiss(args, state);
        }

        private void TriggerHitEvent(NoteObject note, JudgmentRating rating)
        {
            state.combo++;
            var args = new NoteHitEventArgs { note = note, rating = rating, laneIndex = note.Data.laneIndex, timeMs = state.currentTimeMs, combo = state.combo };
            RhythmEvents.OnNoteHit?.Invoke(args);

            foreach (var mod in modifiers) mod.OnNoteHit(args, state);
        }

        private void UpdateHoldNotes()
        {
            for (int i = activeHoldNotes.Count - 1; i >= 0; i--)
            {
                var note = activeHoldNotes[i];
                float endTime = note.Data.time + note.Data.length;

                if (state.currentTimeMs >= endTime)
                {
                    TriggerHitEvent(note, JudgmentRating.Perfect);
                    note.CompleteHold();
                    activeHoldNotes.RemoveAt(i);
                }
            }
        }

        private void CheckForMisses()
        {
            var spawnedNotes = spawner.GetSpawnedNotes();
            foreach (var note in spawnedNotes)
            {
                if (note.IsJudged || note.State == NoteState.Holding) continue;

                float diff = state.currentTimeMs - note.GetNoteTime();
                if (diff > missWindow)
                {
                    ProcessMiss(note, "Miss");
                }
            }
        }

        public void Clear()
        {
            activeHoldNotes.Clear();
        }
    }
}
