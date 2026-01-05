// =======================================================
// File: Assets/R3Agent/Relationship/RelationshipModel.cs
// =======================================================
using System.Collections.Generic;
using R3Agent.Core;
using UnityEngine;

namespace R3Agent.Relationship
{
    public sealed class RelationshipModel
    {
        private readonly AgentConfig _cfg;
        private readonly Dictionary<string, RelationshipState> _map = new Dictionary<string, RelationshipState>(8);

        public RelationshipModel(AgentConfig cfg) => _cfg = cfg;

        public RelationshipState GetOrCreate(string entityId)
        {
            if (!_map.TryGetValue(entityId, out var st))
            {
                st = new RelationshipState();
                _map[entityId] = st;
            }
            return st;
        }

        public void ApplyExperience(string entityId, float experience, float violation)
        {
            var r = GetOrCreate(entityId);

            float trustBefore = r.Trust;

            r.Trust += _cfg.alphaExperience * experience;
            r.Trust -= _cfg.betaViolation * violation * 0.5f;

            r.Anxiety += _cfg.betaViolation * violation;
            r.Anxiety -= _cfg.alphaExperience * Mathf.Max(0f, experience) * 0.25f;

            float deltaTrust = Mathf.Abs(r.Trust - trustBefore);
            r.Stability = Mathf.Clamp01(r.Stability + 0.10f * (1f - deltaTrust) - 0.08f * violation);

            r.Clamp();
        }

        public void ApplyReflection(string entityId, float reflectionScore)
        {
            var r = GetOrCreate(entityId);

            r.Trust = Mathf.Clamp01(r.Trust + _cfg.gammaReflection * reflectionScore);
            r.Anxiety = Mathf.Clamp01(r.Anxiety - _cfg.gammaReflection * reflectionScore * 0.6f);

            r.Stability = Mathf.Clamp01(r.Stability + 0.05f * reflectionScore);

            r.Clamp();
        }
    }
}