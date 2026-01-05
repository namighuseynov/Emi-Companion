// =======================================================
// File: Assets/R3Agent/Motivation/MotivationState.cs
// =======================================================
using System;
using UnityEngine;

namespace R3Agent.Motivation
{
    [Serializable]
    public class MotivationState
    {
        [Range(0f, 1f)] public float Socialize = 0.50f;
        [Range(0f, 1f)] public float Avoid = 0.20f;
        [Range(0f, 1f)] public float Help = 0.40f;

        public void Clamp()
        {
            Socialize = Mathf.Clamp01(Socialize);
            Avoid = Mathf.Clamp01(Avoid);
            Help = Mathf.Clamp01(Help);
        }
    }
}