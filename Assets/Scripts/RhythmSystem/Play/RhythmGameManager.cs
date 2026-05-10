using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;
using RhythmSystem;
using System.Collections.Generic;
using TMPro;

namespace RhythmSystem.Play
{
    public class RhythmGameManager : MonoBehaviour
    {
        public PlayNoteSpawner playNoteSpawner;
        public RhythmOptionsUIManager optionsUIManager;
        public MergeManager mergeManager;
        public AudioSource audioSource;
        public PlayerInput playerInput;
        
        [Header("UI Feedback")]
        public TMP_Text judgmentText;
        public TMP_Text comboText;

        [Header("Judgment Settings")]
        public float perfectWindow = 50f;
        public float greatWindow = 100f;
        public float goodWindow = 150f;
        public float missWindow = 200f;
        public float earlyMissWindow = 400f;

        [Header("Settings")]
        public string chartToLoad = "TestChart";
        public string musicFolderName = "Musics";

        private ChartData currentChart;
        private float currentTimeMs = 0f;
        private bool isPlaying = false;
        private bool isPaused = false;
        private bool isMusicStarted = false;

        private int combo = 0;
        private List<GimmickEvent> pendingGimmicks = new List<GimmickEvent>();

        void Start()
        {
            if (playerInput != null)
            {
                playerInput.onActionTriggered += OnActionTriggered;
                
                var rhythmMap = playerInput.actions.FindActionMap("Rhythm");
                if (rhythmMap != null)
                {
                    rhythmMap.Enable();
                    playerInput.SwitchCurrentActionMap("Rhythm");
                }
            }

            if (EditorTestSession.IsTestMode && EditorTestSession.CurrentChart != null)
            {
                currentChart = EditorTestSession.CurrentChart;
                InitializeGame(EditorTestSession.StartSeekTime * 1000f, EditorTestSession.ScrollSpeed);
            }
            else
            {
                LoadAndStartGame();
            }
        }

        private void OnDestroy()
        {
            if (playerInput != null)
            {
                playerInput.onActionTriggered -= OnActionTriggered;
            }
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            string actionName = context.action.name;

            if (actionName == "Back")
            {
                if (optionsUIManager != null)
                {
                    optionsUIManager.TogglePanel();
                }
            }
            else if (actionName.StartsWith("Lane"))
            {
                if (int.TryParse(actionName.Replace("Lane", ""), out int laneIndex))
                {
                    OnLanePressed(laneIndex);
                }
            }
        }

        public void PauseGame(bool pause)
        {
            isPaused = pause;
            if (audioSource != null && audioSource.clip != null)
            {
                if (isPaused) audioSource.Pause();
                else if (isMusicStarted) audioSource.UnPause();
            }
        }

        public void RestartGame()
        {
            isPlaying = false;
            isMusicStarted = false;
            isPaused = false;
            combo = 0;
            UpdateComboUI();
            if (judgmentText != null) judgmentText.text = "";

            if (audioSource != null) audioSource.Stop();
            playNoteSpawner.ClearNotes();
            if (mergeManager != null) mergeManager.ClearAllObjects();

            if (EditorTestSession.IsTestMode)
            {
                InitializeGame(EditorTestSession.StartSeekTime * 1000f, EditorTestSession.ScrollSpeed);
            }
            else
            {
                LoadAndStartGame();
            }
        }

        private void OnLanePressed(int laneIndex)
        {
            if (isPaused) return;

            var activeLanes = playNoteSpawner.GetActiveLanes();
            if (activeLanes.TryGetValue(laneIndex, out var lane))
            {
                lane.OnPress();
            }
            else
            {
                return;
            }

            var spawnedNotes = playNoteSpawner.GetSpawnedNotes();
            NoteObject bestNote = null;
            float minDiff = float.MaxValue;
            bool isEarly = false;

            foreach (var note in spawnedNotes)
            {
                if (note.IsJudged || note.Data.laneIndex != laneIndex) continue;

                float rawDiff = note.GetNoteTime() - currentTimeMs; 
                float absDiff = Mathf.Abs(rawDiff);
                
                if (absDiff < minDiff && absDiff <= earlyMissWindow)
                {
                    minDiff = absDiff;
                    bestNote = note;
                    isEarly = rawDiff > 0;
                }
            }

            if (bestNote != null)
            {
                ProcessJudgment(bestNote, minDiff, isEarly);
            }
        }

        private void ProcessJudgment(NoteObject note, float absDiff, bool isEarly)
        {
            string rating = "Miss";
            
            if (absDiff <= perfectWindow) rating = "Perfect";
            else if (absDiff <= greatWindow) rating = "Great";
            else if (absDiff <= goodWindow) rating = "Good";
            else if (absDiff <= missWindow) rating = "Miss";
            else if (isEarly && absDiff <= earlyMissWindow) rating = "Early Miss";

            ShowJudgment(rating);
            
            var activeLanes = playNoteSpawner.GetActiveLanes();
            if (activeLanes.TryGetValue(note.Data.laneIndex, out var lane))
            {
                if (rating != "Miss" && rating != "Early Miss") lane.OnHit(rating);
                else lane.OnMiss();
            }

            if (rating != "Miss" && rating != "Early Miss")
            {
                combo++;
                if (mergeManager != null)
                {
                    mergeManager.CreateMergeObject(note.Data.mergeType, note.Data.objectIndex);
                }
            }
            else
            {
                combo = 0;
            }
            
            UpdateComboUI();
            note.OnJudged();
        }

        private void ShowJudgment(string rating)
        {
            if (judgmentText != null)
            {
                judgmentText.text = rating;
                if (rating == "Perfect") judgmentText.color = Color.yellow;
                else if (rating == "Great") judgmentText.color = Color.green;
                else if (rating == "Good") judgmentText.color = Color.cyan;
                else judgmentText.color = Color.red;
            }
        }

        private void UpdateComboUI()
        {
            if (comboText != null)
            {
                comboText.text = combo > 0 ? $"{combo}" : "";
            }
        }

        public void QuitGame()
        {
            if (EditorTestSession.IsTestMode) ReturnToEditor();
            else ReturnToTitle();
        }

        private void ReturnToTitle()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
        }

        public void LoadAndStartGame()
        {
            currentChart = ChartIO.LoadFromFile(chartToLoad);
            if (currentChart != null)
            {
                InitializeGame(-currentChart.startOffset, null);
            }
            else
            {
                Debug.LogError($"Failed to load chart: {chartToLoad}");
            }
        }

        private void InitializeGame(float startTimeMs, float? speedOverride)
        {
            currentTimeMs = startTimeMs;
            
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            string musicName = currentChart.metadata.audioFileName;
            if (!string.IsNullOrEmpty(musicName))
            {
                StartCoroutine(LoadMusicCoroutine(musicName));
            }

            pendingGimmicks = new List<GimmickEvent>(currentChart.gimmicks);
            pendingGimmicks.Sort((a, b) => a.time.CompareTo(b.time));

            playNoteSpawner.SpawnNotes(currentChart, currentTimeMs, speedOverride);
            isPlaying = true;
        }

        private IEnumerator LoadMusicCoroutine(string fileName)
        {
            string path = Path.Combine(Application.dataPath, musicFolderName, fileName);
            if (!path.Contains("://")) path = "file://" + path;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }

        void Update()
        {
            if (!isPlaying || isPaused) return;

            currentTimeMs += Time.deltaTime * 1000f;
            
            playNoteSpawner.UpdateLanes(currentTimeMs);
            
            ProcessGimmicks();
            playNoteSpawner.UpdateNotes(currentTimeMs);
            CheckForMisses();

            if (audioSource != null && audioSource.clip != null)
            {
                float targetAudioTime = (currentTimeMs + currentChart.musicOffset) / 1000f;

                if (targetAudioTime >= 0 && targetAudioTime < audioSource.clip.length)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.time = targetAudioTime;
                        audioSource.Play();
                        isMusicStarted = true;
                    }
                    else
                    {
                        float drift = Mathf.Abs(audioSource.time - targetAudioTime);
                        if (drift > 0.05f) audioSource.time = targetAudioTime;
                    }
                }
                else if (targetAudioTime >= audioSource.clip.length)
                {
                    if (audioSource.isPlaying) audioSource.Stop();
                }
            }
        }

        private void ProcessGimmicks()
        {
            while (pendingGimmicks.Count > 0 && pendingGimmicks[0].time <= currentTimeMs)
            {
                var g = pendingGimmicks[0];
                ApplyGimmick(g);
                pendingGimmicks.RemoveAt(0);
            }
        }

        private void ApplyGimmick(GimmickEvent g)
        {
            switch (g.type)
            {
                case GimmickType.BPMChange:
                    break;
            }
        }

        private void CheckForMisses()
        {
            var spawnedNotes = playNoteSpawner.GetSpawnedNotes();
            foreach (var note in spawnedNotes)
            {
                if (note.IsJudged) continue;

                float diff = currentTimeMs - note.GetNoteTime();
                if (diff > missWindow)
                {
                    combo = 0;
                    UpdateComboUI();
                    ShowJudgment("Miss");

                    var activeLanes = playNoteSpawner.GetActiveLanes();
                    if (activeLanes.TryGetValue(note.Data.laneIndex, out var lane))
                    {
                        lane.OnMiss();
                    }

                    note.OnJudged(); 
                }
            }
        }

        public float GetCurrentTimeMs() => currentTimeMs;

        public void ReturnToEditor()
        {
            EditorTestSession.IsTestMode = false;
            EditorTestSession.IsReturningFromTest = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(EditorTestSession.ReturnSceneName);
        }
    }
}
