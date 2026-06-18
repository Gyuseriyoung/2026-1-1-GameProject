using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

namespace RhythmSystem.Play
{
    public class EndingFadeManager : MonoBehaviour
    {
        public static EndingFadeManager Instance { get; private set; }

        [Header("UI References (드래그 앤 드롭으로 연결)")]
        [Tooltip("엔딩 연출용 전체 캔버스 오브젝트")]
        [SerializeField] private GameObject fadeCanvas;
        
        [Tooltip("완전 암전용 검정색 단색 패널 이미지")]
        [SerializeField] private Image blackPanel;
        
        [Tooltip("검은 화면 위에 출력될 텍스트 MeshPro 컴포넌트")]
        [SerializeField] private TextMeshProUGUI endingText;

        [Header("Ending Dialogue Configuration (인스펙터 수정 가능)")]
        [Tooltip("글자당 타이핑 속도 (초 단위)")]
        [SerializeField] private float typingSpeed = 0.05f;
        
        [TextArea]
        [Tooltip("엔딩 화면에 순차적으로 누적 출력될 엔딩 대사 목록")]
        [SerializeField] private string[] endingLines = new string[]
        {
            "그렇게 마지막 손님마저 떠나가고...",
            "이곳의 조명이 조용히 가라앉는다.",
            "우리가 함께 나눴던 맛과 리듬은,",
            "기억 속에 따뜻한 불씨로 남아 숨 쉴 것이다.",
            "플레이해주셔서 감사합니다."
        };

        private bool isWaitingForInput = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ResetManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 화면을 검은색으로 서서히 페이드아웃 시킵니다.
        /// </summary>
        public void FadeOut(float duration, System.Action onComplete = null)
        {
            if (fadeCanvas != null) fadeCanvas.SetActive(true);
            if (blackPanel != null) blackPanel.gameObject.SetActive(true);

            StartCoroutine(FadeOutCo(duration, onComplete));
        }

        private IEnumerator FadeOutCo(float duration, System.Action onComplete)
        {
            float elapsed = 0f;
            Color startColor = blackPanel != null ? blackPanel.color : Color.clear;
            startColor.a = 0f;
            
            if (blackPanel != null) blackPanel.color = startColor;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

                if (blackPanel != null)
                {
                    Color col = blackPanel.color;
                    col.a = Mathf.Lerp(0f, 1f, t);
                    blackPanel.color = col;
                }
                yield return null;
            }

            if (blackPanel != null)
            {
                Color finalCol = blackPanel.color;
                finalCol.a = 1f;
                blackPanel.color = finalCol;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 인스펙터에 지정된 기본 엔딩 코멘트 리스트를 사용하여 타이핑 연출을 시작합니다.
        /// </summary>
        public void PlayEndingCredits(System.Action onComplete)
        {
            PlayEndingCredits(endingLines, typingSpeed, onComplete);
        }

        /// <summary>
        /// 특정 텍스트 리스트를 넘겨 받아 검은 화면 위에서 다이얼로그 누적 타이핑 연출을 시작합니다.
        /// </summary>
        public void PlayEndingCredits(string[] lines, float typeSpeed, System.Action onComplete)
        {
            StartCoroutine(PlayEndingCreditsCo(lines, typeSpeed, onComplete));
        }

        private IEnumerator PlayEndingCreditsCo(string[] lines, float typeSpeed, System.Action onComplete)
        {
            if (endingText == null || lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            endingText.gameObject.SetActive(true);
            endingText.text = "";
            string accumulatedText = "";

            foreach (var line in lines)
            {
                if (accumulatedText != "")
                {
                    accumulatedText += "\n\n"; // 가독성을 위해 한 줄 띄우고 개행
                }

                string currentLineText = "";
                isWaitingForInput = false;

                // 한 글자씩 타이핑
                foreach (char c in line.ToCharArray())
                {
                    currentLineText += c;
                    endingText.text = accumulatedText + currentLineText;
                    yield return new WaitForSecondsRealtime(typeSpeed);
                }

                accumulatedText += currentLineText;
                endingText.text = accumulatedText;

                // 타이핑 완료 후 대기
                isWaitingForInput = true;
                while (isWaitingForInput)
                {
                    if (WasAdvancePressed())
                    {
                        isWaitingForInput = false;
                    }
                    yield return null;
                }
            }

            onComplete?.Invoke();
        }

        private bool WasAdvancePressed()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            
            bool keyPress = keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
            bool mousePress = mouse != null && mouse.leftButton.wasPressedThisFrame;

            return keyPress || mousePress;
        }

        /// <summary>
        /// 엔딩 연출 상태 리셋 (투명화 및 비활성화)
        /// </summary>
        public void ResetManager()
        {
            if (endingText != null)
            {
                endingText.text = "";
                endingText.gameObject.SetActive(false);
            }

            if (blackPanel != null)
            {
                Color col = blackPanel.color;
                col.a = 0f;
                blackPanel.color = col;
                blackPanel.gameObject.SetActive(false);
            }

            if (fadeCanvas != null)
            {
                fadeCanvas.SetActive(false);
            }
        }
    }
}
