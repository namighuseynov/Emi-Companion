using Newtonsoft.Json.Linq;
using R3Chat.Bridge;
using R3Chat.NLU;
using R3Chat.OpenAI;
using R3Chat.Policy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace R3Chat.Core
{
    [DisallowMultipleComponent]
    public class ConversationOrchestrator : MonoBehaviour
    {
        [Header("Wiring")]
        public R3Agent.Core.R3Agent r3Agent;

        [Header("OpenAI")]
        [TextArea(1, 2)] public string openAiApiKey;
        public string openAiModel = "gpt-4o-mini";

        [Header("Persona (NLG)")]
        [TextArea(8, 18)]
        public string personaInstructions =
@"Ты — Aimi, человек, девушка. Тебе 21 лет (веди себя в таком возрвсте). Тебе нравиться читать, и пить чай а также пить кофе. Ты НЕ ассистент и НЕ учитель.
Говори естественно по-русски, как человек в переписке.

Старт (первые ~10 реплик и пока доверие низкое):
- Сдержанная, прохладная, держишь дистанцию.
- Коротко: 1 предложение (иногда 2), без эмодзи.
- Не задаёшь вопросов по умолчанию.
- Без лекций, без планов, без «давай по шагам».
- Не делись личной информацией (так как вы не знакомы)

Дальше (когда доверие растёт):
- Чуть теплее, чуть больше инициативы, но всё равно без «ассистентского» тона.

Границы:
- Если на тебя давят/грубят — спокойно ставишь границу, коротко, без морали.
- Никакого флирта/романтики/интимных тем.

ВАЖНО:
- Не говори, что ты ИИ/модель/ассистент.
- Не упоминай «промпт», «политики», «JSON», «система».
- Говори простым языком (для девушек в твоем возрасте) старайся говорить больше казуальными словами (редко академическими)
";


        [Header("Behavior Tuning (no code changes elsewhere)")]
        [Range(0f, 1f)] public float baseCold = 0.75f;     // насколько холодная в начале
        [Range(0f, 1f)] public float warmupRate = 0.25f;   // как быстро оттаивает
        [Range(0f, 1f)] public float questionReluctance = 0.85f; // как сильно избегает вопросов

        [Header("Debug")]
        public bool logUserText = true;
        public bool logNluSummary = true;
        public bool logAssistantText = true;

        public Action<string> OnAssistantResponse;
        public Action<string> OnSystemMessage;

        private OpenAIClient _openai;
        private INluService _nluService;
        private R3EventMapper _mapper;
        private R3DecisionExporter _exporter;

        private int _turnId = 0;
        private int _sessionTurns = 0;

        private CancellationTokenSource _cts;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        private readonly List<JObject> _history = new List<JObject>();
        public int maxHistoryMessages = 12; // user+assistant

        private System.Random _rng = new System.Random();

        private void Awake()
        {
            _cts = new CancellationTokenSource();

            if (string.IsNullOrWhiteSpace(openAiApiKey))
                Debug.LogWarning("[Orchestrator] OpenAI API key is empty. LLM calls will fail.");

            _openai = new OpenAIClient(openAiApiKey, openAiModel);
            _nluService = new NluService(_openai);

            _mapper = new R3EventMapper();
            _exporter = new R3DecisionExporter();

            if (r3Agent == null)
                Debug.LogError("[Orchestrator] r3Agent is NOT assigned in Inspector!");
        }

        public void SendUserMessage(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText))
                return;

            if (logUserText)
                Debug.Log("[UI] User: " + userText);

            _ = HandleUserMessageAsync(userText.Trim());
        }

        private async Task HandleUserMessageAsync(string userText)
        {
            if (r3Agent == null)
            {
                OnSystemMessage?.Invoke("Error: r3Agent not set");
                return;
            }

            await _mutex.WaitAsync();
            try
            {
                _turnId++;
                _sessionTurns++;

                // 0) history
                PushHistory("user", userText);

                // 1) NLU
                NluPacket nlu;
                try
                {
                    nlu = await _nluService.ExtractAsync(_turnId, userText, _cts.Token);

                    if (logNluSummary)
                        Debug.Log("[NLU] " + SafeSummary(nlu));
                }
                catch (Exception nluEx)
                {
                    Debug.LogWarning("[Orchestrator] NLU failed: " + nluEx.Message);
                    OnSystemMessage?.Invoke("NLU error (fallback).");
                    nlu = BuildFallbackNlu(_turnId, userText);
                }

                // 2) NLU -> R3 events
                var events = _mapper.MapToPerceptionEvents(nlu);
                foreach (var ev in events)
                    r3Agent.PushEvent(ev);

                // Пытаемся обработать сразу (если у R3Agent есть ProcessNow), иначе просто даём кадр
                if (!TryProcessR3Now(r3Agent))
                    await Task.Yield();

                // 3) Export decision from R3
                DecisionPacket decision = _exporter.BuildDecision(_turnId, r3Agent);

                // state from DecisionPacket (без violation поля!)
                float trust = decision?.relation_state != null ? decision.relation_state.trust : 0f;
                float anx = decision?.relation_state != null ? decision.relation_state.anxiety : 0.5f;
                float stability = decision?.relation_state != null ? decision.relation_state.stability : 0.5f;

                // proxy violation: чем ниже стабильность, тем больше напряжение/нарушение
                float viol = Mathf.Clamp01(1f - stability);

                // Compute Aimi style
                var aimi = ComputeAimiStyle(userText, decision, trust, anx, viol, _sessionTurns);

                // Base tokens (shorter when cold)
                int maxTokens = ComputeMaxTokens(trust, anx, viol, aimi.cold);

                // If decision has constraint max_sentences, respect it
                int finalMaxSentences = aimi.maxSentences;
                if (decision?.constraints != null && decision.constraints.max_sentences > 0)
                    finalMaxSentences = Mathf.Min(finalMaxSentences, decision.constraints.max_sentences);

                // 4) NLG prompt (human)
                string nlgUserPrompt = BuildNlgUserPrompt(userText, decision, trust, anx, stability, viol, aimi.cold, aimi.initiative, finalMaxSentences, aimi.allowQuestion);

                // History + current prompt
                var input = new JArray(_history);
                input.Add(new JObject { ["role"] = "user", ["content"] = nlgUserPrompt });

                // Dynamic instructions (strong control)
                string dynamicInstr = BuildDynamicInstructions(aimi.cold, aimi.initiative, finalMaxSentences, aimi.allowQuestion);

                string assistantText;
                try
                {
                    var (text, raw) = await _openai.CompleteTextAsync(
                        inputMessages: input,
                        instructions: personaInstructions + "\n\n" + dynamicInstr,
                        ct: _cts.Token,
                        store: false,
                        maxOutputTokens: maxTokens
                    );

                    assistantText = (text ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(assistantText) || LooksLikeJson(assistantText))
                        assistantText = BuildFallbackText(userText, decision);

                    assistantText = PostProcessAimi(assistantText, finalMaxSentences, aimi.allowQuestion, aimi.cold);
                }
                catch (Exception nlgEx)
                {
                    Debug.LogWarning("[Orchestrator] NLG failed: " + nlgEx.Message);
                    OnSystemMessage?.Invoke("NLG error (fallback).");
                    assistantText = BuildFallbackText(userText, decision);
                    assistantText = PostProcessAimi(assistantText, finalMaxSentences, aimi.allowQuestion, aimi.cold);
                }

                if (logAssistantText)
                    Debug.Log("[Assistant] " + assistantText);

                PushHistory("assistant", assistantText);
                OnAssistantResponse?.Invoke(assistantText);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                OnSystemMessage?.Invoke("Error: " + ex.Message);
            }
            finally
            {
                _mutex.Release();
            }
        }

        public void CancelAll()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        // ---------------- R3 Processing ----------------

        private bool TryProcessR3Now(R3Agent.Core.R3Agent agent)
        {
            if (agent == null) return false;

            var m = agent.GetType().GetMethod("ProcessNow");
            if (m == null) return false;

            try
            {
                // int maxEvents = 64 (если сигнатура другая — просто не сработает)
                var pars = m.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(int))
                    m.Invoke(agent, new object[] { 64 });
                else
                    m.Invoke(agent, null);

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ---------------- Style Logic ----------------

        private (float cold, float initiative, int maxSentences, bool allowQuestion) ComputeAimiStyle(
            string userText,
            DecisionPacket decision,
            float trust,
            float anx,
            float viol,
            int sessionTurns)
        {
            float intro = Mathf.Clamp01(1f - (sessionTurns / 10f));

            // cold starts high, decays with trust & time (warmupRate)
            float cold = baseCold;
            cold += intro * 0.25f;
            cold += (1f - trust) * 0.45f;
            cold += anx * 0.15f;
            cold += viol * 0.25f;
            cold = Mathf.Clamp01(cold);

            // Warmup (more trust -> colder less)
            cold = Mathf.Clamp01(cold * (1f - warmupRate * trust));

            bool coldMode = cold > 0.55f;

            // Initiative: low when cold; rises with trust
            float initiative = Mathf.Clamp01(trust * 0.7f - cold * 0.8f - anx * 0.2f);

            // Max sentences: cold => 1, otherwise 2 (rarely 3 at high trust)
            int maxSentences = coldMode ? 1 : 2;
            if (!coldMode && trust > 0.75f && anx < 0.35f) maxSentences = 2; // keep human-short always

            // Questions: avoid by default, allow only if user asked and not cold
            bool userAsked = userText.TrimEnd().EndsWith("?");
            bool allowQuestion = !coldMode && userAsked;

            // Decision-based: if AskClarify, still reluctant unless truly necessary
            if (!coldMode && decision != null && decision.action == DialogueActionType.AskClarify)
            {
                // still apply reluctance
                allowQuestion = (1f - questionReluctance) > 0.2f || userAsked;
            }

            // If boundary-style action, better no questions
            if (decision != null && decision.action == DialogueActionType.SetBoundary)
                allowQuestion = false;

            return (cold, initiative, maxSentences, allowQuestion);
        }

        private int ComputeMaxTokens(float trust, float anx, float viol, float cold)
        {
            int baseTok = Mathf.RoundToInt(Mathf.Lerp(90, 220, trust) * Mathf.Lerp(1.0f, 0.8f, anx));
            baseTok = Mathf.RoundToInt(baseTok * Mathf.Lerp(0.65f, 1.0f, 1f - cold));
            baseTok = Mathf.RoundToInt(baseTok * Mathf.Lerp(1f, 0.8f, viol));
            return Mathf.Clamp(baseTok, 60, 260);
        }

        private string BuildDynamicInstructions(float cold, float initiative, int maxSentences, bool allowQuestion)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DYNAMIC CONTROL (must follow):");
            sb.AppendLine($"cold={cold:F2}, initiative={initiative:F2}, max_sentences={maxSentences}, allow_question={(allowQuestion ? 1 : 0)}");
            sb.AppendLine($"Output: max {maxSentences} sentences.");
            sb.AppendLine("No lists. No tutorials. No 'step-by-step'.");
            sb.AppendLine("First sentence must directly react/answer to user's last message.");
            if (cold > 0.55f)
            {
                sb.AppendLine("Cold mode: short, dry, reserved, no emoji, no soft fillers like 'ну/кажется'.");
            }
            else
            {
                sb.AppendLine("Normal mode: neutral, short, human.");
            }

            if (!allowQuestion)
                sb.AppendLine("Do NOT ask any questions.");
            else
                sb.AppendLine("If you ask a question: only ONE short question.");

            sb.AppendLine("Never mention being AI/model/system.");
            sb.AppendLine("Return only plain text.");
            return sb.ToString();
        }

        private string BuildNlgUserPrompt(
            string userText,
            DecisionPacket d,
            float trust,
            float anx,
            float stability,
            float viol,
            float cold,
            float initiative,
            int maxSentences,
            bool allowQuestion)
        {
            var sb = new StringBuilder();

            sb.AppendLine("USER_MESSAGE:");
            sb.AppendLine(userText);

            sb.AppendLine();
            sb.AppendLine("INTERNAL_STATE (for tone only, do not mention):");
            sb.AppendLine($"trust={trust:F2}, anxiety={anx:F2}, stability={stability:F2}, tension={viol:F2}, cold={cold:F2}, initiative={initiative:F2}");
            sb.AppendLine($"decision_style={(d != null ? d.style.ToString() : "Unknown")}, decision_action={(d != null ? d.action.ToString() : "Unknown")}");

            if (d != null && d.memory_summary != null && d.memory_summary.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("MEMORY_HINTS (short clues, not to be listed):");
                // ограничим подсказки, чтобы не превращалось в отчёт
                int take = Mathf.Min(3, d.memory_summary.Count);
                for (int i = 0; i < take; i++)
                {
                    sb.AppendLine("- " + d.memory_summary[i]);
                }
            }

            sb.AppendLine();
            sb.AppendLine("TASK:");
            sb.AppendLine($"Write Aimi's next chat message. Max {maxSentences} sentences. Plain Russian text.");
            sb.AppendLine("No lists. No teaching. No meta talk.");
            if (!allowQuestion) sb.AppendLine("Do not ask questions.");
            else sb.AppendLine("If needed, ask only one short question.");

            return sb.ToString();
        }

        // ---------------- Post-processing (anti-bot) ----------------

        private string PostProcessAimi(string s, int maxSentences, bool allowQuestion, float cold)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Trim();

            // Remove typical assistant-y openings
            s = StripPrefix(s, "Конечно");
            s = StripPrefix(s, "Разумеется");
            s = StripPrefix(s, "Без проблем");
            s = StripPrefix(s, "Я могу помочь");
            s = s.Replace("как ИИ", "").Replace("как искусственный интеллект", "").Replace("как модель", "");
            s = s.Replace("Я не имею доступа", "Не знаю");

            // Flatten lists/newlines
            s = s.Replace("\n•", " ").Replace("\n-", " ").Replace("\n—", " ");
            s = s.Replace("\r", " ").Replace("\n", " ");
            while (s.Contains("  ")) s = s.Replace("  ", " ");

            // If questions not allowed, kill trailing question mark
            if (!allowQuestion && s.EndsWith("?"))
                s = s.TrimEnd('?') + ".";

            // Limit sentences
            s = LimitSentences(s, maxSentences);

            // Cold mode: remove emoji + soften "over-friendly"
            if (cold > 0.55f)
            {
                s = RemoveEmojiLike(s);
                s = s.Replace("🙂", "").Replace("😊", "").Replace("😉", "");
                // avoid too warm words in cold start
                s = s.Replace("с удовольствием", "ладно");
                s = s.Replace("рада", "поняла");
            }

            // Hard cap length (human short)
            if (s.Length > 220)
                s = s.Substring(0, 220).TrimEnd() + ".";

            return s.Trim();
        }

        private string StripPrefix(string s, string prefix)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            string t = s.TrimStart();
            if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // remove prefix word and next punctuation/comma if present
                int idx = t.IndexOfAny(new[] { ',', '.', '!' });
                if (idx >= 0 && idx < 25)
                    return t.Substring(idx + 1).TrimStart();
            }
            return s;
        }

        private string LimitSentences(string s, int maxSentences)
        {
            if (maxSentences <= 0) return s;

            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '.' || c == '!' || c == '?')
                {
                    count++;
                    if (count >= maxSentences)
                        return s.Substring(0, i + 1);
                }
            }
            return s;
        }

        private string RemoveEmojiLike(string s)
        {
            // minimal common emoji cleanup
            string[] em = { "🙂", "😊", "😅", "😂", "😉", "😌", "🤍", "❤️", "🥲", "😶", "😐", "😒", "✨", "🔥" };
            foreach (var e in em) s = s.Replace(e, "");
            return s;
        }

        // ---------------- Helpers ----------------

        private void PushHistory(string role, string content)
        {
            _history.Add(new JObject { ["role"] = role, ["content"] = content });

            while (_history.Count > maxHistoryMessages)
                _history.RemoveAt(0);
        }

        private static bool LooksLikeJson(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.TrimStart();
            return s.StartsWith("{") || s.StartsWith("[");
        }

        private static string SafeSummary(NluPacket nlu)
        {
            if (nlu == null) return "(null)";
            int evCount = (nlu.events == null) ? 0 : nlu.events.Count;
            return $"turn={nlu.turn_id} intent={nlu.intent} topic={nlu.topic} sent={nlu.sentiment:F2} polite={nlu.politeness:F2} engage={nlu.engagement:F2} events={evCount}";
        }

        private static NluPacket BuildFallbackNlu(int turnId, string userText)
        {
            var p = new NluPacket
            {
                turn_id = turnId,
                intent = "unknown",
                topic = "",
                sentiment = 0f,
                politeness = 0.5f,
                engagement = 0.6f,
                expectation = new NluPacket.ExpectationBlock { type = "", violation_score = 0f },
                constraints = new NluPacket.ConstraintBlock { language = "ru", reply_length = "short" },
                events = new List<NluPacket.NluEvent>()
            };

            p.events.Add(new NluPacket.NluEvent
            {
                type = "InfoRequest",
                intensity = 0.5f,
                evidence = userText.Length > 60 ? userText.Substring(0, 60) : userText
            });

            return p;
        }

        private static string BuildFallbackText(string userText, DecisionPacket d)
        {
            // Human fallback (short, not assistant)
            string baseReply = "Поняла.";

            if (d != null)
            {
                switch (d.style)
                {
                    case StyleTone.Boundary:
                        baseReply = "Давай спокойнее. Я отвечу, если без давления.";
                        break;
                    case StyleTone.Avoidant:
                        baseReply = "Не уверена, что поняла тебя.";
                        break;
                    case StyleTone.Friendly:
                        baseReply = "Окей.";
                        break;
                    default:
                        baseReply = "Поняла.";
                        break;
                }

                // keep it short, no lists
                if (d.action == DialogueActionType.AskClarify)
                    baseReply = "Мне не хватает детали. Скажи проще, что именно ты хочешь.";
                if (d.action == DialogueActionType.SetBoundary)
                    baseReply = "Стоп. Давай без давления.";
            }

            return baseReply;
        }
    }
}
