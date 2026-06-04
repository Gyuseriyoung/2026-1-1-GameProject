using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 화면 아무 곳이나 클릭하면 효과음을 재생합니다.
/// 빈 오브젝트 하나에 붙여서 씬에 하나만 두면 됩니다.
/// New Input System 기반
/// </summary>
public class ClickSFX : MonoBehaviour
{
    [Header("클릭 효과음 — 여기에 드래그하세요")]
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    public float pitch = 1f;

    [Tooltip("버튼 등 UI 위를 클릭할 때도 재생할지 여부")]
    public bool playOnUI = false;

    private AudioSource _source;

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;

        if (clip == null)
            Debug.LogError("[ClickSFX] clip이 비어 있습니다! Inspector에서 AudioClip을 드래그해 주세요.");
    }

    private void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // UI 위 클릭 제외 (버튼 등)
        if (!playOnUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        PlaySFX();
    }

    public void PlaySFX()
    {
        if (clip == null) return;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayDirect(clip, volume, pitch);
        else
        {
            _source.clip = clip;
            _source.volume = volume;
            _source.pitch = pitch;
            _source.Play();
        }
    }
}