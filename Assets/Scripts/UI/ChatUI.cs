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
            AddMessage(conversation.content, isUser);
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
