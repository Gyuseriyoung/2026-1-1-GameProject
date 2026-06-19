using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Core;

/// <summary>
/// 통합 오디오 매니저 (프로젝트 내 유일한 오디오 관리 스크립트)
/// BGM 자동 전환, SFX 풀링, 볼륨 설정을 총괄합니다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public class SFXClipConfig
    {
        public string clipName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitch = 1f;
    }

    [System.Serializable]
    public class SceneBGMConfig
    {
        public string sceneName;
        public AudioClip bgmClip;
        public bool loop = true;
        public float fadeDuration = 0.5f;
    }

    [Header("Audio Mixer & Groups")]
    public AudioMixer audioMixer;
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Exposed Parameters")]
    public string masterParam = "MasterVol";
    public string bgmParam = "BGMVol";
    public string sfxParam = "SFXVol";

    [Header("Global SFX Clips")]
    public List<SFXClipConfig> globalSfxClips = new List<SFXClipConfig>();
    private Dictionary<string, SFXClipConfig> _sfxDict = new Dictionary<string, SFXClipConfig>();

    [Header("Scene BGM Configurations")]
    public List<SceneBGMConfig> sceneBgmConfigs = new List<SceneBGMConfig>();

    // BGM Sources
    private AudioSource _bgmSource1;
    private AudioSource _bgmSource2;
    private AudioSource _activeBgmSource;
    private Coroutine _bgmCoroutine;

    // SFX Sources Pool (순환 풀링 방식)
    private AudioSource[] _sfxSources;
    private int _sfxIndex = 0;
    private const int SFX_POOL_SIZE = 32;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM Sources 초기화
        _bgmSource1 = CreateAudioSource("BGM_Source_1", bgmGroup, true);
        _bgmSource2 = CreateAudioSource("BGM_Source_2", bgmGroup, true);
        _activeBgmSource = _bgmSource1;

        // SFX Pool 초기화 (32개 채널)
        _sfxSources = new AudioSource[SFX_POOL_SIZE];
        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            _sfxSources[i] = CreateAudioSource($"SFX_Source_{i}", sfxGroup, false);
        }

        // SFX 딕셔너리 구축
        BuildSFXDictionary();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private AudioSource CreateAudioSource(string name, AudioMixerGroup group, bool loop)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f; // 2D 사운드 고정
        if (group != null) source.outputAudioMixerGroup = group;
        return source;
    }

    public void BuildSFXDictionary()
    {
        _sfxDict.Clear();
        foreach (var config in globalSfxClips)
        {
            if (config != null && !string.IsNullOrEmpty(config.clipName))
            {
                _sfxDict[config.clipName] = config;
            }
        }
    }

    private void Start()
    {
        ApplySavedSettings();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var config in sceneBgmConfigs)
        {
            if (config.sceneName == scene.name)
            {
                PlayBGM(config.bgmClip, config.loop, config.fadeDuration);
                return;
            }
        }
    }

    public void ApplySavedSettings()
    {
        if (audioMixer == null) return;

        if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.Settings != null)
        {
            var sound = GameSettingsManager.Instance.Settings.sound;
            SetVolume(masterParam, sound.muteMaster ? 0f : sound.masterVolume);
            SetVolume(bgmParam, sound.muteBgm ? 0f : sound.bgmVolume);
            SetVolume(sfxParam, sound.muteSfx ? 0f : sound.sfxVolume);
        }
    }

    public void SetVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null) return;
        float db = normalizedVolume <= 0.0001f ? -80f : Mathf.Log10(normalizedVolume) * 20f;
        audioMixer.SetFloat(parameterName, db);
    }

    // ─────────────────────────────────────────────
    // BGM 제어 (단순화된 크로스페이드)
    // ─────────────────────────────────────────────

    public void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 0.5f)
    {
        if (clip == null)
        {
            StopBGM(fadeDuration);
            return;
        }

        if (_activeBgmSource.clip == clip && _activeBgmSource.isPlaying)
        {
            _activeBgmSource.loop = loop;
            return;
        }

        if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);
        _bgmCoroutine = StartCoroutine(CrossFadeBGM(clip, loop, fadeDuration));
    }

    public void StopBGM(float fadeDuration = 0.5f)
    {
        if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);
        _bgmCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));
    }

    public void PauseBGM()
    {
        if (_activeBgmSource != null && _activeBgmSource.isPlaying) _activeBgmSource.Pause();
    }

    public void ResumeBGM()
    {
        if (_activeBgmSource != null && !_activeBgmSource.isPlaying && _activeBgmSource.clip != null) _activeBgmSource.UnPause();
    }

    private IEnumerator CrossFadeBGM(AudioClip newClip, bool loop, float duration)
    {
        AudioSource incoming = (_activeBgmSource == _bgmSource1) ? _bgmSource2 : _bgmSource1;
        AudioSource outgoing = _activeBgmSource;

        incoming.clip = newClip;
        incoming.loop = loop;
        incoming.volume = 0f;
        incoming.Play();

        _activeBgmSource = incoming;

        if (duration <= 0f)
        {
            incoming.volume = 1f;
            if (outgoing != null) outgoing.Stop();
            yield break;
        }

        float elapsed = 0f;
        float startVol = outgoing != null ? outgoing.volume : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            incoming.volume = Mathf.Lerp(0f, 1f, t);
            if (outgoing != null) outgoing.volume = Mathf.Lerp(startVol, 0f, t);

            yield return null;
        }

        incoming.volume = 1f;
        if (outgoing != null) outgoing.Stop();
    }

    private IEnumerator FadeOutBGM(float duration)
    {
        if (_activeBgmSource == null || !_activeBgmSource.isPlaying) yield break;

        float startVal = _activeBgmSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _activeBgmSource.volume = Mathf.Lerp(startVal, 0f, elapsed / duration);
            yield return null;
        }

        _activeBgmSource.Stop();
        _activeBgmSource.volume = 1f;
    }

    // ─────────────────────────────────────────────
    // SFX 재생
    // ─────────────────────────────────────────────

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        float sfxVol = 1f;
        bool mixerHasSfxParam = false;
        if (audioMixer != null)
        {
            mixerHasSfxParam = audioMixer.GetFloat(sfxParam, out _);
        }

        if (sfxGroup == null || !mixerHasSfxParam)
        {
            if (GameSettingsManager.Instance != null && GameSettingsManager.Instance.Settings != null)
            {
                var sound = GameSettingsManager.Instance.Settings.sound;
                sfxVol = sound.muteSfx ? 0f : sound.sfxVolume;
            }
        }

        AudioSource source = _sfxSources[_sfxIndex];
        source.clip = clip;
        source.volume = volume * sfxVol;
        source.pitch = pitch;
        source.Play();

        _sfxIndex = (_sfxIndex + 1) % SFX_POOL_SIZE;
    }

    public void PlaySFX(string clipName, float volumeMultiplier = 1f)
    {
        if (string.IsNullOrEmpty(clipName)) return;

        if (_sfxDict.TryGetValue(clipName, out var sfx))
        {
            PlaySFX(sfx.clip, sfx.volume * volumeMultiplier, sfx.pitch);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX '{clipName}' not found.");
        }
    }
}
