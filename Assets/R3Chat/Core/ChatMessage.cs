using System;

namespace R3Chat.Core
{
    public enum ChatRole { User, Assistant, System }

    [Serializable]
    public struct ChatMessage
    {
        public ChatRole role;
        public string content;
        public long unixMs;

        public ChatMessage(ChatRole role, string content, long unixMs)
        {
            this.role = role;
            this.content = content;
            this.unixMs = unixMs;
        }
    }
}