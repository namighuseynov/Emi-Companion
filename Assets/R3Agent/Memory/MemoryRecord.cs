// =======================================================
// File: Assets/R3Agent/Memory/MemoryRecord.cs
// =======================================================
using System;
using R3Agent.Emotion;
using R3Agent.Perception;

namespace R3Agent.Memory
{
    [Serializable]
    public struct MemoryRecord
    {
        public PerceptionEvent Event;
        public float ValenceSnapshot;
        public float ArousalSnapshot;
        public float RelationImpact;   // RelationDelta at the time
        public float Time;            // Time.time

        public MemoryRecord(PerceptionEvent ev, EmotionalState emo, float relationImpact)
        {
            Event = ev;
            ValenceSnapshot = emo.Valence;
            ArousalSnapshot = emo.Arousal;
            RelationImpact = relationImpact;
            Time = ev.Time;
        }
    }
}