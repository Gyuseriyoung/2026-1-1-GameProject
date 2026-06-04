using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using RhythmSystem;

namespace RhythmSystem.Play
{
    public class RhythmGameManager : MonoBehaviour
    {
        [Header("Components")]
        public PlayNoteSpawner playNoteSpawner;
        public RhythmOptionsUIManager optionsUIManager;
        public MergeManager mergeManager;
        public PlayerInput playerInput;
        public AudioSource audioSource;

        [Header("New Decoupled Components")]
        public RhythmClock clock;
        public RhythmInputProcessor inputProcessor;
        public RhythmJudger judger;
        public LaneManager laneManager;
        public RhythmVisualEffectManager visualManager;

        [Header("Settings")]
        public string chartToLoad = "TEST";

        private ChartData currentChart;
        private RhythmState gameState = new RhythmState();
        private List<GimmickEvent> scrollSpeedGimmicks = new List<GimmickEvent>();
        private readonly StopTimeline stopTimeline = new StopTimeline();
        private float globalTimerMs = 0;

        void Start()
        {
            InitializeComponents();

            // Priority: 1. Editor Test, 2. Cooking Session, 3. Default
            if (EditorTestSession.IsTestMode && EditorTestSession.CurrentChart != null)
            {
                currentChart = EditorTestSession.CurrentChart;
                InitializeGame(EditorTestSession.StartSeekTime * 1000f);
            }
            else if (CookingGame.CookingSession.CurrentCustomer != null && CookingGame.CookingSession.CurrentCustomer.chartJson != null)
            {
                chartToLoad = CookingGame.CookingSession.CurrentCustomer.chartJson.name;
                LoadAndStartGame();
            }
            else
            {
                LoadAndStartGame();
            }
        }

        private void InitializeComponents()
        {
            if (clock == null) clock = gameObject.AddComponent<RhythmClock>();
            if (inputProcessor == null) inputProcessor = gameObject.AddComponent<RhythmInputProcessor>();
            if (judger == null) judger = gameObject.AddComponent<RhythmJudger>();
            if (laneManager == null) laneManager = GetComponentInChildren<LaneManager>(); 
            if (visualManager == null) visualManager = gameObject.AddComponent<RhythmVisualEffectManager>();

            inputProcessor.Initialize(playerInput);
            visualManager.Initialize();
            
            // Subscribe to pause event from input
            RhythmEvents.OnGamePause += PauseGame;
        }

        private void OnDestroy()
        {
            RhythmEvents.ClearAll();
        }

        private void OnChartEnd()
        {
            gameState.isPlaying = false;
            
            bool success = false;
            if (CookingGame.CookingSession.CurrentCustomer != null && mergeManager != null)
            {
                // Check if current items EXACTLY match the orders at the end
                success = mergeManager.IsOrderExactMatch(CookingGame.CookingSession.CurrentCustomer.orders);
                Debug.Log($"Chart Completed! Success: {success}");
            }
            
            ReturnToDialogue(success);
        }

        public void LoadAndStartGame()
        {
            currentChart = ChartIO.LoadFromFile(chartToLoad);
            if (currentChart != null)
            {
                InitializeGame(-currentChart.startOffset);
            }
            else
            {
                Debug.LogError($"Failed to load chart: {chartToLoad}");
            }
        }

        private void InitializeGame(float startTimeMs)
        {
            gameState.currentTimeMs = startTimeMs;
            gameState.isPlaying = true;
            gameState.combo = 0;
            gameState.scrollSpeedMultiplier = 1f;
            
            stopTimeline.Rebuild(currentChart.gimmicks);

            // In our system, startTimeMs is already the logical start time.
            // We need to calculate the initial globalTimerMs.
            // Since lead-in time (negative) usually doesn't have stops:
            globalTimerMs = startTimeMs;

            // Reset pause state via public method to ensure UI sync
            gameState.isPaused = true; 
            PauseGame(false);

            if (audioSource == null) audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            // AudioManager의 BGM 그룹에 연결하여 전역 설정을 따르도록 함
            if (AudioManager.Instance != null && AudioManager.Instance.bgmGroup != null)
            {
                audioSource.outputAudioMixerGroup = AudioManager.Instance.bgmGroup;
            }

            SetupAudio();

            scrollSpeedGimmicks = currentChart.gimmicks
                .Where(g => g.type == GimmickType.ScrollSpeed)
                .OrderBy(g => g.time)
                .ToList();

            laneManager.Initialize(currentChart, gameState);
            playNoteSpawner.Initialize(laneManager, gameState);
            playNoteSpawner.SpawnNotes(currentChart);

            judger.Initialize(gameState, playNoteSpawner, new List<IRhythmModifier>());

            // Initial input mapping
            inputProcessor.UpdateMapping(laneManager.GetCurrentKeyMapping());

            RhythmEvents.OnGameStart?.Invoke();
        }

        private float GetLogicalTime(float audioTimeMs)
        {
            return stopTimeline.GetLogicalTime(audioTimeMs);
        }

        private void SetupAudio()
        {
            string musicName = currentChart.metadata.audioFileName;
            if (!string.IsNullOrEmpty(musicName))
            {
                string pathWithoutExtension = musicName;
                int lastDotIndex = musicName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    pathWithoutExtension = musicName.Substring(0, lastDotIndex);
                }

                AudioClip clip = Resources.Load<AudioClip>("Musics/" + pathWithoutExtension);
                if (clip != null)
                {
                    audioSource.clip = clip;
                }
            }
            clock.Initialize(gameState, audioSource, currentChart.musicOffset);
        }

        private int lastActiveLaneCount = -1;

        void Update()
        {
            if (!gameState.isPlaying || gameState.isPaused) return;

            // Update Global Timer
            globalTimerMs += Time.deltaTime * 1000f;

            // If audio is playing and synced, we can potentially sync globalTimerMs to audioSource.time
            // But for now, let's keep globalTimerMs as the source of truth for "Real Time"
            // and derive gameState.currentTimeMs (Logical Time) from it.
            
            gameState.currentTimeMs = GetLogicalTime(globalTimerMs);

            clock.SyncUpdate(globalTimerMs);
            laneManager.UpdateLanes();

            // Handle Scroll Speed Gimmicks
            float newMultiplier = 1f;
            foreach (var g in scrollSpeedGimmicks)
            {
                if (g.time > gameState.currentTimeMs) break;
                newMultiplier = g.value;
            }
            gameState.scrollSpeedMultiplier = newMultiplier;

            // Check for Chart End
            if (currentChart != null && gameState.currentTimeMs >= currentChart.length)
            {
                OnChartEnd();
                return;
            }

            // Refresh input mapping if lane count changed via gimmicks
            var activeLanes = laneManager.GetActiveLanes();
            if (activeLanes.Count != lastActiveLaneCount)
            {
                inputProcessor.UpdateMapping(laneManager.GetCurrentKeyMapping());
                lastActiveLaneCount = activeLanes.Count;
            }

            judger.UpdateLogic();
            playNoteSpawner.UpdateNotes(gameState.scrollSpeedMultiplier);
        }

        public bool IsPaused => gameState.isPaused;

        public void PauseGame(bool pause)
        {
            if (gameState.isPaused == pause) return;

            gameState.isPaused = pause;
            inputProcessor.SetPaused(pause);
            
            if (audioSource != null && audioSource.clip != null)
            {
                if (gameState.isPaused) audioSource.Pause();
                else audioSource.UnPause();
            }

            if (optionsUIManager != null) optionsUIManager.SetPanelActive(pause);
        }

        public void RestartGame()
        {
            judger.Clear();
            playNoteSpawner.ClearNotes();
            laneManager.ClearLanes();
            if (mergeManager != null) mergeManager.ClearAllObjects();

            if (EditorTestSession.IsTestMode)
                InitializeGame(EditorTestSession.StartSeekTime * 1000f);
            else
                LoadAndStartGame();
        }

        public void QuitGame()
        {
            if (EditorTestSession.IsTestMode) ReturnToEditor();
            else if (CookingGame.CookingSession.CurrentCustomer != null) ReturnToDialogue(false);
            else ReturnToTitle();
        }

        private void ReturnToTitle() => SceneTransitionManager.Instance.LoadScene("TitleScene");

        private void ReturnToEditor()
        {
            EditorTestSession.IsTestMode = false;
            EditorTestSession.IsReturningFromTest = true;
            SceneTransitionManager.Instance.LoadScene(EditorTestSession.ReturnSceneName);
        }

        public void ReturnToDialogue(bool success)
        {
            CookingGame.CookingSession.LastGameSuccess = success;
            CookingGame.CookingSession.IsReturningFromResult = true;
            SceneTransitionManager.Instance.LoadScene("Customer Debug Scene");
        }

        public void RefreshInputMapping()
        {
            if (laneManager != null && inputProcessor != null)
            {
                inputProcessor.UpdateMapping(laneManager.GetCurrentKeyMapping());
            }
        }

        public float GetCurrentTimeMs() => gameState.currentTimeMs;
    }
}
