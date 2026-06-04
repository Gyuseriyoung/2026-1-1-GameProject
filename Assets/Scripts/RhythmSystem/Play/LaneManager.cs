using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.Audio;

namespace RhythmSystem.Play
{
    /// <summary>
    /// 리듬 게임의 레인(Lane)들을 관리하며, 노트 타격음 재생 등을 담당합니다.
    /// </summary>
    public class LaneManager : MonoBehaviour
    {
        public GameObject laneControllerPrefab;

        private Dictionary<int, LaneController> allLanes = new Dictionary<int, LaneController>();
        private Dictionary<int, float> defaultLaneY = new Dictionary<int, float>();
        private List<GimmickEvent> laneGimmicks = new List<GimmickEvent>();
        private List<AudioClip> soundBankClips = new List<AudioClip>();
        private HashSet<int> initiallyActiveLanes = new HashSet<int>();
        private RhythmState state;

        public void Initialize(ChartData chartData, RhythmState state)
        {
            this.state = state;
            ClearLanes();

            laneGimmicks = chartData.gimmicks
                .Where(g => g.type == GimmickType.LaneAdd || g.type == GimmickType.LaneRemove || 
                            g.type == GimmickType.LaneMoveX || g.type == GimmickType.LaneMoveY)
                .OrderBy(g => g.time)
                .ToList();

            // 사운드 뱅크 로드
            soundBankClips.Clear();
            foreach (var soundName in chartData.soundBank)
            {
                string soundPath = soundName;
                int lastDot = soundName.LastIndexOf('.');
                if (lastDot > 0) soundPath = soundName.Substring(0, lastDot);

                AudioClip clip = Resources.Load<AudioClip>("Sound/" + soundPath);
                if (clip != null)
                {
                    soundBankClips.Add(clip);
                }
                else
                {
                    Debug.LogError($"[LaneManager] Failed to load sound: Sound/{soundPath}");
                    soundBankClips.Add(null);
                }
            }

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

            UpdateLanes();
        }

        public Dictionary<Key, int> GetCurrentKeyMapping()
        {
            var mapping = new Dictionary<Key, int>();
            var settings = Core.GameSettingsManager.Instance.Settings.rhythm;

            foreach (var kvp in allLanes)
            {
                int laneIndex = kvp.Key;
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

            float baseJudgmentX = EditorTestSession.IsTestMode ? 
                EditorTestSession.JudgmentX : 
                settings.judgmentX;

            float spacing = settings.laneSpacing;

            foreach (var kvp in allLanes)
            {
                int idx = kvp.Key;
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

                        if (g.type == GimmickType.LaneMoveX) targetX = g.value;
                        else if (g.type == GimmickType.LaneMoveY) yOffset = g.value;
                    }

                    float initialY;
                    if (defaultLaneY.TryGetValue(idx, out var dy))
                    {
                        initialY = dy / 100f;
                    }
                    else
                    {
                        float firstLaneY = defaultLaneY.TryGetValue(0, out var fdy) ? fdy / 100f : 0f;
                        initialY = firstLaneY - (idx * spacing);
                    }

                    float targetY = initialY + settings.judgmentY + yOffset;
                    kvp.Value.UpdateJudgmentPosition(new Vector2(targetX, targetY));
                }
            }
        }

        /// <summary>
        /// 노트 타격음을 재생합니다. 
        /// 통합 AudioManager를 사용하여 풀링된 소스를 통해 재생됩니다.
        /// </summary>
        public void PlayNoteSound(NoteData note)
        {
            if (note.soundIndex >= 0 && note.soundIndex < soundBankClips.Count)
            {
                AudioClip clip = soundBankClips[note.soundIndex];
                if (clip != null && AudioManager.Instance != null)
                {
                    // 통합 오디오 매니저를 통해 효과음 재생
                    AudioManager.Instance.PlaySFX(clip);
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
            soundBankClips.Clear();
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
