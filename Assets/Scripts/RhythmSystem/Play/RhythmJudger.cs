using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

        private float GetCurrentGameTime()
        {
            // By using state.currentTimeMs, we ensure perfect synchronization 
            // with the visual position of the notes, and it automatically includes 
            // global offsets and Stop gimmick logic processed in GameManager.
            return state.currentTimeMs;
        }

        private void OnLaneDown(int laneIndex)
        {
            var activeLanes = spawner.GetActiveLanes();
            if (!activeLanes.ContainsKey(laneIndex)) return;

            activeLanes[laneIndex].OnPress();

            float currentTime = GetCurrentGameTime();
            var spawnedNotes = spawner.GetSpawnedNotes();
            
            NoteObject bestNote = null;
            float minDiff = float.MaxValue;
            bool isEarly = false;

            foreach (var note in spawnedNotes)
            {
                float rawDiff = note.GetNoteTime() - currentTime;
                
                // Since notes are sorted chronologically, we can stop searching 
                // if the current note is too far in the future.
                if (rawDiff > earlyMissWindow)
                {
                    break;
                }

                if (note.IsJudged || note.Data.laneIndex != laneIndex) continue;

                float absDiff = Mathf.Abs(rawDiff);

                if (absDiff < minDiff && absDiff <= earlyMissWindow)
                {
                    minDiff = absDiff;
                    bestNote = note;
                    isEarly = rawDiff > 0;
                }
            }
            
            if (bestNote != null || spawnedNotes.Any(n => !n.IsJudged && n.Data.laneIndex == laneIndex))
            {
                var noteToPlay = bestNote ?? spawnedNotes.FirstOrDefault(n => !n.IsJudged && n.Data.laneIndex == laneIndex);
                var laneManager = GetComponent<LaneManager>() ?? GetComponentInParent<LaneManager>();
                if (laneManager != null && noteToPlay != null) laneManager.PlayNoteSound(noteToPlay.Data);
            }
            else 
            {
                // Play a fallback sound or at least trigger visual feedback even if NO note is present 
                // so the player knows their input was registered.
                var laneManager = GetComponent<LaneManager>() ?? GetComponentInParent<LaneManager>();
                if (laneManager != null) 
                {
                    // Create a dummy NoteData with soundIndex 0 just to trigger the default hit sound
                    NoteData dummyData = new NoteData { soundIndex = 0 }; 
                    laneManager.PlayNoteSound(dummyData);
                }
            }

            if (bestNote != null)
            {
                ProcessHit(bestNote, minDiff, isEarly, currentTime);
            }
        }

        private void OnLaneUp(int laneIndex)
        {
            float currentTime = GetCurrentGameTime();
            for (int i = activeHoldNotes.Count - 1; i >= 0; i--)
            {
                var note = activeHoldNotes[i];
                if (note.Data.laneIndex == laneIndex)
                {
                    float endTime = note.Data.time + note.Data.length;
                    if (currentTime < endTime - perfectWindow)
                    {
                        ProcessMiss(note, "Miss (Released Early)", currentTime);
                        activeHoldNotes.RemoveAt(i);
                    }
                }
            }
        }

        private void ProcessHit(NoteObject note, float absDiff, bool isEarly, float currentTime)
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
                TriggerHitEvent(note, rating, currentTime);
                spawner.GetActiveLanes()[note.Data.laneIndex].OnHit(rating.ToString());
            }
            else
            {
                if (rating == JudgmentRating.Miss || rating == JudgmentRating.EarlyMiss)
                {
                    ProcessMiss(note, rating.ToString(), currentTime);
                }
                else
                {
                    TriggerHitEvent(note, rating, currentTime);
                    note.OnJudged();
                    spawner.RemoveNote(note);
                    
                    var activeLanes = spawner.GetActiveLanes();
                    if (activeLanes.ContainsKey(note.Data.laneIndex))
                        activeLanes[note.Data.laneIndex].OnHit(rating.ToString());
                }
            }
        }

        private void TriggerHitEvent(NoteObject note, JudgmentRating rating, float currentTime)
        {
            state.combo++;
            var args = new NoteHitEventArgs { note = note, rating = rating, laneIndex = note.Data.laneIndex, timeMs = currentTime, combo = state.combo };
            RhythmEvents.OnNoteHit?.Invoke(args);

            foreach (var mod in modifiers) mod.OnNoteHit(args, state);
        }

        private void ProcessMiss(NoteObject note, string rating, float currentTime)
        {
            state.combo = 0;
            note.SetMissed();
            spawner.RemoveNote(note);

            var args = new NoteMissEventArgs { note = note, laneIndex = note.Data.laneIndex, timeMs = currentTime, combo = state.combo };
            RhythmEvents.OnNoteMiss?.Invoke(args);

            foreach (var mod in modifiers) mod.OnNoteMiss(args, state);
        }

        private void UpdateHoldNotes()
        {
            float currentTime = GetCurrentGameTime();
            for (int i = activeHoldNotes.Count - 1; i >= 0; i--)
            {
                var note = activeHoldNotes[i];
                float endTime = note.Data.time + note.Data.length;

                if (currentTime >= endTime)
                {
                    TriggerHitEvent(note, JudgmentRating.Perfect, currentTime);
                    note.CompleteHold();
                    spawner.RemoveNote(note);
                    activeHoldNotes.RemoveAt(i);
                }
            }
        }

        private void CheckForMisses()
        {
            float currentTime = GetCurrentGameTime();
            var spawnedNotes = spawner.GetSpawnedNotes();
            foreach (var note in spawnedNotes)
            {
                if (note.IsJudged || note.State == NoteState.Holding) continue;

                float diff = currentTime - note.GetNoteTime();
                if (diff > missWindow)
                {
                    ProcessMiss(note, "Miss", currentTime);
                }
            }
        }

        public void Clear()
        {
            activeHoldNotes.Clear();
        }
    }
}