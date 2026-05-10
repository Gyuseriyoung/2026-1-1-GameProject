using UnityEngine;
using System.IO;

namespace RhythmSystem
{
    [System.Serializable]
    public class UserSettings
    {
        public float scrollSpeed = 500f; // Pixels per second
        public float judgmentX = 0f;     // Default hit zone X
        public float laneSpacing = 0.7f; // Vertical distance between lanes
    }

    public static class RhythmSettingsManager
    {
        private static string SettingsPath => Path.Combine(Application.persistentDataPath, "RhythmSettings.json");
        private static UserSettings _settings;

        public static UserSettings Settings
        {
            get
            {
                if (_settings == null) LoadSettings();
                return _settings;
            }
        }

        public static void LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                _settings = JsonUtility.FromJson<UserSettings>(json);
            }
            else
            {
                _settings = new UserSettings();
                SaveSettings();
            }
        }

        public static void SaveSettings()
        {
            if (_settings == null) _settings = new UserSettings();
            string json = JsonUtility.ToJson(_settings, true);
            File.WriteAllText(SettingsPath, json);
        }

        public static float GetWorldScrollSpeed()
        {
            return Settings.scrollSpeed / 100f;
        }
    }
}
