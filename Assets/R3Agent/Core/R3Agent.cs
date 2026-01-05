using R3Agent.Action;
using R3Agent.Adaptation;
using R3Agent.Decision;
using R3Agent.Emotion;
using R3Agent.Perception;
using R3Agent.Relationship;
using UnityEngine;

namespace R3Agent.Core
{
    [DisallowMultipleComponent]
    public class R3Agent : MonoBehaviour
    {
        [Header("Config")]
        public AgentConfig config;

        [Header("Wiring")]
        public ActionController actionController;

        [Header("Debug")]
        public bool logDebug = true;

        private AgentContext _ctx;
        private EmotionalState _emotion;
        private float _nextReflectionAt;

        private const string USER_ID = "User";

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[R3Agent] AgentConfig is not assigned.");
                enabled = false;
                return;
            }

            if (actionController == null)
            {
                actionController = GetComponent<ActionController>();
                if (actionController == null) actionController = gameObject.AddComponent<ActionController>();
            }

            _ctx = new AgentContext(config, transform);
            _emotion = new EmotionalState();

            _nextReflectionAt = Time.time + config.reflectionIntervalSec;


            _ctx.Relationships.GetOrCreate(USER_ID);
        }

        private void Update()
        {

            while (_ctx.Perception.TryDequeue(out PerceptionEvent ev))
            {

                RelationshipState rel = _ctx.Relationships.GetOrCreate(ev.SourceId);

                var appraisal = _ctx.Appraisal.Appraise(ev, rel, _emotion);
                _emotion.Apply(appraisal.EmotionDelta);

                LastViolationScore = Mathf.Clamp01(appraisal.ViolationScore);

                _ctx.Relationships.ApplyExperience(ev.SourceId, appraisal.RelationDelta, appraisal.ViolationScore);
                _ctx.Memory.AddRecord(ev, _emotion, appraisal.RelationDelta);

                var style = _ctx.RL.SelectStyle(rel, ev);


                style = StyleGuard.Apply(style, ev, rel, appraisal.ViolationScore);
                CurrentStyle = style;

                _ctx.RL.Learn(rel, ev, _ctx.Relationships.GetOrCreate(ev.SourceId));

                IAction action = _ctx.Decision.ChooseAction(rel, _emotion, style);
                actionController.Execute(action, style);

                if (logDebug)
                {
                    Debug.Log($"[R3] Ev={ev.Type} src={ev.SourceId} emo(V={_emotion.Valence:F2},A={_emotion.Arousal:F2}) " +
                              $"rel(T={rel.Trust:F2},Anx={rel.Anxiety:F2}) style={style}");
                }
            }


            if (Time.time >= _nextReflectionAt)
            {
                _nextReflectionAt = Time.time + config.reflectionIntervalSec;

                float reflectionScore = _ctx.Reflection.ComputeReflectionScore(_ctx.Memory, _ctx.Relationships, USER_ID);
                _ctx.Relationships.ApplyReflection(USER_ID, reflectionScore);
                _ctx.Motivation.ApplyReflection(reflectionScore);

                if (logDebug)
                    Debug.Log($"[R3] ReflectionScore={reflectionScore:F3} -> motivations updated");
            }
        }


        public void PushEvent(PerceptionEvent ev) => _ctx.Perception.Enqueue(ev);


        [ContextMenu("TEST: User praise")]
        public void TestHelped()
        {
            Debug.Log("[R3] TestPraise clicked");
            PushEvent(new PerceptionEvent(
                PerceptionEventType.Praise,
                USER_ID,
                1.0f,
                "test"
            ));
        }

        [ContextMenu("TEST: User boundary violation")]
        public void TestViolated()
        {
            Debug.Log("[R3] TestBoundaryViolation clicked");
            PushEvent(new PerceptionEvent(
                PerceptionEventType.BoundaryViolation,
                USER_ID,
                1.0f,
                "test"
            ));
        }

        public float LastViolationScore { get; private set; } = 0f;

        public RelationshipState GetRelation(string id = "User") => _ctx.Relationships.GetOrCreate(id);
        public EmotionalState GetEmotion() => _emotion;

        public InteractionStyle CurrentStyle { get; private set; } = InteractionStyle.Neutral;
    }
}