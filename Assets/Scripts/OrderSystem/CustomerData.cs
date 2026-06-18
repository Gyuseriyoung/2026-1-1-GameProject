using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookingGame
{
    [Serializable]
    public class OrderItem
    {
        public int targetMergeType;  // Category index
        public int targetMergeIndex; // Item index within category
        public int count = 1;        // Required quantity
    }

    [CreateAssetMenu(fileName = "CustomerData", menuName = "CookingGame/CustomerData")]
    public class CustomerData : ScriptableObject
    {
        [Header("Customer Info")]
        public string customerName;
        public Sprite portrait;
        public Sprite backgroundImage;
        public AnimatorOverrideController animatorOverride;

        [Header("Opening Dialogues")]
        [TextArea] public string[] openingDialogues;

        [Header("Order Requirements")]
        public List<OrderItem> orders;

        [Header("Chart Json")]
        public TextAsset chartJson;
        
        [Header("Result Dialogues")]
        [TextArea] public string successDialogue;
        [TextArea] public string failureDialogue;

        [Header("혼잣말")]
        [TextArea] public string[] soliloquies;
    }
}
