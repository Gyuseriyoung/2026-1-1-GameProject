using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmSystem
{
    public class EditorGimmickController : MonoBehaviour
    {
        public GimmickEvent data;
        private Image image;
        private TMP_Text text;
        private bool isSelected = false;
        private EditorManager editorManager;

        private void Awake()
        {
            image = GetComponentInChildren<Image>();
            text = GetComponentInChildren<TMP_Text>();
            editorManager = FindFirstObjectByType<EditorManager>();
        }

        public void SetSelection(bool selected)
        {
            isSelected = selected;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (image != null)
            {
                // Highlight if selected
                if (isSelected)
                {
                    image.color = Color.white; // Or any highlight color
                }
                else
                {
                    // Restore color based on type
                    switch (data.type)
                    {
                        case GimmickType.LaneMoveY: image.color = Color.yellow; break;
                        case GimmickType.LaneMoveX: image.color = new Color(1f, 0.5f, 0f); break;
                        case GimmickType.BPMChange: image.color = Color.cyan; break;
                        case GimmickType.LaneAdd: image.color = Color.green; break;
                        case GimmickType.LaneRemove: image.color = Color.red; break;
                    }
                }
            }

            if (text != null)
            {
                string info = "";
                switch (data.type)
                {
                    case GimmickType.LaneMoveY: info = $"Y:{data.value:F1}"; break;
                    case GimmickType.LaneMoveX: info = $"X:{data.value:F1}"; break;
                    case GimmickType.BPMChange: info = $"BPM:{data.value}"; break;
                    case GimmickType.LaneAdd: info = $"+{data.value}"; break;
                    case GimmickType.LaneRemove: info = $"-{data.value}"; break;
                }
                text.text = info;
            }
        }
    }
}
