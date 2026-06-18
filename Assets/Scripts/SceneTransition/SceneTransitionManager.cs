using System;
using System.Collections;
using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; // 텍스트 제어를 위해 추가

/// <summary>
/// Sweet Wipe 씬 전환 매니저 (텍스트 랜덤 출력 버전)
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    public bool IsBusy => _busy;

    [Header("UI 수동 연결")]
    [Tooltip("에디터에서 만든 Canvas를 넣어주세요.")]
    [SerializeField] private Canvas transitionCanvas;
    [Tooltip("움직일 스윕 이미지 오브젝트의 RectTransform을 넣어주세요.")]
    [SerializeField] private RectTransform sweepRect;

    [Tooltip("이미지 안에 배치한 TextMeshPro 컴포넌트를 연결해주세요.")]
    [SerializeField] private TextMeshProUGUI transitionText;

    [Header("랜덤 텍스트 설정")]
    [Tooltip("씬이 넘어갈 때 무작위로 나올 문구들을 입력하세요.")]
    [SerializeField]
    private List<string> randomTexts = new List<string>()
    {
        "Are you ready?",
        "Perfect를 노려보세요!",
        "Loading..."
    };

    [Header("전환 설정")]
    [Tooltip("전환 전체 시간 (초) — 스윕 인 + 스윕 아웃")]
    public float transitionDuration = 1f;

    [Tooltip("화면을 완전히 덮은 채 머무는 시간 (초)")]
    public float holdDuration = 2f;

    [Header("방향 설정")]
    [Tooltip("물결이 쓸고 가는 방향")]
    public SweepDirection direction = SweepDirection.Left;

    public enum SweepDirection { Left, Right, Up, Down }

    private bool _busy;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sweepRect != null)
        {
            SetOffScreen();
        }
        else
        {
            Debug.LogError("[SceneTransitionManager] sweepRect가 연결되지 않았습니다!");
        }

        // 시작할 때는 일단 텍스트 비워두기
        if (transitionText != null) transitionText.text = "";
    }

    public void LoadScene(string sceneName) => TryTransition(() => SceneManager.LoadSceneAsync(sceneName));
    public void LoadScene(int index) => TryTransition(() => SceneManager.LoadSceneAsync(index));
    public void LoadNextScene() => LoadScene((SceneManager.GetActiveScene().buildIndex + 1) % SceneManager.sceneCountInBuildSettings);
    public void LoadPreviousScene() => LoadScene((SceneManager.GetActiveScene().buildIndex - 1 + SceneManager.sceneCountInBuildSettings) % SceneManager.sceneCountInBuildSettings);

    /// <summary>
    /// 이미 완전히 암전된 상태에서 스윕 모션 없이 씬을 로드하고, 암전 커버를 씌운 채 유지하는 씬 로더
    /// </summary>
    public void LoadSceneBlackout(string sceneName)
    {
        if (_busy) return;
        StartCoroutine(RunBlackoutTransition(() => SceneManager.LoadSceneAsync(sceneName)));
    }

    private IEnumerator RunBlackoutTransition(Func<AsyncOperation> load)
    {
        _busy = true;

        // 스윕 인 생략하고 즉각 커버 배치
        PlaceCover();
        if (transitionText != null) transitionText.text = "";

        AsyncOperation op = load.Invoke();
        if (op != null) 
        { 
            while (!op.isDone) yield return null; 
        }

        PlaceCover();
        _busy = false;
    }

    /// <summary>
    /// 강제로 암전 상태의 스윕 커버를 스무스하게 걷어내는 페이드아웃 기능
    /// </summary>
    public void FadeInClear(float duration)
    {
        StartCoroutine(SweepOut(duration));
    }


    private void TryTransition(Func<AsyncOperation> load)
    {
        if (_busy) return;
        StartCoroutine(RunTransition(load));
    }

    private IEnumerator RunTransition(Func<AsyncOperation> load)
    {
        _busy = true;

        // 텍스트 랜덤 변경 호출!
        SetRandomText();

        float half = transitionDuration * 0.5f;

        yield return StartCoroutine(SweepIn(half));

        AsyncOperation op = load.Invoke();
        if (op != null) { while (!op.isDone) yield return null; }
        else { yield return null; }

        PlaceCover();

        if (holdDuration > 0f) yield return new WaitForSecondsRealtime(holdDuration);
        else yield return null;

        yield return StartCoroutine(SweepOut(half));

        // 완전히 끝나면 텍스트 지우기
        if (transitionText != null) transitionText.text = "";

        _busy = false;
    }

    /// <summary>
    /// 등록된 텍스트 풀 중에서 랜덤으로 하나를 골라 UI에 반영합니다.
    /// </summary>
    private void SetRandomText()
    {
        if (transitionText == null) return;
        if (randomTexts == null || randomTexts.Count == 0)
        {
            transitionText.text = "";
            return;
        }

        // 무작위 인덱스 추첨
        int randomIndex = UnityEngine.Random.Range(0, randomTexts.Count);
        transitionText.text = randomTexts[randomIndex];
    }

    private IEnumerator SweepIn(float duration)
    {
        if (duration <= 0f) { PlaceCover(); yield break; }
        SetOffScreen();
        Vector2 startPos = sweepRect.anchoredPosition;
        Vector2 endPos = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            sweepRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, EaseOut(t));
            yield return null;
        }
        PlaceCover();
    }

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
            sweepRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, EaseIn(t));
            yield return null;
        }
        SetOffScreen();
    }

    private void PlaceCover() => sweepRect.anchoredPosition = Vector2.zero;

    private void SetOffScreen()
    {
        float w = transitionCanvas.pixelRect.width;
        float h = transitionCanvas.pixelRect.height;
        sweepRect.anchoredPosition = direction switch
        {
            SweepDirection.Left => new Vector2(w, 0),
            SweepDirection.Right => new Vector2(-w, 0),
            SweepDirection.Up => new Vector2(0, -h),
            SweepDirection.Down => new Vector2(0, h),
            _ => new Vector2(w, 0),
        };
    }

    private Vector2 GetOppositeOffScreenPos()
    {
        float w = transitionCanvas.pixelRect.width;
        float h = transitionCanvas.pixelRect.height;
        return direction switch
        {
            SweepDirection.Left => new Vector2(-w, 0),
            SweepDirection.Right => new Vector2(w, 0),
            SweepDirection.Up => new Vector2(0, h),
            SweepDirection.Down => new Vector2(0, -h),
            _ => new Vector2(-w, 0),
        };
    }

    private static float EaseOut(float t) { float f = 1f - t; return 1f - f * f * f; }
    private static float EaseIn(float t) { return t * t * t; }
}