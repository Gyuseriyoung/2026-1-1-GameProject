using UnityEngine;
using System.Collections.Generic;
using RhythmSystem;

namespace RhythmSystem.Play
{
    public class PlayNoteSpawner : MonoBehaviour
    {
        [Header("Assets")]
        public GameObject notePrefab;
        public GameObject laneControllerPrefab;

        private List<NoteObject> spawnedNotes = new List<NoteObject>();
        private Dictionary<int, LaneController> allLanes = new Dictionary<int, LaneController>();
        private List<GimmickEvent> laneGimmicks = new List<GimmickEvent>();
        private HashSet<int> initiallyActiveLanes = new HashSet<int>();

        public void SpawnNotes(ChartData chartData, float startTimeMs, float? speedOverride = null)
        {
            if (chartData == null || notePrefab == null) return;

            var settings = RhythmSettingsManager.Settings;
            float worldScrollSpeed = speedOverride.HasValue ? speedOverride.Value / 100f : RhythmSettingsManager.GetWorldScrollSpeed();

            InitializeLanes(chartData, settings);

            foreach (var noteData in chartData.notes)
            {
                allLanes.TryGetValue(noteData.laneIndex, out var lane);
                
                GameObject go = Instantiate(notePrefab, Vector3.zero, Quaternion.identity, transform);
                NoteObject noteObj = go.GetComponent<NoteObject>();
                
                if (noteObj != null)
                {
                    noteObj.Initialize(noteData, lane, worldScrollSpeed, startTimeMs);
                    spawnedNotes.Add(noteObj);
                }
            }

            UpdateLanes(startTimeMs);
        }

        private void InitializeLanes(ChartData chartData, UserSettings settings)
        {
            foreach (var lane in allLanes.Values) if (lane != null) Destroy(lane.gameObject);
            allLanes.Clear();
            
            laneGimmicks = new List<GimmickEvent>(chartData.gimmicks);
            laneGimmicks.Sort((a, b) => a.time.CompareTo(b.time));
            
            initiallyActiveLanes.Clear();

            HashSet<int> potentialLanes = new HashSet<int>();
            foreach (var l in chartData.lanes)
            {
                potentialLanes.Add(l.laneIndex);
                initiallyActiveLanes.Add(l.laneIndex);
            }
            foreach (var n in chartData.notes) potentialLanes.Add(n.laneIndex);
            foreach (var g in chartData.gimmicks)
            {
                if (g.type == GimmickType.LaneAdd || g.type == GimmickType.LaneRemove || 
                    g.type == GimmickType.LaneMoveX || g.type == GimmickType.LaneMoveY)
                {
                    potentialLanes.Add(g.targetLane);
                }
            }

            foreach (int laneIndex in potentialLanes)
            {
                SetupLaneController(laneIndex, Vector2.zero);
            }
        }

        private void SetupLaneController(int laneIndex, Vector2 pos)
        {
            LaneController controller = null;
            if (laneControllerPrefab != null)
            {
                GameObject go = Instantiate(laneControllerPrefab, transform);
                controller = go.GetComponent<LaneController>();
            }
            else
            {
                GameObject go = new GameObject($"Lane_{laneIndex}");
                go.transform.SetParent(transform);
                controller = go.AddComponent<LaneController>();
            }

            if (controller != null)
            {
                controller.Initialize(laneIndex, pos);
                allLanes[laneIndex] = controller;
            }
        }

        public void UpdateLanes(float currentTimeMs)
        {
            var settings = RhythmSettingsManager.Settings;
            
            int activeCount = initiallyActiveLanes.Count;
            foreach (var g in laneGimmicks)
            {
                if (g.time > currentTimeMs) break;
                if (g.type == GimmickType.LaneAdd) activeCount += (int)g.value;
                else if (g.type == GimmickType.LaneRemove) activeCount -= (int)g.value;
            }
            activeCount = Mathf.Clamp(activeCount, 1, 12);

            Dictionary<int, float> xPositions = new Dictionary<int, float>();
            Dictionary<int, float?> yOverrides = new Dictionary<int, float?>();

            foreach (var idx in allLanes.Keys)
            {
                float x = settings.judgmentX;
                float? yOverride = null;

                foreach (var g in laneGimmicks)
                {
                    if (g.time > currentTimeMs) break;
                    if (g.targetLane != idx) continue;

                    if (g.type == GimmickType.LaneMoveX) x = g.value;
                    else if (g.type == GimmickType.LaneMoveY) yOverride = g.value;
                }
                xPositions[idx] = x;
                yOverrides[idx] = yOverride;
            }

            float spacing = settings.laneSpacing;
            float totalHeight = (activeCount - 1) * spacing;
            float startY = totalHeight / 2f;

            foreach (var kvp in allLanes)
            {
                int idx = kvp.Key;
                bool isActive = idx < activeCount;
                
                kvp.Value.gameObject.SetActive(isActive);

                if (isActive)
                {
                    float targetX = xPositions[idx];
                    float targetY = yOverrides[idx] ?? (startY - (idx * spacing));
                    kvp.Value.UpdateJudgmentPosition(new Vector2(targetX, targetY));
                }
            }
        }

        public void UpdateNotes(float currentTimeMs)
        {
            foreach (var note in spawnedNotes)
            {
                note.UpdatePosition(currentTimeMs);
            }
        }

        public void ClearNotes()
        {
            foreach (var note in spawnedNotes)
            {
                if (note != null) Destroy(note.gameObject);
            }
            spawnedNotes.Clear();

            foreach (var lane in allLanes.Values)
            {
                if (lane != null) Destroy(lane.gameObject);
            }
            allLanes.Clear();
        }

        public List<NoteObject> GetSpawnedNotes() => spawnedNotes;
        public Dictionary<int, LaneController> GetActiveLanes() 
        {
            var active = new Dictionary<int, LaneController>();
            foreach(var kvp in allLanes) if(kvp.Value.gameObject.activeSelf) active[kvp.Key] = kvp.Value;
            return active;
        }
    }
}
