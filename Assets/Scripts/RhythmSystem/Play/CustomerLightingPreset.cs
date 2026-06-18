using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RhythmSystem.Play
{
    [CreateAssetMenu(fileName = "CustomerLightingPreset", menuName = "CookingGame/LightingPreset")]
    public class CustomerLightingPreset : ScriptableObject
    {
        [Tooltip("음악 시간에 맞춰 동작할 2D 조명 연출 리스트")]
        public List<LightingEvent> lightingEvents = new List<LightingEvent>();
    }

    [Serializable]
    public class LightingEvent
    {
        [Header("트리거 타이밍")]
        [Tooltip("곡의 진행 시간 (ms 단위)")]
        public float triggerTimeMs;

        [Header("글로벌 2D 조명 설정")]
        [Tooltip("글로벌 2D Light (Global Light 2D)의 목표 색상")]
        public Color globalLightColor = Color.white;
        
        [Tooltip("글로벌 2D Light (Global Light 2D)의 목표 밝기")]
        public float globalLightIntensity = 1f;

        [Header("개별 2D 조명 설정")]
        [Tooltip("제어할 개별 2D 라이트들의 상태 리스트")]
        public SpotlightState[] spotlightStates;

        [Header("연출 설정")]
        [Tooltip("목표 값으로 부드럽게 전환되는 시간 (초)")]
        public float transitionDuration = 0.2f;
    }

    [Serializable]
    public struct SpotlightState
    {
        [Tooltip("제어할 씬 내 2D 라이트의 인덱스 (0부터 시작)")]
        public int spotlightIndex;
        
        [Tooltip("2D 라이트 On/Off 여부")]
        public bool isOn;
        
        [Tooltip("2D 라이트 밝기 세기")]
        public float intensity;
        
        [Tooltip("2D 라이트 색상")]
        public Color color;
    }
}
