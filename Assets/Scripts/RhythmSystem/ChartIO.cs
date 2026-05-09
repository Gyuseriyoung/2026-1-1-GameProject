using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RhythmSystem
{
    public static class ChartIO
    {
        public static string DefaultChartDirectory => Path.Combine(Application.dataPath, "Charts");

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
            string jsonPath = Path.Combine(DefaultChartDirectory, fileName + ".json");
            string osuPath = Path.Combine(DefaultChartDirectory, fileName + ".osu");

            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                return LoadChartFromJson(json);
            }
            
            if (File.Exists(osuPath))
            {
                return ImportFromOsu(osuPath);
            }

            Debug.LogError($"Chart file not found: {fileName} in {DefaultChartDirectory}");
            return null;
        }

        public static ChartData ImportFromOsu(string filePath)
        {
            ChartData chart = new ChartData();
            string[] lines = File.ReadAllLines(filePath);
            string currentSection = "";
            int laneCount = 4; // Default for mania

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    continue;
                }

                ProcessOsuLine(currentSection, trimmed, chart, ref laneCount);
            }

            // Initialize lanes if none found
            if (chart.lanes.Count == 0)
            {
                for (int i = 0; i < laneCount; i++)
                {
                    chart.lanes.Add(new LaneConfig { laneIndex = i });
                }
            }

            return chart;
        }

        private static void ProcessOsuLine(string section, string line, ChartData chart, ref int laneCount)
        {
            switch (section)
            {
                case "General":
                    if (line.StartsWith("AudioFilename:"))
                        chart.metadata.audioFileName = line.Substring(14).Trim();
                    break;

                case "Metadata":
                    if (line.StartsWith("Title:")) chart.metadata.title = line.Substring(6).Trim();
                    if (line.StartsWith("Artist:")) chart.metadata.artist = line.Substring(7).Trim();
                    if (line.StartsWith("Creator:")) chart.metadata.creator = line.Substring(8).Trim();
                    break;

                case "Difficulty":
                    if (line.StartsWith("CircleSize:"))
                    {
                        if (int.TryParse(line.Substring(11).Trim(), out int cs))
                            laneCount = cs;
                    }
                    break;

                case "TimingPoints":
                    ParseTimingPoint(line, chart);
                    break;

                case "HitObjects":
                    ParseHitObject(line, chart, laneCount);
                    break;
            }
        }

        private static void ParseTimingPoint(string line, ChartData chart)
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 2)
            {
                float time = float.Parse(parts[0]);
                float beatLength = float.Parse(parts[1]);
                int meter = parts.Length >= 3 ? int.Parse(parts[2]) : 4;

                if (beatLength > 0) // Uninherited timing point (BPM change)
                {
                    float bpm = 60000f / beatLength;
                    chart.timingPoints.Add(new TimingPoint { time = time, bpm = bpm, meter = meter });
                }
            }
        }

        private static void ParseHitObject(string line, ChartData chart, int laneCount)
        {
            string[] parts = line.Split(',');
            if (parts.Length >= 5)
            {
                float x = float.Parse(parts[0]);
                float time = float.Parse(parts[2]);
                int type = int.Parse(parts[3]);

                int lane = Mathf.FloorToInt(x * laneCount / 512f);
                lane = Mathf.Clamp(lane, 0, laneCount - 1);

                NoteData note = new NoteData { time = time, laneIndex = lane, type = NoteType.Tap };

                // Long Note (Hold) check: bit 7 (128)
                if ((type & 128) != 0)
                {
                    note.type = NoteType.Hold;
                    if (parts.Length >= 6)
                    {
                        string[] extraParts = parts[5].Split(':');
                        if (extraParts.Length > 0)
                        {
                            float endTime = float.Parse(extraParts[0]);
                            note.length = endTime - time;
                        }
                    }
                }

                chart.notes.Add(note);
            }
        }
    }
}
