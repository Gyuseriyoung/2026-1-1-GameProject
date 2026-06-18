using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal; // 2D Light2D 사용을 위한 URP 네임스페이스

namespace RhythmSystem.Play
{
    public class LightingManager : MonoBehaviour
    {
        public static LightingManager Instance { get; private set; }

        [Header("Scene References (2D Light)")]
        [SerializeField] private Light2D globalLight2D; // 2D 환경의 글로벌 라이트
        [SerializeField] private List<Light2D> stageSpotlights = new List<Light2D>(); // 2D 개별 라이트들

        // 씬 시작 시 조명들의 원래 원본 설정값들을 캐싱해두는 리스트
        private List<float> originalSpotIntensities = new List<float>();
        private List<Color> originalSpotColors = new List<Color>();

        private CustomerLightingPreset activePreset;
        private List<LightingEvent> sortedEvents = new List<LightingEvent>();
        private int currentEventIndex = 0;
        private Coroutine activeTransitionCo;
        private LightingEvent lastAppliedEvent; // 직전 이벤트 상태 보관용

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            // 씬에 배치된 조명들의 원래 디자인 설정값을 미리 저장합니다.
            CacheOriginalSpotlightSettings();
        }

        private void CacheOriginalSpotlightSettings()
        {
            originalSpotIntensities.Clear();
            originalSpotColors.Clear();

            foreach (var spot in stageSpotlights)
            {
                if (spot != null)
                {
                    originalSpotIntensities.Add(spot.intensity);
                    originalSpotColors.Add(spot.color);
                }
                else
                {
                    originalSpotIntensities.Add(1f);
                    originalSpotColors.Add(Color.white);
                }
            }
        }

        /// <summary>
        /// 곡 시작 시 호출하여 프리셋 데이터를 로드하고 상태를 초기화합니다.
        /// </summary>
        public void Initialize(CustomerLightingPreset preset)
        {
            if (activeTransitionCo != null)
            {
                StopCoroutine(activeTransitionCo);
                activeTransitionCo = null;
            }

            activePreset = preset;
            sortedEvents.Clear();
            currentEventIndex = 0;
            lastAppliedEvent = null;

            if (activePreset != null && activePreset.lightingEvents != null)
            {
                sortedEvents.AddRange(activePreset.lightingEvents);
                // 시간 순 정렬 (오름차순)
                sortedEvents.Sort((a, b) => a.triggerTimeMs.CompareTo(b.triggerTimeMs));

                // 시작 시점(0ms 이하)에 해당하는 조명 연출이 있다면 게임이 시작할 때 즉시(보간 없이) 적용합니다.
                if (sortedEvents.Count > 0 && sortedEvents[0].triggerTimeMs <= 0f)
                {
                    ApplyLightingEventImmediate(sortedEvents[0]);
                    currentEventIndex = 1; // 0번은 적용되었으므로 다음부터 업데이트 대기
                }
            }
        }

        /// <summary>
        /// RhythmGameManager의 Update 루프에서 호출되어 시간 진행에 따라 조명을 업데이트합니다.
        /// </summary>
        public void UpdateLighting(float currentTimeMs)
        {
            if (sortedEvents == null || currentEventIndex >= sortedEvents.Count) return;

            // 프레임 스킵 시에도 누락 없이 순차적으로 이벤트가 체인 실행되도록 처리
            while (currentEventIndex < sortedEvents.Count && currentTimeMs >= sortedEvents[currentEventIndex].triggerTimeMs)
            {
                ApplyLightingEvent(sortedEvents[currentEventIndex]);
                currentEventIndex++;
            }
        }

        private void ApplyLightingEvent(LightingEvent ev)
        {
            if (activeTransitionCo != null)
            {
                StopCoroutine(activeTransitionCo);
                // 중요: 새 이벤트 보간이 실행되기 전, 직전 이벤트의 조명 최종 목적지 상태를 강제로 완전히 굳힙니다.
                if (lastAppliedEvent != null)
                {
                    ApplyLightingEventImmediate(lastAppliedEvent);
                }
            }
            
            lastAppliedEvent = ev;
            activeTransitionCo = StartCoroutine(TransitionLightingCo(ev));
        }

        private void ApplyLightingEventImmediate(LightingEvent ev)
        {
            if (globalLight2D != null)
            {
                globalLight2D.color = ev.globalLightColor;
                globalLight2D.intensity = ev.globalLightIntensity;
            }

            if (ev.spotlightStates != null)
            {
                foreach (var state in ev.spotlightStates)
                {
                    int idx = state.spotlightIndex;
                    if (idx >= 0 && idx < stageSpotlights.Count && stageSpotlights[idx] != null)
                    {
                        var spot = stageSpotlights[idx];
                        
                        // [자동 씬 원본값 매핑] 기획자가 강도를 따로 지정하지 않았거나(0.01 이하) 색상이 투명하면, 씬에 세팅된 원본 값을 기본으로 가져옵니다.
                        float targetIntensity = (state.isOn && state.intensity <= 0.01f) ? GetOriginalIntensity(idx) : state.intensity;
                        Color targetColor = GetValidColor(state.color, GetOriginalColor(idx));

                        spot.color = targetColor;
                        spot.intensity = targetIntensity;
                        
                        // 조명 게임오브젝트 상태를 활성화/비활성화 처리
                        spot.gameObject.SetActive(state.isOn);
                    }
                }
            }
        }

        private IEnumerator TransitionLightingCo(LightingEvent ev)
        {
            float elapsed = 0f;
            float duration = ev.transitionDuration;

            // 시작 조명 상태 캡처
            Color startGlobalColor = globalLight2D != null ? globalLight2D.color : Color.white;
            float startGlobalIntensity = globalLight2D != null ? globalLight2D.intensity : 1f;

            List<Color> startSpotColors = new List<Color>();
            List<float> startSpotIntensities = new List<float>();
            foreach (var spot in stageSpotlights)
            {
                if (spot != null)
                {
                    startSpotColors.Add(spot.color);
                    // 꺼져있던 오브젝트라면 시작 강도를 0f로 상정하고 켭니다.
                    startSpotIntensities.Add(spot.gameObject.activeSelf ? spot.intensity : 0f);
                }
                else
                {
                    startSpotColors.Add(Color.white);
                    startSpotIntensities.Add(0f);
                }
            }

            // 전환 시작 시: 켜야 할 조명 게임오브젝트를 즉시 활성화(SetActive) 시켜 렌더링에 반영되게 합니다.
            if (ev.spotlightStates != null)
            {
                foreach (var state in ev.spotlightStates)
                {
                    int idx = state.spotlightIndex;
                    if (idx >= 0 && idx < stageSpotlights.Count && stageSpotlights[idx] != null)
                    {
                        if (state.isOn)
                        {
                            stageSpotlights[idx].gameObject.SetActive(true);
                        }
                    }
                }
            }

            // 부드러운 전환 (Lerp)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

                // 1. 글로벌 2D 라이트 제어
                if (globalLight2D != null)
                {
                    globalLight2D.color = Color.Lerp(startGlobalColor, ev.globalLightColor, t);
                    globalLight2D.intensity = Mathf.Lerp(startGlobalIntensity, ev.globalLightIntensity, t);
                }

                // 2. 개별 2D 라이트 제어
                if (ev.spotlightStates != null)
                {
                    foreach (var state in ev.spotlightStates)
                    {
                        int idx = state.spotlightIndex;
                        if (idx >= 0 && idx < stageSpotlights.Count && stageSpotlights[idx] != null)
                        {
                            var spot = stageSpotlights[idx];
                            
                            // 켜질 조명은 설정된 밝기(혹은 씬 원본 밝기), 꺼질 조명은 0으로 보간
                            float targetIntensity = state.isOn ? ((state.intensity <= 0.01f) ? GetOriginalIntensity(idx) : state.intensity) : 0f;
                            Color targetColor = GetValidColor(state.color, GetOriginalColor(idx));

                            spot.color = Color.Lerp(startSpotColors[idx], targetColor, t);
                            spot.intensity = Mathf.Lerp(startSpotIntensities[idx], targetIntensity, t);
                        }
                    }
                }

                yield return null;
            }

            // 최종 설정값 적용 및 확정 보정
            if (globalLight2D != null)
            {
                globalLight2D.color = ev.globalLightColor;
                globalLight2D.intensity = ev.globalLightIntensity;
            }

            if (ev.spotlightStates != null)
            {
                foreach (var state in ev.spotlightStates)
                {
                    int idx = state.spotlightIndex;
                    if (idx >= 0 && idx < stageSpotlights.Count && stageSpotlights[idx] != null)
                    {
                        var spot = stageSpotlights[idx];
                        
                        float finalIntensity = state.isOn ? ((state.intensity <= 0.01f) ? GetOriginalIntensity(idx) : state.intensity) : 0f;
                        Color finalColor = GetValidColor(state.color, GetOriginalColor(idx));

                        spot.color = finalColor;
                        spot.intensity = finalIntensity;
                        
                        // 꺼지는 조명은 연출 완료 후 비활성화 처리
                        spot.gameObject.SetActive(state.isOn);
                    }
                }
            }

            activeTransitionCo = null;
        }

        #region Helpers
        private float GetOriginalIntensity(int index)
        {
            if (index >= 0 && index < originalSpotIntensities.Count)
                return originalSpotIntensities[index];
            return 1.0f;
        }

        private Color GetOriginalColor(int index)
        {
            if (index >= 0 && index < originalSpotColors.Count)
                return originalSpotColors[index];
            return Color.white;
        }

        private Color GetValidColor(Color input, Color fallback)
        {
            // 투명색이거나 검은색에 알파가 없는 경우 씬 원본 컬러를 사용하게 보정
            if (input.a <= 0.01f || (input.r == 0f && input.g == 0f && input.b == 0f && input.a == 1f))
            {
                return fallback;
            }
            return input;
        }
        #endregion
    }
}
