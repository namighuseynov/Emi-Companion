// =======================================================
// File: Assets/R3Agent/Core/AgentContext.cs
// =======================================================
using R3Agent.Adaptation;
using R3Agent.Decision;
using R3Agent.Emotion;
using R3Agent.Memory;
using R3Agent.Motivation;
using R3Agent.Perception;
using R3Agent.Reflection;
using R3Agent.Relationship;
using UnityEngine;

namespace R3Agent.Core
{
    public sealed class AgentContext
    {
        public readonly AgentConfig Config;

        public readonly PerceptionSystem Perception;
        public readonly AppraisalSystem Appraisal;

        public readonly RelationshipModel Relationships;
        public readonly RelationalMemory Memory;

        public readonly ReflectionEngine Reflection;
        public readonly MotivationSystem Motivation;

        public readonly DecisionSystem Decision;
        public readonly ReinforcementAdapter RL;

        public AgentContext(AgentConfig cfg, Transform agentTransform)
        {
            Config = cfg;

            Perception = new PerceptionSystem();
            Appraisal = new AppraisalSystem();

            Relationships = new RelationshipModel(cfg);
            Memory = new RelationalMemory(cfg);

            Reflection = new ReflectionEngine(cfg);
            Motivation = new MotivationSystem();

            Decision = new DecisionSystem(cfg);
            RL = new ReinforcementAdapter(cfg);

        }
    }
}