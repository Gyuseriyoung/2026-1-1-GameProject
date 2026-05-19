using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace RhythmSystem.Play
{
    public class RhythmOptionsUIManager : MonoBehaviour
    {
        public RhythmGameManager gameManager;
        
        [Header("UI Elements")]
        public GameObject panel;
        public Button restartButton;
        public Button quitButton;
        
        [Header("Scroll Speed (1.0 - 10.0)")]
        public TMP_InputField scrollSpeedInput;
        public Button speedUpBtn;
        public Button speedDownBtn;
        
        [Header("Layout (Sliders)")]
        public Slider judgmentXSlider;
        public TMP_Text judgmentXText;
        
        public Slider laneSpacingSlider;
        public TMP_Text laneSpacingText;

        public Slider judgmentYSlider;
        public TMP_Text judgmentYText;

        [Header("Key Bindings")]
        public GameObject keyBindingContainer;
        public GameObject keyBindingItemPrefab;

        private int bindingLaneIndex = -1;

        private void Start()
        {
            LoadCurrentSettings();
            SetupListeners();
            
            if (panel != null) panel.SetActive(false);
        }

        private void SetupListeners()
        {
            // Scroll Speed
            if (scrollSpeedInput != null)
                scrollSpeedInput.onEndEdit.AddListener(OnScrollSpeedEndEdit);
            
            speedUpBtn?.onClick.AddListener(() => ChangeScrollSpeed(0.1f));
            speedDownBtn?.onClick.AddListener(() => ChangeScrollSpeed(-0.1f));
                
            // Judgment X
            if (judgmentXSlider != null)
                judgmentXSlider.onValueChanged.AddListener(OnJudgmentXChanged);
                
            // Lane Spacing
            if (laneSpacingSlider != null)
                laneSpacingSlider.onValueChanged.AddListener(OnLaneSpacingChanged);

            // Judgment Y
            if (judgmentYSlider != null)
                judgmentYSlider.onValueChanged.AddListener(OnJudgmentYChanged);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        public void SetPanelActive(bool active)
        {
            if (panel == null) return;
            
            if (panel.activeSelf != active)
            {
                panel.SetActive(active);
                if (!active)
                {
                    Core.GameSettingsManager.Instance.SaveSettings();
                }
                else
                {
                    LoadCurrentSettings();
                }
            }
        }

        public void TogglePanel()
        {
            if (gameManager == null) return;
            gameManager.PauseGame(!gameManager.IsPaused);
        }

        public void OnRestartClicked()
        {
            if (gameManager != null)
            {
                gameManager.RestartGame();
                gameManager.PauseGame(false);
            }
        }

        public void OnQuitClicked()
        {
            if (gameManager != null) gameManager.QuitGame();
        }

        private void LoadCurrentSettings()
        {
            var s = Core.GameSettingsManager.Instance.Settings.rhythm;
            
            if (scrollSpeedInput != null)
                scrollSpeedInput.text = s.scrollSpeed.ToString("F1");
            
            if (judgmentXSlider != null)
            {
                judgmentXSlider.minValue = -10f;
                judgmentXSlider.maxValue = 15f;
                judgmentXSlider.value = s.judgmentX;
                if (judgmentXText != null) judgmentXText.text = $"Judgment X: {s.judgmentX:F1}";
            }

            if (judgmentYSlider != null)
            {
                judgmentYSlider.minValue = -10f;
                judgmentYSlider.maxValue = 10f;
                judgmentYSlider.value = s.judgmentY;
                if (judgmentYText != null) judgmentYText.text = $"Judgment Y Offset: {s.judgmentY:F1}";
            }
            
            if (laneSpacingSlider != null)
            {
                laneSpacingSlider.minValue = 0.2f;
                laneSpacingSlider.maxValue = 1.5f;
                laneSpacingSlider.value = s.laneSpacing;
                if (laneSpacingText != null) laneSpacingText.text = $"Lane Spacing: {s.laneSpacing:F2}";
            }

            RefreshKeyBindingUI();
        }

        private void OnScrollSpeedEndEdit(string value)
        {
            if (float.TryParse(value, out float result))
            {
                ApplyScrollSpeed(result);
            }
            else
            {
                scrollSpeedInput.text = Core.GameSettingsManager.Instance.Settings.rhythm.scrollSpeed.ToString("F1");
            }
        }

        private void ChangeScrollSpeed(float delta)
        {
            float newSpeed = Core.GameSettingsManager.Instance.Settings.rhythm.scrollSpeed + delta;
            ApplyScrollSpeed(newSpeed);
        }

        private void ApplyScrollSpeed(float speed)
        {
            speed = Mathf.Clamp(speed, 1.0f, 10.0f);
            Core.GameSettingsManager.Instance.Settings.rhythm.scrollSpeed = speed;
            if (scrollSpeedInput != null) scrollSpeedInput.text = speed.ToString("F1");
            
            // Immediate visual refresh for all notes
            if (gameManager != null && gameManager.playNoteSpawner != null)
            {
                gameManager.playNoteSpawner.UpdateAllNotePositions();
            }
        }

        private void OnJudgmentXChanged(float value)
        {
            Core.GameSettingsManager.Instance.Settings.rhythm.judgmentX = value;
            if (judgmentXText != null) judgmentXText.text = $"Judgment X: {value:F1}";
            
            if (gameManager != null)
            {
                if (gameManager.laneManager != null) gameManager.laneManager.UpdateLanes();
                if (gameManager.playNoteSpawner != null) gameManager.playNoteSpawner.UpdateAllNotePositions();
            }
        }

        private void OnLaneSpacingChanged(float value)
        {
            Core.GameSettingsManager.Instance.Settings.rhythm.laneSpacing = value;
            if (laneSpacingText != null) laneSpacingText.text = $"Lane Spacing: {value:F2}";
            
            if (gameManager != null)
            {
                if (gameManager.laneManager != null) gameManager.laneManager.UpdateLanes();
                if (gameManager.playNoteSpawner != null) gameManager.playNoteSpawner.UpdateAllNotePositions();
            }
        }

        private void OnJudgmentYChanged(float value)
        {
            Core.GameSettingsManager.Instance.Settings.rhythm.judgmentY = value;
            if (judgmentYText != null) judgmentYText.text = $"Judgment Y Offset: {value:F1}";
            
            if (gameManager != null)
            {
                if (gameManager.laneManager != null) gameManager.laneManager.UpdateLanes();
                if (gameManager.playNoteSpawner != null) gameManager.playNoteSpawner.UpdateAllNotePositions();
            }
        }

        private void Update()
        {
            HandleKeyBindingInput();
        }

        private void HandleKeyBindingInput()
        {
            if (bindingLaneIndex == -1) return;

            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                foreach (var control in Keyboard.current.allControls)
                {
                    if (control is UnityEngine.InputSystem.Controls.KeyControl keyControl && keyControl.wasPressedThisFrame)
                    {
                        Key k = keyControl.keyCode;
                        if (k == Key.None) continue;

                        var s = Core.GameSettingsManager.Instance.Settings.rhythm;
                        while (s.laneKeys.Count <= bindingLaneIndex) s.laneKeys.Add(Key.None);
                        
                        s.laneKeys[bindingLaneIndex] = k;
                        bindingLaneIndex = -1;
                        RefreshKeyBindingUI();
                        
                        // Notify game manager to refresh input mapping
                        if (gameManager != null) gameManager.RefreshInputMapping();
                        break;
                    }
                }
            }
        }

        public void RefreshKeyBindingUI()
        {
            if (keyBindingContainer == null || keyBindingItemPrefab == null) return;

            foreach (Transform child in keyBindingContainer.transform) Destroy(child.gameObject);

            var s = Core.GameSettingsManager.Instance.Settings.rhythm;
            int maxLanes = 8; 
            for (int i = 0; i < maxLanes; i++)
            {
                int laneIdx = i;
                GameObject item = Instantiate(keyBindingItemPrefab, keyBindingContainer.transform);
                
                // Find Lane Name Text and Binding Button
                TMP_Text laneNameText = null;
                Button bindingButton = item.GetComponentInChildren<Button>();
                
                // Search for a text component that is NOT the button's child (the label)
                var allTexts = item.GetComponentsInChildren<TMP_Text>();
                foreach(var t in allTexts)
                {
                    if (bindingButton != null && t.transform.IsChildOf(bindingButton.transform)) continue;
                    laneNameText = t;
                    break;
                }

                if (laneNameText != null) laneNameText.text = $"Lane {laneIdx + 1}";
                if (bindingButton != null)
                {
                    var btnText = bindingButton.GetComponentInChildren<TMP_Text>();
                    if (btnText != null) 
                    {
                        Key currentKey = laneIdx < s.laneKeys.Count ? s.laneKeys[laneIdx] : Key.None;
                        btnText.text = currentKey.ToString();
                        bindingButton.onClick.AddListener(() => StartBinding(laneIdx, btnText));
                    }
                }
            }
        }

        private void StartBinding(int laneIdx, TMP_Text btnText)
        {
            bindingLaneIndex = laneIdx;
            if (btnText != null) btnText.text = "...Press Key...";
        }
    }
}
