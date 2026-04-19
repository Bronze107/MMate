using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMate.Core.LLM
{
    public class OpenAICompatibleClient : LLMClient
    {
        [SerializeField] private LLMConfig config;
        [SerializeField] private float timeout = 120f;

        private bool isProcessing = false;
        private static HttpClient _httpClient;
        private static readonly object _clientLock = new object();

        private static HttpClient GetHttpClient()
        {
            if (_httpClient == null)
            {
                lock (_clientLock)
                {
                    if (_httpClient == null)
                    {
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                        };
                        _httpClient = new HttpClient(handler);
                    }
                }
            }
            return _httpClient;
        }

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

        public override async Task<string> SendAsync(List<ChatMessage> messages)
        {
            if (isProcessing)
            {
                Debug.LogWarning("Request already in progress");
                return null;
            }

            if (config == null || string.IsNullOrEmpty(config.apiKey))
            {
                Debug.LogError("Invalid config or missing API key");
                return null;
            }

            isProcessing = true;

            string url = $"{config.baseUrl.TrimEnd('/')}/chat/completions";
            string requestBody = BuildRequestBody(messages, false);

            Debug.Log($"[LLM] URL: {url}");
            Debug.Log($"[LLM] RequestBody: {requestBody}");

            try
            {
                var client = GetHttpClient();
                client.Timeout = TimeSpan.FromSeconds(timeout);

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", $"Bearer {config.apiKey}");

                    var response = await client.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Debug.Log($"[LLM] Response code: {(int)response.StatusCode}");
                    Debug.Log($"[LLM] Response: {responseBody}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogError($"[LLM] Request failed: HTTP {(int)response.StatusCode} - {responseBody}");
                        return null;
                    }

                    return ParseAndStreamResponse(responseBody, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LLM] Request exception: {e.Message}");
                throw;
            }
            finally
            {
                isProcessing = false;
            }
        }

        public override async Task SendStreamingAsync(List<ChatMessage> messages, Action<string> onChunk)
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

            isProcessing = true;

            string url = $"{config.baseUrl.TrimEnd('/')}/chat/completions";
            string requestBody = BuildRequestBody(messages, true);
            var fullContent = new StringBuilder();

            Debug.Log($"[LLM] URL: {url}");
            Debug.Log($"[LLM] Streaming request...");

            try
            {
                var client = GetHttpClient();
                client.Timeout = TimeSpan.FromSeconds(timeout);

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", $"Bearer {config.apiKey}");

                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        Debug.LogError($"[LLM] Request failed: HTTP {(int)response.StatusCode} - {errorBody}");
                        return;
                    }

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                                continue;

                            var data = line.Substring(6).Trim();
                            if (data == "[DONE]")
                                break;

                            try
                            {
                                var streamChunk = JsonUtility.FromJson<OpenAIStreamChunk>(data);
                                if (streamChunk.choices?.Length > 0)
                                {
                                    var content = streamChunk.choices[0].delta?.content;
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        fullContent.Append(content);
                                        onChunk?.Invoke(content);
                                        InvokeResponseChunk(content);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"[LLM] Failed to parse stream chunk: {e.Message}, data: {data}");
                            }
                        }
                    }

                    string completeResponse = fullContent.ToString();
                    Debug.Log($"[LLM] Stream complete. Total chars: {completeResponse.Length}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LLM] Streaming request exception: {e.Message}");
                throw;
            }
            finally
            {
                isProcessing = false;
            }
        }

        private string BuildRequestBody(List<ChatMessage> messages, bool streaming)
        {
            var sb = new StringBuilder();
            sb.Append("{\"model\":");
            EscapeJsonString(sb, config.model);
            sb.Append(",\"messages\":[");

            for (int i = 0; i < messages.Count; i++)
            {
                sb.Append("{\"role\":");
                EscapeJsonString(sb, messages[i].role);
                sb.Append(",\"content\":");
                EscapeJsonString(sb, messages[i].content);
                sb.Append("}");
                if (i < messages.Count - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append("],\"max_tokens\":");
            sb.Append(config.maxTokens);
            sb.Append(",\"temperature\":");
            sb.Append(config.temperature.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stream\":");
            sb.Append(streaming ? "true" : "false");
            sb.Append("}");

            return sb.ToString();
        }

        private static void EscapeJsonString(StringBuilder sb, string str)
        {
            if (str == null)
            {
                sb.Append("\"\"");
                return;
            }

            sb.Append('"');
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
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

        [Serializable]
        private class OpenAIStreamChunk
        {
            public string id;
            public StreamChoice[] choices;
        }

        [Serializable]
        private class StreamChoice
        {
            public StreamDelta delta;
            public int index;
            public string finish_reason;
        }

        [Serializable]
        private class StreamDelta
        {
            public string role;
            public string content;
        }
    }
}
