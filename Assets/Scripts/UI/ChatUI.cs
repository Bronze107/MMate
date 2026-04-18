using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MMate.Interaction;

namespace MMate.UI
{
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

        private GameObject _streamingMessageObj;
        private TMP_Text _streamingMessageText;

        private void Start()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
            }

            if (messageInput != null)
            {
                messageInput.onSubmit.AddListener(OnInputSubmit);
            }

            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnConversationUpdate += HandleConversationUpdate;
                ChatManager.Instance.OnStreamingChunk += HandleStreamingChunk;
                ChatManager.Instance.OnResponseComplete += HandleResponseComplete;
            }
        }

        private void OnDestroy()
        {
            if (sendButton != null)
            {
                sendButton.onClick.RemoveListener(OnSendClicked);
            }

            if (messageInput != null)
            {
                messageInput.onSubmit.RemoveListener(OnInputSubmit);
            }

            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.OnConversationUpdate -= HandleConversationUpdate;
                ChatManager.Instance.OnStreamingChunk -= HandleStreamingChunk;
                ChatManager.Instance.OnResponseComplete -= HandleResponseComplete;
            }
        }

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

            TMP_Text textComponent = messageObj.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = content;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);
            ScrollToBottom();
        }

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

            messageInput.text = string.Empty;
            messageInput.ActivateInputField();

            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.SendMessage(content);
            }
            else
            {
                Debug.LogWarning("[ChatUI] ChatManager instance is not available.");
            }
        }

        private void HandleConversationUpdate(ChatConversation conversation)
        {
            bool isUser = conversation.role == "user";

            if (!isUser && _streamingMessageObj != null && _streamingMessageText != null)
            {
                _streamingMessageText.text = conversation.content;
                LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);
                ScrollToBottom();
                return;
            }

            AddMessage(conversation.content, isUser);
        }

        private void HandleStreamingChunk(string chunk)
        {
            if (_streamingMessageObj == null)
            {
                if (messageContainer == null || aiMessagePrefab == null)
                    return;

                _streamingMessageObj = Instantiate(aiMessagePrefab, messageContainer);
                _streamingMessageObj.SetActive(true);
                _streamingMessageText = _streamingMessageObj.GetComponentInChildren<TMP_Text>();
            }

            if (_streamingMessageText != null)
            {
                _streamingMessageText.text += chunk;
                LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);
                ScrollToBottom();
            }
        }

        private void HandleResponseComplete()
        {
            _streamingMessageObj = null;
            _streamingMessageText = null;
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
