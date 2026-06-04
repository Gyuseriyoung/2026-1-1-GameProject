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
        public NoteType currentSelectedNoteType = NoteType.Tap;
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
        public int currentSelectedSoundIndex = -1; // -1 means no sound assigned by default

        [Header("Merge Integration")]
        public MergeObjectData mergeObjectData;
        public int currentSelectedMergeType = 0;
        public int currentSelectedMergeIndex = 0;

        public const string musicDirectory = "Assets/Resources/Musics/";

        public float EditorTime => editorTime;
        private float editorTime = 0f; // Logical Grid Time
        private float globalEditorTimerMs = 0f; // Physical Audio Time (ms)
        private readonly StopTimeline stopTimeline = new StopTimeline();

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
            LoadProjectState();
        }

        public void LoadProjectState(string chartName = null)
        {
            var settings = Core.GameSettingsManager.Instance.Settings;
            var editorSettings = settings.editor;

            if (EditorTestSession.IsReturningFromTest && EditorTestSession.CurrentChart != null)
            {
                currentChart = EditorTestSession.CurrentChart;
                currentBPM = EditorTestSession.LastBPM;
                snapDivisor = EditorTestSession.LastSnapDivisor;
                currentMode = EditorTestSession.LastMode;
                
                // Session stores world units, convert to editor pixels
                currentScrollSpeed = EditorTestSession.ScrollSpeed * 100f;
                editorTime = EditorTestSession.StartSeekTime;
                JudgeLineX = EditorTestSession.JudgmentX * 100f;
                
                if (!string.IsNullOrEmpty(currentChart.metadata.audioFileName)) 
                    LoadMusic(currentChart.metadata.audioFileName);

                EditorTestSession.IsReturningFromTest = false;
            }
            else
            {
                if (!string.IsNullOrEmpty(chartName))
                {
                    var loadedChart = ChartIO.LoadFromFile(chartName);
                    if (loadedChart != null) currentChart = loadedChart;
                }

                // Load natively in pixels from editor settings
                currentScrollSpeed = editorSettings.scrollSpeed;
                JudgeLineX = editorSettings.judgmentX;
                snapDivisor = editorSettings.snapDivisor;

                if (currentChart.timingPoints.Count == 0)
                    currentChart.timingPoints.Add(new TimingPoint { time = 0, bpm = currentBPM, meter = 4 });
                
                if (currentChart.lanes.Count == 0)
                    currentChart.lanes.Add(new LaneConfig { laneIndex = 0, defaultY = 0 });

                if (currentChart.timingPoints.Count > 0) 
                    currentBPM = currentChart.timingPoints[0].bpm;

                if (!string.IsNullOrEmpty(currentChart.metadata.audioFileName)) 
                    LoadMusic(currentChart.metadata.audioFileName);
            }

            RecalculateStopMappings();
            globalEditorTimerMs = GetAudioTime(editorTime * 1000f);

            RefreshAllVisuals();
            editorUIController.RefreshUI();
            if (!string.IsNullOrEmpty(chartName)) editorUIController.RefreshMusicList();
        }

        public void RecalculateStopMappings()
        {
            stopTimeline.Rebuild(currentChart.gimmicks);
        }

        public float GetLogicalTime(float audioTimeMs)
        {
            return stopTimeline.GetLogicalTime(audioTimeMs);
        }

        public float GetAudioTime(float logicalTimeMs)
        {
            return stopTimeline.GetAudioTime(logicalTimeMs);
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

            globalEditorTimerMs += Time.deltaTime * 1000f;
            editorTime = GetLogicalTime(globalEditorTimerMs) / 1000f;

            float targetAudioTime = (globalEditorTimerMs + currentChart.musicOffset) / 1000f;

            if (audioSource != null && audioSource.clip != null)
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
                globalEditorTimerMs = GetAudioTime(editorTime * 1000f);
                float targetAudioTime = (globalEditorTimerMs + currentChart.musicOffset) / 1000f;
                if (audioSource != null && targetAudioTime >= 0 && audioSource.clip != null && targetAudioTime < audioSource.clip.length)
                {
                    audioSource.time = targetAudioTime;
                    audioSource.Play();
                }
            }
            else
            {
                if (audioSource != null) audioSource.Pause();
            }
            timelineManager.SyncTimeline();
        }

        public void StopPlayback()
        {
            isPlaying = false;
            if (audioSource != null) audioSource.Stop();
            editorTime = -currentChart.startOffset / 1000f;
            globalEditorTimerMs = GetAudioTime(editorTime * 1000f);
            timelineManager.SyncTimeline();
        }

        public void SetPlaybackTime(float time)
        {
            GetNavigationBounds(out float minTime, out float maxTime);
            
            editorTime = Mathf.Clamp(time, minTime, maxTime);
            globalEditorTimerMs = GetAudioTime(editorTime * 1000f);
            float targetAudioTime = (globalEditorTimerMs + currentChart.musicOffset) / 1000f;
            
            if (audioSource != null && audioSource.clip != null)
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

            RecalculateStopMappings();
            AutoDistributeLanes();
            timelineManager.UpdateGrid();
            noteManager.UpdateNoteVisuals();
            noteManager.UpdateGimmickVisuals();
            timelineManager.SyncTimeline();
        }

        private void AutoDistributeLanes()
        {
            float spacing = Core.GameSettingsManager.Instance.Settings.editor.laneSpacing; 
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
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Musics/" + fileName);
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
            return Directory.GetFiles(ChartIO.DefaultChartDirectory)
                    .Where(f => !f.EndsWith(".meta"))
                    .Select(f => Path.GetFileName(f).Replace(".json", ""))
                    .ToList();
        }

        public void LoadChart(string fileName)
        {
            LoadProjectState(fileName);
        }

        public void AddLane() 
        { 
            int nextIndex = currentChart.lanes.Count > 0 ? currentChart.lanes.Max(l => l.laneIndex) + 1 : 0;
            currentChart.lanes.Add(new LaneConfig { laneIndex = nextIndex, defaultY = 0 }); 
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

        public void UpdateScrollSpeed(float uiValue)
        {
            var editorSettings = Core.GameSettingsManager.Instance.Settings.editor;
            editorSettings.scrollSpeed = uiValue;
            currentScrollSpeed = uiValue;
            Core.GameSettingsManager.Instance.SaveSettings();
            RefreshAllVisuals();
            editorUIController.RefreshScrollField();
        }

        public void UpdateJudgeLineX(float uiValue)
        {
            var editorSettings = Core.GameSettingsManager.Instance.Settings.editor;
            editorSettings.judgmentX = uiValue;
            JudgeLineX = uiValue;
            Core.GameSettingsManager.Instance.SaveSettings();
            RefreshAllVisuals();
            editorUIController.RefreshJudgeLineXField();
        }

        public void StartTestPlay()
        {
            var editorSettings = Core.GameSettingsManager.Instance.Settings.editor;

            EditorTestSession.IsTestMode = true;
            EditorTestSession.IsReturningFromTest = false;
            EditorTestSession.CurrentChart = currentChart;
            EditorTestSession.MergeObjectData = mergeObjectData;
            EditorTestSession.MusicFileName = currentChart.metadata.audioFileName;
            EditorTestSession.StartSeekTime = editorTime;
            
            // Pass world unit equivalents to test session
            EditorTestSession.ScrollSpeed = editorSettings.scrollSpeed / 100f;
            EditorTestSession.JudgmentX = editorSettings.judgmentX / 100f;

            EditorTestSession.LastBPM = currentBPM;
            EditorTestSession.LastSnapDivisor = snapDivisor;
            EditorTestSession.LastMode = currentMode;
            EditorTestSession.ReturnSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Save snap divisor for persistence
            editorSettings.snapDivisor = snapDivisor;
            Core.GameSettingsManager.Instance.SaveSettings();

            ChartIO.SaveToFile("__EditorTemp", currentChart);

            SceneTransitionManager.Instance.LoadScene("Game Debug Scene");
        }
    }
}
