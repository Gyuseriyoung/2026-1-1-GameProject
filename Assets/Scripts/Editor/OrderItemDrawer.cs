using UnityEditor;
using UnityEngine;
using CookingGame;
using System.Linq;

[CustomPropertyDrawer(typeof(OrderItem))]
public class OrderItemDrawer : PropertyDrawer
{
    private MergeObjectData mergeDataCache;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (mergeDataCache == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:MergeObjectData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                mergeDataCache = AssetDatabase.LoadAssetAtPath<MergeObjectData>(path);
            }
        }

        EditorGUI.BeginProperty(position, label, property);
        
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        var typeProp = property.FindPropertyRelative("targetMergeType");
        var indexProp = property.FindPropertyRelative("targetMergeIndex");
        var countProp = property.FindPropertyRelative("count");

        if (mergeDataCache != null && mergeDataCache.MergeData != null && mergeDataCache.MergeData.Length > 0)
        {
            float totalWidth = position.width;
            Rect typeRect = new Rect(position.x, position.y, totalWidth * 0.38f, position.height);
            Rect indexRect = new Rect(position.x + totalWidth * 0.40f, position.y, totalWidth * 0.43f, position.height);
            Rect countRect = new Rect(position.x + totalWidth * 0.85f, position.y, totalWidth * 0.15f, position.height);

            string[] categoryNames = mergeDataCache.MergeData.Select(m => string.IsNullOrEmpty(m.TypeName) ? "Unnamed" : m.TypeName).ToArray();
            
            typeProp.intValue = EditorGUI.Popup(typeRect, typeProp.intValue, categoryNames);
            
            int validCategoryIdx = Mathf.Clamp(typeProp.intValue, 0, categoryNames.Length - 1);
            typeProp.intValue = validCategoryIdx;

            var currentCategory = mergeDataCache.MergeData[validCategoryIdx];
            if (currentCategory.MergeDataList != null && currentCategory.MergeDataList.Length > 0)
            {
                string[] itemNames = currentCategory.MergeDataList.Select(i => string.IsNullOrEmpty(i.Name) ? "Unnamed Item" : i.Name).ToArray();
                int validItemIdx = Mathf.Clamp(indexProp.intValue, 0, itemNames.Length - 1);
                indexProp.intValue = EditorGUI.Popup(indexRect, validItemIdx, itemNames);
            }
            else
            {
                EditorGUI.LabelField(indexRect, "No Items");
                indexProp.intValue = 0;
            }

            EditorGUI.PropertyField(countRect, countProp, GUIContent.none);
        }
        else
        {
            EditorGUI.LabelField(position, "Need MergeObjectData asset");
        }

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
