using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace RhythmSystem.Play
{
    public class LaneManager : MonoBehaviour
    {
        public GameObject laneControllerPrefab;
        
        private Dictionary<int, LaneController> allLanes = new Dictionary<int, LaneController>();
        private Dictionary<int, float> defaultLaneY = new Dictionary<int, float>();
        private List<GimmickEvent> laneGimmicks = new List<GimmickEvent>();
        private HashSet<int> initiallyActiveLanes = new HashSet<int>();
        private RhythmState state;

        public void Initialize(ChartData chartData, RhythmState state)
        {
            this.state = state;
            ClearLanes();

            laneGimmicks = new List<GimmickEvent>(chartData.gimmicks);
            laneGimmicks.Sort((a, b) => a.time.CompareTo(b.time));

            HashSet<int> potentialLanes = new HashSet<int>();
            foreach (var l in chartData.lanes)
            {
                potentialLanes.Add(l.laneIndex);
                initiallyActiveLanes.Add(l.laneIndex);
                defaultLaneY[l.laneIndex] = l.defaultY;
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
                SetupLaneController(laneIndex);
            }

            // Ensure lanes are correctly activated/deactivated before GetCurrentKeyMapping is called
            UpdateLanes();
        }

        public Dictionary<Key, int> GetCurrentKeyMapping()
        {
            var mapping = new Dictionary<Key, int>();
            var settings = Core.GameSettingsManager.Instance.Settings.rhythm;

            foreach (var kvp in allLanes)
            {
                int laneIndex = kvp.Key;
                // activeSelf check is fine here since UpdateLanes was called in Initialize
                if (kvp.Value.gameObject.activeSelf)
                {
                    if (laneIndex >= 0 && laneIndex < settings.laneKeys.Count)
                    {
                        Key key = settings.laneKeys[laneIndex];
                        if (key != Key.None) mapping[key] = laneIndex;
                    }
                }
            }
            return mapping;
        }

        private void SetupLaneController(int laneIndex)
        {
            GameObject go = laneControllerPrefab != null ? 
                Instantiate(laneControllerPrefab, transform) : 
                new GameObject($"Lane_{laneIndex}");
            
            if (laneControllerPrefab == null) go.transform.SetParent(transform);

            LaneController controller = go.GetComponent<LaneController>() ?? go.AddComponent<LaneController>();
            
            float initialY = defaultLaneY.TryGetValue(laneIndex, out var dy) ? dy : 0f;
            controller.Initialize(laneIndex, new Vector2(Core.GameSettingsManager.Instance.Settings.rhythm.judgmentX, initialY));
            allLanes[laneIndex] = controller;
        }

        public void UpdateLanes()
        {
            var settings = Core.GameSettingsManager.Instance.Settings.rhythm;
            float currentTimeMs = state.currentTimeMs;

            int activeCount = initiallyActiveLanes.Count;
            foreach (var g in laneGimmicks)
            {
                if (g.time > currentTimeMs) break;
                if (g.type == GimmickType.LaneAdd) activeCount += (int)g.value;
                else if (g.type == GimmickType.LaneRemove) activeCount -= (int)g.value;
            }
            activeCount = Mathf.Clamp(activeCount, 1, 12);

            // Use JudgmentX from TestSession if in test mode, otherwise from settings
            float baseJudgmentX = EditorTestSession.IsTestMode ? 
                EditorTestSession.JudgmentX : 
                settings.judgmentX;

            float spacing = settings.laneSpacing; // Already in world units (0.7f)

            foreach (var kvp in allLanes)
            {
                int idx = kvp.Key;
                // A lane is active if it was in the chart OR its index is within the dynamic activeCount
                bool isActive = initiallyActiveLanes.Contains(idx) || idx < activeCount;
                kvp.Value.gameObject.SetActive(isActive);

                if (isActive)
                {
                    float targetX = baseJudgmentX;
                    float yOffset = 0f;

                    foreach (var g in laneGimmicks)
                    {
                        if (g.time > currentTimeMs) break;
                        if (g.targetLane != idx) continue;

                        // Gimmick values are now assumed to be in World units
                        if (g.type == GimmickType.LaneMoveX) targetX = g.value;
                        else if (g.type == GimmickType.LaneMoveY) yOffset = g.value;
                    }

                    float initialY;
                    if (defaultLaneY.TryGetValue(idx, out var dy))
                    {
                        // Chart defaultY is still in UI pixels, so we scale it
                        initialY = dy / 100f;
                    }
                    else
                    {
                        // Fallback layout for added lanes: 
                        // If index 0 exists, offset from it. Otherwise offset from 0.
                        float firstLaneY = defaultLaneY.TryGetValue(0, out var fdy) ? fdy / 100f : 0f;
                        initialY = firstLaneY - (idx * spacing);
                    }

                    float targetY = initialY + settings.judgmentY + yOffset;
                    kvp.Value.UpdateJudgmentPosition(new Vector2(targetX, targetY));
                }
            }
        }

        public void ClearLanes()
        {
            foreach (var lane in allLanes.Values) if (lane != null) Destroy(lane.gameObject);
            allLanes.Clear();
            defaultLaneY.Clear();
            initiallyActiveLanes.Clear();
            laneGimmicks.Clear();
        }

        public Dictionary<int, LaneController> GetActiveLanes()
        {
            var active = new Dictionary<int, LaneController>();
            foreach (var kvp in allLanes) if (kvp.Value.gameObject.activeSelf) active[kvp.Key] = kvp.Value;
            return active;
        }

        public LaneController GetLane(int index) => allLanes.TryGetValue(index, out var lane) ? lane : null;
    }
}
