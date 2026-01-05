using R3Agent.Adaptation;
using R3Agent.Decision;

namespace R3Agent.Action
{
    public sealed class DialogueAction : IAction
    {
        public string Name { get; }
        public float BaseCost { get; }

        public DialogueAction(InteractionStyle style)
        {
            BaseCost = 0.10f;
            Name = style switch
            {
                InteractionStyle.Friendly => "Dialogue: Friendly",
                InteractionStyle.Neutral => "Dialogue: Neutral",
                InteractionStyle.Avoidant => "Dialogue: Avoidant",
                InteractionStyle.Boundary => "Dialogue: Boundary",
                _ => "Dialogue"
            };
        }
    }
}
