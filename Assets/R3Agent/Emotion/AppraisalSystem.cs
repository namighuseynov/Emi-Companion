using R3Agent.Perception;
using R3Agent.Relationship;
using UnityEngine;

namespace R3Agent.Emotion
{
    public struct AppraisalResult
    {
        public EmotionalDelta EmotionDelta; // valence/arousal delta
        public float RelationDelta;        
        public float ViolationScore;        // Violation term 0..1
        public float EmotionalWeight;       
    }

    public sealed class AppraisalSystem
    {
        public AppraisalResult Appraise(PerceptionEvent ev, RelationshipState rel, EmotionalState emo)
        {
            float intensity = Mathf.Clamp01(ev.Intensity);

            float sentiment = Mathf.Clamp(ev.Sentiment, -1f, 1f);
            float politeness = Mathf.Clamp01(ev.Politeness);
            float engagement = Mathf.Clamp01(ev.Engagement);
            float expViol = Mathf.Clamp01(ev.ExpectationViolation);

            GetBaseProfile(ev.Type, out float baseVal, out float baseArousal, out float baseTrust, out float baseViol);

            float engageFactor = Mathf.Lerp(0.85f, 1.10f, engagement);
            float politePosBoost = Mathf.Lerp(0.90f, 1.15f, politeness);
            float politeNegDamp = Mathf.Lerp(1.15f, 0.85f, politeness); 

            float biasPos = 1f + 0.60f * rel.Trust - 0.40f * rel.Anxiety;
            float biasNeg = 1f + 0.60f * rel.Anxiety - 0.40f * rel.Trust;


            float val = (baseVal * intensity) + (0.35f * sentiment * intensity);
            float ar = (baseArousal * intensity) * engageFactor;

            if (val >= 0f) val *= biasPos * politePosBoost;
            else val *= biasNeg * politeNegDamp;

            ar += 0.08f * rel.Anxiety * (val < 0 ? 1f : 0.3f);

            val = Mathf.Clamp(val, -1f, 1f);
            ar = Mathf.Clamp01(ar);

            float relDelta = baseTrust * intensity * engageFactor;
            relDelta += 0.12f * sentiment * intensity;

            if (relDelta >= 0f) relDelta *= biasPos * politePosBoost;
            else relDelta *= biasNeg * politeNegDamp;


            float viol = baseViol * intensity;
            viol = Mathf.Clamp01(viol + expViol);

            viol = Mathf.Clamp01(viol + 0.15f * Mathf.Max(0f, -sentiment) * (1f - politeness));

            float emotionalWeight = Mathf.Clamp01(Mathf.Abs(val) + ar);

            return new AppraisalResult
            {
                EmotionDelta = new EmotionalDelta { ValenceDelta = val, ArousalDelta = ar },
                RelationDelta = relDelta,
                ViolationScore = viol,
                EmotionalWeight = emotionalWeight
            };
        }

        private static void GetBaseProfile(PerceptionEventType t, out float val, out float ar, out float trust, out float viol)
        {
            switch (t)
            {
                case PerceptionEventType.Greeting: val = +0.08f; ar = 0.03f; trust = +0.05f; viol = 0.00f; break;
                case PerceptionEventType.Farewell: val = 0.00f; ar = 0.02f; trust = 0.00f; viol = 0.00f; break;

                case PerceptionEventType.InfoRequest: val = +0.02f; ar = 0.03f; trust = +0.04f; viol = 0.00f; break;
                case PerceptionEventType.PoliteRequest: val = +0.04f; ar = 0.04f; trust = +0.06f; viol = 0.02f; break;
                case PerceptionEventType.Demand: val = -0.08f; ar = 0.05f; trust = -0.06f; viol = 0.10f; break;

                case PerceptionEventType.Thanks: val = +0.20f; ar = 0.04f; trust = +0.18f; viol = 0.00f; break;
                case PerceptionEventType.Praise: val = +0.25f; ar = 0.05f; trust = +0.22f; viol = 0.00f; break;
                case PerceptionEventType.Apology: val = +0.18f; ar = 0.04f; trust = +0.15f; viol = 0.00f; break;

                case PerceptionEventType.Agreement: val = +0.10f; ar = 0.03f; trust = +0.08f; viol = 0.00f; break;
                case PerceptionEventType.Disagreement: val = -0.05f; ar = 0.04f; trust = -0.03f; viol = 0.02f; break;
                case PerceptionEventType.Confusion: val = -0.03f; ar = 0.05f; trust = -0.02f; viol = 0.03f; break;

                case PerceptionEventType.Criticism: val = -0.15f; ar = 0.06f; trust = -0.10f; viol = 0.08f; break;
                case PerceptionEventType.Insult: val = -0.35f; ar = 0.12f; trust = -0.30f; viol = 0.35f; break;
                case PerceptionEventType.Threat: val = -0.45f; ar = 0.18f; trust = -0.40f; viol = 0.50f; break;

                case PerceptionEventType.BoundaryViolation: val = -0.30f; ar = 0.10f; trust = -0.25f; viol = 0.60f; break;

                case PerceptionEventType.PromiseMade: val = +0.06f; ar = 0.03f; trust = +0.05f; viol = 0.02f; break;
                case PerceptionEventType.PromiseKept: val = +0.28f; ar = 0.06f; trust = +0.25f; viol = 0.00f; break;
                case PerceptionEventType.PromiseBroken: val = -0.32f; ar = 0.10f; trust = -0.28f; viol = 0.65f; break;

                case PerceptionEventType.TopicShift: val = 0.00f; ar = 0.02f; trust = 0.00f; viol = 0.05f; break;
                case PerceptionEventType.NoResponse: val = -0.10f; ar = 0.04f; trust = -0.08f; viol = 0.10f; break;

                default: val = 0.00f; ar = 0.02f; trust = 0.00f; viol = 0.00f; break;
            }
        }
    }
}
