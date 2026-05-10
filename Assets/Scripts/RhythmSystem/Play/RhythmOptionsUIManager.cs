using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmSystem.Play
{
    public class RhythmOptionsUIManager : MonoBehaviour
    {
        public PlayNoteSpawner playNoteSpawner;
        public RhythmGameManager gameManager;
        
        [Header("UI Elements")]
        public GameObject panel;
        public Button restartButton;
        public Button quitButton;
        
        [Header("Scroll Speed (Input Field)")]
        public TMP_InputField scrollSpeedInput;
        
        [Header("Layout (Sliders)")]
        public Slider judgmentXSlider;
        public TMP_Text judgmentXText;
        
        public Slider laneSpacingSlider;
        public TMP_Text laneSpacingText;

        private void Start()
        {
            LoadCurrentSettings();
            
            // Setup Listeners
            if (scrollSpeedInput != null)
                scrollSpeedInput.onEndEdit.AddListener(OnScrollSpeedEndEdit);
                
            if (judgmentXSlider != null)
                judgmentXSlider.onValueChanged.AddListener(OnJudgmentXChanged);
                
            if (laneSpacingSlider != null)
                laneSpacingSlider.onValueChanged.AddListener(OnLaneSpacingChanged);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            
            if (panel != null) panel.SetActive(false);
        }

        public void TogglePanel()
        {
            if (panel == null || gameManager == null) return;
            
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);
            
            gameManager.PauseGame(isActive);
            
            if (!isActive)
            {
                RhythmSettingsManager.SaveSettings();
            }
        }

        public void OnRestartClicked()
        {
            if (gameManager != null)
            {
                gameManager.RestartGame();
                TogglePanel();
            }
        }

        public void OnQuitClicked()
        {
            if (gameManager != null)
            {
                gameManager.QuitGame();
            }
        }

        private void LoadCurrentSettings()
        {
            var s = RhythmSettingsManager.Settings;
            
            if (scrollSpeedInput != null)
                scrollSpeedInput.text = $"{s.scrollSpeed:F0}";
            
            if (judgmentXSlider != null)
            {
                judgmentXSlider.value = s.judgmentX;
                judgmentXText.text = $"{s.judgmentX:F1}";
            }
            
            if (laneSpacingSlider != null)
            {
                laneSpacingSlider.value = s.laneSpacing;
                laneSpacingText.text = $"{s.laneSpacing:F1}";
            }
        }

        private void OnScrollSpeedEndEdit(string value)
        {
            if (float.TryParse(value, out float result))
            {
                result = Mathf.Clamp(result, 100f, 3000f);
                RhythmSettingsManager.Settings.scrollSpeed = result;
                scrollSpeedInput.text = $"{result:F0}";
            }
            else
            {
                scrollSpeedInput.text = $"{RhythmSettingsManager.Settings.scrollSpeed:F0}";
            }
        }

        private void OnJudgmentXChanged(float value)
        {
            RhythmSettingsManager.Settings.judgmentX = value;
            if (judgmentXText != null) judgmentXText.text = $"{value:F1}";
            if (playNoteSpawner != null && gameManager != null)
                playNoteSpawner.UpdateLanes(gameManager.GetCurrentTimeMs());
        }

        private void OnLaneSpacingChanged(float value)
        {
            RhythmSettingsManager.Settings.laneSpacing = value;
            if (laneSpacingText != null) laneSpacingText.text = $"{value:F1}";
            if (playNoteSpawner != null && gameManager != null)
                playNoteSpawner.UpdateLanes(gameManager.GetCurrentTimeMs());
        }
    }
}
