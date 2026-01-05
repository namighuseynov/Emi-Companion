// =======================================================
// File: Assets/R3Agent/Core/AgentConfig.cs
// =======================================================
using UnityEngine;

namespace R3Agent.Core
{
    [CreateAssetMenu(menuName = "R3Agent/Agent Config", fileName = "R3AgentConfig")]
    public class AgentConfig : ScriptableObject
    {
        [Header("Persona (Aimi)")]
        [Range(0f, 1f)] public float baseShyness = 0.7f;     
        [Range(0f, 1f)] public float warmupRate = 0.25f;    
        [Range(0f, 1f)] public float talkativeness = 0.35f;  

        [Header("Relationship dynamics")]
        [Range(0f, 2f)] public float alphaExperience = 0.25f;   
        [Range(0f, 2f)] public float betaViolation = 0.40f;   
        [Range(0f, 2f)] public float gammaReflection = 0.15f;  

        [Header("Reflection")]
        [Min(1f)] public float reflectionIntervalSec = 30f;     
        [Range(0f, 1f)] public float timeDecayLambda = 0.02f;  

        [Header("Memory")]
        [Min(10)] public int memoryCapacity = 200;

        [Header("RL Adapter")]
        [Range(0f, 1f)] public float epsilon = 0.10f;          
        [Range(0f, 1f)] public float learningRate = 0.20f;
        [Range(0f, 0.99f)] public float discount = 0.85f;

        [Header("Decision")]
        [Range(0f, 2f)] public float utilityTrustWeight = 1.0f;
        [Range(0f, 2f)] public float utilityAnxietyWeight = 0.8f;
    }
}