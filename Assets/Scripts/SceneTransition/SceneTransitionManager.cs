using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sweet Wipe 씬 전환 매니저 (싱글톤)
/// 달콤한 물결이 화면을 쓸고 지나가며 씬을 전환합니다.
/// SceneTransitionBootstrapper에 의해 자동 생성됩니다.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("전환 설정")]
    [Tooltip("전환 전체 시간 (초) — 스윕 인 + 스윕 아웃")]
    public float transitionDuration = 1f;

    [Tooltip("화면을 완전히 덮은 채 머무는 시간 (초)")]
    public float holdDuration = 2f;

    [Tooltip("스윕 색상 (기본: 내추럴 베이지)")]
    public Color sweepColor = new Color(0.90f, 0.84f, 0.76f, 1f); // #E6D5C3

    [Header("방향 설정")]
    [Tooltip("물결이 쓸고 가는 방향")]
    public SweepDirection direction = SweepDirection.Left;

    public enum SweepDirection { Left, Right, Up, Down }

    // ── 내부 ──────────────────────────────────────────
    private Canvas _canvas;
    private RectTransform _sweepRect;
    private bool _busy;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void BuildUI()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var go = new GameObject("SweetWipe");
        go.transform.SetParent(transform, false);

        var img = go.AddComponent<Image>();
        img.color = sweepColor;
        img.raycastTarget = false;

        _sweepRect = go.GetComponent<RectTransform>();
        _sweepRect.anchorMin = Vector2.zero;
        _sweepRect.anchorMax = Vector2.one;
        _sweepRect.sizeDelta = Vector2.zero;
        _sweepRect.anchoredPosition = Vector2.zero;

        // 시작은 화면 밖에 숨겨둠
        SetOffScreen();
    }

    // ────────────────────────────────────────────────
    //  공개 API
    // ────────────────────────────────────────────────

    public void LoadScene(string sceneName)
        => TryTransition(() => SceneManager.LoadSceneAsync(sceneName));

    public void LoadScene(int index)
        => TryTransition(() => SceneManager.LoadSceneAsync(index));

    public void LoadNextScene()
    {
        int next = (SceneManager.GetActiveScene().buildIndex + 1)
                   % SceneManager.sceneCountInBuildSettings;
        LoadScene(next);
    }

    public void LoadPreviousScene()
    {
        int total = SceneManager.sceneCountInBuildSettings;
        int prev = (SceneManager.GetActiveScene().buildIndex - 1 + total) % total;
        LoadScene(prev);
    }

    // ────────────────────────────────────────────────
    //  내부 전환 로직
    // ────────────────────────────────────────────────

    private void TryTransition(Func<AsyncOperation> load)
    {
        if (_busy) return;
        StartCoroutine(RunTransition(load));
    }

    private IEnumerator RunTransition(Func<AsyncOperation> load)
    {
        _busy = true;

        float half = transitionDuration * 0.5f;

        // ① 스윕 인 — 화면을 덮음 (EaseOut: 빠르게 튀어나와 부드럽게 안착)
        yield return StartCoroutine(SweepIn(half));

        // ② 씬 로드
        AsyncOperation op = load.Invoke();
        if (op != null)
        {
            while (!op.isDone)
                yield return null;
        }
        else
        {
            yield return null;
        }

        // 로드 직후 화면을 완전히 덮은 상태로 고정
        PlaceCover();

        // ③ 잔류 — 베이지 화면이 잠깐 머물다 나감
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);
        else
            yield return null;

        // ④ 스윕 아웃 — 반대쪽으로 퇴장 (EaseIn: 천천히 시작해서 빠르게 빠짐)
        yield return StartCoroutine(SweepOut(half));

        _busy = false;
    }

    // 화면 밖에서 → 화면을 완전히 덮는 위치로 이동
    private IEnumerator SweepIn(float duration)
    {
        if (duration <= 0f) { PlaceCover(); yield break; }

        SetOffScreen();
        Vector2 startPos = _sweepRect.anchoredPosition;
        Vector2 endPos = Vector2.zero; // 화면 중앙 = 완전히 덮음

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _sweepRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, EaseOut(t));
            yield return null;
        }
        PlaceCover();
    }

    // 화면을 완전히 덮은 위치에서 → 반대쪽 화면 밖으로 이동
    private IEnumerator SweepOut(float duration)
    {
        if (duration <= 0f) { SetOffScreen(); yield break; }

        PlaceCover();
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = GetOppositeOffScreenPos();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _sweepRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, EaseIn(t));
            yield return null;
        }
        SetOffScreen();
    }

    // ────────────────────────────────────────────────
    //  위치 헬퍼
    // ────────────────────────────────────────────────

    /// <summary>화면을 완전히 덮는 중앙 위치로 고정합니다.</summary>
    private void PlaceCover()
    {
        _sweepRect.anchoredPosition = Vector2.zero;
    }

    /// <summary>현재 방향 기준 시작 쪽 화면 밖으로 이동합니다 (진입 전 초기 위치).</summary>
    private void SetOffScreen()
    {
        float w = _canvas.pixelRect.width;
        float h = _canvas.pixelRect.height;
        _sweepRect.anchoredPosition = direction switch
        {
            SweepDirection.Left => new Vector2(w, 0),
            SweepDirection.Right => new Vector2(-w, 0),
            SweepDirection.Up => new Vector2(0, -h),
            SweepDirection.Down => new Vector2(0, h),
            _ => new Vector2(w, 0),
        };
    }

    /// <summary>현재 방향 기준 반대쪽 화면 밖 위치를 반환합니다 (퇴장 목표 위치).</summary>
    private Vector2 GetOppositeOffScreenPos()
    {
        float w = _canvas.pixelRect.width;
        float h = _canvas.pixelRect.height;
        return direction switch
        {
            SweepDirection.Left => new Vector2(-w, 0),
            SweepDirection.Right => new Vector2(w, 0),
            SweepDirection.Up => new Vector2(0, h),
            SweepDirection.Down => new Vector2(0, -h),
            _ => new Vector2(-w, 0),
        };
    }

    // ────────────────────────────────────────────────
    //  이징
    //  EaseOut: 빠르게 시작 → 부드럽게 안착 (진입에 사용)
    //  EaseIn:  천천히 시작 → 빠르게 가속  (퇴장에 사용)
    // ────────────────────────────────────────────────

    /// <summary>Cubic EaseOut — 빠르게 튀어나와 자연스럽게 멈춤</summary>
    private static float EaseOut(float t)
    {
        float f = 1f - t;
        return 1f - f * f * f;
    }

    /// <summary>Cubic EaseIn — 천천히 시작해서 빠르게 빠짐</summary>
    private static float EaseIn(float t)
    {
        return t * t * t;
    }
}