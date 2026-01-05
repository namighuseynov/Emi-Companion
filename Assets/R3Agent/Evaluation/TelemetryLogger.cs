// =======================================================
// File: Assets/R3Agent/Evaluation/TelemetryLogger.cs
// =======================================================
using System.Collections.Generic;
using UnityEngine;

namespace R3Agent.Evaluation
{
    public class TelemetryLogger : MonoBehaviour
    {
        private readonly List<string> _lines = new List<string>(1024);

        public void Log(string line)
        {
            _lines.Add($"{Time.time:F2};{line}");
        }

        [ContextMenu("Dump to Console")]
        public void Dump()
        {
            foreach (var l in _lines)
                Debug.Log("[Telemetry] " + l);
        }
    }
}