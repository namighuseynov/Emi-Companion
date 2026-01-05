using System;
using UnityEngine;

namespace R3Agent.Perception
{
    public enum PerceptionEventType
    {
        Greeting,
        Farewell,
        InfoRequest,
        PoliteRequest,
        Demand,
        Thanks,
        Praise,
        Apology,
        Agreement,
        Disagreement,
        Confusion,
        Criticism,
        Insult,
        Threat,
        BoundaryViolation,
        PromiseMade,
        PromiseKept,
        PromiseBroken,
        TopicShift,
        NoResponse
    }

    [Serializable]
    public struct PerceptionEvent
    {
        public PerceptionEventType Type;


        public string SourceId;


        public float Intensity;

        public string ContextTag;


        public string Intent;              // "request_help", "complaint", ...
        public float Sentiment;            // -1..+1
        public float Politeness;           // 0..1
        public float Engagement;           // 0..1
        public float ExpectationViolation; // 0..1 


        public string Evidence;

        public float Time;

        public PerceptionEvent(PerceptionEventType type, string sourceId, float intensity, string contextTag)
        {
            Type = type;
            SourceId = sourceId;
            Intensity = Mathf.Clamp01(intensity);
            ContextTag = contextTag;

            Intent = "";
            Sentiment = 0f;
            Politeness = 0.5f;
            Engagement = 0.5f;
            ExpectationViolation = 0f;
            Evidence = "";

            Time = UnityEngine.Time.time;
        }

        public PerceptionEvent(
            PerceptionEventType type,
            string sourceId,
            float intensity,
            string contextTag,
            string intent,
            float sentiment,
            float politeness,
            float engagement,
            float expectationViolation,
            string evidence)
        {
            Type = type;
            SourceId = sourceId;
            Intensity = Mathf.Clamp01(intensity);
            ContextTag = contextTag;

            Intent = intent ?? "";
            Sentiment = Mathf.Clamp(sentiment, -1f, 1f);
            Politeness = Mathf.Clamp01(politeness);
            Engagement = Mathf.Clamp01(engagement);
            ExpectationViolation = Mathf.Clamp01(expectationViolation);
            Evidence = evidence ?? "";

            Time = UnityEngine.Time.time;
        }
    }
}
