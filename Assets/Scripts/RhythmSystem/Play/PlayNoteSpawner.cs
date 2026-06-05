using UnityEngine;
using System.Collections.Generic;
using RhythmSystem;

namespace RhythmSystem.Play
{
    public class PlayNoteSpawner : MonoBehaviour
    {
        [Header("Assets")]
        public GameObject notePrefab;
        public MergeObjectData mergeObjectData;
        
        private List<NoteObject> spawnedNotes = new List<NoteObject>();
        private LaneManager laneManager;
        private RhythmState state;

        public void Initialize(LaneManager laneManager, RhythmState state)
        {
            this.laneManager = laneManager;
            this.state = state;
        }

        public void SpawnNotes(ChartData chartData)
        {
            if (chartData == null || notePrefab == null) return;

            foreach (var noteData in chartData.notes)
            {
                var lane = laneManager.GetLane(noteData.laneIndex);
                
                GameObject go = Instantiate(notePrefab, Vector3.zero, Quaternion.identity, transform);
                NoteObject noteObj = go.GetComponent<NoteObject>();
                
                if (noteObj != null)
                {
                    noteObj.Initialize(noteData, lane, state.currentTimeMs, mergeObjectData);
                    spawnedNotes.Add(noteObj);
                }
            }
        }

        public void UpdateNotes(float speedMultiplier = 1f)
        {
            float baseSpeed = EditorTestSession.IsTestMode ? 
                EditorTestSession.ScrollSpeed : 
                Core.GameSettingsManager.Instance.Settings.rhythm.scrollSpeed;
                
            float worldSpeed = baseSpeed * speedMultiplier;
            foreach (var note in spawnedNotes)
            {
                if (note != null) note.UpdatePosition(state.currentTimeMs, worldSpeed);
            }
        }

        public void UpdateAllNotePositions()
        {
            UpdateNotes(state.scrollSpeedMultiplier);
        }

        public void ClearNotes()
        {
            foreach (var note in spawnedNotes)
            {
                if (note != null) Destroy(note.gameObject);
            }
            spawnedNotes.Clear();
        }

        public List<NoteObject> GetSpawnedNotes() => spawnedNotes;
        public Dictionary<int, LaneController> GetActiveLanes() => laneManager.GetActiveLanes();
    }
}
