using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 화면 클릭 시 효과음을 재생합니다. 
/// 통합 오디오 매니저를 통해 재생됩니다.
/// </summary>
public class ClickSFX : MonoBehaviour
{
    [Header("클릭 효과음")]
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    public float pitch = 1f;

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current == null) return;

        var results = new List<RaycastResult>();
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        EventSystem.current.RaycastAll(pointerData, results);

        // UI 아무것도 없으면 (빈 배경 클릭) → 소리 재생
        if (results.Count == 0)
        {
            PlaySFX();
            return;
        }

        // 클릭된 UI 중 하나라도 슬라이더/토글/인풋필드/텍스트면 → 소리 안 냄
        foreach (var result in results)
        {
            var go = result.gameObject;
            if (go.GetComponentInParent<Slider>() != null) return;
            if (go.GetComponentInParent<Toggle>() != null) return;
            if (go.GetComponentInParent<TMP_InputField>() != null) return;
            if (go.GetComponentInParent<TMP_Text>() != null) return;
        }

        PlaySFX();
    }

    public void PlaySFX()
    {
        if (clip == null) return;

        // 우선적으로 씬의 SFXManager를 사용하고, 없으면 전역 AudioManager를 직접 사용
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
