using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MMate.UI
{
    /// <summary>
    /// 聊天气泡项组件。自动管理背景、文本子对象，支持自适应尺寸和左右对齐。
    /// 挂在 VirtualScrollList 的 itemPrefab 上使用。
    /// </summary>
    public class ChatMessageItem : MonoBehaviour
    {
        [Header("References (Auto-initialized if null)")]
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text textComponent;

        [Header("Layout")]
        [Tooltip("气泡最大宽度。文本超过此宽度会自动换行。")]
        [SerializeField] private float maxBubbleWidth = 400f;
        [Tooltip("背景比文本大多少。X=左右总和，Y=上下总和。")]
        [SerializeField] private Vector2 bubblePadding = new Vector2(24f, 16f);
        [Tooltip("气泡距离屏幕左右边缘的边距。")]
        [SerializeField] private float sideMargin = 12f;
        [Tooltip("最小高度，用于预计算回退。")]
        [SerializeField] private float minHeight = 40f;

        [Header("User Style")]
        [SerializeField] private Sprite userBubbleSprite;
        [SerializeField] private Color userBubbleColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color userTextColor = Color.white;

        [Header("AI Style")]
        [SerializeField] private Sprite aiBubbleSprite;
        [SerializeField] private Color aiBubbleColor = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color aiTextColor = Color.black;

        private RectTransform _itemRect;
        private bool _initialized;

        public TMP_Text TextComponent => textComponent;
        public float MaxBubbleWidth => maxBubbleWidth;
        public Vector2 BubblePadding => bubblePadding;

        private void Awake()
        {
            Initialize();
        }

        private void Reset()
        {
            Initialize();
        }

        [ContextMenu("Initialize")]
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _itemRect = GetComponent<RectTransform>();

            EnsureBackground();
            EnsureText();
            ConfigureRectTransforms();
        }

        private void EnsureBackground()
        {
            if (backgroundRect != null && backgroundImage != null) return;

            // 尝试按名称查找
            if (backgroundRect == null)
            {
                Transform bgTrans = transform.Find("Background");
                if (bgTrans != null)
                {
                    backgroundRect = bgTrans.GetComponent<RectTransform>();
                    backgroundImage = bgTrans.GetComponent<Image>();
                }
            }

            // 尝试从子对象查找
            if (backgroundRect == null)
            {
                backgroundImage = GetComponentInChildren<Image>();
                if (backgroundImage != null)
                    backgroundRect = backgroundImage.rectTransform;
            }

            // 创建
            if (backgroundRect == null)
            {
                GameObject bgObj = new GameObject("Background", typeof(RectTransform));
                bgObj.transform.SetParent(transform, false);
                backgroundRect = bgObj.GetComponent<RectTransform>();
            }

            if (backgroundImage == null)
                backgroundImage = backgroundRect.gameObject.AddComponent<Image>();

            backgroundImage.type = Image.Type.Sliced;
        }

        private void EnsureText()
        {
            if (textComponent != null) return;

            // 尝试按路径查找
            Transform textTrans = null;

            if (backgroundRect != null)
                textTrans = backgroundRect.Find("Text");

            if (textTrans == null)
                textTrans = transform.Find("Text");

            if (textTrans != null)
                textComponent = textTrans.GetComponent<TMP_Text>();

            // 尝试从子对象查找
            if (textComponent == null)
                textComponent = GetComponentInChildren<TMP_Text>();

            // 创建
            if (textComponent == null)
            {
                GameObject textObj = new GameObject("Text", typeof(RectTransform));
                if (backgroundRect != null)
                    textObj.transform.SetParent(backgroundRect, false);
                else
                    textObj.transform.SetParent(transform, false);

                textComponent = textObj.AddComponent<TextMeshProUGUI>();
            }

            // 配置 TMP_Text
            textComponent.enableWordWrapping = true;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.autoSizeTextContainer = false;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
        }

        private void ConfigureRectTransforms()
        {
            if (_itemRect != null)
            {
                _itemRect.anchorMin = new Vector2(0, 1);
                _itemRect.anchorMax = new Vector2(1, 1);
                _itemRect.pivot = new Vector2(0.5f, 1);
            }

            if (backgroundRect != null)
            {
                backgroundRect.anchorMin = new Vector2(0, 1);
                backgroundRect.anchorMax = new Vector2(0, 1);
                backgroundRect.pivot = new Vector2(0, 1);
                backgroundRect.localScale = Vector3.one;
                backgroundRect.rotation = Quaternion.identity;
            }

            if (textComponent != null)
            {
                RectTransform textRect = textComponent.rectTransform;
                textRect.anchorMin = new Vector2(0, 1);
                textRect.anchorMax = new Vector2(0, 1);
                textRect.pivot = new Vector2(0, 1);
                textRect.localScale = Vector3.one;
                textRect.rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// 设置消息内容、对齐和样式。
        /// </summary>
        public void Setup(string text, bool isUser)
        {
            Initialize();

            textComponent.text = text;

            // 计算文本在限制宽度下的尺寸
            Vector2 textSize = textComponent.GetPreferredValues(text, maxBubbleWidth, float.PositiveInfinity);
            float textW = Mathf.Min(textSize.x, maxBubbleWidth);
            float textH = textSize.y;

            // 设置文本尺寸和位置（相对于背景左上角）
            RectTransform textRect = textComponent.rectTransform;
            textRect.sizeDelta = new Vector2(textW, textH);
            textRect.anchoredPosition = new Vector2(bubblePadding.x * 0.5f, -bubblePadding.y * 0.5f);

            // 设置背景尺寸
            float bgW = textW + bubblePadding.x;
            float bgH = textH + bubblePadding.y;
            backgroundRect.sizeDelta = new Vector2(bgW, bgH);

            // 应用样式和对齐
            ApplyStyle(isUser);
        }

        private void ApplyStyle(bool isUser)
        {
            if (isUser)
            {
                backgroundRect.anchorMin = new Vector2(1, 1);
                backgroundRect.anchorMax = new Vector2(1, 1);
                backgroundRect.pivot = new Vector2(1, 1);
                backgroundRect.anchoredPosition = new Vector2(-sideMargin, 0);

                if (userBubbleSprite != null)
                    backgroundImage.sprite = userBubbleSprite;
                backgroundImage.color = userBubbleColor;
                textComponent.color = userTextColor;
            }
            else
            {
                backgroundRect.anchorMin = new Vector2(0, 1);
                backgroundRect.anchorMax = new Vector2(0, 1);
                backgroundRect.pivot = new Vector2(0, 1);
                backgroundRect.anchoredPosition = new Vector2(sideMargin, 0);

                if (aiBubbleSprite != null)
                    backgroundImage.sprite = aiBubbleSprite;
                backgroundImage.color = aiBubbleColor;
                textComponent.color = aiTextColor;
            }
        }

        /// <summary>
        /// 预计算指定文本在此配置下的渲染高度。
        /// </summary>
        public float PredictHeight(string text)
        {
            if (textComponent == null) return minHeight;

            Vector2 size = textComponent.GetPreferredValues(text, maxBubbleWidth, float.PositiveInfinity);
            return Mathf.Max(size.y + bubblePadding.y, minHeight);
        }
    }
}
