using System;
using System.Collections.Generic;
using UnityEngine;

public class MergeManager : MonoBehaviour
{
    public List<MergeObject> mergeObjects;
    [SerializeField] private GameObject ObjectPrefab;
    [SerializeField] private Transform ObjectContainer;

    [SerializeField] private int currentType = 0;
    [SerializeField] private int currentIndex = 0;
    private MergeObject currentObject;


    void OnEnable()
    {
        EventManager.OnMergeDataAdd += AutoMergeObject;
    }

    void OnDisable()
    {
        EventManager.OnMergeDataAdd -= AutoMergeObject;
    }

    public void CreateMergeObject()
    {
        GameObject GO = Instantiate(ObjectPrefab, ObjectContainer);
        GO.GetComponent<MergeObject>().Init(currentType, currentIndex);
        mergeObjects.Add(GO.GetComponent<MergeObject>());
        currentObject = GO.GetComponent<MergeObject>();
        EventManager.OnMergeDataAdd?.Invoke();
    }

    private void AutoMergeObject()
    {
        MergeObject target = null;
        foreach (MergeObject mergeObject in mergeObjects)
        {
            if (mergeObject != currentObject && currentObject.type == mergeObject.type && currentObject.index == mergeObject.index)
            {
                target = mergeObject;
                break;
            }
        }

        if (target != null)
        {
            int nextIndex = currentObject.index + 1;
            int type = currentObject.type;

            if (nextIndex >= currentObject.data.MergeData[type].MergeDataList.Length)
            {
                return;
            }

            mergeObjects.Remove(currentObject);
            mergeObjects.Remove(target);

            GameObject obj1 = currentObject.gameObject;
            GameObject obj2 = target.gameObject;

            GameObject GO = Instantiate(ObjectPrefab, ObjectContainer);
            currentObject = GO.GetComponent<MergeObject>();
            currentObject.Init(type, nextIndex);
            mergeObjects.Add(currentObject);

            Destroy(obj1);
            Destroy(obj2);

            AutoMergeObject();
        }
    }
}
