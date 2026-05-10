using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

namespace RhythmSystem
{
    public enum EditorMode
    {
        View,   // Navigation only
        Place,  // Note placement/removal
        Select, // Selecting/Moving notes
        Gimmick // Gimmick placement
    }

    public class EditorManager : MonoBehaviour
    {
        public EditorUIController editorUIController;

        [Header("Sub-Managers")]
        public EditorTimelineManager timelineManager;
        public EditorNoteManager noteManager;
        public EditorInputHandler inputHandler;

        [Header("Editor Mode")]
        public EditorMode currentMode = EditorMode.Place;
        public GimmickType currentSelectedGimmickType = GimmickType.LaneMoveY;

        [Header("Data")]
        public ChartData currentChart = new ChartData();

        [Header("Audio")]
        public AudioSource audioSource;
        public bool isPlaying = false;

        [Header("Editor Settings")]
        public float currentBPM = 120f;
        public float currentScrollSpeed = 500f; // Pixels per second
        public int snapDivisor = 4;
        public float JudgeLineX = 400f; 

        [Header("Merge Integration")]
        public MergeObjectData mergeObjectData;
        public int currentSelectedMergeType = 0;
        public int currentSelectedMergeIndex = 0;

        [Header("Path Settings")]
        public string musicDirectory = "Assets/Musics";

        public float EditorTime => editorTime;
        private float editorTime = 0f;

        [Header("UI & Visualization")]
        public RectTransform timelineContent; 

        private void Awake()
        {
            if (timelineManager == null) timelineManager = GetComponentInChildren<EditorTimelineManager>();
            if (noteManager == null) noteManager = GetComponentInChildren<EditorNoteManager>();
            if (inputHandler == null) inputHandler = GetComponentInChildren<EditorInputHandler>();

            timelineManager.Init(this);
            noteManager.Init(this);
            inputHandler.Init(this);
        }

        private void OnEnable()
        {
            inputHandler.Enable();
        }

        private void OnDisable()
        {
            inputHandler.Disable();
        }

        private void Start()
        {
            if (EditorTestSession.IsReturningFromTest && EditorTestSession.CurrentChart != null)
            {
                currentChart = EditorTestSession.CurrentChart;
                currentBPM = EditorTestSession.LastBPM;
                snapDivisor = EditorTestSession.LastSnapDivisor;
                currentMode = EditorTestSession.LastMode;
                currentScrollSpeed = EditorTestSession.ScrollSpeed;
                editorTime = EditorTestSession.StartSeekTime;
                
                if (!string.IsNullOrEmpty(currentChart.metadata.audioFileName)) 
                    LoadMusic(currentChart.metadata.audioFileName);

                EditorTestSession.IsReturningFromTest = false;
            }
            else
            {
                currentScrollSpeed = RhythmSettingsManager.Settings.scrollSpeed;

                if (currentChart.timingPoints.Count == 0)
                    currentChart.timingPoints.Add(new TimingPoint { time = 0, bpm = currentBPM, meter = 4 });
                
                if (currentChart.lanes.Count == 0)
                    currentChart.lanes.Add(new LaneConfig { laneIndex = 0, defaultY = 0, keyBinding = KeyCode.Space });
            }

            RefreshAllVisuals();
            editorUIController.RefreshUI();
        }

        private void Update()
        {
            inputHandler.HandleInputs();
            UpdatePlayback();
        }

        public void ChangeMode(EditorMode newMode)
        {
            currentMode = newMode;
            Debug.Log($"Editor Mode changed to: {newMode}");
            if (newMode != EditorMode.Select) noteManager.ClearSelection();
        }

        private void UpdatePlayback()
        {
            if (!isPlaying) return;

            editorTime += Time.deltaTime;
            float targetAudioTime = editorTime + (currentChart.musicOffset / 1000f);

            if (audioSource.clip != null)
            {
                if (targetAudioTime >= 0 && targetAudioTime < audioSource.clip.length)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.time = targetAudioTime;
                        audioSource.Play();
                    }
                    else if (Mathf.Abs(targetAudioTime - audioSource.time) > 0.05f)
                    {
                        audioSource.time = targetAudioTime;
                    }
                }
                else
                {
                    if (audioSource.isPlaying) audioSource.Stop();
                    if (targetAudioTime >= audioSource.clip.length) isPlaying = false;
                }
            }
            
            timelineManager.SyncTimeline();
        }

        public void PlayPause()
        {
            isPlaying = !isPlaying;
            if (isPlaying)
            {
                float targetAudioTime = editorTime + (currentChart.musicOffset / 1000f);
                if (targetAudioTime >= 0 && audioSource.clip != null && targetAudioTime < audioSource.clip.length)
                {
                    audioSource.time = targetAudioTime;
                    audioSource.Play();
                }
            }
            else
            {
                audioSource.Pause();
            }
            timelineManager.SyncTimeline();
        }

        public void StopPlayback()
        {
            isPlaying = false;
            audioSource.Stop();
            editorTime = -currentChart.startOffset / 1000f;
            timelineManager.SyncTimeline();
        }

        public void SetPlaybackTime(float time)
        {
            GetNavigationBounds(out float minTime, out float maxTime);
            
            editorTime = Mathf.Clamp(time, minTime, maxTime);
            float targetAudioTime = editorTime + (currentChart.musicOffset / 1000f);
            
            if (audioSource.clip != null)
            {
                if (targetAudioTime >= 0 && targetAudioTime < audioSource.clip.length)
                {
                    audioSource.time = targetAudioTime;
                    if (isPlaying) audioSource.Play();
                }
                else
                {
                    audioSource.Stop();
                }
            }
            
            timelineManager.SyncTimeline();
        }

        public void GetNavigationBounds(out float min, out float max)
        {
            min = -currentChart.startOffset / 1000f;
            
            if (currentChart.timingPoints.Count > 0)
            {
                TimingPoint tp = currentChart.timingPoints[0];
                float beatDuration = 60f / tp.bpm;
                float measureDuration = (tp.meter / (float)tp.denominator) * 4f * beatDuration;
                int startMeasure = Mathf.FloorToInt(min / measureDuration);
                min = startMeasure * measureDuration;
            }

            max = currentChart.length / 1000f;
            if (max <= 0 && audioSource.clip != null) max = audioSource.clip.length;
        }

        public void RefreshAllVisuals()
        {
            if (currentChart.length <= 0 && audioSource.clip != null)
                currentChart.length = audioSource.clip.length * 1000f;

            AutoDistributeLanes();
            timelineManager.UpdateGrid();
            noteManager.UpdateNoteVisuals();
            noteManager.UpdateGimmickVisuals();
            timelineManager.SyncTimeline();
        }

        private void AutoDistributeLanes()
        {
            float spacing = 60f;
            int count = currentChart.lanes.Count;
            if (count == 0) return;

            var sortedLanes = currentChart.lanes.OrderBy(l => l.laneIndex).ToList();
            float startY = (count - 1) * spacing / 2f;

            for (int i = 0; i < count; i++)
            {
                sortedLanes[i].defaultY = startY - i * spacing;
            }
        }

        public float GetSnappedTime(float rawTime)
        {
            if (currentChart.timingPoints.Count == 0) return rawTime;

            TimingPoint tp = currentChart.timingPoints[0];
            float beatDuration = 60f / tp.bpm;
            int numerator = tp.meter > 0 ? tp.meter : 4;
            int denominator = tp.denominator > 0 ? tp.denominator : 4;
            
            float measureDuration = numerator / (float)denominator * 4f * beatDuration;

            float snapInterval = measureDuration / snapDivisor;
            return Mathf.Round(rawTime / snapInterval) * snapInterval;
        }

        public void QuickSave() => SaveChart(currentChart.metadata.title);

        public void SaveChart(string fileName)
        {
            // Update user settings
            RhythmSettingsManager.Settings.scrollSpeed = currentScrollSpeed;
            RhythmSettingsManager.SaveSettings();

            ChartIO.SaveToFile(string.IsNullOrEmpty(fileName) ? "NewChart" : fileName, currentChart);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public List<string> GetMusicFileList()
        {
            if (!Directory.Exists(musicDirectory)) return new List<string>();
            return Directory.GetFiles(musicDirectory)
                .Where(f => !f.EndsWith(".meta"))
                .Select(Path.GetFileName).ToList();
        }

        public void LoadMusic(string fileName)
        {
            currentChart.metadata.audioFileName = fileName;
#if UNITY_EDITOR
            string path = Path.Combine(musicDirectory, fileName).Replace("\\", "/");
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                audioSource.clip = clip;
                RefreshAllVisuals();
            }
#endif
        }

        public List<string> GetChartFileList()
        {
            if (!Directory.Exists(ChartIO.DefaultChartDirectory)) return new List<string>();
            return Directory.GetFiles(ChartIO.DefaultChartDirectory, "*.json")
                .Concat(Directory.GetFiles(ChartIO.DefaultChartDirectory, "*.osu"))
                .Select(Path.GetFileNameWithoutExtension).Distinct().ToList();
        }

        public void LoadChart(string fileName)
        {
            var chart = ChartIO.LoadFromFile(fileName);
            if (chart != null)
            {
                currentChart = chart;
                if (currentChart.timingPoints.Count > 0) currentBPM = currentChart.timingPoints[0].bpm;
                if (!string.IsNullOrEmpty(currentChart.metadata.audioFileName)) LoadMusic(currentChart.metadata.audioFileName);
                RefreshAllVisuals();
            }
        }

        public void AddLane() 
        { 
            int nextIndex = currentChart.lanes.Count > 0 ? currentChart.lanes.Max(l => l.laneIndex) + 1 : 0;
            currentChart.lanes.Add(new LaneConfig { laneIndex = nextIndex, defaultY = 0, keyBinding = KeyCode.None }); 
            RefreshAllVisuals(); 
        }

        public void RemoveLane() 
        { 
            if (currentChart.lanes.Count > 1) 
            { 
                currentChart.lanes.RemoveAt(currentChart.lanes.Count - 1); 
                RefreshAllVisuals(); 
            } 
        }

        public void UpdateScrollSpeed(float newSpeed)
        {
            currentScrollSpeed = newSpeed;
            RefreshAllVisuals();
            editorUIController.RefreshScrollField();
        }

        public void UpdateJudgeLineX(float newX)
        {
            JudgeLineX = newX;
            RefreshAllVisuals();
            editorUIController.RefreshJudgeLineXField();
        }

        public void StartTestPlay()
        {
            EditorTestSession.IsTestMode = true;
            EditorTestSession.IsReturningFromTest = false;
            EditorTestSession.CurrentChart = currentChart;
            EditorTestSession.MergeObjectData = mergeObjectData;
            EditorTestSession.MusicFileName = currentChart.metadata.audioFileName;
            EditorTestSession.StartSeekTime = editorTime;
            EditorTestSession.ScrollSpeed = currentScrollSpeed;
            EditorTestSession.LastBPM = currentBPM;
            EditorTestSession.LastSnapDivisor = snapDivisor;
            EditorTestSession.LastMode = currentMode;
            EditorTestSession.JudgmentX = JudgeLineX / 100f; 
            EditorTestSession.ReturnSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            ChartIO.SaveToFile("__EditorTemp", currentChart);

            UnityEngine.SceneManagement.SceneManager.LoadScene("Game Debug Scene");
        }
    }
}
