// =======================================================
// File: Assets/R3Agent/Perception/PerceptionSystem.cs
// =======================================================
using System.Collections.Generic;

namespace R3Agent.Perception
{
    public sealed class PerceptionSystem
    {
        private readonly Queue<PerceptionEvent> _queue = new Queue<PerceptionEvent>(64);

        public void Enqueue(PerceptionEvent ev) => _queue.Enqueue(ev);

        public bool TryDequeue(out PerceptionEvent ev)
        {
            if (_queue.Count > 0)
            {
                ev = _queue.Dequeue();
                return true;
            }
            ev = default;
            return false;
        }
    }
}