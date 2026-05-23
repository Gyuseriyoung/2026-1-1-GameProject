using UnityEngine;
using UnityEngine.UI;

namespace RhythmSystem
{
    [RequireComponent(typeof(Image))]
    public class EditorNoteController : MonoBehaviour
    {
        public NoteData data;
        public RectTransform holdBody;
        
        private Image noteImage;
        private bool isSelected = false;
        private Color holdBarColor = Color.yellow; // Default color

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
                holdBarColor = category.HoldBodyColor;

                if (data.objectIndex >= 0 && data.objectIndex < category.MergeDataList.Length)
                {
                    var obj = category.MergeDataList[data.objectIndex];
                    if (obj.sprite != null)
                    {
                        noteImage.sprite = obj.sprite;
                        UpdateVisuals();
                    }
                }
            }
        }

        public void UpdateVisuals()
        {
            if (noteImage != null)
                noteImage.color = isSelected ? Color.cyan : Color.white;

            if (holdBody != null)
            {
                if (data.type == NoteType.Hold)
                {
                    holdBody.gameObject.SetActive(true);
                    
                    // Width = (length in seconds) * pixels per second
                    float width = (data.length / 1000f) * FindAnyObjectByType<EditorManager>().currentScrollSpeed;
                    holdBody.sizeDelta = new Vector2(width, holdBody.sizeDelta.y);
                    
                    // Position the body to start from the note and extend backwards (since notes move left)
                    // Or extension depends on how your timeline is oriented.
                    // Assuming notes move left (time increases to the left), body should extend to the left.
                    holdBody.anchoredPosition = new Vector2(-width / 2f, 0); 
                    // Actually, if anchor is left, it's easier.
                    
                    Image bodyImage = holdBody.GetComponent<Image>();
                    if (bodyImage != null)
                    {
                        bodyImage.type = Image.Type.Tiled; // Prevent stretching, use tiling
                        Color c = isSelected ? Color.cyan : holdBarColor;
                        c.a = 0.6f;
                        bodyImage.color = c;
                    }
                }
                else
                {
                    holdBody.gameObject.SetActive(false);
                }
            }
        }
    }
}
