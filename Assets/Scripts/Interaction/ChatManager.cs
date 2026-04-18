using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MMate.Interaction
{
    /// <summary>
    /// Represents a single message in the conversation.
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    /// <summary>
    /// Represents a conversation entry with timestamp.
    /// </summary>
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

    /// <summary>
    /// Manages chat conversations with LLM integration.
    /// Singleton MonoBehaviour that handles message sending, history management,
    /// and communication with LLMClient.
    /// </summary>
    public class ChatManager : MonoBehaviour
    {
        #region Singleton

        private static ChatManager _instance;

        /// <summary>
        /// Singleton instance of ChatManager.
        /// </summary>
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

        /// <summary>
        /// Event fired when a new conversation message is added.
        /// UI components can subscribe to this event to update display.
        /// </summary>
        public event Action<ChatConversation> OnConversationUpdate;

        /// <summary>
        /// Event fired when an error occurs during message processing.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Event fired when a streaming response chunk is received.
        /// </summary>
        public event Action<string> OnStreamingChunk;

        /// <summary>
        /// Event fired when a response is completed.
        /// </summary>
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
        [Tooltip("Reference to LLMClient. If not set, will try to find via FindObjectOfType.")]
        private MonoBehaviour llmClientReference;

        #endregion

        #region Private Fields

        private List<ChatConversation> conversationHistory = new List<ChatConversation>();
        private ILLMClient llmClient;
        private bool isProcessing = false;
        private StringBuilder currentResponseBuilder = new StringBuilder();

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the manager is currently processing a message.
        /// </summary>
        public bool IsProcessing => isProcessing;

        /// <summary>
        /// Current system prompt.
        /// </summary>
        public string SystemPrompt
        {
            get => systemPrompt;
            set => systemPrompt = value;
        }

        /// <summary>
        /// Maximum history length.
        /// </summary>
        public int MaxHistoryLength
        {
            get => maxHistoryLength;
            set => maxHistoryLength = Mathf.Max(1, value);
        }

        /// <summary>
        /// Read-only access to conversation history.
        /// </summary>
        public IReadOnlyList<ChatConversation> ConversationHistory => conversationHistory;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            InitializeLLMClient();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a user message to the LLM and handles the response.
        /// </summary>
        /// <param name="content">The user's message content.</param>
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
                // Add user message to history
                var userMessage = new ChatConversation("user", content);
                AddToHistory(userMessage);
                OnConversationUpdate?.Invoke(userMessage);

                // Build messages for LLM
                var messages = BuildMessages();

                // Send to LLM and handle streaming response
                await llmClient.SendStreamingAsync(messages, HandleLLMResponse);

                // Finalize the response
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

        /// <summary>
        /// Sends a user message using callback pattern for non-async callers.
        /// </summary>
        /// <param name="content">The user's message content.</param>
        /// <param name="onComplete">Callback when the operation completes.</param>
        public void SendMessage(string content, Action onComplete = null)
        {
            SendMessageAsync(content).ContinueWith(task =>
            {
                onComplete?.Invoke();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Clears all conversation history.
        /// </summary>
        public void ClearHistory()
        {
            conversationHistory.Clear();
            Debug.Log("[ChatManager] Conversation history cleared.");
        }

        /// <summary>
        /// Sets the LLM client reference.
        /// </summary>
        /// <param name="client">The LLM client to use.</param>
        public void SetLLMClient(ILLMClient client)
        {
            llmClient = client;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the LLM client reference.
        /// </summary>
        private void InitializeLLMClient()
        {
            // First try the serialized reference
            if (llmClientReference != null)
            {
                llmClient = llmClientReference as ILLMClient;
                if (llmClient != null)
                {
                    Debug.Log("[ChatManager] LLMClient initialized from serialized reference.");
                    return;
                }
            }

            // Try to find via FindObjectOfType
            var foundClient = FindObjectOfType<ILLMClient>();
            if (foundClient != null)
            {
                llmClient = foundClient;
                Debug.Log("[ChatManager] LLMClient initialized from FindObjectOfType.");
                return;
            }

            // Try to find MonoBehaviour implementing ILLMClient
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var mb in allMonoBehaviours)
            {
                if (mb is ILLMClient client)
                {
                    llmClient = client;
                    Debug.Log("[ChatManager] LLMClient initialized from MonoBehaviour search.");
                    return;
                }
            }

            Debug.LogWarning("[ChatManager] No LLMClient found. Please ensure one is available in the scene.");
        }

        /// <summary>
        /// Handles streaming response chunks from the LLM.
        /// </summary>
        /// <param name="chunk">The response chunk.</param>
        private void HandleLLMResponse(string chunk)
        {
            if (string.IsNullOrEmpty(chunk))
                return;

            currentResponseBuilder.Append(chunk);
            OnStreamingChunk?.Invoke(chunk);
        }

        /// <summary>
        /// Builds the message array to send to the LLM.
        /// Includes system prompt and conversation history.
        /// </summary>
        /// <returns>List of messages for the LLM request.</returns>
        private List<ChatMessage> BuildMessages()
        {
            var messages = new List<ChatMessage>();

            // Add system prompt
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new ChatMessage("system", systemPrompt));
            }

            // Add conversation history
            foreach (var conversation in conversationHistory)
            {
                messages.Add(new ChatMessage(conversation.role, conversation.content));
            }

            return messages;
        }

        /// <summary>
        /// Adds a conversation to history, respecting max history length.
        /// </summary>
        /// <param name="conversation">The conversation to add.</param>
        private void AddToHistory(ChatConversation conversation)
        {
            conversationHistory.Add(conversation);

            // Trim history if needed
            while (conversationHistory.Count > maxHistoryLength)
            {
                conversationHistory.RemoveAt(0);
            }
        }

        #endregion
    }

    /// <summary>
    /// Interface for LLM client implementations.
    /// Provides abstraction for different LLM providers.
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Sends messages to the LLM and handles streaming response.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        /// <param name="onChunk">Callback for each response chunk.</param>
        Task SendStreamingAsync(List<ChatMessage> messages, Action<string> onChunk);

        /// <summary>
        /// Sends messages to the LLM and returns the complete response.
        /// </summary>
        /// <param name="messages">The messages to send.</param>
        /// <returns>The complete response content.</returns>
        Task<string> SendAsync(List<ChatMessage> messages);
    }
}
