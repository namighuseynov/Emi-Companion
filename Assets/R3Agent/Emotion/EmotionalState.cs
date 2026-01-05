// =======================================================
// File: Assets/R3Agent/Emotion/EmotionalState.cs
// =======================================================
using System;
using UnityEngine;

namespace R3Agent.Emotion
{
    [Serializable]
    public struct EmotionalDelta
    {
        public float ValenceDelta;  // -1..+1
        public float ArousalDelta;  // 0..1
    }

    [Serializable]
    public class EmotionalState
    {
        [Range(-1f, 1f)] public float Valence; 
        [Range(0f, 1f)] public float Arousal;  

        public void Apply(EmotionalDelta d)
        {
            Valence = Mathf.Clamp(Valence + d.ValenceDelta, -1f, 1f);
            Arousal = Mathf.Clamp01(Arousal + d.ArousalDelta);
        }
    }
}
