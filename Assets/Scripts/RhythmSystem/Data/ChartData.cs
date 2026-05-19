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
        LaneRemove     // Remove a lane
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
        public List<TimingPoint> timingPoints = new List<TimingPoint>();
        public List<LaneConfig> lanes = new List<LaneConfig>();
        public List<NoteData> notes = new List<NoteData>();
        public List<GimmickEvent> gimmicks = new List<GimmickEvent>();
    }
}
