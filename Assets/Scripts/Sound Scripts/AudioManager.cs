using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using Core;

/// <summary>
/// 통합 오디오 설정 및 SFX 풀링 매니저
/// 볼륨 설정(Mixer)과 SFX 재생을 담당하며, BGM은 각 씬의 BGMPlayer가 담당합니다.
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

    [Header("Audio Mixer & Groups")]
    public AudioMixer audioMixer;
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Exposed Parameters")]
    public string masterParam = "MasterVol";
    public string bgmParam = "BGMVol";
    public string sfxParam = "SFXVol";

    [Header("SFX Pooling")]
    public int initialSfxPoolSize = 10;
    private List<AudioSource> _sfxPool = new List<AudioSource>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSFXPool();
    }

    private void InitializeSFXPool()
    {
        for (int i = 0; i < initialSfxPoolSize; i++)
        {
            CreateNewSFXSource();
        }
    }

    private AudioSource CreateNewSFXSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        if (sfxGroup != null) source.outputAudioMixerGroup = sfxGroup;
        _sfxPool.Add(source);
        return source;
    }

    private void Start()
    {
        ApplySavedSettings();
    }

    // ─────────────────────────────────────────────
    //  볼륨 설정 제어
    // ─────────────────────────────────────────────

    public void ApplySavedSettings()
    {
        if (audioMixer == null) return;

        var sound = GameSettingsManager.Instance.Settings.sound;
        SetVolume(masterParam, sound.muteMaster ? 0f : sound.masterVolume);
        SetVolume(bgmParam, sound.muteBgm ? 0f : sound.bgmVolume);
        SetVolume(sfxParam, sound.muteSfx ? 0f : sound.sfxVolume);
    }

    public void SetVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null) return;
        float db = normalizedVolume <= 0.0001f ? -80f : Mathf.Log10(normalizedVolume) * 20f;
        audioMixer.SetFloat(parameterName, db);
    }

    // ─────────────────────────────────────────────
    //  SFX 재생 (풀링)
    // ─────────────────────────────────────────────

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        AudioSource source = GetAvailableSFXSource();
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (var source in _sfxPool)
        {
            if (!source.isPlaying) return source;
        }
        return CreateNewSFXSource();
    }
}
