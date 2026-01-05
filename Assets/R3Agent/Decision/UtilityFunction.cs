// =======================================================
// File: Assets/R3Agent/Decision/UtilityFunction.cs
// =======================================================
using R3Agent.Core;
using R3Agent.Emotion;
using R3Agent.Relationship;
using UnityEngine;

namespace R3Agent.Decision
{
    public static class UtilityFunction
    {
        public static float Compute(AgentConfig cfg, RelationshipState rel, EmotionalState emo, float actionCost)
        {
            float u = 0f;
            u += cfg.utilityTrustWeight * rel.Trust;
            u -= cfg.utilityAnxietyWeight * rel.Anxiety;

            u += 0.20f * (emo.Valence + 1f) * 0.5f;

            u -= 0.15f * actionCost;

            return Mathf.Clamp(u, -1f, +1f);
        }
    }
}