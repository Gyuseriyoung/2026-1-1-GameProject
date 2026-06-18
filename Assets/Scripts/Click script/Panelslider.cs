using System.Collections;
using UnityEngine;

/// <summary>
/// 캐릭터 클릭 → 패널이 오른쪽에서 슬라이드 인/아웃
/// 
/// [유니티 설정 방법]
/// 1. 하얀 패널 오브젝트에 이 스크립트 추가
/// 2. Panel 필드에 패널 RectTransform 연결 (자기 자신이면 GetComponent 자동 사용)
/// 3. 캐릭터 오브젝트에 Collider2D 추가 (PixelPerfect면 PolygonCollider2D 추천)
/// 4. 캐릭터 오브젝트에 CharacterClickDetector 스크립트 추가 후 이 컴포넌트 연결
/// </summary>
public class PanelSlider : MonoBehaviour
{
    [Header("패널")]
    [Tooltip("슬라이드할 패널의 RectTransform (비워두면 자동으로 자기 자신 사용)")]
    public RectTransform panel;

    [Header("슬라이드 설정")]
    [Tooltip("슬라이드 애니메이션 시간 (초)")]
    public float duration = 0.35f;

    [Tooltip("화면 밖 대기 위치 X (패널 너비보다 크게, 양수 = 오른쪽)")]
    public float hiddenOffsetX = 400f;

    // ── 내부 ──────────────────────────────
    private Vector2 _shownPos;   // 보이는 위치 (Inspector에서 배치한 위치)
    private Vector2 _hiddenPos;  // 숨겨진 위치 (오른쪽 밖)
    private bool _isShown = false;
    private Coroutine _current;

    private void Awake()
    {
        if (panel == null) panel = GetComponent<RectTransform>();

        _shownPos = panel.anchoredPosition;
        _hiddenPos = _shownPos + new Vector2(hiddenOffsetX, 0f);

        // 시작 시 화면 밖에 숨겨둠
        panel.anchoredPosition = _hiddenPos;
    }

    /// <summary>캐릭터에서 호출 — 패널 열기/닫기 토글</summary>
    public void Toggle()
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(SlideTo(_isShown ? _hiddenPos : _shownPos));
        _isShown = !_isShown;
    }

    private IEnumerator SlideTo(Vector2 target)
    {
        Vector2 start = panel.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            panel.anchoredPosition = Vector2.LerpUnclamped(start, target, EaseOut(t));
            yield return null;
        }

        panel.anchoredPosition = target;
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
}