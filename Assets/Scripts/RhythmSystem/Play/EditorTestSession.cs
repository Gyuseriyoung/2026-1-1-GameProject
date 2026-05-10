using UnityEngine;

namespace RhythmSystem
{
    public static class EditorTestSession
    {
        public static bool IsTestMode = false;
        public static bool IsReturningFromTest = false;
        
        public static ChartData CurrentChart = null;
        public static MergeObjectData MergeObjectData = null;
        public static string MusicFileName = "";
        public static float StartSeekTime = 0f;
        public static float ScrollSpeed = 500f;
        public static float JudgmentX = 8f;
        public static float LastBPM = 120f;
        public static int LastSnapDivisor = 4;
        public static EditorMode LastMode = EditorMode.Place;
        public static string ReturnSceneName = "Chart Editor Scene";
    }
}
