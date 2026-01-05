using System;

namespace R3Chat.NLU
{
    public static class NluValidator
    {
        public static void ValidateOrThrow(NluPacket p)
        {
            if (p == null) throw new Exception("NLU packet is null");
            if (p.events == null) p.events = new System.Collections.Generic.List<NluPacket.NluEvent>();
            if (p.constraints == null) p.constraints = new NluPacket.ConstraintBlock { language = "ru", reply_length = "short" };
            if (p.expectation == null) p.expectation = new NluPacket.ExpectationBlock { type = "", violation_score = 0f };
        }
    }
}
