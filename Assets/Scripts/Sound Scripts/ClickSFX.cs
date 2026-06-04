using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ClickSFX : MonoBehaviour
{
    [Header("클릭 효과음 — 여기에 드래그하세요")]
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    public float pitch = 1f;

    private AudioSource _source;

    private void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;

        if (clip == null)
            Debug.LogError("[ClickSFX] clip이 비어 있습니다!");
    }

    private void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
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