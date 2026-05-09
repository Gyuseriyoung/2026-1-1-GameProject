using UnityEngine;
using UnityEngine.UI;

namespace RhythmSystem
{
    [RequireComponent(typeof(Image))]
    public class NoteController : MonoBehaviour
    {
        public NoteData data;
        
        private Image noteImage;
        private bool isSelected = false;

        private void Awake()
        {
            noteImage = GetComponent<Image>();
        }

        public void SetSelection(bool selected)
        {
            isSelected = selected;
            UpdateVisuals();
        }

        public void ApplyMergeSprite(MergeObjectData mergeObjectData)
        {
            if (mergeObjectData == null || noteImage == null) return;

            if (data.mergeType >= 0 && data.mergeType < mergeObjectData.MergeData.Length)
            {
                var category = mergeObjectData.MergeData[data.mergeType];
                if (data.objectIndex >= 0 && data.objectIndex < category.MergeDataList.Length)
                {
                    var obj = category.MergeDataList[data.objectIndex];
                    if (obj.sprite != null)
                    {
                        noteImage.sprite = obj.sprite;
                        // Optional: Reset color to white so sprite isn't tinted
                        noteImage.color = isSelected ? Color.cyan : Color.white;
                    }
                }
            }
        }

        private void UpdateVisuals()
        {
            if (noteImage == null) return;
            
            // Highlight selected notes (e.g., Cyan), otherwise White
            noteImage.color = isSelected ? Color.cyan : Color.white;
        }
    }
}
