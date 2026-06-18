using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sweet Wipe 씬 전환 매니저 (싱글톤)
/// 달콤한 핑크 물결이 화면을 쓸고 지나가며 씬을 전환합니다.
/// SceneTransitionBootstrapper에 의해 자동 생성됩니다.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("전환 설정")]
    [Tooltip("전환 전체 시간 (초) — 스윕 인 + 스윕 아웃")]
    public float transitionDuration = 1f;

    [Tooltip("스윕 색상 (기본: 내추럴 베이지)")]
    public Color sweepColor = new Color(0.90f, 0.84f, 0.76f, 1f); // #E6D5C3

    [Header("방향 설정")]
    [Tooltip("물결이 쓸고 가는 방향")]
    public SweepDirection direction = SweepDirection.Left;

    public enum SweepDirection { Left, Right, Up, Down }

    // ── 내부 ──────────────────────────────────────────
    private Canvas         _canvas;
    private RectTransform  _sweepRect;
    private bool           _busy;

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
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var go   = new GameObject("SweetWipe");
        go.transform.SetParent(transform, false);

        var img  = go.AddComponent<Image>();
        img.color = sweepColor;
        img.raycastTarget = false;

        _sweepRect = go.GetComponent<RectTransform>();
        _sweepRect.anchorMin       = Vector2.zero;
        _sweepRect.anchorMax       = Vector2.one;
        _sweepRect.sizeDelta       = Vector2.zero;
        _sweepRect.anchoredPosition = Vector2.zero;

        // 시작은 화면 밖에 숨겨둠
        MoveSweep(0f, visible: false);
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
        int prev  = (SceneManager.GetActiveScene().buildIndex - 1 + total) % total;
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

        // ① 스윕 인 — 핑크 물결이 화면을 덮음
        yield return StartCoroutine(Sweep(entering: true, duration: half));

        // ② 씬 로드
        AsyncOperation operation = load.Invoke();
        if (operation != null)
        {
            while (!operation.isDone)
                yield return null;
        }
        else
        {
            yield return null;
        }

        MoveSweep(1f, visible: true);
        yield return null;

        // ③ 스윕 아웃 — 물결이 반대쪽으로 빠져나감
        yield return StartCoroutine(Sweep(entering: false, duration: half));

        _busy = false;
    }

    private IEnumerator Sweep(bool entering, float duration)
    {
        if (duration <= 0f)
        {
            MoveSweep(entering ? 1f : 0f, visible: entering);
            yield break;
        }

        float elapsed = 0f;
        MoveSweep(entering ? 0f : 1f, visible: true);
        yield return null;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float e = EaseInOut(t);

            // entering: 0→1 (화면을 덮는다)
            // exiting : 1→2 (화면을 벗어난다)  offset 범위를 [0,1]로 쓰면
            float progress = entering ? e : 1f + e;
            MoveSweep(progress, visible: true);

            yield return null;
        }

        // 완전히 덮음 or 완전히 빠짐 — 화면 밖으로 정리
        if (entering)
            MoveSweep(1f, visible: true);
        else
            MoveSweep(0f, visible: false);
    }

    /// <summary>
    /// progress 0 = 화면 완전 밖(왼쪽 기준: 오른쪽에 숨김)
    /// progress 1 = 화면 완전 덮음
    /// progress 2 = 화면 완전 밖(반대쪽으로 나감)
    /// </summary>
    private void MoveSweep(float progress, bool visible)
    {
        if (!visible)
        {
            // 화면 밖 — 기본적으로 앵커 위치를 완전히 밖으로
            SetOffScreen();
            return;
        }

        // 현재 화면 크기
        float w = _canvas.pixelRect.width;
        float h = _canvas.pixelRect.height;

        // progress 1.0 기준: 화면을 완전히 덮는 위치 = (0, 0)
        // progress 0.0: 시작 위치 (화면 밖)
        // progress 2.0: 끝 위치 (반대쪽 밖)

        // t ∈ [0,1] → 시작 밖 → 화면 완전 덮음(progress=1)
        // t ∈ [1,2] → 화면 완전 덮음 → 반대쪽 밖

        Vector2 pos = Vector2.zero;

        switch (direction)
        {
            case SweepDirection.Left:
                // 오른쪽에서 왼쪽으로
                // progress=0: x=+w, progress=1: x=0, progress=2: x=-w
                pos.x = Mathf.Lerp(w, -w, progress * 0.5f);
                break;

            case SweepDirection.Right:
                pos.x = Mathf.Lerp(-w, w, progress * 0.5f);
                break;

            case SweepDirection.Up:
                pos.y = Mathf.Lerp(-h, h, progress * 0.5f);
                break;

            case SweepDirection.Down:
                pos.y = Mathf.Lerp(h, -h, progress * 0.5f);
                break;
        }

        _sweepRect.anchoredPosition = pos;
    }

    private void SetOffScreen()
    {
        float w = Screen.width;
        float h = Screen.height;
        _sweepRect.anchoredPosition = direction switch
        {
            SweepDirection.Left  => new Vector2( w, 0),
            SweepDirection.Right => new Vector2(-w, 0),
            SweepDirection.Up    => new Vector2(0, -h),
            SweepDirection.Down  => new Vector2(0,  h),
            _                    => new Vector2( w, 0),
        };
    }

    // ────────────────────────────────────────────────
    //  이징
    // ────────────────────────────────────────────────
    private static float EaseInOut(float t)
        => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
}
