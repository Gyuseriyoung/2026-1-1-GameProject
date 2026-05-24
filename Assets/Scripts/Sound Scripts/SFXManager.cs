using UnityEngine;

/// <summary>
/// 효과음(SFX) 매니저
/// 빈 오브젝트에 붙이면 해당 씬에서만 동작합니다.
/// 씬이 바뀌면 자동으로 제거됩니다.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("효과음 목록")]
    [Tooltip("효과음을 이름과 함께 등록하세요")]
    public SFXClip[] sfxClips;

    [Range(0f, 1f)]
    [Tooltip("효과음 전체 볼륨")]
    public float masterVolume = 1f;

    [Tooltip("동시에 재생 가능한 최대 채널 수")]
    public int maxChannels = 8;

    private AudioSource[] _channels;
    private int _channelIndex = 0;

    [System.Serializable]
    public class SFXClip
    {
        [Tooltip("코드에서 호출할 이름 (예: Jump, Merge, Hit)")]
        public string clipName;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.8f, 1.2f)] public float pitch = 1f;
        [Tooltip("켜면 재생마다 피치가 랜덤하게 약간 달라져 자연스러워짐")]
        public bool randomPitch = false;
    }

    private void Awake()
    {
        // 씬마다 새로 생성 — 이전 인스턴스는 씬 전환 시 자동 제거됨
        Instance = this;

        _channels = new AudioSource[maxChannels];
        for (int i = 0; i < maxChannels; i++)
        {
            var go = new GameObject($"SFXChannel_{i}");
            go.transform.SetParent(transform);
            _channels[i] = go.AddComponent<AudioSource>();
            _channels[i].playOnAwake = false;
        }
    }

    private void OnDestroy()
    {
        // 씬 전환 시 인스턴스 참조 정리
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────────────────────────
    //  공개 API
    // ─────────────────────────────────────────────

    /// <summary>이름으로 효과음 재생</summary>
    public void Play(string clipName)
    {
        var sfx = FindClip(clipName);
        if (sfx == null) { Debug.LogWarning($"[SFXManager] '{clipName}' 을 찾을 수 없습니다."); return; }
        PlayClip(sfx, 1f);
    }

    /// <summary>이름 + 볼륨 배율로 재생</summary>
    public void Play(string clipName, float volumeMultiplier)
    {
        var sfx = FindClip(clipName);
        if (sfx == null) { Debug.LogWarning($"[SFXManager] '{clipName}' 을 찾을 수 없습니다."); return; }
        PlayClip(sfx, volumeMultiplier);
    }

    /// <summary>AudioClip 직접 재생 (등록 없이 바로 쓸 때)</summary>
    public void PlayDirect(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var src = GetChannel();
        src.clip = clip;
        src.volume = volume * masterVolume;
        src.pitch = pitch;
        src.Play();
    }

    public void SetMasterVolume(float v) => masterVolume = Mathf.Clamp01(v);

    // ─────────────────────────────────────────────
    //  내부
    // ─────────────────────────────────────────────

    private void PlayClip(SFXClip sfx, float volumeMultiplier)
    {
        var src = GetChannel();
        src.clip = sfx.clip;
        src.volume = sfx.volume * masterVolume * volumeMultiplier;
        src.pitch = sfx.randomPitch
            ? sfx.pitch * Random.Range(0.9f, 1.1f)
            : sfx.pitch;
        src.Play();
    }

    private AudioSource GetChannel()
    {
        foreach (var ch in _channels)
            if (!ch.isPlaying) return ch;

        var fallback = _channels[_channelIndex % maxChannels];
        _channelIndex++;
        return fallback;
    }

    private SFXClip FindClip(string name)
    {
        if (sfxClips == null) return null;
        foreach (var s in sfxClips)
            if (s.clipName == name) return s;
        return null;
    }
}