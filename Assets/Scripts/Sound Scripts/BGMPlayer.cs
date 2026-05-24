using UnityEngine;

/// <summary>
/// 빈 오브젝트에 붙이고 Inspector에서 AudioClip을 연결하면
/// 게임 시작 시 자동으로 BGM이 재생됩니다.
/// 씬이 바뀌면 자동으로 제거됩니다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    [Header("BGM 설정")]
    [Tooltip("재생할 음악 파일을 여기에 드래그하세요")]
    public AudioClip bgmClip;

    [Range(0f, 1f)]
    [Tooltip("볼륨 (0 = 무음, 1 = 최대)")]
    public float volume = 0.5f;

    [Tooltip("반복 재생 여부")]
    public bool loop = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = bgmClip;
        _audioSource.volume = volume;
        _audioSource.loop = loop;
        _audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[BGMPlayer] AudioClip이 연결되지 않았습니다. Inspector에서 bgmClip을 설정해주세요.");
            return;
        }
        _audioSource.Play();
    }

    public void Play() => _audioSource.Play();
    public void Stop() => _audioSource.Stop();
    public void Pause() => _audioSource.Pause();
    public void Resume() => _audioSource.UnPause();

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        _audioSource.volume = volume;
    }

    public void ChangeBGM(AudioClip newClip)
    {
        _audioSource.Stop();
        _audioSource.clip = newClip;
        bgmClip = newClip;
        _audioSource.Play();
    }
}