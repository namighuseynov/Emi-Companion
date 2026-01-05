// =======================================================
// File: Assets/R3Agent/Reflection/ReflectionEngine.cs
// =======================================================
using R3Agent.Core;
using R3Agent.Memory;
using R3Agent.Relationship;
using UnityEngine;

namespace R3Agent.Reflection
{
    public sealed class ReflectionEngine
    {
        private readonly AgentConfig _cfg;

        public ReflectionEngine(AgentConfig cfg) => _cfg = cfg;

        public float ComputeReflectionScore(RelationalMemory mem, RelationshipModel relModel, string entityId)
        {
            float now = Time.time;
            float sum = 0f;

            var rel = relModel.GetOrCreate(entityId);

            foreach (var rec in mem.Records)
            {
                if (rec.Event.SourceId != entityId) continue;

                float dt = Mathf.Max(0f, now - rec.Time);
                float timeDecay = Mathf.Exp(-_cfg.timeDecayLambda * dt);

                float emotionalWeight = Mathf.Clamp01(Mathf.Abs(rec.ValenceSnapshot) + rec.ArousalSnapshot);

                // RelationDelta = rec.RelationImpact
                sum += emotionalWeight * rec.RelationImpact * timeDecay;
            }

            float normalized = sum * (0.75f + 0.5f * rel.Trust);
            return Mathf.Clamp(normalized, -1f, +1f);
        }
    }
}
