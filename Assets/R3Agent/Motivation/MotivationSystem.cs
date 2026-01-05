// =======================================================
// File: Assets/R3Agent/Motivation/MotivationSystem.cs
// =======================================================
using UnityEngine;

namespace R3Agent.Motivation
{
    public sealed class MotivationSystem
    {
        public readonly MotivationState State = new MotivationState();

        public void ApplyReflection(float reflectionScore)
        {
            State.Socialize = Mathf.Clamp01(State.Socialize + 0.15f * reflectionScore);
            State.Help = Mathf.Clamp01(State.Help + 0.10f * reflectionScore);
            State.Avoid = Mathf.Clamp01(State.Avoid - 0.12f * reflectionScore);
            State.Clamp();
        }
    }
}