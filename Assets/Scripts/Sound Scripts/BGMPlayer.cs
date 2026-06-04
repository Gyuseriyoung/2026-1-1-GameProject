using UnityEngine;

/// <summary>
/// 씬에 배치되어 해당 씬의 배경음(BGM)을 담당합니다.
/// 오브젝트가 파괴되면(씬 전환 시) 자동으로 소리가 멈춥니다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMPlayer : MonoBehaviour
{
    [Header("BGM 설정")]
    public AudioClip bgmClip;

    [Range(0f, 1f)]
    public float volume = 1f;

    public bool loop = true;
    public bool playOnStart = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = bgmClip;
        _audioSource.volume = volume;
        _audioSource.loop = loop;
        _audioSource.playOnAwake = false;

        // AudioManager로부터 전역 BGM 믹서 그룹을 가져와 연결
        if (AudioManager.Instance != null && AudioManager.Instance.bgmGroup != null)
        {
            _audioSource.outputAudioMixerGroup = AudioManager.Instance.bgmGroup;
        }
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (_audioSource.clip != null)
        {
            _audioSource.Play();
        }
    }

    public void Stop() => _audioSource.Stop();
    public void Pause() => _audioSource.Pause();
    public void Resume() => _audioSource.UnPause();

    public void ChangeBGM(AudioClip newClip)
    {
        _audioSource.Stop();
        _audioSource.clip = newClip;
        _audioSource.Play();
    }
}
