using System.Collections.Generic;
using UnityEngine;

namespace CookingGame
{
    [CreateAssetMenu(fileName = "StageData", menuName = "CookingGame/StageData")]
    public class StageData : ScriptableObject
    {
        public string stageName;
        public List<CustomerData> customerQueue;
        
        [Header("Clear Conditions")]
        public int targetGold;
    }
}
