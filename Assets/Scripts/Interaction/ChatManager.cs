using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MMate.Core.LLM;

namespace MMate.Interaction
{
    [Serializable]
    public class ChatConversation
    {
        public string role;
        public string content;
        public long timestamp;

        public ChatConversation(string role, string content)
        {
            this.role = role;
            this.content = content;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public ChatConversation(string role, string content, long timestamp)
        {
            this.role = role;
            this.content = content;
            this.timestamp = timestamp;
        }
    }

    public class ChatManager : MonoBehaviour
    {
        #region Singleton

        private static ChatManager _instance;

        public static ChatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ChatManager>();
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        public event Action<ChatConversation> OnConversationUpdate;
        public event Action<string> OnError;
        public event Action<string> OnStreamingChunk;
        public event Action OnResponseComplete;

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Maximum number of messages to keep in conversation history.")]
        private int maxHistoryLength = 20;

        [Header("System Prompt")]
        [SerializeField]
        [TextArea(3, 10)]
        [Tooltip("System prompt to prepend to all conversations.")]
        private string systemPrompt = "You are a friendly and helpful desktop companion. Respond naturally and engagingly.";

        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to LLMClient component.")]
        private LLMClient llmClient;

        #endregion

        #region Private Fields

        private List<ChatConversation> conversationHistory = new List<ChatConversation>();
        private bool isProcessing = false;
        private StringBuilder currentResponseBuilder = new StringBuilder();

        #endregion

        #region Public Properties

        public bool IsProcessing => isProcessing;

        public string SystemPrompt
        {
            get => systemPrompt;
            set => systemPrompt = value;
        }

        public int MaxHistoryLength
        {
            get => maxHistoryLength;
            set => maxHistoryLength = Mathf.Max(1, value);
        }

        public IReadOnlyList<ChatConversation> ConversationHistory => conversationHistory;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            if (llmClient == null)
            {
                llmClient = FindObjectOfType<LLMClient>();
            }

            if (llmClient == null)
            {
                Debug.LogWarning("[ChatManager] No LLMClient found. Please ensure one is available in the scene.");
            }
        }

        #endregion

        #region Public Methods

        public async Task SendMessageAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                Debug.LogWarning("[ChatManager] Cannot send empty message.");
                return;
            }

            if (isProcessing)
            {
                Debug.LogWarning("[ChatManager] Already processing a message. Please wait.");
                return;
            }

            if (llmClient == null)
            {
                Debug.LogError("[ChatManager] LLMClient is not available.");
                OnError?.Invoke("LLM client not available.");
                return;
            }

            isProcessing = true;
            currentResponseBuilder.Clear();

            try
            {
                var userMessage = new ChatConversation("user", content);
                AddToHistory(userMessage);
                OnConversationUpdate?.Invoke(userMessage);

                var messages = BuildMessages();

                await llmClient.SendStreamingAsync(messages, HandleLLMResponse);

                string assistantContent = currentResponseBuilder.ToString().Trim();
                if (!string.IsNullOrEmpty(assistantContent))
                {
                    var assistantMessage = new ChatConversation("assistant", assistantContent);
                    AddToHistory(assistantMessage);
                    OnConversationUpdate?.Invoke(assistantMessage);
                }

                OnResponseComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] Error sending message: {ex.Message}");
                OnError?.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
            }
        }

        public void SendMessage(string content, Action onComplete = null)
        {
            SendMessageAsync(content).ContinueWith(task =>
            {
                onComplete?.Invoke();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void ClearHistory()
        {
            conversationHistory.Clear();
            Debug.Log("[ChatManager] Conversation history cleared.");
        }

        public void SetLLMClient(LLMClient client)
        {
            llmClient = client;
        }

        #endregion

        #region Private Methods

        private void HandleLLMResponse(string chunk)
        {
            if (string.IsNullOrEmpty(chunk))
                return;

            currentResponseBuilder.Append(chunk);
            OnStreamingChunk?.Invoke(chunk);
        }

        private List<ChatMessage> BuildMessages()
        {
            var messages = new List<ChatMessage>();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new ChatMessage("system", systemPrompt));
            }

            foreach (var conversation in conversationHistory)
            {
                messages.Add(new ChatMessage(conversation.role, conversation.content));
            }

            return messages;
        }

        private void AddToHistory(ChatConversation conversation)
        {
            conversationHistory.Add(conversation);

            while (conversationHistory.Count > maxHistoryLength)
            {
                conversationHistory.RemoveAt(0);
            }
        }

        #endregion
    }
}
