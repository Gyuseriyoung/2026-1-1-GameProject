using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Linq;
using TMPro;
using System.IO;

namespace RhythmSystem
{
    public enum EditorMode
    {
        View,   // Navigation only
        Place,  // Note placement/removal
        Select  // Selecting/Moving notes will be added...
    }

    public class EditorManager : MonoBehaviour
    {
        public EditorUIController editorUIController;

        [Header("Editor Mode")]
        public EditorMode currentMode = EditorMode.Place;

        [Header("Input Actions")]
        public InputActionAsset inputActions;
        private InputAction playPauseAction;
        private InputAction stopAction;
        private InputAction addNoteAction;
        private InputAction removeNoteAction;
        private InputAction scrollAction;
        private InputAction modifierAction;
        private InputAction mousePosAction;
        private InputAction copyAction;
        private InputAction pasteAction;
        private InputAction deleteAction;
        private InputAction quickSaveAction;

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
        private float editorTime = 0f; // Virtual clock in seconds

        [Header("UI & Visualization")]
        public RectTransform timelineContent; 
        public GameObject gridLinePrefab;
        public GameObject endLinePrefab; // Red line to mark audio end
        public GameObject notePrefab;
        public GameObject gimmickMarkerPrefab;
        public RectTransform JudgeLine;

        private List<GameObject> activeGridLines = new List<GameObject>();
        private List<NoteController> activeNotes = new List<NoteController>();
        private List<GameObject> activeGimmicks = new List<GameObject>();

        private GameObject endLineInstance; // Keep track of the end line

        private HashSet<NoteData> selectedNotes = new HashSet<NoteData>();
        private List<NoteData> clipboard = new List<NoteData>();

        // Dragging state
        private bool isDraggingTimeline = false;
        private Vector2 lastMousePosition;

        private void OnEnable()
        {
            SetupInputs();
            inputActions.Enable();
        }

        private void OnDisable()
        {
            if (inputActions != null) inputActions.Disable();
        }

        private void SetupInputs()
        {
            if (inputActions == null) return;
            var editorMap = inputActions.FindActionMap("Editor");
            if (editorMap == null) return;

            playPauseAction = editorMap.FindAction("PlayPause");
            stopAction = editorMap.FindAction("Stop");
            addNoteAction = editorMap.FindAction("AddNote");
            removeNoteAction = editorMap.FindAction("RemoveNote");
            scrollAction = editorMap.FindAction("Scroll");
            modifierAction = editorMap.FindAction("Modifier");
            mousePosAction = editorMap.FindAction("MousePos");
            copyAction = editorMap.FindAction("Copy");
            pasteAction = editorMap.FindAction("Paste");
            deleteAction = editorMap.FindAction("Delete");
            quickSaveAction = editorMap.FindAction("Save");

            if (playPauseAction != null) playPauseAction.performed += _ => PlayPause();
            if (stopAction != null) stopAction.performed += _ => StopPlayback();
            if (copyAction != null) copyAction.performed += _ => CopySelection();
            if (pasteAction != null) pasteAction.performed += _ => PasteClipboard();
            if (deleteAction != null) deleteAction.performed += _ => DeleteSelection();
            if (quickSaveAction != null) quickSaveAction.performed += _ => QuickSave();
        }

        void Start()
        {
            InitializeChart();
            RefreshAllVisuals();
        }

        private void InitializeChart()
        {
            if (currentChart.timingPoints.Count == 0)
                currentChart.timingPoints.Add(new TimingPoint { time = 0, bpm = currentBPM, meter = 4 });
            
            if (currentChart.lanes.Count == 0)
                currentChart.lanes.Add(new LaneConfig { laneIndex = 0, defaultY = 0, keyBinding = KeyCode.Space });
        }

        void Update()
        {
            bool overUI = IsOverBlockingUI();

            if (!overUI)
            {
                HandleNavigation();
            }
            
            if (!isPlaying && !overUI)
            {
                switch (currentMode)
                {
                    case EditorMode.Place:
                        HandlePlaceMode();
                        break;
                    case EditorMode.Select:
                        HandleSelectMode();
                        break;
                    case EditorMode.View:
                        // No specific mouse interaction for now
                        break;
                }
            }

            UpdatePlayback();
        }

        private void HandlePlaceMode()
        {
            if (addNoteAction.WasPressedThisFrame()) AddNoteAtMouse();
            else if (removeNoteAction.WasPressedThisFrame()) RemoveNoteAtMouse();
        }

        private void HandleSelectMode()
        {
            if (addNoteAction.WasPressedThisFrame() || removeNoteAction.WasPressedThisFrame())
            {
                ToggleSelectionAtMouse();
            }
        }

        public void ChangeMode(EditorMode newMode)
        {
            currentMode = newMode;
            Debug.Log($"Editor Mode changed to: {newMode}");
            if (newMode != EditorMode.Select) ClearSelection();
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
            
            SyncTimeline();
        }

        private bool IsOverBlockingUI()
        {
            if (EventSystem.current == null) return false;
            
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePosAction.ReadValue<Vector2>();
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            return results.Any(r => r.gameObject.GetComponentInParent<Selectable>() != null);
        }

        private void HandleNavigation()
        {
            // --- Timeline Dragging (Middle Mouse) ---
            if (Mouse.current != null)
            {
                if (Mouse.current.middleButton.wasPressedThisFrame)
                {
                    isDraggingTimeline = true;
                    lastMousePosition = Mouse.current.position.ReadValue();
                }
                else if (Mouse.current.middleButton.wasReleasedThisFrame)
                {
                    isDraggingTimeline = false;
                }

                if (isDraggingTimeline)
                {
                    Vector2 currentMousePos = Mouse.current.position.ReadValue();
                    float deltaX = currentMousePos.x - lastMousePosition.x;
                    
                    if (Mathf.Abs(deltaX) > 0.01f)
                    {
                        float timeDelta = deltaX / currentScrollSpeed;
                        SetPlaybackTime(editorTime + timeDelta);
                        lastMousePosition = currentMousePos;
                    }
                    return; // Skip scroll if dragging
                }
            }

            // --- Scroll Logic ---
            if (scrollAction == null) return;

            Vector2 scrollDelta = scrollAction.ReadValue<Vector2>();
            if (scrollDelta.y == 0) return;

            if (modifierAction != null && modifierAction.IsPressed())
            {
                currentScrollSpeed = Mathf.Clamp(currentScrollSpeed + (scrollDelta.y > 0 ? 10f : -10f), 100f, 5000f);
                editorUIController.RefreshScrollField();
                RefreshAllVisuals();
            }
            else
            {
                float targetTime = editorTime + (scrollDelta.y > 0 ? -0.1f : 0.1f);
                SetPlaybackTime(targetTime);
            }
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
            SyncTimeline();
        }

        public void StopPlayback()
        {
            isPlaying = false;
            audioSource.Stop();
            editorTime = -currentChart.startOffset / 1000f;
            SyncTimeline();
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
            
            SyncTimeline();
        }

        public void RefreshAllVisuals()
        {
            // Only auto-set length if it's currently 0
            if (currentChart.length <= 0 && audioSource.clip != null)
                currentChart.length = audioSource.clip.length * 1000f;

            AutoDistributeLanes();
            UpdateGrid();
            UpdateNoteVisuals();
            UpdateGimmickVisuals();
            SyncTimeline();
        }

        private void AutoDistributeLanes()
        {
            float spacing = 60f;
            int count = currentChart.lanes.Count;
            float startY = (count - 1) * spacing / 2f;

            for (int i = 0; i < count; i++)
            {
                currentChart.lanes[i].defaultY = startY - i * spacing;
                currentChart.lanes[i].laneIndex = i;
            }
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
            float endTime = currentChart.length / 1000f;
            if (endTime <= 0 && audioSource.clip != null) endTime = audioSource.clip.length;
            if (endTime <= 0) return;

            GameObject prefabToUse = endLinePrefab != null ? endLinePrefab : gridLinePrefab;
            if (prefabToUse == null) return;
            
            endLineInstance = Instantiate(prefabToUse, timelineContent);
            RectTransform rt = endLineInstance.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-endTime * currentScrollSpeed, 0);

            if (endLinePrefab == null)
            {
                Image img = endLineInstance.GetComponent<Image>();
                if (img != null) img.color = Color.red;
                rt.sizeDelta = new Vector2(5f, rt.sizeDelta.y);
            }
        }

        private void DrawVerticalLines()
        {
            if (currentChart.timingPoints.Count == 0) return;

            TimingPoint tp = currentChart.timingPoints[0];
            float beatDuration = 60f / tp.bpm;
            int numerator = tp.meter > 0 ? tp.meter : 4;
            int denominator = tp.denominator > 0 ? tp.denominator : 4;
            
            float measureDuration = (numerator / (float)denominator) * 4f * beatDuration;

            float chartDuration = currentChart.length / 1000f;
            if (chartDuration <= 0 && audioSource.clip != null) chartDuration = audioSource.clip.length;
            
            float startOffsetSec = currentChart.startOffset / 1000f;

            int startMeasure = Mathf.FloorToInt(-startOffsetSec / measureDuration);
            int endMeasure = Mathf.CeilToInt(chartDuration / measureDuration);

            for (int m = startMeasure; m <= endMeasure; m++)
            {
                for (int s = 0; s < snapDivisor; s++)
                {
                    float t = m * measureDuration + s * (measureDuration / snapDivisor);
                    
                    if (t > chartDuration + 0.001f) break;

                    GameObject line = Instantiate(gridLinePrefab, timelineContent);
                    RectTransform rt = line.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(-t * currentScrollSpeed, 0);
                    
                    ConfigureGridLineVisual(line, m, s);
                    activeGridLines.Add(line);
                }
            }
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

            if (beatIndex == 0) // Measure Line
            {
                img.color = Color.white;
                rt.sizeDelta = new Vector2(3f, rt.sizeDelta.y);
            }
            else // Snap Line
            {
                img.color = new Color(1, 1, 1, 0.5f);
                rt.sizeDelta = new Vector2(1f, rt.sizeDelta.y);
            }
        }

        private void DrawHorizontalLines()
        {
            foreach (var lane in currentChart.lanes)
            {
                GameObject line = Instantiate(gridLinePrefab, timelineContent);
                RectTransform rt = line.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, lane.defaultY);
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.sizeDelta = new Vector2(0, 2f); 
                line.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
                activeGridLines.Add(line);
            }
        }

        public void UpdateNoteVisuals()
        {
            foreach (var note in activeNotes) Destroy(note.gameObject);
            activeNotes.Clear();

            foreach (var noteData in currentChart.notes)
            {
                SpawnNoteVisual(noteData);
            }
        }

        private void SpawnNoteVisual(NoteData note)
        {
            if (notePrefab == null || timelineContent == null) return;

            GameObject noteObj = Instantiate(notePrefab, timelineContent);
            NoteController controller = noteObj.GetComponent<NoteController>();
            
            if (controller == null)
                controller = noteObj.AddComponent<NoteController>();

            controller.data = note;
            controller.SetSelection(selectedNotes.Contains(note));
            controller.ApplyMergeSprite(mergeObjectData);

            RectTransform rt = noteObj.GetComponent<RectTransform>();
            float yPos = currentChart.lanes.FirstOrDefault(l => l.laneIndex == note.laneIndex)?.defaultY ?? 0;
            rt.anchoredPosition = new Vector2(-(note.time / 1000f) * currentScrollSpeed, yPos);
            
            activeNotes.Add(controller);
        }

        public void SyncTimeline()
        {
            float targetX = JudgeLineX + editorTime * currentScrollSpeed;
            timelineContent.anchoredPosition = new Vector2(targetX, timelineContent.anchoredPosition.y);

            if (JudgeLine != null)
                JudgeLine.anchoredPosition = new Vector2(JudgeLineX, -90);
        }

        // --- Note Operations ---

        private void AddNoteAtMouse()
        {
            Vector2 mousePos = mousePosAction.ReadValue<Vector2>();
            float time = GetTimeFromMouse(mousePos);
            float snappedTimeMs = GetSnappedTime(time) * 1000f;
            int laneIndex = GetLaneFromMouse(mousePos);

            if (currentChart.notes.Any(n => Mathf.Abs(n.time - snappedTimeMs) < 0.5f && n.laneIndex == laneIndex)) return;

            NoteData newNote = new NoteData 
            { 
                time = snappedTimeMs, 
                laneIndex = laneIndex, 
                type = NoteType.Tap,
                mergeType = currentSelectedMergeType,
                objectIndex = currentSelectedMergeIndex
            };
            currentChart.notes.Add(newNote);
            SpawnNoteVisual(newNote);
        }

        private void RemoveNoteAtMouse()
        {
            var note = GetNoteAtMouse();
            if (note != null)
            {
                currentChart.notes.Remove(note);
                selectedNotes.Remove(note);
                UpdateNoteVisuals();
            }
        }

        private void ToggleSelectionAtMouse()
        {
            var note = GetNoteAtMouse();
            if (note != null)
            {
                if (selectedNotes.Contains(note)) selectedNotes.Remove(note);
                else selectedNotes.Add(note);
                
                activeNotes.FirstOrDefault(c => c.data == note)?.SetSelection(selectedNotes.Contains(note));
            }
            else ClearSelection();
        }

        private NoteData GetNoteAtMouse()
        {
            Vector2 mousePos = mousePosAction.ReadValue<Vector2>();
            float timeMs = GetTimeFromMouse(mousePos) * 1000f;
            int lane = GetLaneFromMouse(mousePos);

            return currentChart.notes
                .Where(n => n.laneIndex == lane && Mathf.Abs(n.time - timeMs) < 50f)
                .OrderBy(n => Mathf.Abs(n.time - timeMs))
                .FirstOrDefault();
        }

        public void ClearSelection()
        {
            selectedNotes.Clear();
            foreach (var note in activeNotes) note.SetSelection(false);
        }

        public void CopySelection()
        {
            if (selectedNotes.Count == 0) return;
            float minTime = selectedNotes.Min(n => n.time);
            clipboard = selectedNotes.Select(n => new NoteData {
                time = n.time - minTime, laneIndex = n.laneIndex, type = n.type, length = n.length
            }).ToList();
        }

        public void PasteClipboard()
        {
            if (clipboard.Count == 0) return;
            ClearSelection();
            float pasteBaseTimeMs = editorTime * 1000f;
            foreach (var clipNote in clipboard)
            {
                NoteData newNote = new NoteData {
                    time = pasteBaseTimeMs + clipNote.time, laneIndex = clipNote.laneIndex, type = clipNote.type, length = clipNote.length
                };
                currentChart.notes.Add(newNote);
                selectedNotes.Add(newNote);
            }
            UpdateNoteVisuals();
        }

        public void DeleteSelection()
        {
            currentChart.notes.RemoveAll(n => selectedNotes.Contains(n));
            selectedNotes.Clear();
            UpdateNoteVisuals();
        }

        public void QuickSave() => SaveChart(currentChart.metadata.title);

        public void SaveChart(string fileName)
        {
            currentChart.globalScrollSpeed = currentScrollSpeed;
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
            else
            {
                Debug.LogError($"Failed to load music at path: {path}");
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
                currentScrollSpeed = currentChart.globalScrollSpeed;
                if (currentChart.timingPoints.Count > 0) currentBPM = currentChart.timingPoints[0].bpm;
                
                // Try to load audio automatically
                if (!string.IsNullOrEmpty(currentChart.metadata.audioFileName))
                {
                    LoadMusic(currentChart.metadata.audioFileName);
                }

                RefreshAllVisuals();
            }
        }

        // --- Helpers ---

        private float GetTimeFromMouse(Vector2 mousePos)
        {
            RectTransform parentRT = timelineContent.parent as RectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, mousePos, null, out Vector2 localPos)) return editorTime;
            return editorTime + (JudgeLineX - localPos.x) / currentScrollSpeed;
        }

        private int GetLaneFromMouse(Vector2 mousePos)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(timelineContent, mousePos, null, out Vector2 localPos)) return 0;
            return currentChart.lanes.OrderBy(l => Mathf.Abs(localPos.y - l.defaultY)).FirstOrDefault()?.laneIndex ?? 0;
        }

        public float GetSnappedTime(float rawTime)
        {
            if (currentChart.timingPoints.Count == 0) return rawTime;

            TimingPoint tp = currentChart.timingPoints[0];
            float beatDuration = 60f / tp.bpm;
            int numerator = tp.meter > 0 ? tp.meter : 4;
            int denominator = tp.denominator > 0 ? tp.denominator : 4;
            
            float measureDuration = (numerator / (float)denominator) * 4f * beatDuration;

            float snapInterval = measureDuration / snapDivisor;
            return Mathf.Round(rawTime / snapInterval) * snapInterval;
        }

        public void AddLane() { currentChart.lanes.Add(new LaneConfig()); RefreshAllVisuals(); }
        public void RemoveLane() { if (currentChart.lanes.Count > 1) { currentChart.lanes.RemoveAt(currentChart.lanes.Count - 1); RefreshAllVisuals(); } }

        public void UpdateGimmickVisuals()
        {
            foreach (var gm in activeGimmicks) Destroy(gm);
            activeGimmicks.Clear();
            foreach (var gimmick in currentChart.gimmicks)
            {
                if (gimmickMarkerPrefab == null) break;
                GameObject gm = Instantiate(gimmickMarkerPrefab, timelineContent);
                gm.GetComponent<RectTransform>().anchoredPosition = new Vector2(-(gimmick.time / 1000f) * currentScrollSpeed, 0);
                activeGimmicks.Add(gm);
            }
        }
    }
}
