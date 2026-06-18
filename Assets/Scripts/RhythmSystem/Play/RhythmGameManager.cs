using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using RhythmSystem;
using TMPro;
using UnityEngine.UI;

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

        [Header("Dialogue Support")]
        public CookingGame.DialogueManager dialogueManager;
        private List<CookingGame.MidPlayDialogue> pendingMidPlayDialogues = new List<CookingGame.MidPlayDialogue>();

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
            StartCoroutine(StartGameSequence());
        }

        private System.Collections.IEnumerator StartGameSequence()
        {
            if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsBusy)
            {
                while (SceneTransitionManager.Instance.IsBusy)
                {
                    yield return null;
                }
            }

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
            if (dialogueManager == null) dialogueManager = FindFirstObjectByType<CookingGame.DialogueManager>();

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
            pendingMidPlayDialogues.Clear();
            if (CookingGame.CookingSession.CurrentCustomer != null && 
                CookingGame.CookingSession.CurrentCustomer.midPlayDialogues != null)
            {
                foreach (var dialogue in CookingGame.CookingSession.CurrentCustomer.midPlayDialogues)
                {
                    if (dialogue.triggerTimeMs > startTimeMs)
                    {
                        pendingMidPlayDialogues.Add(dialogue);
                    }
                }
                pendingMidPlayDialogues.Sort((a, b) => a.triggerTimeMs.CompareTo(b.triggerTimeMs));
            }

            gameState.currentTimeMs = startTimeMs;
            gameState.isPlaying = true;
            gameState.combo = 0;
            gameState.scrollSpeedMultiplier = 1f;
            
            stopTimeline.Rebuild(currentChart.gimmicks);

            globalTimerMs = startTimeMs;

            if (audioSource == null) audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            // 리듬게임 시작 전 기존 전역 BGM을 멈춰 음악이 겹치는 현상을 해결합니다.
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM(0.3f);
                if (AudioManager.Instance.bgmGroup != null)
                {
                    audioSource.outputAudioMixerGroup = AudioManager.Instance.bgmGroup;
                }
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
                    if (audioSource.clip != clip)
                    {
                        audioSource.clip = clip;
                    }
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

            // Check for Mid-Play Dialogues
            if (pendingMidPlayDialogues.Count > 0 && gameState.currentTimeMs >= pendingMidPlayDialogues[0].triggerTimeMs)
            {
                var dialogue = pendingMidPlayDialogues[0];
                pendingMidPlayDialogues.RemoveAt(0);
                TriggerMidPlayDialogue(dialogue.dialogueLines);
                return;
            }

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
            if (audioSource != null)
            {
                audioSource.Stop();
                float targetTime = (initialTimeMs + musicOffset) / 1000f;
                audioSource.time = Mathf.Max(0f, targetTime);
            }
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

        private void TriggerMidPlayDialogue(string[] lines)
        {
            if (dialogueManager == null)
            {
                Debug.LogWarning("DialogueManager is not assigned in RhythmGameManager. Skipping mid-play dialogue.");
                return;
            }

            // Pause the gameplay
            gameState.isPaused = true;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }

            // Play the dialogue, and on complete, resume
            dialogueManager.PlayDialogue(lines, () => {
                ResumeFromMidPlayDialogue();
            });
        }

        private void ResumeFromMidPlayDialogue()
        {
            StartCoroutine(ResumeSequence());
        }

        private System.Collections.IEnumerator ResumeSequence()
        {
            GameObject canvasGo = new GameObject("CountdownCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;
            canvasGo.AddComponent<CanvasScaler>();

            GameObject textGo = new GameObject("CountdownText");
            textGo.transform.SetParent(canvasGo.transform, false);
            
            TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 80;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.outlineColor = Color.black;
            text.outlineWidth = 0.2f;

            for (int i = 3; i > 0; i--)
            {
                text.text = i.ToString();
                
                float elapsed = 0f;
                float duration = 0.7f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float scale = Mathf.Lerp(1.5f, 1.0f, elapsed / duration);
                    text.transform.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }
            }

            text.text = "GO!";
            float goElapsed = 0f;
            float goDuration = 0.4f;
            
            gameState.isPaused = false;
            if (audioSource != null)
            {
                audioSource.UnPause();
            }

            while (goElapsed < goDuration)
            {
                goElapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(1.0f, 2.0f, goElapsed / goDuration);
                text.transform.localScale = new Vector3(scale, scale, 1f);
                
                Color col = text.color;
                col.a = Mathf.Lerp(1f, 0f, goElapsed / goDuration);
                text.color = col;
                yield return null;
            }

            Destroy(canvasGo);
        }
    }
}