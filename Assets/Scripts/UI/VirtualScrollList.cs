using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MMate.UI
{
    /// <summary>
    /// 虚拟滚动列表组件。通过对象池复用固定数量的 UI 项，支持大量数据的高效显示。
    /// 支持固定高度与动态高度两种模式。动态高度通过前缀和数组 + 二分查找定位可见项。
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

        [Header("TMP Pre-calculation (Optional)")]
        [Tooltip("用于预计算文本高度的 TMP_Text 模板。建议放置一个非激活的 TMP_Text 对象，其字体、字号等配置与列表项一致。")]
        [SerializeField] private TMP_Text textTemplate;
        [Tooltip("文本预计算时扣除的横向边距总和。")]
        [SerializeField] private float textWidthMargin = 20f;

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

        // 动态高度
        private float[] _itemHeights;
        private float[] _prefixSum;

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
        /// 固定高度模式：设置数据总量并刷新列表。
        /// </summary>
        public void SetDataCount(int count)
        {
            if (_dataCount == count && _itemHeights == null) return;

            _dataCount = count;
            _itemHeights = null;
            _prefixSum = null;
            UpdateContentSize();
            UpdateVisibility();
        }

        /// <summary>
        /// 动态高度模式：传入每项的实际高度数组。
        /// </summary>
        public void SetDataCount(int count, float[] heights)
        {
            if (heights == null || heights.Length != count)
            {
                Debug.LogWarning("[VirtualScrollList] heights array is null or length mismatch. Falling back to fixed height.");
                SetDataCount(count);
                return;
            }

            if (_dataCount == count && _itemHeights != null && ArraysEqual(_itemHeights, heights))
                return;

            _dataCount = count;
            _itemHeights = new float[count];
            Array.Copy(heights, _itemHeights, count);
            RebuildPrefixSum();
            UpdateContentSize();
            UpdateVisibility();
        }

        /// <summary>
        /// 文本列表专用：使用 TMP_Text 预计算每项高度，并启用动态高度模式。
        /// </summary>
        public void SetTextData(List<string> texts)
        {
            if (texts == null)
            {
                SetDataCount(0);
                return;
            }

            if (textTemplate == null)
            {
                Debug.LogWarning("[VirtualScrollList] textTemplate is not assigned. Falling back to fixed height.");
                SetDataCount(texts.Count);
                return;
            }

            float maxWidth = GetTextMaxWidth();
            float[] heights = new float[texts.Count];

            for (int i = 0; i < texts.Count; i++)
            {
                heights[i] = PredictTextHeight(texts[i], maxWidth);
                heights[i] = Mathf.Max(heights[i], itemHeight);
            }

            SetDataCount(texts.Count, heights);
        }

        private float GetTextMaxWidth()
        {
            if (viewportTransform != null)
                return Mathf.Max(10f, viewportTransform.rect.width - textWidthMargin);

            return 400f;
        }

        private float PredictTextHeight(string text, float maxWidth)
        {
            try
            {
                // TMP_Text.GetPreferredValues(string, float, float) 在 TMP 3.0+ 可用
                Vector2 preferred = textTemplate.GetPreferredValues(text, maxWidth, float.PositiveInfinity);
                return preferred.y;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VirtualScrollList] TMP pre-calculation failed: {ex.Message}. Using fallback.");
                return itemHeight;
            }
        }

        private void RebuildPrefixSum()
        {
            if (_dataCount <= 0)
            {
                _prefixSum = null;
                return;
            }

            _prefixSum = new float[_dataCount + 1];
            _prefixSum[0] = padding.top;

            for (int i = 0; i < _dataCount; i++)
            {
                _prefixSum[i + 1] = _prefixSum[i] + _itemHeights[i] + spacing;
            }
        }

        private static bool ArraysEqual(float[] a, float[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!Mathf.Approximately(a[i], b[i]))
                    return false;
            }
            return true;
        }

        private void UpdateContentSize()
        {
            if (contentTransform == null) return;

            float totalHeight;

            if (_prefixSum != null && _dataCount > 0)
            {
                totalHeight = _prefixSum[_dataCount - 1] + _itemHeights[_dataCount - 1] + padding.bottom;
            }
            else if (_dataCount > 0)
            {
                totalHeight = padding.top + padding.bottom;
                totalHeight += _dataCount * itemHeight + (_dataCount - 1) * spacing;
            }
            else
            {
                totalHeight = padding.top + padding.bottom;
            }

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

            int startIndex;
            int endIndex;

            if (_prefixSum != null)
            {
                // 动态高度：前缀和 + 二分查找
                startIndex = UpperBound(_prefixSum, scrollOffset, _dataCount) - 1;
                startIndex = Mathf.Max(0, startIndex);

                while (startIndex < _dataCount && _prefixSum[startIndex] + _itemHeights[startIndex] <= scrollOffset)
                    startIndex++;

                endIndex = UpperBound(_prefixSum, scrollOffset + _viewportHeight, _dataCount) - 1;
                endIndex = Mathf.Min(_dataCount - 1, endIndex);

                while (endIndex > startIndex && _prefixSum[endIndex] >= scrollOffset + _viewportHeight)
                    endIndex--;
            }
            else
            {
                // 固定高度
                startIndex = Mathf.FloorToInt((scrollOffset - padding.top) / _itemStride);
                endIndex = Mathf.CeilToInt((scrollOffset + _viewportHeight - padding.top) / _itemStride);

                startIndex = Mathf.Max(0, startIndex);
                endIndex = Mathf.Min(_dataCount - 1, endIndex);
            }

            if (startIndex > endIndex || startIndex >= _dataCount)
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

                // 设置位置与高度
                float yPos;
                float targetHeight;

                if (_prefixSum != null)
                {
                    yPos = -_prefixSum[dataIndex];
                    targetHeight = _itemHeights[dataIndex];
                }
                else
                {
                    yPos = -(padding.top + dataIndex * _itemStride);
                    targetHeight = itemHeight;
                }

                item.anchoredPosition = new Vector2(0, yPos);

                if (!Mathf.Approximately(item.sizeDelta.y, targetHeight))
                {
                    item.sizeDelta = new Vector2(item.sizeDelta.x, targetHeight);
                }

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

        /// <summary>
        /// 二分查找：返回第一个满足 arr[i] > target 的索引 i。搜索范围为 [0, length)。
        /// </summary>
        private static int UpperBound(float[] arr, float target, int length)
        {
            int lo = 0;
            int hi = length;

            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (arr[mid] <= target)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            return lo;
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

            float targetOffset;
            float targetHeight;

            if (_prefixSum != null)
            {
                targetOffset = _prefixSum[index];
                targetHeight = _itemHeights[index];
            }
            else
            {
                targetOffset = padding.top + index * _itemStride;
                targetHeight = itemHeight;
            }

            if (!alignTop)
                targetOffset -= (_viewportHeight - targetHeight) * 0.5f;

            targetOffset = Mathf.Clamp(targetOffset, 0, scrollableHeight);
            float normalizedPos = 1f - targetOffset / scrollableHeight;
            scrollRect.verticalNormalizedPosition = normalizedPos;
        }

        public int DataCount => _dataCount;
        public int FirstVisibleIndex => _firstVisibleIndex;
        public int LastVisibleIndex => _lastVisibleIndex;
    }
}
