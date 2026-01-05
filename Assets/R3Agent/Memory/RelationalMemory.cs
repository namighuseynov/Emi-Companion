// =======================================================
// File: Assets/R3Agent/Memory/RelationalMemory.cs
// =======================================================
using System.Collections.Generic;
using R3Agent.Core;
using R3Agent.Emotion;
using R3Agent.Perception;

namespace R3Agent.Memory
{
    public sealed class RelationalMemory
    {
        private readonly AgentConfig _cfg;
        private readonly List<MemoryRecord> _records;

        public IReadOnlyList<MemoryRecord> Records => _records;

        public RelationalMemory(AgentConfig cfg)
        {
            _cfg = cfg;
            _records = new List<MemoryRecord>(cfg.memoryCapacity);
        }

        public void AddRecord(PerceptionEvent ev, EmotionalState emo, float relationImpact)
        {
            if (_records.Count >= _cfg.memoryCapacity)
            {
                // FIFO
                _records.RemoveAt(0);
            }
            _records.Add(new MemoryRecord(ev, emo, relationImpact));
        }
    }
}