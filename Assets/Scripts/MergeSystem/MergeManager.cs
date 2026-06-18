using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CookingGame;

public class MergeManager : MonoBehaviour
{
    public List<MergeObject> mergeObjects;
    [SerializeField] private GameObject ObjectPrefab;
    [SerializeField] private Transform ObjectContainer1;
    [SerializeField] private Transform ObjectContainer2;

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
        CreateMergeObject(currentType, currentIndex);
    }

    public void CreateMergeObject(int type, int index)
    {
        GameObject GO = null;;
        if (type == 0){ GO = Instantiate(ObjectPrefab, ObjectContainer1); }
        else if (type == 1){ GO = Instantiate(ObjectPrefab, ObjectContainer2); }
        MergeObject mo = GO.GetComponent<MergeObject>();
        mo.Init(type, index);
        mergeObjects.Add(mo);
        currentObject = mo;
        EventManager.OnMergeDataAdd?.Invoke();
    }

    public bool IsOrderExactMatch(List<OrderItem> orders)
    {
        if (orders == null || orders.Count == 0) return mergeObjects.Count == 0;

        // 1. Check if all required items exist in exact counts
        foreach (var order in orders)
        {
            int count = mergeObjects.Count(mo => mo.type == order.targetMergeType && mo.index == order.targetMergeIndex);
            if (count != order.count) return false;
        }

        // 2. Check if there are any extra items not in the order
        int totalRequiredCount = orders.Sum(o => o.count);
        if (mergeObjects.Count != totalRequiredCount) return false;

        return true;
    }

    public void ClearAllObjects()
    {
        foreach (var obj in mergeObjects)
        {
            if (obj != null) Destroy(obj.gameObject);
        }
        mergeObjects.Clear();
        currentObject = null;
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

            GameObject GO = null;
            if (type == 0){ GO = Instantiate(ObjectPrefab, ObjectContainer1); }
            else if (type == 1){ GO = Instantiate(ObjectPrefab, ObjectContainer2); }
            currentObject = GO.GetComponent<MergeObject>();
            currentObject.Init(type, nextIndex);
            mergeObjects.Add(currentObject);

            Destroy(obj1);
            Destroy(obj2);

            AutoMergeObject();
        }
    }
}
