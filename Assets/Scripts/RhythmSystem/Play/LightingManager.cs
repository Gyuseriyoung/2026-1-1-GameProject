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
        [SerializeField] private Light2D globalLight2D;
        [SerializeField] private List<Light2D> stageSpotlights = new List<Light2D>();

        private List<float> originalSpotIntensities = new List<float>();
        private List<Color> originalSpotColors = new List<Color>();
        private Color originalGlobalColor = Color.white;
        private float originalGlobalIntensity = 1f;
        private List<bool> originalSpotActiveStates = new List<bool>();

        private CustomerLightingPreset activePreset;
        private List<LightingEvent> sortedEvents = new List<LightingEvent>();
        private int currentEventIndex = 0;
        private Coroutine activeTransitionCo;
        private LightingEvent lastAppliedEvent;

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

            CacheOriginalSpotlightSettings();
        }

        private void CacheOriginalSpotlightSettings()
        {
            originalSpotIntensities.Clear();
            originalSpotColors.Clear();
            originalSpotActiveStates.Clear();

            if (globalLight2D != null)
            {
                originalGlobalColor = globalLight2D.color;
                originalGlobalIntensity = globalLight2D.intensity;
            }

            foreach (var spot in stageSpotlights)
            {
                if (spot != null)
                {
                    originalSpotIntensities.Add(spot.intensity);
                    originalSpotColors.Add(spot.color);
                    originalSpotActiveStates.Add(spot.gameObject.activeSelf);
                }
                else
                {
                    originalSpotIntensities.Add(1f);
                    originalSpotColors.Add(Color.white);
                    originalSpotActiveStates.Add(false);
                }
            }
        }

        public void Initialize(CustomerLightingPreset preset)
        {
            ResetToOriginalSettings();

            activePreset = preset;

            if (activePreset != null && activePreset.lightingEvents != null)
            {
                sortedEvents.AddRange(activePreset.lightingEvents);
                sortedEvents.Sort((a, b) => a.triggerTimeMs.CompareTo(b.triggerTimeMs));

                if (sortedEvents.Count > 0 && sortedEvents[0].triggerTimeMs <= 0f)
                {
                    ApplyLightingEvent(sortedEvents[0]);
                    currentEventIndex = 1;
                }
            }
        }

        public void ResetToOriginalSettings()
        {
            if (activeTransitionCo != null)
            {
                StopCoroutine(activeTransitionCo);
                activeTransitionCo = null;
            }

            if (globalLight2D != null)
            {
                globalLight2D.color = originalGlobalColor;
                globalLight2D.intensity = originalGlobalIntensity;
            }

            for (int i = 0; i < stageSpotlights.Count; i++)
            {
                if (stageSpotlights[i] != null)
                {
                    stageSpotlights[i].color = GetOriginalColor(i);
                    stageSpotlights[i].intensity = GetOriginalIntensity(i);
                    if (i < originalSpotActiveStates.Count)
                    {
                        stageSpotlights[i].gameObject.SetActive(originalSpotActiveStates[i]);
                    }
                }
            }

            sortedEvents.Clear();
            currentEventIndex = 0;
            lastAppliedEvent = null;
        }

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
                if (lastAppliedEvent != null)
                {
                    ApplySpotlightsImmediate(lastAppliedEvent);
                }
            }
            
            lastAppliedEvent = ev;
            activeTransitionCo = StartCoroutine(TransitionLightingCo(ev));
        }

        private void ApplySpotlightsImmediate(LightingEvent ev)
        {
            if (ev.spotlightStates != null)
            {
                foreach (var state in ev.spotlightStates)
                {
                    int idx = state.spotlightIndex;
                    if (idx >= 0 && idx < stageSpotlights.Count && stageSpotlights[idx] != null)
                    {
                        var spot = stageSpotlights[idx];
                        float targetIntensity = (state.isOn && state.intensity <= 0.01f) ? GetOriginalIntensity(idx) : state.intensity;
                        Color targetColor = GetValidColor(state.color, GetOriginalColor(idx));

                        spot.color = targetColor;
                        spot.intensity = targetIntensity;
                        spot.gameObject.SetActive(state.isOn);
                    }
                }
            }
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
