// Assets/R3Chat/NLU/NluPacket.cs
using System;
using System.Collections.Generic;

namespace R3Chat.NLU
{
    [Serializable]
    public class NluPacket
    {
        public int turn_id;
        public string intent;                 // "request_help", "complaint", ...
        public string topic;                  // "unity_ai", ...
        public float sentiment;               // -1..+1
        public float politeness;              // 0..1
        public float engagement;             

        public ExpectationBlock expectation;  

        public List<NluEvent> events;
        public ConstraintBlock constraints;

        [Serializable]
        public class ExpectationBlock
        {
            public string type;               
            public float violation_score;    
        }

        [Serializable]
        public class ConstraintBlock
        {
            public string language;          
            public string reply_length;      
        }

        [Serializable]
        public class NluEvent
        {
            public string type;              
            public float intensity;           
            public string evidence;           
        }
    }
}
