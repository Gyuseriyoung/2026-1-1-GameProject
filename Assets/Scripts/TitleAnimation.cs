using UnityEngine;
using TMPro;
using System.Collections;

public class TitleAnimation : MonoBehaviour
{
    [Header("던·져·라 글자")]
    public RectTransform charDun;   // 던
    public RectTransform charJyeo;  // 져
    public RectTransform charRa;    // 라

    [Header("디저트 텍스트")]
    public RectTransform dessertText;

    //각 글자의 최종 위치 
    private Vector2 dunOrigin, jyeoOrigin, raOrigin, dessertOrigin;


    void Start()
    {
        // null 체크
        if (charDun == null || charJyeo == null || charRa == null || dessertText == null)
        {
            Debug.LogError("TitleAnimation: Inspector 연결 누락!");
            return;
        }
        dunOrigin = charDun.anchoredPosition;
        jyeoOrigin = charJyeo.anchoredPosition;
        raOrigin = charRa.anchoredPosition;
        dessertOrigin = dessertText.anchoredPosition;

        StartCoroutine(PlayTitleAnimation());
    }

    IEnumerator PlayTitleAnimation()
    {
        // 시작 위치 세팅 (화면 위 / 화면 왼쪽 밖)
        charDun.anchoredPosition = dunOrigin + Vector2.up * 600f;
        charJyeo.anchoredPosition = jyeoOrigin + Vector2.up * 600f;
        charRa.anchoredPosition = raOrigin + Vector2.up * 600f;
        dessertText.anchoredPosition = dessertOrigin + Vector2.left * 1500f;

        yield return new WaitForSeconds(0.3f);

        // 던 → 져 → 라 순서대로 낙하
        StartCoroutine(DropChar(charDun, dunOrigin, 0.42f));
        yield return new WaitForSeconds(0.15f);

        StartCoroutine(DropChar(charJyeo, jyeoOrigin, 0.42f));
        yield return new WaitForSeconds(0.15f);

        StartCoroutine(DropChar(charRa, raOrigin, 0.42f));
        yield return new WaitForSeconds(0.35f);

        // 디저트 왼쪽에서 날아와 브레이크
        StartCoroutine(SlideInBrake(dessertText, dessertOrigin, 0.5f));
    }

    // 위에서 낙하 + 바운스
    IEnumerator DropChar(RectTransform rt, Vector2 target, float duration)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, EaseOutBounce(Mathf.Clamp01(t)));
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    // 왼쪽에서 슬라이드 + 브레이크 반동
    IEnumerator SlideInBrake(RectTransform rt, Vector2 target, float duration)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float ease = EaseOutBack(Mathf.Clamp01(t));
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, ease);
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    // 낙하용 - 통통 튀기는 느낌
    float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f) return 7.5625f * t * t;
        else if (t < 2f / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
        else if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
        else { t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f; }
    }

    // 브레이크용 - 지나쳤다가 돌아오는 느낌
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
