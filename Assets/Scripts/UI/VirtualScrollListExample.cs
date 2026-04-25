using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MMate.UI
{
    /// <summary>
    /// VirtualScrollList 使用示例：聊天消息列表。
    /// </summary>
    public class VirtualScrollListExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VirtualScrollList scrollList;

        [Header("Data")]
        [SerializeField] private List<string> messages = new List<string>();

        private void Start()
        {
            if (scrollList == null)
            {
                Debug.LogWarning("[VirtualScrollListExample] ScrollList is not assigned.");
                return;
            }

            scrollList.OnItemBind += OnItemBind;
            scrollList.SetDataCount(messages.Count);
        }

        private void OnDestroy()
        {
            if (scrollList != null)
                scrollList.OnItemBind -= OnItemBind;
        }

        private void OnItemBind(int index, GameObject itemObj)
        {
            TMP_Text text = itemObj.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = $"[{index}] {messages[index]}";
            }
        }

        /// <summary>
        /// 添加一条消息并滚动到底部。
        /// </summary>
        public void AddMessage(string message)
        {
            messages.Add(message);
            scrollList.SetDataCount(messages.Count);
            scrollList.ScrollToIndex(messages.Count - 1);
        }

        /// <summary>
        /// 清空所有消息。
        /// </summary>
        public void ClearMessages()
        {
            messages.Clear();
            scrollList.SetDataCount(0);
        }
    }
}
