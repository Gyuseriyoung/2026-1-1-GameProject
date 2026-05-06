using UnityEngine;

[System.Serializable]
public class MergeData
{
    public string TypeName;
    public int MergeType;
    public ObjectData[] MergeDataList;
}

[System.Serializable]
public class ObjectData
{
    public string Name;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "MergeObjectData.asset", menuName = "MergeObjectData")]
public class MergeObjectData : ScriptableObject
{
    public MergeData[] MergeData;
}
