using R3Agent.Perception;
using R3Agent.Relationship;

namespace R3Agent.Adaptation
{
    public static class StyleGuard
    {
        public static InteractionStyle Apply(InteractionStyle proposed, PerceptionEvent ev, RelationshipState rel, float violationScore)
        {
            if (violationScore > 0.55f ||
                ev.Type == PerceptionEventType.Threat ||
                ev.Type == PerceptionEventType.Insult ||
                ev.Type == PerceptionEventType.BoundaryViolation ||
                ev.Type == PerceptionEventType.PromiseBroken)
                return InteractionStyle.Boundary;

            if (proposed == InteractionStyle.Friendly && rel.Trust < 0.35f)
                return InteractionStyle.Neutral;

            if (rel.Anxiety > 0.65f)
                return InteractionStyle.Boundary;

            return proposed;
        }
    }
}
