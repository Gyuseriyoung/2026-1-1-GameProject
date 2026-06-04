using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmSystem
{
    [Serializable]
    public enum NoteType
    {
        Tap,
        Hold
    }

    [Serializable]
    public enum GimmickType
    {
        LaneMoveY,     // Y-axis movement
        LaneMoveX,     // X-axis (Judgment) movement
        BPMChange,     // BPM change
        LaneAdd,       // Add a new lane
        LaneRemove,    // Remove a lane
        ScrollSpeed,   // Change scroll speed
        Stop           // Stop chart progression for a duration
    }

    [Serializable]
    public class Metadata
    {
        public string title;
        public string artist;
        public string creator;
        public string audioFileName;
    }

    [Serializable]
    public class TimingPoint
    {
        public float time; // ms
        public float bpm;
        public int meter = 4; // Numerator
        public int denominator = 4; // Denominator
    }

    [Serializable]
    public class LaneConfig
    {
        public int laneIndex;
        public float defaultY;
    }

    [Serializable]
    public class NoteData
    {
        public float time; // ms
        public int laneIndex;
        public NoteType type;
        public float length; // ms (for Hold notes)
        public int mergeType;
        public int objectIndex;
        public int soundIndex = -1; // Index in soundBank, -1 for no sound
    }

    [Serializable]
    public class GimmickEvent
    {
        public float time; // ms
        public GimmickType type;
        public int targetLane;
        public float value;
    }

    [Serializable]
    public class ChartData
    {
        public Metadata metadata = new Metadata();
        public float startOffset = 2000f; // ms (Visual Lead-in time)
        public float musicOffset = 0f;    // ms (Audio playback offset)
        public float length = 120000f;         // ms (Total chart length)
        public List<string> soundBank = new List<string>(); // List of hit sound filenames
        public List<TimingPoint> timingPoints = new List<TimingPoint>();
        public List<LaneConfig> lanes = new List<LaneConfig>();
        public List<NoteData> notes = new List<NoteData>();
        public List<GimmickEvent> gimmicks = new List<GimmickEvent>();
    }

    public class StopTimeline
    {
        private struct StopMapping
        {
            public float logicalStartTime;
            public float audioStartTime;
            public float duration;
        }

        private readonly List<StopMapping> stopMappings = new List<StopMapping>();

        public void Rebuild(List<GimmickEvent> gimmicks)
        {
            stopMappings.Clear();
            if (gimmicks == null) return;

            List<GimmickEvent> sortedStops = gimmicks.FindAll(g => g.type == GimmickType.Stop);
            sortedStops.Sort((a, b) => a.time.CompareTo(b.time));

            float cumulativeStop = 0f;
            foreach (var stop in sortedStops)
            {
                stopMappings.Add(new StopMapping
                {
                    logicalStartTime = stop.time,
                    audioStartTime = stop.time + cumulativeStop,
                    duration = stop.value
                });
                cumulativeStop += stop.value;
            }
        }

        public float GetLogicalTime(float audioTimeMs)
        {
            float cumulativeStop = 0f;
            foreach (var mapping in stopMappings)
            {
                if (audioTimeMs <= mapping.audioStartTime) break;
                if (audioTimeMs < mapping.audioStartTime + mapping.duration)
                {
                    return mapping.logicalStartTime;
                }

                cumulativeStop += mapping.duration;
            }

            return audioTimeMs - cumulativeStop;
        }

        public float GetAudioTime(float logicalTimeMs)
        {
            float cumulativeStop = 0f;
            foreach (var mapping in stopMappings)
            {
                if (logicalTimeMs < mapping.logicalStartTime) break;
                cumulativeStop += mapping.duration;
            }

            return logicalTimeMs + cumulativeStop;
        }
    }
}
