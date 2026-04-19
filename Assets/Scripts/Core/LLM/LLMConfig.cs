using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MMate.Core.LLM
{
    [Serializable]
    public class LLMConfig
    {
        public string provider = "openai";
        public string baseUrl = "https://api.openai.com/v1";
        public string model = "gpt-4";
        public string apiKey = "";
        public int maxTokens = 2048;
        public float temperature = 0.7f;
        public string systemPrompt = "You are a helpful assistant.";

        public static LLMConfig LoadFromJson(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"Config file not found: {path}, creating default config");
                    return new LLMConfig();
                }

                string json = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<LLMConfig>(json);
                return config ?? new LLMConfig();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load config: {e.Message}");
                return new LLMConfig();
            }
        }

        public void SaveToJson(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(this);
                File.WriteAllText(path, json);
                Debug.Log($"Config saved to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save config: {e.Message}");
            }
        }
    }
}
