using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public interface ILLMClient
    {
        Task SendStreamingAsync(List<ChatMessage> messages, Action<string> onChunk);
        Task<string> SendAsync(List<ChatMessage> messages);
        bool CheckConnection();
    }

    public abstract class LLMClient : MonoBehaviour, ILLMClient
    {
        public event Action<string> OnResponseChunk;

        protected void InvokeResponseChunk(string chunk)
        {
            OnResponseChunk?.Invoke(chunk);
        }

        public abstract Task SendStreamingAsync(List<ChatMessage> messages, Action<string> onChunk);
        public abstract Task<string> SendAsync(List<ChatMessage> messages);
        public abstract bool CheckConnection();
    }
}
