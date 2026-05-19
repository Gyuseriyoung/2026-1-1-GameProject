using System;
using UnityEngine;

namespace RhythmSystem.Play
{
    public enum JudgmentRating
    {
        Perfect,
        Great,
        Good,
        Miss,
        EarlyMiss
    }

    public struct NoteHitEventArgs
    {
        public NoteObject note;
        public JudgmentRating rating;
        public int laneIndex;
        public float timeMs;
        public int combo;
    }

    public struct NoteMissEventArgs
    {
        public NoteObject note;
        public int laneIndex;
        public float timeMs;
        public int combo;
    }

    public static class RhythmEvents
    {
        // Gameplay Events
        public static Action<NoteHitEventArgs> OnNoteHit;
        public static Action<NoteMissEventArgs> OnNoteMiss;
        public static Action<int> OnLaneDown;
        public static Action<int> OnLaneUp;
        
        // System Events
        public static Action OnGameStart;
        public static Action OnGameEnd;
        public static Action<bool> OnGamePause;

        public static void ClearAll()
        {
            OnNoteHit = null;
            OnNoteMiss = null;
            OnLaneDown = null;
            OnLaneUp = null;
            OnGameStart = null;
            OnGameEnd = null;
            OnGamePause = null;
        }
    }

    [Serializable]
    public class RhythmState
    {
        public float currentTimeMs;
        public float scrollSpeedMultiplier = 1f;
        public int combo;
        public bool isPlaying;
        public bool isPaused;
    }
}
