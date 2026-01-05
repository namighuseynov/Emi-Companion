// =======================================================
// File: Assets/R3Agent/Action/ActionController.cs
// =======================================================
using R3Agent.Adaptation;
using R3Agent.Decision;
using UnityEngine;

namespace R3Agent.Action
{
    public class ActionController : MonoBehaviour
    {
        public void Execute(IAction action, InteractionStyle style)
        {
            Debug.Log($"[R3 Action] {action.Name} (style={style}, cost={action.BaseCost:F2})");
        }
    }
}