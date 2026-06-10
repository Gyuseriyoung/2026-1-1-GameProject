using System.Collections.Generic;
using UnityEngine;

namespace CookingGame
{
    [CreateAssetMenu(fileName = "StageData", menuName = "CookingGame/StageData")]
    public class StageData : ScriptableObject
    {
        public string stageName;
        
        [Header("Stage Intro")]
        [TextArea] public string[] introDialogues;
        
        public List<CustomerData> customerQueue;
    }
}
