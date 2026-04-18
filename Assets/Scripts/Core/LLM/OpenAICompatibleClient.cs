using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MMate.Core.LLM
{
    public class OpenAICompatibleClient : LLMClient
    {
        [SerializeField] private LLMConfig config;
        [SerializeField] private float timeout = 30f;

        private bool isProcessing = false;
        private TaskCompletionSource<string> currentTaskSource;

        public LLMConfig Config
        {
            get => config;
            set => config = value;
        }

        public override bool CheckConnection()
        {
            if (config == null || string.IsNullOrEmpty(config.baseUrl) || string.IsNullOrEmpty(config.apiKey))
            {
                return false;
            }
            return true;
        }

        public override Task<string> SendAsync(List<ChatMessage> messages)
        {
            if (isProcessing)
            {
                Debug.LogWarning("Request already in progress");
                return Task.FromResult<string>(null);
            }

            if (config == null || string.IsNullOrEmpty(config.apiKey))
            {
                Debug.LogError("Invalid config or missing API key");
                return Task.FromResult<string>(null);
            }

            currentTaskSource = new TaskCompletionSource<string>();
            StartCoroutine(SendChatRequest(messages, false, null));
            return currentTaskSource.Task;
        }

        public override Task SendStreamingAsync(List<ChatMessage> messages, Action<string> onChunk)
        {
            if (isProcessing)
            {
                Debug.LogWarning("Request already in progress");
                return Task.CompletedTask;
            }

            if (config == null || string.IsNullOrEmpty(config.apiKey))
            {
                Debug.LogError("Invalid config or missing API key");
                return Task.CompletedTask;
            }

            currentTaskSource = new TaskCompletionSource<string>();
            StartCoroutine(SendChatRequest(messages, true, onChunk));
            return currentTaskSource.Task;
        }

        private IEnumerator SendChatRequest(List<ChatMessage> messages, bool streaming, Action<string> onChunk)
        {
            isProcessing = true;

            string url = $"{config.baseUrl.TrimEnd('/')}/chat/completions";
            string requestBody = BuildRequestBody(messages, streaming);

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");
                request.timeout = (int)timeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string error = $"Request failed: {request.error}";
                    Debug.LogError(error);
                    onChunk?.Invoke($"[Error] {request.error}");
                    currentTaskSource?.TrySetResult(null);
                }
                else
                {
                    string content = ParseAndStreamResponse(request.downloadHandler.text, onChunk);
                    currentTaskSource?.TrySetResult(content);
                }
            }

            isProcessing = false;
        }

        private string BuildRequestBody(List<ChatMessage> messages, bool streaming)
        {
            var messageList = new StringBuilder();
            messageList.Append("[");

            if (!string.IsNullOrEmpty(config.systemPrompt))
            {
                messageList.Append($"{{\"role\":\"system\",\"content\":{EscapeJsonString(config.systemPrompt)}}},");
            }

            for (int i = 0; i < messages.Count; i++)
            {
                messageList.Append($"{{\"role\":\"{messages[i].role}\",\"content\":{EscapeJsonString(messages[i].content)}}}");
                if (i < messages.Count - 1)
                {
                    messageList.Append(",");
                }
            }

            messageList.Append("]");

            return $"{{\"model\":\"{config.model}\",\"messages\":{messageList},\"max_tokens\":{config.maxTokens},\"temperature\":{config.temperature},\"stream\":{streaming.ToString().ToLower()}}}";
        }

        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "\"\"";
            str = str.Replace("\\", "\\\\");
            str = str.Replace("\"", "\\\"");
            str = str.Replace("\n", "\\n");
            str = str.Replace("\r", "\\r");
            str = str.Replace("\t", "\\t");
            return $"\"{str}\"";
        }

        private string ParseAndStreamResponse(string response, Action<string> onChunk)
        {
            try
            {
                var responseData = JsonUtility.FromJson<OpenAIResponse>(response);

                if (responseData.choices != null && responseData.choices.Length > 0)
                {
                    string content = responseData.choices[0].message.content;
                    onChunk?.Invoke(content);
                    InvokeResponseChunk(content);
                    return content;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse response: {e.Message}");
                onChunk?.Invoke("[Error] Failed to parse response");
            }
            return null;
        }

        [Serializable]
        private class OpenAIResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
            public int index;
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }
    }
}
