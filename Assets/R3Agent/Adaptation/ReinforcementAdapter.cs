// =======================================================
// File: Assets/R3Agent/Adaptation/ReinforcementAdapter.cs
// =======================================================
using System;
using System.Collections.Generic;
using R3Agent.Core;
using R3Agent.Perception;
using R3Agent.Relationship;
using UnityEngine;

namespace R3Agent.Adaptation
{
    public sealed class ReinforcementAdapter
    {
        private readonly AgentConfig _cfg;

        private readonly Dictionary<StateKey, float[]> _q = new Dictionary<StateKey, float[]>(128);
        private StateKey _prevState;
        private int _prevAction;
        private bool _hasPrev;

        public ReinforcementAdapter(AgentConfig cfg) => _cfg = cfg;

        public InteractionStyle SelectStyle(RelationshipState rel, PerceptionEvent ev)
        {
            var s = MakeState(rel, ev);

            float[] qv = GetQ(s);

            int action;
            if (UnityEngine.Random.value < _cfg.epsilon)
            {
                action = UnityEngine.Random.Range(0, 4);
            }
            else
            {
                action = ArgMax(qv);
            }

            _prevState = s;
            _prevAction = action;
            _hasPrev = true;

            return (InteractionStyle)action;
        }

        public void Learn(RelationshipState relBefore, PerceptionEvent ev, RelationshipState relAfter)
        {
            if (!_hasPrev) return;

            float reward = (relAfter.Stability - relBefore.Stability) - 0.5f * (relAfter.Anxiety - relBefore.Anxiety);
            reward = Mathf.Clamp(reward, -1f, +1f);

            var s = _prevState;
            var a = _prevAction;

            var s2 = MakeState(relAfter, ev);
            float[] qS = GetQ(s);
            float[] qS2 = GetQ(s2);

            float tdTarget = reward + _cfg.discount * qS2[ArgMax(qS2)];
            float tdError = tdTarget - qS[a];
            qS[a] += _cfg.learningRate * tdError;
        }

        private float[] GetQ(StateKey s)
        {
            if (!_q.TryGetValue(s, out var arr))
            {
                arr = new float[4]; 
                _q[s] = arr;
            }
            return arr;
        }

        private static int ArgMax(float[] a)
        {
            int best = 0;
            float v = a[0];
            for (int i = 1; i < a.Length; i++)
            {
                if (a[i] > v) { v = a[i]; best = i; }
            }
            return best;
        }

        private StateKey MakeState(RelationshipState rel, PerceptionEvent ev)
        {
            int trustBin = Mathf.Clamp(Mathf.FloorToInt(rel.Trust * 4f), 0, 3);
            int anxBin = Mathf.Clamp(Mathf.FloorToInt(rel.Anxiety * 4f), 0, 3);
            int eType = (int)ev.Type;
            return new StateKey(trustBin, anxBin, eType);
        }

        private readonly struct StateKey : IEquatable<StateKey>
        {
            private readonly int _t, _a, _e;
            public StateKey(int t, int a, int e) { _t = t; _a = a; _e = e; }

            public bool Equals(StateKey other) => _t == other._t && _a == other._a && _e == other._e;
            public override bool Equals(object obj) => obj is StateKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(_t, _a, _e);
        }
    }
}