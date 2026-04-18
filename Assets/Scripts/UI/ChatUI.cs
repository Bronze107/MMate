using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MMate.UI
{
    /// <summary>
    /// Chat UI component for displaying and managing chat messages.
    /// </summary>
    public class ChatUI : MonoBehaviour
    {
        [Header("Input Components")]
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private Button sendButton;

        [Header("Message Display")]
        [SerializeField] private Transform messageContainer;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject aiMessagePrefab;

        [Header("Scrolling")]
        [SerializeField] private ScrollRect scrollRect;

        private void Start()
        {
            // Bind button events
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
            }

            // Bind input field submit event
            if (messageInput != null)
            {
                messageInput.onSubmit.AddListener(OnInputSubmit);
            }

            // Subscribe to ChatManager events
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnConversationUpdate += HandleConversationUpdate;
            }
        }

        private void OnDestroy()
        {
            // Unbind button events
            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(OnSendClicked);
            }

            // Unbind input field events
            if (messageInput != null)
            {
                messageInput.onSubmit.RemoveListener(OnInputSubmit);
            }

            // Unsubscribe from ChatManager events
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnConversationUpdate -= HandleConversationUpdate;
            }
        }

        /// <summary>
        /// Adds a message to the chat list.
        /// </summary>
        /// <param name="content">The message content.</param>
        /// <param name="isUser">True if the message is from the user, false if from AI.</param>
        public void AddMessage(string content, bool isUser)
        {
            if (messageContainer == null)
            {
                Debug.LogWarning("[ChatUI] Message container is not assigned.");
                return;
            }

            GameObject prefab = isUser ? userMessagePrefab : aiMessagePrefab;

            if (prefab == null)
            {
                Debug.LogWarning($"[ChatUI] {(isUser ? "User" : "AI")} message prefab is not assigned.");
                return;
            }

            GameObject messageObj = Instantiate(prefab, messageContainer);
            messageObj.SetActive(true);

            // Try to set the message text
            TMP_Text textComponent = messageObj.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = content;
            }

            // Force layout rebuild for proper sizing
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);

            // Scroll to bottom
            ScrollToBottom();
        }

        /// <summary>
        /// Clears all messages from the chat list.
        /// </summary>
        public void ClearMessages()
        {
            if (messageContainer == null)
            {
                return;
            }

            for (int i = messageContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = messageContainer.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void OnSendClicked()
        {
            SendMessageFromInput();
        }

        private void OnInputSubmit(string text)
        {
            SendMessageFromInput();
        }

        private void SendMessageFromInput()
        {
            if (messageInput == null)
            {
                return;
            }

            string content = messageInput.text.Trim();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            // Clear input field
            messageInput.text = string.Empty;
            messageInput.ActivateInputField();

            // Send message through ChatManager
            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.SendMessage(content);
            }
            else
            {
                Debug.LogWarning("[ChatUI] ChatManager instance is not available.");
            }
        }

        private void HandleConversationUpdate(string content, bool isUser)
        {
            AddMessage(content, isUser);
        }

        private void ScrollToBottom()
        {
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
