// =======================================================
// File: Assets/R3Agent/Decision/DecisionSystem.cs
// =======================================================
using R3Agent.Adaptation;
using R3Agent.Emotion;
using R3Agent.Relationship;

namespace R3Agent.Decision
{
    public sealed class DecisionSystem
    {
        private readonly Core.AgentConfig _cfg;

        public DecisionSystem(Core.AgentConfig cfg) => _cfg = cfg;

        public IAction ChooseAction(RelationshipState rel, EmotionalState emo, InteractionStyle style)
        {
            return new R3Agent.Action.DialogueAction(style);
        }
    }
}