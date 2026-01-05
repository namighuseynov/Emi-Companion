using System;
using System.Collections.Generic;
using R3Chat.NLU;
using R3Agent.Perception;
using UnityEngine;

namespace R3Chat.Bridge
{
    public class R3EventMapper
    {
        private const string USER_ID = "User";

        public List<PerceptionEvent> MapToPerceptionEvents(NluPacket nlu)
        {
            var list = new List<PerceptionEvent>();

            if (nlu == null) return list;
            if (nlu.events == null) return list;

            foreach (var e in nlu.events)
            {
                var type = ParseEventType(e.type);
                var intensity = Mathf.Clamp01(e.intensity);


                var pe = new PerceptionEvent(
                    type,
                    USER_ID,
                    intensity,
                    nlu.topic ?? "",
                    nlu.intent ?? "",
                    nlu.sentiment,
                    nlu.politeness,
                    nlu.engagement,
                    nlu.expectation != null ? nlu.expectation.violation_score : 0f,
                    e.evidence ?? ""
                );

                list.Add(pe);
            }

            if (list.Count == 0)
            {
                list.Add(new PerceptionEvent(
                    PerceptionEventType.InfoRequest,
                    USER_ID,
                    0.3f,
                    nlu.topic ?? "",
                    nlu.intent ?? "unknown",
                    nlu.sentiment,
                    nlu.politeness,
                    nlu.engagement,
                    nlu.expectation != null ? nlu.expectation.violation_score : 0f,
                    "fallback"
                ));
            }

            return list;
        }

        private static PerceptionEventType ParseEventType(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return PerceptionEventType.InfoRequest;


            if (Enum.TryParse(raw.Trim(), ignoreCase: true, out PerceptionEventType t))
                return t;

            string s = raw.Trim().ToLowerInvariant();

            return s switch
            {
                "compliment" => PerceptionEventType.Praise,
                "helped" => PerceptionEventType.Praise,
                "threatened" => PerceptionEventType.Threat,
                "ignored" => PerceptionEventType.NoResponse,
                "neutralinteraction" => PerceptionEventType.InfoRequest,
                "violatedexpectation" => PerceptionEventType.BoundaryViolation,
                _ => PerceptionEventType.InfoRequest
            };
        }
    }
}
