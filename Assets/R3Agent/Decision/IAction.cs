// =======================================================
// File: Assets/R3Agent/Decision/IAction.cs
// =======================================================
namespace R3Agent.Decision
{
    public interface IAction
    {
        string Name { get; }
        float BaseCost { get; }
    }
}