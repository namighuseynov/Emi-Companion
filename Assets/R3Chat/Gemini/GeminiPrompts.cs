using System.Text;
using R3Chat.Core;
using R3Chat.Policy;

namespace R3Chat.Gemini
{
    public static class GeminiPrompts
    {
        public static string BuildNluPrompt(string userText, ChatMessage[] recent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an NLU extractor. Return ONLY valid JSON, no comments.");
            sb.AppendLine("Return ONLY a single JSON object. No markdown, no code fences, no extra text.");
            sb.AppendLine("JSON must have double quotes, no trailing commas, numbers must be numbers (not strings).");
            sb.AppendLine("If unsure, return events: [{\"type\":\"InfoRequest\",\"intensity\":0.4,\"evidence\":\"\"}]");
            sb.AppendLine("Schema: {turn_id,intent,topic,sentiment(-1..1),politeness(0..1),engagement(0..1),expectation{type,violation_score(0..1)},events[{type,intensity(0..1),evidence}],constraints{language,reply_length}}");
            sb.AppendLine("Allowed event types: InfoRequest, PoliteRequest, Thanks, Praise, Criticism, Insult, Apology, PromiseMade, PromiseKept, PromiseBroken, BoundaryViolation, Confusion, Agreement, Disagreement, NoResponse, FollowUpQuestion, SafetyConcern.");
            sb.AppendLine("Return ONLY valid JSON. No extra text. No markdown. No comments.");
            sb.AppendLine("User text:");
            sb.AppendLine(userText);
            return sb.ToString();
        }

        public static string BuildNlgPrompt(string userText, DecisionPacket decision, string[] memorySummary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are a response verbalizer. Follow the policy strictly. Do NOT change action or style.");
            sb.AppendLine($"Action: {decision.action}");
            sb.AppendLine($"Style: {decision.style}");
            sb.AppendLine($"Constraints: max_sentences={decision.constraints.max_sentences}, no_jargon={decision.constraints.no_jargon}, be_concise={decision.constraints.be_concise}");
            sb.AppendLine("Memory summary (use lightly, do not reveal private details):");
            foreach (var s in memorySummary) sb.AppendLine("- " + s);
            sb.AppendLine("User message:");
            sb.AppendLine(userText);
            sb.AppendLine("Produce the final assistant answer in Russian.");
            return sb.ToString();
        }
    }
}
