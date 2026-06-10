using UnityEngine;
using System.Collections.Generic;

namespace CookingGame
{
    /// <summary>
    /// Static class to carry session data between Dialogue Scene and Gameplay Scene.
    /// </summary>
    public static class CookingSession
    {
        public static StageData CurrentStage;
        public static int CurrentCustomerIndex;
        public static CustomerData CurrentCustomer;
        
        public static bool LastGameSuccess;
        public static bool IsReturningFromResult;

        public static void StartSession(StageData stage)
        {
            CurrentStage = stage;
            CurrentCustomerIndex = 0;
            IsReturningFromResult = false;
        }

        public static void Clear()
        {
            CurrentStage = null;
            CurrentCustomerIndex = 0;
            CurrentCustomer = null;
            IsReturningFromResult = false;
        }
    }
}
