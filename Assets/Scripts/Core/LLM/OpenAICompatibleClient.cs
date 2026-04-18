using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MMate.Core.LLM
{
    public class OpenAICompatibleClient : LLMClient
    {
        [SerializeField] private LLMConfig config;
        [SerializeField] private float timeout = 30f;

        private bool isProcessing = false;

        public LLMConfig Config
        {
            get => config;
            set => config = value;
        }

        public override void SendChatAsync(List<ChatMessage> messages)
        {
            if (isProcessing)
            {
                Debug.LogWarning("Request already in progress");
                return;
            }

            if (config == null || string.IsNullOrEmpty(config.apiKey))
            {
                Debug.LogError("Invalid config or missing API key");
                return;
            }

            StartCoroutine(SendChatRequest(messages));
        }

        public override bool CheckConnection()
        {
            if (config == null || string.IsNullOrEmpty(config.baseUrl) || string.IsNullOrEmpty(config.apiKey))
            {
                return false;
            }
            return true;
        }

        private IEnumerator SendChatRequest(List<ChatMessage> messages)
        {
            isProcessing = true;

            string url = $"{config.baseUrl.TrimEnd('/')}/chat/completions";
            string requestBody = BuildRequestBody(messages);

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
                    Debug.LogError($"Request failed: {request.error}");
                    InvokeResponseChunk($"[Error] {request.error}");
                }
                else
                {
                    ParseAndStreamResponse(request.downloadHandler.text);
                }
            }

            isProcessing = false;
        }

        private string BuildRequestBody(List<ChatMessage> messages)
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

            return $"{{\"model\":\"{config.model}\",\"messages\":{messageList},\"max_tokens\":{config.maxTokens},\"temperature\":{config.temperature},\"stream\":false}}";
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

        private void ParseAndStreamResponse(string response)
        {
            try
            {
                var responseData = JsonUtility.FromJson<OpenAIResponse>(response);

                if (responseData.choices != null && responseData.choices.Length > 0)
                {
                    string content = responseData.choices[0].message.content;
                    InvokeResponseChunk(content);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse response: {e.Message}");
                InvokeResponseChunk($"[Error] Failed to parse response");
            }
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
