using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace RhythmSystem
{
    [System.Serializable]
    public class RhythmSettings
    {
        public float scrollSpeed = 5.0f;  // World units per second
        public float judgmentX = 0f;     // World units
        public float judgmentY = -3.0f;     // World units offset
        public float laneSpacing = 0.65f; // World units
        public float globalOffset = 0;   // ms, to compensate for audio/input latency in build
        public float editorGlobalOffset = 0; // ms, latency compensation specific to Unity Editor
        
        public List<Key> laneKeys = new List<Key>();

        public void EnsureDefaults()
        {
            if (laneKeys == null) laneKeys = new List<Key>();
            if (laneKeys.Count == 0)
            {
                laneKeys.AddRange(new Key[] { 
                    Key.D, Key.F, Key.J, Key.K, 
                    Key.S, Key.L, Key.A, Key.Semicolon 
                });
            }
            // Ensure at least 8 slots
            while (laneKeys.Count < 8) laneKeys.Add(Key.None);
        }
    }

    [System.Serializable]
    public class EditorSettings
    {
        public float scrollSpeed = 500f;  // Pixels per second
        public float judgmentX = 400f;   // Pixels
        public float laneSpacing = 65f;  // Pixels
        public int snapDivisor = 4;
    }
}

namespace Core
{
    using RhythmSystem;

    [System.Serializable]
    public class SoundSettings
    {
        public float masterVolume = 1f;
        public float bgmVolume = 0.5f;
        public float sfxVolume = 1f;
        public bool muteMaster = false;
        public bool muteBgm = false;
        public bool muteSfx = false;
    }

    [System.Serializable]
    public class GameSettings
    {
        public RhythmSettings rhythm = new RhythmSettings();
        public EditorSettings editor = new EditorSettings();
        public SoundSettings sound = new SoundSettings();

        public void EnsureDefaults()
        {
            if (rhythm == null) rhythm = new RhythmSettings();
            if (editor == null) editor = new EditorSettings();
            if (sound == null) sound = new SoundSettings();

            rhythm.EnsureDefaults();
        }
    }

    public class GameSettingsManager : MonoBehaviour
    {
        private static GameSettingsManager _instance;
        public static GameSettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameSettingsManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameSettingsManager");
                        _instance = go.AddComponent<GameSettingsManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private static string SettingsPath => Path.Combine(Application.persistentDataPath, "GameSettings.json");
        
        [SerializeField] private GameSettings _settings;
        public GameSettings Settings
        {
            get
            {
                if (_settings == null) LoadSettings();
                return _settings;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(SettingsPath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsPath);
                    _settings = JsonUtility.FromJson<GameSettings>(json);
                    if (_settings == null) _settings = new GameSettings();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load settings: {e.Message}");
                    _settings = new GameSettings();
                }
            }
            else
            {
                _settings = new GameSettings();
                SaveSettings();
            }

            _settings.EnsureDefaults();
        }

        public void SaveSettings()
        {
            if (_settings == null) _settings = new GameSettings();
            _settings.EnsureDefaults();
            string json = JsonUtility.ToJson(_settings, true);
            File.WriteAllText(SettingsPath, json);
        }
    }
}
