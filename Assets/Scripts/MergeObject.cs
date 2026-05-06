using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MergeObject : MonoBehaviour
{
    public MergeObjectData data;
    [SerializeField] private Image objectImage;
    [SerializeField] private TextMeshProUGUI text;
    public int type;
    public int index;

    public void Init(int _type, int _index)
    {
        objectImage.sprite = data.MergeData[_type].MergeDataList[_index].sprite;
        text.text = data.MergeData[_type].MergeDataList[_index].Name;
        type = data.MergeData[_type].MergeType;
        index = _index;
    }
}
