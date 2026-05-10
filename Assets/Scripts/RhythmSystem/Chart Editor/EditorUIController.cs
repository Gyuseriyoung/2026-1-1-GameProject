using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

namespace RhythmSystem
{
    public class EditorUIController : MonoBehaviour
    {
        public EditorManager editorManager;

        [Header("Metadata Panel")]
        public TMP_InputField titleInput;
        public TMP_InputField artistInput;
        public TMP_InputField creatorInput;
        public TMP_InputField chartLengthInput; // Manual chart Length

        [Header("Timing & Offset Panel")]
        public TMP_InputField bpmInput;
        public TMP_InputField meterInput;
        public TMP_InputField offsetInput; // Visual Start Offset(Leadin time)
        public TMP_InputField musicOffsetInput; // Audio Playback Offset

        [Header("Note Settings Panel")]
        public TMP_InputField scrollSpeedField;
        public TMP_InputField judgeLineXInput;
        public TMP_Dropdown snapDropdown;
        public TMP_Dropdown modeDropdown;
        public TMP_Dropdown gimmickTypeDropdown;
        public TMP_InputField gimmickValueInput;

        [Header("Playback Panel")]
        public TextMeshProUGUI timeText;
        public TMP_Dropdown musicListDropdown;
        public Button playPauseButton;
        public Button stopButton;
        public Button testPlayButton;
        public Slider miniMapSlider;

        [Header("File Sidebar")]
        public TMP_Dropdown chartListDropdown;
        public Button saveButton;
        public Button loadButton;
        public Button refreshButton;

        [Header("Lane Controls")]
        public Button addLaneButton;
        public Button removeLaneButton;

        [Header("Merge Object Selection")]
        public TMP_Dropdown mergeCategoryDropdown;
        public RectTransform mergeObjectContainer;
        public GameObject objectIconPrefab;
        public MergeObjectUIItem currentSelectionDisplay;

        private readonly int[] snapValues = { 1, 2, 4, 8, 16, 32, 3, 6 };
        private readonly string[] snapLabels = { "1/1", "1/2", "1/4", "1/8", "1/16", "1/32", "1/3", "1/6" };

        void Start()
        {
            InitializeDropdowns();
            SetupListeners();
            RefreshUI();
            RefreshFileList();
        }

        private void InitializeDropdowns()
        {
            if (snapDropdown != null)
            {
                snapDropdown.ClearOptions();
                snapDropdown.AddOptions(snapLabels.ToList());
                int index = System.Array.IndexOf(snapValues, editorManager.snapDivisor);
                snapDropdown.SetValueWithoutNotify(index >= 0 ? index : 2);
            }

            if (modeDropdown != null)
            {
                modeDropdown.ClearOptions();
                modeDropdown.AddOptions(System.Enum.GetNames(typeof(EditorMode)).ToList());
                modeDropdown.SetValueWithoutNotify((int)editorManager.currentMode);
            }

            if (gimmickTypeDropdown != null)
            {
                gimmickTypeDropdown.ClearOptions();
                gimmickTypeDropdown.AddOptions(System.Enum.GetNames(typeof(GimmickType)).ToList());
                gimmickTypeDropdown.SetValueWithoutNotify((int)editorManager.currentSelectedGimmickType);
                gimmickTypeDropdown.onValueChanged.AddListener(HandleGimmickTypeChange);
            }

            if (musicListDropdown != null)
            {
                RefreshMusicList();
                musicListDropdown.onValueChanged.AddListener(HandleMusicChange);
            }

            if (mergeCategoryDropdown != null && editorManager.mergeObjectData != null)
            {
                mergeCategoryDropdown.ClearOptions();
                var categoryNames = editorManager.mergeObjectData.MergeData.Select(d => d.TypeName).ToList();
                mergeCategoryDropdown.AddOptions(categoryNames);
                mergeCategoryDropdown.onValueChanged.AddListener(HandleMergeCategoryChanged);
                
                HandleMergeCategoryChanged(0);
            }
        }

        public void RefreshMusicList()
        {
            if (musicListDropdown == null) return;
            var files = editorManager.GetMusicFileList();
            musicListDropdown.ClearOptions();
            musicListDropdown.AddOptions(files);

            string current = editorManager.currentChart.metadata.audioFileName;
            int index = files.IndexOf(current);
            if (index >= 0) musicListDropdown.SetValueWithoutNotify(index);
        }

        private void HandleMusicChange(int index)
        {
            if (musicListDropdown.options.Count == 0) return;
            string fileName = musicListDropdown.options[index].text;
            editorManager.LoadMusic(fileName);
        }

        private void HandleMergeCategoryChanged(int categoryIndex)
        {
            if (editorManager.mergeObjectData == null) return;
            editorManager.currentSelectedMergeType = categoryIndex;

            foreach (Transform child in mergeObjectContainer) Destroy(child.gameObject);

            var category = editorManager.mergeObjectData.MergeData[categoryIndex];
            for (int i = 0; i < category.MergeDataList.Length; i++)
            {
                int index = i;
                var objData = category.MergeDataList[i];
                GameObject iconObj = Instantiate(objectIconPrefab, mergeObjectContainer);
                
                MergeObjectUIItem uiItem = iconObj.GetComponent<MergeObjectUIItem>();
                if (uiItem != null)
                {
                    uiItem.Setup(categoryIndex, index, objData.sprite, objData.Name);
                }

                Button btn = iconObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => SelectMergeObject(categoryIndex, index));
                }
            }

            if (category.MergeDataList.Length > 0) SelectMergeObject(categoryIndex, 0);
        }

        private void SelectMergeObject(int categoryIndex, int objectIndex)
        {
            editorManager.currentSelectedMergeType = categoryIndex;
            editorManager.currentSelectedMergeIndex = objectIndex;

            if (currentSelectionDisplay != null && editorManager.mergeObjectData != null)
            {
                var objData = editorManager.mergeObjectData.MergeData[categoryIndex].MergeDataList[objectIndex];
                currentSelectionDisplay.Setup(categoryIndex, objectIndex, objData.sprite, objData.Name);
            }
        }

        private void SetupListeners()
        {
            bpmInput.onEndEdit.AddListener(HandleBpmChange);
            meterInput.onEndEdit.AddListener(HandleMeterChange);
            titleInput.onEndEdit.AddListener(HandleTitleChange);
            artistInput.onEndEdit.AddListener(val => editorManager.currentChart.metadata.artist = val);
            creatorInput.onEndEdit.AddListener(val => editorManager.currentChart.metadata.creator = val);
            offsetInput.onEndEdit.AddListener(HandleOffsetChange);
            musicOffsetInput?.onEndEdit.AddListener(HandleMusicOffsetChange);
            chartLengthInput?.onEndEdit.AddListener(HandleChartLengthChange);
            
            scrollSpeedField.onValueChanged.AddListener(HandleScrollSpeedChange);
            judgeLineXInput?.onEndEdit.AddListener(HandleJudgeLineXChange);
            snapDropdown.onValueChanged.AddListener(HandleSnapChange);
            modeDropdown?.onValueChanged.AddListener(HandleModeChange);
            gimmickValueInput?.onEndEdit.AddListener(HandleGimmickValueChange);

            if (miniMapSlider != null) miniMapSlider.onValueChanged.AddListener(HandleMiniMapChange);

            playPauseButton?.onClick.AddListener(HandlePlayPauseClick);
            stopButton?.onClick.AddListener(HandleStopClick);
            testPlayButton?.onClick.AddListener(() => editorManager.StartTestPlay());
            saveButton?.onClick.AddListener(HandleSaveClick);
            loadButton?.onClick.AddListener(HandleLoadClick);
            refreshButton?.onClick.AddListener(RefreshFileList);

            addLaneButton?.onClick.AddListener(() => editorManager.AddLane());
            removeLaneButton?.onClick.AddListener(() => editorManager.RemoveLane());
        }

        public void RefreshUI()
        {
            var chart = editorManager.currentChart;
            bpmInput.text = editorManager.currentBPM.ToString();
            
            if (chart.timingPoints.Count > 0)
            {
                var tp = chart.timingPoints[0];
                meterInput.text = $"{tp.meter}/{tp.denominator}";
            }
            else
            {
                meterInput.text = "4/4";
            }

            scrollSpeedField.text = (editorManager.currentScrollSpeed / 100f).ToString();
            if (judgeLineXInput != null) judgeLineXInput.text = editorManager.JudgeLineX.ToString();
            titleInput.text = chart.metadata.title;
            artistInput.text = chart.metadata.artist;
            creatorInput.text = chart.metadata.creator;
            offsetInput.text = chart.startOffset.ToString();
            if (musicOffsetInput != null) musicOffsetInput.text = chart.musicOffset.ToString();
            if (chartLengthInput != null) chartLengthInput.text = (chart.length / 1000f).ToString("F2");

            if (modeDropdown != null)
                modeDropdown.SetValueWithoutNotify((int)editorManager.currentMode);
            
            if (gimmickTypeDropdown != null)
                gimmickTypeDropdown.SetValueWithoutNotify((int)editorManager.currentSelectedGimmickType);

            InitializeMiniMap();
        }

        private void InitializeMiniMap()
        {
            if (miniMapSlider == null) return;
            
            editorManager.GetNavigationBounds(out float min, out float max);
            miniMapSlider.minValue = min;
            miniMapSlider.maxValue = max;
            miniMapSlider.value = 0;
        }

        void Update()
        {
            UpdatePlaybackUI();
        }

        private void UpdatePlaybackUI()
        {
            float time = editorManager.EditorTime;
            editorManager.GetNavigationBounds(out float min, out float max);

            timeText.text = $"{time:F2} / {max:F2}";

            if (miniMapSlider != null && !Mouse.current.leftButton.isPressed)
            {
                miniMapSlider.SetValueWithoutNotify(time);
            }

            if (playPauseButton != null)
            {
                var text = playPauseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = editorManager.isPlaying ? "Pause" : "Play";
            }
        }

        // --- Event Handlers ---

        private void HandleBpmChange(string val)
        {
            if (float.TryParse(val, out float bpm))
            {
                editorManager.currentBPM = bpm;
                if (editorManager.currentChart.timingPoints.Count > 0)
                    editorManager.currentChart.timingPoints[0].bpm = bpm;
                editorManager.RefreshAllVisuals();
            }
        }

        private void HandleMeterChange(string val)
        {
            string[] parts = val.Split('/');
            if (parts.Length == 1)
            {
                if (int.TryParse(parts[0], out int numerator) && numerator > 0)
                {
                    if (editorManager.currentChart.timingPoints.Count > 0)
                    {
                        editorManager.currentChart.timingPoints[0].meter = numerator;
                        editorManager.currentChart.timingPoints[0].denominator = 4;
                        meterInput.text = $"{numerator}/4";
                    }
                    editorManager.RefreshAllVisuals();
                }
            }
            else if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int numerator) && int.TryParse(parts[1], out int denominator) 
                    && numerator > 0 && denominator > 0)
                {
                    if (editorManager.currentChart.timingPoints.Count > 0)
                    {
                        editorManager.currentChart.timingPoints[0].meter = numerator;
                        editorManager.currentChart.timingPoints[0].denominator = denominator;
                    }
                    editorManager.RefreshAllVisuals();
                }
            }
        }

        private void HandleTitleChange(string val) => editorManager.currentChart.metadata.title = val;

        private void HandleOffsetChange(string val)
        {
            if (float.TryParse(val, out float offset))
            {
                editorManager.currentChart.startOffset = offset;
                editorManager.RefreshAllVisuals();
                InitializeMiniMap();
            }
        }

        private void HandleMusicOffsetChange(string val)
        {
            if (float.TryParse(val, out float offset))
            {
                editorManager.currentChart.musicOffset = offset;
            }
        }

        private void HandleChartLengthChange(string val)
        {
            if (float.TryParse(val, out float len))
            {
                editorManager.currentChart.length = len * 1000f;
                editorManager.RefreshAllVisuals();
                InitializeMiniMap();
            }
        }

        private void HandleScrollSpeedChange(string val)
        {
            if (float.TryParse(val, out float num))
            {
                editorManager.currentScrollSpeed = num * 100;
            }
            else
            {
                editorManager.currentScrollSpeed = 500;
            }
            editorManager.RefreshAllVisuals();
        }

        private void HandleJudgeLineXChange(string val)
        {
            if (float.TryParse(val, out float x))
            {
                editorManager.JudgeLineX = x;
                editorManager.RefreshAllVisuals();
            }
        }

        private void HandleSnapChange(int index)
        {
            if (index >= 0 && index < snapValues.Length)
            {
                editorManager.snapDivisor = snapValues[index];
                editorManager.RefreshAllVisuals();
            }
        }

        private void HandleModeChange(int index)
        {
            editorManager.ChangeMode((EditorMode)index);
        }

        private void HandleGimmickTypeChange(int index)
        {
            editorManager.currentSelectedGimmickType = (GimmickType)index;
        }

        private void HandleGimmickValueChange(string val)
        {
            if (float.TryParse(val, out float res))
            {
                if (editorManager.currentSelectedGimmickType == GimmickType.BPMChange)
                {
                    editorManager.currentBPM = res;
                    bpmInput.text = res.ToString();
                }
            }
        }

        private void HandleMiniMapChange(float val)
        {
            if (Mouse.current.leftButton.isPressed) 
            {
                editorManager.SetPlaybackTime(val);
                timeText.text = $"{val:F2} / {editorManager.audioSource.clip.length:F2}";
            }
        }

        private void HandlePlayPauseClick() => editorManager.PlayPause();
        private void HandleStopClick() => editorManager.StopPlayback();

        private void HandleSaveClick()
        {
            editorManager.SaveChart(titleInput.text);
            RefreshFileList();
        }

        private void HandleLoadClick()
        {
            if (chartListDropdown.options.Count == 0) return;
            string selectedFile = chartListDropdown.options[chartListDropdown.value].text;
            editorManager.LoadChart(selectedFile);
            RefreshUI();
        }

        public void RefreshFileList()
        {
            var files = editorManager.GetChartFileList();
            chartListDropdown.ClearOptions();
            chartListDropdown.AddOptions(files);
        }

        public void RefreshScrollField()
        {
            if (scrollSpeedField != null)
            {
                scrollSpeedField.text = (editorManager.currentScrollSpeed / 100f).ToString();
            }
        }

        public void RefreshJudgeLineXField()
        {
            if (judgeLineXInput != null)
            {
                judgeLineXInput.text = editorManager.JudgeLineX.ToString();
            }
        }
    }
}
