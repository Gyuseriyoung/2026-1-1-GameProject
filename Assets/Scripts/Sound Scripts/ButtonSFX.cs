using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Button 클릭 효과음 컴포넌트
/// 통합 오디오 매니저를 통해 재생됩니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    [Header("클릭 효과음")]
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    public float pitch = 1f;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (clip == null)
            Debug.LogError($"[ButtonSFX] '{gameObject.name}' — clip이 비어 있습니다!");
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
        if (clip == null) return;

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayDirect(clip, volume, pitch);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip, volume, pitch);
        }
    }
}
