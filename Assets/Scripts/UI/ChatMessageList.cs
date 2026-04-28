using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MMate.UI
{
    /// <summary>
    /// 聊天消息列表管理器。整合 VirtualScrollList 与 ChatMessageItem，自动预计算高度并管理消息数据。
    /// </summary>
    public class ChatMessageList : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VirtualScrollList scrollList;
        [SerializeField] private ChatMessageItem itemPrefab;

        [Header("Input (Optional)")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private UnityEngine.UI.Button sendButton;

        private List<MessageData> _messages = new List<MessageData>();
        private ChatMessageItem _measureItem;

        private struct MessageData
        {
            public string text;
            public bool isUser;
        }

        private void Start()
        {
            if (scrollList == null)
            {
                Debug.LogWarning("[ChatMessageList] scrollList is not assigned.");
                return;
            }

            InitializeMeasureItem();

            scrollList.OnItemBind += OnItemBind;

            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);

            if (inputField != null)
                inputField.onSubmit.AddListener(OnInputSubmit);

            RefreshList();
        }

        private void OnDestroy()
        {
            if (scrollList != null)
                scrollList.OnItemBind -= OnItemBind;

            if (sendButton != null)
                sendButton.onClick.RemoveListener(OnSendClicked);

            if (inputField != null)
                inputField.onSubmit.RemoveListener(OnInputSubmit);
        }

        /// <summary>
        /// 创建一个隐藏的 item 实例用于高度预计算。
        /// </summary>
        private void InitializeMeasureItem()
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning("[ChatMessageList] itemPrefab is not assigned.");
                return;
            }

            GameObject measureObj = Instantiate(itemPrefab.gameObject);
            measureObj.SetActive(false);
            measureObj.transform.SetParent(transform, false);

            _measureItem = measureObj.GetComponent<ChatMessageItem>();
            if (_measureItem != null)
                _measureItem.Initialize();
        }

        private void OnItemBind(int index, GameObject itemObj)
        {
            ChatMessageItem item = itemObj.GetComponent<ChatMessageItem>();
            if (item == null)
            {
                item = itemObj.AddComponent<ChatMessageItem>();
                item.Initialize();
            }

            MessageData msg = _messages[index];
            item.Setup(msg.text, msg.isUser);
        }

        private void OnSendClicked()
        {
            SendUserMessage();
        }

        private void OnInputSubmit(string text)
        {
            SendUserMessage();
        }

        private void SendUserMessage()
        {
            if (inputField == null) return;

            string content = inputField.text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            inputField.text = string.Empty;
            inputField.ActivateInputField();

            AddMessage(content, isUser: true);
        }

        /// <summary>
        /// 添加一条消息并滚动到底部。
        /// </summary>
        public void AddMessage(string text, bool isUser)
        {
            _messages.Add(new MessageData { text = text, isUser = isUser });
            RefreshList();
            scrollList.ScrollToIndex(_messages.Count - 1);
        }

        /// <summary>
        /// 添加 AI 回复（便捷方法）。
        /// </summary>
        public void AddAIResponse(string text)
        {
            AddMessage(text, isUser: false);
        }

        /// <summary>
        /// 清空所有消息。
        /// </summary>
        public void ClearMessages()
        {
            _messages.Clear();
            RefreshList();
        }

        private void RefreshList()
        {
            if (scrollList == null) return;

            if (_measureItem != null)
            {
                float[] heights = new float[_messages.Count];
                for (int i = 0; i < _messages.Count; i++)
                {
                    heights[i] = _measureItem.PredictHeight(_messages[i].text);
                }
                scrollList.SetDataCount(_messages.Count, heights);
            }
            else
            {
                scrollList.SetDataCount(_messages.Count);
            }
        }
    }
}
