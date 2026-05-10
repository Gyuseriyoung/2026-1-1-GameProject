using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace RhythmSystem
{
    public class EditorTimelineManager : MonoBehaviour
    {
        private EditorManager editorManager;

        public RectTransform timelineContent;
        public GameObject gridLinePrefab;
        public GameObject endLinePrefab;
        public RectTransform JudgeLine;

        private List<GameObject> activeGridLines = new List<GameObject>();
        private GameObject endLineInstance;

        public void Init(EditorManager manager)
        {
            editorManager = manager;
        }

        public void UpdateGrid()
        {
            foreach (var line in activeGridLines) Destroy(line);
            activeGridLines.Clear();

            if (endLineInstance != null) Destroy(endLineInstance);

            DrawVerticalLines();
            DrawHorizontalLines();
            DrawEndLine();
        }

        private void DrawEndLine()
        {
            float endTime = editorManager.currentChart.length / 1000f;
            if (endTime <= 0 && editorManager.audioSource.clip != null) endTime = editorManager.audioSource.clip.length;
            if (endTime <= 0) return;

            GameObject prefabToUse = endLinePrefab != null ? endLinePrefab : gridLinePrefab;
            if (prefabToUse == null) return;

            endLineInstance = Instantiate(prefabToUse, timelineContent);
            RectTransform rt = endLineInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-endTime * editorManager.currentScrollSpeed, 0);

            if (endLinePrefab == null)
            {
                Image img = endLineInstance.GetComponent<Image>();
                if (img != null) img.color = Color.red;
                rt.sizeDelta = new Vector2(5f, rt.sizeDelta.y);
            }
        }

        private void DrawVerticalLines()
        {
            var timingPoints = editorManager.currentChart.timingPoints.OrderBy(tp => tp.time).ToList();
            if (timingPoints.Count == 0) return;

            float chartDuration = editorManager.currentChart.length / 1000f;
            if (chartDuration <= 0 && editorManager.audioSource.clip != null) chartDuration = editorManager.audioSource.clip.length;
            float startOffsetSec = editorManager.currentChart.startOffset / 1000f;

            for (int i = 0; i < timingPoints.Count; i++)
            {
                var tp = timingPoints[i];
                float startTime = tp.time / 1000f;
                float endTime = (i + 1 < timingPoints.Count) ? (timingPoints[i + 1].time / 1000f) : chartDuration;

                if (i == 0) startTime = -startOffsetSec;

                float beatDuration = 60f / tp.bpm;
                int numerator = tp.meter > 0 ? tp.meter : 4;
                int denominator = tp.denominator > 0 ? tp.denominator : 4;
                float measureDuration = numerator / (float)denominator * 4f * beatDuration;
                float snapInterval = measureDuration / editorManager.snapDivisor;

                float t = tp.time / 1000f;
                
                if (i == 0)
                {
                    while (t > -startOffsetSec)
                    {
                        t -= snapInterval;
                        if (t < -startOffsetSec - 0.001f) break;
                        SpawnGridLine(t, 0, 1); 
                    }
                    t = tp.time / 1000f; 
                }

                int snapIndex = 0;
                while (t < endTime - 0.001f)
                {
                    SpawnGridLine(t, 0, snapIndex % editorManager.snapDivisor); 
                    t += snapInterval;
                    snapIndex++;
                }
            }
        }

        private void SpawnGridLine(float time, int measureIndex, int snapIndex)
        {
            GameObject line = Instantiate(gridLinePrefab, timelineContent);
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-time * editorManager.currentScrollSpeed, 0);
            ConfigureGridLineVisual(line, measureIndex, snapIndex);
            activeGridLines.Add(line);
        }

        private void ConfigureGridLineVisual(GameObject line, int measureIndex, int beatIndex)
        {
            Image img = line.GetComponent<Image>();
            RectTransform rt = line.GetComponent<RectTransform>();

            if (measureIndex == 0 && beatIndex == 0)
            {
                img.color = Color.yellow;
                rt.sizeDelta = new Vector2(7f, rt.sizeDelta.y);
                return;
            }

            if (beatIndex == 0) 
            {
                img.color = Color.white;
                rt.sizeDelta = new Vector2(3f, rt.sizeDelta.y);
            }
            else 
            {
                img.color = new Color(1, 1, 1, 0.5f);
                rt.sizeDelta = new Vector2(1f, rt.sizeDelta.y);
            }
        }

        private void DrawHorizontalLines()
        {
            float chartDuration = editorManager.currentChart.length / 1000f;
            if (chartDuration <= 0 && editorManager.audioSource.clip != null) chartDuration = editorManager.audioSource.clip.length;

            var changeTimes = editorManager.currentChart.gimmicks
                .Where(g => g.type == GimmickType.LaneAdd || g.type == GimmickType.LaneRemove)
                .Select(g => g.time).Distinct().OrderBy(t => t).ToList();

            if (!changeTimes.Contains(0)) changeTimes.Insert(0, 0);
            
            float chartDurationMs = chartDuration * 1000f;
            
            for (int i = 0; i < changeTimes.Count; i++)
            {
                float startTimeMs = changeTimes[i];
                float endTimeMs = (i + 1 < changeTimes.Count) ? changeTimes[i + 1] : chartDurationMs;
                if (endTimeMs <= startTimeMs) continue;

                var activeIndices = GetActiveLaneIndicesAt(startTimeMs + 0.1f);
                if (activeIndices.Count == 0) continue;

                float spacing = RhythmSettingsManager.Settings.laneSpacing * 100f;
                float totalHeight = (activeIndices.Count - 1) * spacing;
                float startY = totalHeight / 2f;

                for (int j = 0; j < activeIndices.Count; j++)
                {
                    float targetY = startY - (j * spacing);
                    DrawLaneSegment(targetY, startTimeMs / 1000f, endTimeMs / 1000f);
                }
            }
        }

        private void DrawLaneSegment(float yPos, float startSec, float endSec)
        {
            if (endSec <= startSec) return;

            GameObject line = Instantiate(gridLinePrefab, timelineContent);
            RectTransform rt = line.GetComponent<RectTransform>();
            
            float width = (endSec - startSec) * editorManager.currentScrollSpeed;
            float centerX = -(startSec + (endSec - startSec) / 2f) * editorManager.currentScrollSpeed;

            rt.anchoredPosition = new Vector2(centerX, yPos);
            rt.sizeDelta = new Vector2(width, 2f);
            
            line.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
            activeGridLines.Add(line);
        }

        public void SyncTimeline()
        {
            float targetX = editorManager.JudgeLineX + editorManager.EditorTime * editorManager.currentScrollSpeed;
            timelineContent.anchoredPosition = new Vector2(targetX, timelineContent.anchoredPosition.y);

            if (JudgeLine != null)
                JudgeLine.anchoredPosition = new Vector2(editorManager.JudgeLineX, -90);
        }

        public float GetTimeFromMouse(Vector2 mousePos)
        {
            RectTransform parentRT = timelineContent.parent as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, mousePos, null, out Vector2 localPos)) return editorManager.EditorTime;
            return editorManager.EditorTime + (editorManager.JudgeLineX - localPos.x) / editorManager.currentScrollSpeed;
        }

        public int GetLaneFromMouse(Vector2 mousePos)
        {
            float timeSec = GetTimeFromMouse(mousePos);
            float timeMs = timeSec * 1000f;

            var activeIndices = GetActiveLaneIndicesAt(timeMs);
            if (activeIndices.Count == 0) return -1;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(timelineContent, mousePos, null, out Vector2 localPos)) return -1;

            int bestLane = -1;
            float minDistance = float.MaxValue;

            foreach (int laneIdx in activeIndices)
            {
                float laneY = GetLaneYAt(laneIdx, timeMs);
                float dist = Mathf.Abs(localPos.y - laneY);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestLane = laneIdx;
                }
            }

            return (minDistance < 50f) ? bestLane : -1;
        }

        public List<int> GetActiveLaneIndicesAt(float timeMs)
        {
            int count = editorManager.currentChart.lanes.Count;
            
            var countGimmicks = editorManager.currentChart.gimmicks
                .Where(g => g.type == GimmickType.LaneAdd || g.type == GimmickType.LaneRemove)
                .OrderBy(g => g.time).ToList();

            foreach (var g in countGimmicks)
            {
                if (g.time > timeMs) break;
                if (g.type == GimmickType.LaneAdd) count += (int)g.value;
                else if (g.type == GimmickType.LaneRemove) count -= (int)g.value;
            }

            count = Mathf.Clamp(count, 1, 12); 
            var indices = new List<int>();
            for (int i = 0; i < count; i++) indices.Add(i);
            return indices;
        }

        public bool IsLaneActiveAt(int laneIndex, float timeMs)
        {
            return GetActiveLaneIndicesAt(timeMs).Contains(laneIndex);
        }

        public float GetLaneYAt(int laneIndex, float timeMs)
        {
            var activeIndices = GetActiveLaneIndicesAt(timeMs);
            if (!activeIndices.Contains(laneIndex)) return 0;

            float spacing = RhythmSettingsManager.Settings.laneSpacing * 100f;
            float totalHeight = (activeIndices.Count - 1) * spacing;
            float startY = totalHeight / 2f;

            int order = activeIndices.IndexOf(laneIndex);
            return startY - (order * spacing);
        }
    }
}
