using System;
using UnityEngine;

namespace R3Agent.Relationship
{
    [Serializable]
    public class RelationshipState
    {
        [Range(0f, 1f)] public float Trust = 0.50f;
        [Range(0f, 1f)] public float Anxiety = 0.20f;
        [Range(0f, 1f)] public float Stability = 0.50f;
        [Range(0f, 1f)] public float Violation = 0.20f;

        public void Clamp()
        {
            Trust = Mathf.Clamp01(Trust);
            Anxiety = Mathf.Clamp01(Anxiety);
            Stability = Mathf.Clamp01(Stability);
            Violation = Mathf.Clamp01(Violation);
        }
    }
}