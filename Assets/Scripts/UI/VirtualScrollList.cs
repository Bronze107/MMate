using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MMate.UI
{
    /// <summary>
    /// 虚拟滚动列表组件。通过对象池复用固定数量的 UI 项，支持大量数据的高效显示。
    /// 适用于固定高度的列表项。如需动态高度，需预估高度或扩展此组件。
    /// </summary>
    public class VirtualScrollList : MonoBehaviour
    {
        [Header("Scroll")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Content")]
        [SerializeField] private RectTransform viewportTransform;
        [SerializeField] private RectTransform contentTransform;
        [SerializeField] private GameObject itemPrefab;

        [Header("Layout")]
        [SerializeField] private float itemHeight = 60f;
        [SerializeField] private float spacing = 10f;
        [SerializeField] private int poolSize = 20;
        [SerializeField] private Padding padding;

        [Header("Options")]
        [SerializeField] private bool updateOnViewportResize = true;

        [Serializable]
        public struct Padding
        {
            public float top;
            public float bottom;
        }

        private int _dataCount;
        private List<RectTransform> _pooledItems = new List<RectTransform>();
        private List<int> _itemToDataIndex = new List<int>();
        private int _firstVisibleIndex = -1;
        private int _lastVisibleIndex = -1;
        private float _itemStride;
        private float _viewportHeight;

        /// <summary>
        /// 参数：数据索引，项实例。仅在项首次进入可视区域或重新绑定时触发。
        /// </summary>
        public event Action<int, GameObject> OnItemBind;

        private void Awake()
        {
            _itemStride = itemHeight + spacing;

            if (viewportTransform == null && scrollRect != null)
                viewportTransform = scrollRect.viewport;

            if (contentTransform == null && scrollRect != null)
                contentTransform = scrollRect.content;
        }

        private void Start()
        {
            if (scrollRect != null)
            {
                if (!scrollRect.vertical)
                {
                    Debug.LogWarning("[VirtualScrollList] ScrollRect is not configured for vertical scrolling.");
                }

                scrollRect.onValueChanged.AddListener(OnScrollChanged);
            }

            if (viewportTransform != null)
            {
                _viewportHeight = viewportTransform.rect.height;
            }

            InitializePool();
        }

        private void Update()
        {
            if (!updateOnViewportResize || viewportTransform == null) return;

            float currentHeight = viewportTransform.rect.height;
            if (!Mathf.Approximately(currentHeight, _viewportHeight))
            {
                _viewportHeight = currentHeight;
                UpdateVisibility();
            }
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            }
        }

        private void OnEnable()
        {
            UpdateVisibility();
        }

        private void InitializePool()
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning("[VirtualScrollList] Item prefab is not assigned.");
                return;
            }

            if (contentTransform == null)
            {
                Debug.LogWarning("[VirtualScrollList] Content transform is not assigned.");
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject item = Instantiate(itemPrefab, contentTransform, false);
                item.SetActive(false);

                RectTransform rt = item.GetComponent<RectTransform>();
                if (rt == null)
                    rt = item.AddComponent<RectTransform>();

                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(0, itemHeight);

                _pooledItems.Add(rt);
                _itemToDataIndex.Add(-1);
            }
        }

        /// <summary>
        /// 设置数据总量并刷新列表。
        /// </summary>
        public void SetDataCount(int count)
        {
            if (_dataCount == count) return;

            _dataCount = count;
            UpdateContentSize();
            UpdateVisibility();
        }

        private void UpdateContentSize()
        {
            if (contentTransform == null) return;

            float totalHeight = padding.top + padding.bottom;
            if (_dataCount > 0)
                totalHeight += _dataCount * itemHeight + (_dataCount - 1) * spacing;

            contentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
        }

        private void OnScrollChanged(Vector2 normalizedPosition)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (viewportTransform == null || contentTransform == null || _dataCount == 0)
            {
                HideAllItems();
                return;
            }

            _viewportHeight = viewportTransform.rect.height;
            float contentHeight = contentTransform.rect.height;
            float maxScroll = Mathf.Max(0, contentHeight - _viewportHeight);
            float scrollOffset = (1f - scrollRect.verticalNormalizedPosition) * maxScroll;

            int startIndex = Mathf.FloorToInt((scrollOffset - padding.top) / _itemStride);
            int endIndex = Mathf.CeilToInt((scrollOffset + _viewportHeight - padding.top) / _itemStride);

            startIndex = Mathf.Max(0, startIndex);
            endIndex = Mathf.Min(_dataCount - 1, endIndex);

            if (startIndex > endIndex)
            {
                HideAllItems();
                return;
            }

            // 回收不在可视范围内的项
            for (int i = 0; i < _pooledItems.Count; i++)
            {
                int dataIndex = _itemToDataIndex[i];
                if (dataIndex >= 0 && (dataIndex < startIndex || dataIndex > endIndex))
                {
                    _pooledItems[i].gameObject.SetActive(false);
                    _itemToDataIndex[i] = -1;
                }
            }

            // 绑定可见项
            for (int dataIndex = startIndex; dataIndex <= endIndex; dataIndex++)
            {
                int poolIndex = GetPoolIndexForData(dataIndex);
                bool isNewBind = false;

                if (poolIndex < 0)
                {
                    poolIndex = GetFreePoolIndex();
                    if (poolIndex < 0)
                    {
                        Debug.LogWarning($"[VirtualScrollList] Pool exhausted. Visible range: {startIndex}-{endIndex}, PoolSize: {poolSize}. Consider increasing poolSize.");
                        continue;
                    }

                    _itemToDataIndex[poolIndex] = dataIndex;
                    isNewBind = true;
                }

                RectTransform item = _pooledItems[poolIndex];

                if (!item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                    isNewBind = true;
                }

                float yPos = -(padding.top + dataIndex * _itemStride);
                item.anchoredPosition = new Vector2(0, yPos);

                if (isNewBind)
                {
                    OnItemBind?.Invoke(dataIndex, item.gameObject);

                    VirtualScrollItem vItem = item.gameObject.GetComponent<VirtualScrollItem>();
                    if (vItem != null)
                    {
                        vItem.DataIndex = dataIndex;
                        vItem.List = this;
                    }
                }
            }

            _firstVisibleIndex = startIndex;
            _lastVisibleIndex = endIndex;
        }

        private int GetPoolIndexForData(int dataIndex)
        {
            for (int i = 0; i < _itemToDataIndex.Count; i++)
            {
                if (_itemToDataIndex[i] == dataIndex)
                    return i;
            }
            return -1;
        }

        private int GetFreePoolIndex()
        {
            for (int i = 0; i < _itemToDataIndex.Count; i++)
            {
                if (_itemToDataIndex[i] < 0)
                    return i;
            }
            return -1;
        }

        private void HideAllItems()
        {
            for (int i = 0; i < _pooledItems.Count; i++)
            {
                _pooledItems[i].gameObject.SetActive(false);
                _itemToDataIndex[i] = -1;
            }
            _firstVisibleIndex = -1;
            _lastVisibleIndex = -1;
        }

        /// <summary>
        /// 强制刷新所有当前可见项的绑定。
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < _itemToDataIndex.Count; i++)
            {
                int dataIndex = _itemToDataIndex[i];
                if (dataIndex >= 0 && _pooledItems[i].gameObject.activeSelf)
                {
                    OnItemBind?.Invoke(dataIndex, _pooledItems[i].gameObject);
                }
            }
        }

        /// <summary>
        /// 刷新指定索引的项（如果当前可见）。
        /// </summary>
        public void RefreshItem(int index)
        {
            if (index < 0 || index >= _dataCount) return;

            int poolIndex = GetPoolIndexForData(index);
            if (poolIndex >= 0 && _pooledItems[poolIndex].gameObject.activeSelf)
            {
                OnItemBind?.Invoke(index, _pooledItems[poolIndex].gameObject);
            }
        }

        /// <summary>
        /// 滚动到指定数据索引的项。
        /// </summary>
        /// <param name="index">数据索引</param>
        /// <param name="alignTop">true 时该项对齐视口顶部，false 时居中对齐</param>
        public void ScrollToIndex(int index, bool alignTop = false)
        {
            if (scrollRect == null || _dataCount == 0) return;
            if (index < 0 || index >= _dataCount) return;

            float contentHeight = contentTransform.rect.height;
            float scrollableHeight = Mathf.Max(0, contentHeight - _viewportHeight);

            if (scrollableHeight <= 0) return;

            float targetOffset = padding.top + index * _itemStride;
            if (!alignTop)
                targetOffset -= (_viewportHeight - itemHeight) * 0.5f;

            targetOffset = Mathf.Clamp(targetOffset, 0, scrollableHeight);
            float normalizedPos = 1f - targetOffset / scrollableHeight;
            scrollRect.verticalNormalizedPosition = normalizedPos;
        }

        public int DataCount => _dataCount;
        public int FirstVisibleIndex => _firstVisibleIndex;
        public int LastVisibleIndex => _lastVisibleIndex;
    }
}
