using UnityEngine;

namespace MMate.UI
{
    /// <summary>
    /// 虚拟滚动列表项辅助组件。附加在 itemPrefab 上可获取当前绑定的数据索引。
    /// </summary>
    public class VirtualScrollItem : MonoBehaviour
    {
        public int DataIndex { get; set; } = -1;
        public VirtualScrollList List { get; set; }

        /// <summary>
        /// 通知列表刷新当前项的绑定。
        /// </summary>
        public void Refresh()
        {
            if (List != null && DataIndex >= 0)
            {
                List.RefreshItem(DataIndex);
            }
        }
    }
}
