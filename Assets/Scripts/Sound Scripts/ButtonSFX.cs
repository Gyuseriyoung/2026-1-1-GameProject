using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Button 클릭 효과음 컴포넌트
/// Button 오브젝트에 붙이고 Inspector에서 AudioClip만 드래그하면 끝
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    [Header("클릭 효과음 — 여기에 드래그하세요")]
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    public float pitch = 1f;

    private Button _button;
    private AudioSource _source;

    private void Awake()
    {
        _button = GetComponent<Button>();

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;

        if (clip == null)
            Debug.LogError($"[ButtonSFX] '{gameObject.name}' — clip이 비어 있습니다! Inspector에서 AudioClip을 드래그해 주세요.");
    }

    private void Start()
    {
        _button.onClick.AddListener(PlaySFX);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(PlaySFX);
    }

    public void PlaySFX()
    {
        if (clip == null)
        {
            Debug.LogError($"[ButtonSFX] '{gameObject.name}' — clip이 없어서 재생할 수 없습니다.");
            return;
        }

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayDirect(clip, volume, pitch);
        }
        else
        {
            _source.clip = clip;
            _source.volume = volume;
            _source.pitch = pitch;
            _source.Play();
        }
    }
}