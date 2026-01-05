using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace R3Chat.Policy
{
    [Serializable]
    public class DecisionPacket
    {
        public int turn_id;
        public StyleTone style;                 
        public DialogueActionType action;       
        public ActionParams action_params;

        public ConstraintParams constraints;

        public RelationState relation_state;    
        public List<string> memory_summary;     

        [Serializable]
        public class ActionParams
        {
            public List<string> bullets;        
            public List<string> questions;      
        }

        [Serializable]
        public class ConstraintParams
        {
            public int max_sentences;
            public bool no_jargon;
            public bool be_concise;
        }

        [Serializable]
        public class RelationState
        {
            public float trust;
            public float anxiety;
            public float stability;

            public float violation;
        }
    }
}
