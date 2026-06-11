using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RhythmSystem
{
    public static class ChartIO
    {
        public const string DefaultChartDirectory = "Assets/Resources/Charts";

        public static string SaveChartToJson(ChartData chartData)
        {
            return JsonUtility.ToJson(chartData, true);
        }

        public static ChartData LoadChartFromJson(string json)
        {
            return JsonUtility.FromJson<ChartData>(json);
        }

        public static void SaveToFile(string fileName, ChartData chartData)
        {
            if (!Directory.Exists(DefaultChartDirectory))
            {
                Directory.CreateDirectory(DefaultChartDirectory);
            }

            string filePath = Path.Combine(DefaultChartDirectory, fileName + ".json");
            string json = SaveChartToJson(chartData);
            File.WriteAllText(filePath, json);
            Debug.Log($"Chart saved to: {filePath}");
        }

        public static ChartData LoadFromFile(string fileName)
        {
#if UNITY_EDITOR
            string jsonPath = Path.Combine(DefaultChartDirectory, fileName + ".json");

            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                return LoadChartFromJson(json);
            }
#endif

            TextAsset chartAsset = Resources.Load<TextAsset>("Charts/" + fileName);
            if (chartAsset != null)
            {
                return LoadChartFromJson(chartAsset.text);
            }

            Debug.LogError($"Chart file not found: {fileName} in Resources/Charts/ or {DefaultChartDirectory}");
            return null;
        }
    }
}
