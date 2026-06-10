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

        // --- Clock & Input Integrated State ---
        private float musicOffset;
        private bool isMusicStarted;
        private float internalTimerMs = 0f;
        private Dictionary<Key, int> keyToLaneMap = new Dictionary<Key, int>();
        private int lastActiveLaneCount = -1;

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
            if (judger == null) judger = gameObject.AddComponent<RhythmJudger>();
            if (laneManager == null) laneManager = GetComponentInChildren<LaneManager>(); 
            if (visualManager == null) visualManager = gameObject.AddComponent<RhythmVisualEffectManager>();

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

            globalTimerMs = startTimeMs;

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
            UpdateInputMapping(laneManager.GetCurrentKeyMapping());

            // START CLOCK (Only the clock starts the music)
            gameState.isPaused = false;
            StartMusic(startTimeMs);

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
                    // Force audio data to load into memory NOW.
                    // If we don't do this, PlayScheduled() will delay in the Build while it reads from disk,
                    // causing a massive 90ms+ offset discrepancy between Editor and Build.
                    if (clip.loadState != AudioDataLoadState.Loaded)
                    {
                        clip.LoadAudioData();
                    }
                }
            }
            musicOffset = currentChart.musicOffset;
        }

        void Update()
        {
            ProcessInput();

            if (!gameState.isPlaying || gameState.isPaused) return;

            // Get precision time from clock AND APPLY GLOBAL OFFSET HERE
            float rawClockTime = CalculateGlobalTimeMs();
            float offset = Core.GameSettingsManager.Instance.Settings.rhythm.globalOffset;
            
            // This is the "Truth" time for the entire system
            globalTimerMs = rawClockTime + offset;

            gameState.currentTimeMs = GetLogicalTime(globalTimerMs);

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
                UpdateInputMapping(laneManager.GetCurrentKeyMapping());
                lastActiveLaneCount = activeLanes.Count;
            }

            judger.UpdateLogic();
            playNoteSpawner.UpdateNotes(gameState.scrollSpeedMultiplier);
        }

        // --- Integrated Input Processor ---
        private void ProcessInput()
        {
            if (playerInput != null && playerInput.actions["Back"].WasPressedThisFrame())
            {
                RhythmEvents.OnGamePause?.Invoke(!gameState.isPaused);
            }

            if (gameState.isPaused || keyToLaneMap == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            foreach (var kvp in keyToLaneMap)
            {
                if (kb[kvp.Key].wasPressedThisFrame) RhythmEvents.OnLaneDown?.Invoke(kvp.Value);
                if (kb[kvp.Key].wasReleasedThisFrame) RhythmEvents.OnLaneUp?.Invoke(kvp.Value);
            }
        }

        public void UpdateInputMapping(Dictionary<Key, int> newMapping)
        {
            keyToLaneMap = new Dictionary<Key, int>(newMapping);
        }

        // --- Integrated Clock ---
        private void StartMusic(float initialTimeMs)
        {
            internalTimerMs = initialTimeMs;
            if (audioSource != null) audioSource.Stop();
            isMusicStarted = true;
        }

        private float CalculateGlobalTimeMs()
        {
            if (!isMusicStarted || gameState.isPaused) return gameState.currentTimeMs;

            internalTimerMs += Time.deltaTime * 1000f;

            if (audioSource != null && audioSource.clip != null)
            {
                float targetAudioTime = (internalTimerMs + musicOffset) / 1000f;

                if (targetAudioTime >= 0 && targetAudioTime < audioSource.clip.length)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.time = targetAudioTime;
                        audioSource.Play();
                    }
                    else
                    {
                        float actualAudioTimeMs = (audioSource.time * 1000f) - musicOffset;
                        
                        if (Mathf.Abs(internalTimerMs - actualAudioTimeMs) > 50f)
                        {
                            internalTimerMs = actualAudioTimeMs;
                        }
                        else
                        {
                            internalTimerMs = Mathf.Lerp(internalTimerMs, actualAudioTimeMs, Time.deltaTime * 10f);
                        }
                    }
                }
            }

            return internalTimerMs;
        }

        public bool IsPaused => gameState.isPaused;

        public void PauseGame(bool pause)
        {
            if (gameState.isPaused == pause) return;

            gameState.isPaused = pause;

            if (isMusicStarted && audioSource != null)
            {
                if (pause) audioSource.Pause();
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
            if (laneManager != null) UpdateInputMapping(laneManager.GetCurrentKeyMapping());
        }

        public float GetCurrentTimeMs() => gameState.currentTimeMs;
    }
}