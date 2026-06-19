using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmSystem
{
    public class MergeObjectUIItem : MonoBehaviour
    {
        [SerializeField] private Image objectImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI countText;
        
        public int categoryIndex { get; private set; }
        public int objectIndex { get; private set; }

        public void Setup(int catIdx, int objIdx, Sprite sprite, string name, int count = 1)
        {
            categoryIndex = catIdx;
            objectIndex = objIdx;
            
            if (objectImage != null) objectImage.sprite = sprite;
            if (nameText != null) nameText.text = name;
            
            if (countText != null)
            {
                if (count > 1)
                {
                    countText.gameObject.SetActive(true);
                    countText.text = $"x{count}";
                }
                else
                {
                    countText.gameObject.SetActive(false);
                }
            }
        }
    }
}
