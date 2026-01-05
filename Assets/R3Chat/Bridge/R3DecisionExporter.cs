using System.Collections.Generic;
using R3Chat.Policy;
using R3Agent.Core;

namespace R3Chat.Bridge
{
    public class R3DecisionExporter
    {
        public DecisionPacket BuildDecision(int turnId, R3Agent.Core.R3Agent agent)
        {
            var rel = agent.GetRelation("User"); 
            var emo = agent.GetEmotion();


            var style = StyleTone.Neutral;
            if (rel.Trust > 0.65f && rel.Anxiety < 0.3f) style = StyleTone.Friendly;
            else if (rel.Anxiety > 0.6f) style = StyleTone.Boundary;

            var action = DialogueActionType.ProposePlan;

            return new DecisionPacket
            {
                turn_id = turnId,
                style = style,
                action = action,
                action_params = new DecisionPacket.ActionParams
                {
                    bullets = new List<string> { "Architecture", "JSON schema", "Unity loop", "Telemetry" },
                    questions = new List<string>()
                },
                constraints = new DecisionPacket.ConstraintParams { max_sentences = 10, no_jargon = true, be_concise = true },
                relation_state = new DecisionPacket.RelationState { trust = rel.Trust, anxiety = rel.Anxiety, stability = rel.Stability, violation = rel.Violation },
                memory_summary = new List<string> { "User wants realistic conversational agent", "Project uses Gemini as language layer" }
            };
        }
    }
}
