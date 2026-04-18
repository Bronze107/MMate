using System;
using System.Collections.Generic;
using UnityEngine;

namespace MMate.Core.LLM
{
    [Serializable]
    public struct ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    public abstract class LLMClient : MonoBehaviour
    {
        public event Action<string> OnResponseChunk;

        protected void InvokeResponseChunk(string chunk)
        {
            OnResponseChunk?.Invoke(chunk);
        }

        public abstract void SendChatAsync(List<ChatMessage> messages);

        public abstract bool CheckConnection();
    }
}
