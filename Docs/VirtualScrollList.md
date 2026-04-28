# VirtualScrollList 使用文档

`VirtualScrollList` 是 MMate 项目的虚拟滚动列表组件，通过对象池复用固定数量的 UI 项，支持大量数据的高效渲染。支持**固定高度**与**动态高度**两种模式，并内置 **TMP 文本预计算**功能，适用于聊天消息、日志列表等场景。

---

## 目录

- [核心特性](#核心特性)
- [文件结构](#文件结构)
- [Inspector 配置](#inspector-配置)
- [三种使用模式](#三种使用模式)
  - [模式一：固定高度](#模式一固定高度)
  - [模式二：动态高度（外部传入）](#模式二动态高度外部传入)
  - [模式三：TMP 预计算（推荐用于聊天）](#模式三tmp-预计算推荐用于聊天)
- [API 参考](#api-参考)
- [性能与注意事项](#性能与注意事项)
- [完整示例](#完整示例)

---

## 核心特性

| 特性 | 说明 |
|------|------|
| 对象池复用 | 只创建 `poolSize` 个 UI 实例，滚动时回收复用，避免频繁 `Instantiate/Destroy` |
| 固定高度模式 | 所有项高度一致，计算简单，性能最佳 |
| 动态高度模式 | 每项高度可不同，通过**前缀和数组 + 二分查找**定位可见项，时间复杂度 O(log n) |
| TMP 预计算 | 利用 `TMP_Text.GetPreferredValues()` 在创建 UI 前预计算文本高度，避免运行时布局抖动 |
| 视口自适应 | 监听 Viewport 尺寸变化，自动重绘列表 |

---

## 文件结构

```
Assets/Scripts/UI/
├── VirtualScrollList.cs         # 核心组件
├── VirtualScrollItem.cs         # 项辅助组件（可选）
├── VirtualScrollListExample.cs  # 使用示例
├── ChatMessageItem.cs           # 聊天气泡项（自动背景/文本/对齐）
└── ChatMessageList.cs           # 聊天列表管理器（自动预计算高度）
```

---

## Inspector 配置

将 `VirtualScrollList` 挂载到 ScrollRect 所在 GameObject（或任意 GameObject），按以下说明配置：

### Scroll

| 字段 | 说明 |
|------|------|
| `Scroll Rect` | 场景中的 ScrollRect 引用。必填。 |

### Content

| 字段 | 说明 |
|------|------|
| `Viewport Transform` | 视口 RectTransform。留空则自动从 ScrollRect 获取。 |
| `Content Transform` | Content RectTransform。留空则自动从 ScrollRect 获取。 |
| `Item Prefab` | 列表项预制体。必须包含 `RectTransform`。 |

### Layout

| 字段 | 说明 |
|------|------|
| `Item Height` | 固定高度模式下每项的高度。动态高度模式下作为**最小高度**和**回退高度**。 |
| `Spacing` | 项与项之间的间隔。 |
| `Pool Size` | 对象池大小。建议设为视口可容纳最大项数的 1.5~2 倍。 |
| `Padding` | 顶部/底部边距。 |

### TMP Pre-calculation (Optional)

| 字段 | 说明 |
|------|------|
| `Text Template` | 用于预计算文本高度的 TMP_Text 模板。建议放置一个**非激活**的 TMP_Text 对象，其字体、字号、行距、自动换行等配置与列表项完全一致。 |
| `Text Width Margin` | 文本预计算时从视口宽度扣除的横向边距总和。用于补偿列表项自身的左右 padding。 |

### Options

| 字段 | 说明 |
|------|------|
| `Update On Viewport Resize` | 是否在 Update 中检测视口尺寸变化并自动刷新。 |

---

## 三种使用模式

### 模式一：固定高度

所有列表项高度相同，计算最简单，滚动时性能最佳。

```csharp
using UnityEngine;
using MMate.UI;

public class FixedHeightExample : MonoBehaviour
{
    [SerializeField] private VirtualScrollList scrollList;

    private void Start()
    {
        // 绑定数据到 UI
        scrollList.OnItemBind += (index, itemObj) =>
        {
            var text = itemObj.GetComponentInChildren<TMPro.TMP_Text>();
            text.text = $"Item {index}";
        };

        // 设置 1000 条数据
        scrollList.SetDataCount(1000);
    }
}
```

### 模式二：动态高度（外部传入）

每项高度由外部预先计算好，通过 `float[]` 传入。

```csharp
// 预计算每项高度（例如从服务器返回的数据中读取）
float[] heights = new float[100];
for (int i = 0; i < 100; i++)
{
    heights[i] = CalculateHeightSomehow(i);
}

scrollList.OnItemBind += (index, itemObj) => { ... };
scrollList.SetDataCount(100, heights);
```

### 模式三：TMP 预计算（推荐用于聊天）

文本内容由 `TMP_Text` 在创建 UI **之前**预计算高度，然后自动进入动态高度模式。

```csharp
using System.Collections.Generic;
using UnityEngine;
using MMate.UI;

public class ChatExample : MonoBehaviour
{
    [SerializeField] private VirtualScrollList scrollList;
    private List<string> messages = new List<string>();

    private void Start()
    {
        scrollList.OnItemBind += (index, itemObj) =>
        {
            var text = itemObj.GetComponentInChildren<TMPro.TMP_Text>();
            text.text = messages[index];
        };

        // 自动 TMP 预计算并启用动态高度
        scrollList.SetTextData(messages);
    }

    public void AddMessage(string msg)
    {
        messages.Add(msg);
        scrollList.SetTextData(messages);
        scrollList.ScrollToIndex(messages.Count - 1);
    }
}
```

**注意**：使用 `SetTextData` 时，确保 Inspector 中 `Text Template` 已配置，且其字体、字号、自动换行等属性与列表项实际显示一致。

---

## API 参考

### 数据设置

```csharp
// 固定高度模式
public void SetDataCount(int count)

// 动态高度模式
public void SetDataCount(int count, float[] heights)

// TMP 预计算模式
public void SetTextData(List<string> texts)
```

### 事件

```csharp
// 项首次进入可视区域时触发
public event Action<int, GameObject> OnItemBind;
// 参数说明：
//   int       - 数据索引
//   GameObject - 复用的项实例（来自对象池）
```

### 刷新与导航

```csharp
// 强制刷新所有当前可见项的绑定
public void Refresh()

// 刷新指定索引的项（如果当前可见）
public void RefreshItem(int index)

// 滚动到指定项
// alignTop: true = 该项对齐视口顶部；false = 该项在视口居中
public void ScrollToIndex(int index, bool alignTop = false)
```

### 只读属性

```csharp
public int DataCount          // 数据总量
public int FirstVisibleIndex  // 当前视口中第一个可见项的索引
public int LastVisibleIndex   // 当前视口中最后一个可见项的索引
```

---

## 性能与注意事项

### 1. 滚动时不会频繁 Canvas Rebuild

平滑滚动时，已可见的项仅改变 `anchoredPosition`，这是 GPU 驱动的位置变换，**不会触发 Canvas Rebuild**。

真正可能触发重建的只有：
- 项**首次进入**可视区域（`OnItemBind` 中修改文本/图片）
- 项**离开**可视区域被回收（`SetActive(false)`）

### 2. Pool Size 建议

设置过小会导致快速滚动时频繁激活/回收，建议：

```
poolSize = (viewportHeight / minItemHeight) * 2 + 5
```

### 3. 避免 itemPrefab 上有 LayoutGroup

如果预制体带有 `VerticalLayoutGroup` / `ContentSizeFitter`，每次激活都会强制所有子对象重新布局。聊天消息项推荐：

- 用 **Anchor + Pivot** 手动定位子元素
- 文本直接使用 `TMP_Text`，不要包在额外的 LayoutGroup 中

### 4. TMP 预计算的精度

`SetTextData` 的计算精度取决于 `Text Template` 的配置是否与运行时完全一致。如果列表项有左右 padding，请通过 `Text Width Margin` 扣除对应宽度。

如果运行时实际宽度与预计算宽度不一致，文本可能出现换行差异，导致高度偏差。

### 5. 动态高度模式的数据更新

每次调用 `SetTextData` 或 `SetDataCount(count, heights)` 都会重新计算所有项的前缀和，时间复杂度 O(n)。对于几百条消息无感知；若达到数千条且需要频繁追加，建议在外部缓存高度数组，只计算新增项，然后调用 `SetDataCount(count, cachedHeights)`。

---

## 完整示例

```csharp
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MMate.UI;

public class ChatMessageList : MonoBehaviour
{
    [SerializeField] private VirtualScrollList scrollList;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    private List<string> _messages = new List<string>();

    private void Start()
    {
        scrollList.OnItemBind += OnItemBind;
        sendButton.onClick.AddListener(OnSendClicked);

        // 初始化
        scrollList.SetTextData(_messages);
    }

    private void OnDestroy()
    {
        scrollList.OnItemBind -= OnItemBind;
        sendButton.onClick.RemoveListener(OnSendClicked);
    }

    private void OnItemBind(int index, GameObject itemObj)
    {
        TMP_Text text = itemObj.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = _messages[index];
        }
    }

    private void OnSendClicked()
    {
        string content = inputField.text.Trim();
        if (string.IsNullOrEmpty(content)) return;

        inputField.text = string.Empty;
        inputField.ActivateInputField();

        _messages.Add(content);
        scrollList.SetTextData(_messages);
        scrollList.ScrollToIndex(_messages.Count - 1);
    }
}
```

---

## 常见问题

| 问题 | 原因 | 解决 |
|------|------|------|
| 文本显示不全/换行错误 | `Text Template` 宽度与实际显示宽度不一致 | 调整 `Text Width Margin`，确保 `Text Template` 的字体、字号、Rich Text 设置与列表项一致 |
| 滚动时项闪烁或位置跳跃 | `Pool Size` 太小 | 增大 `Pool Size` |
| 项高度始终等于 `Item Height` | 使用了 `SetDataCount` 而非 `SetTextData` | 聊天场景改用 `SetTextData` |
| 添加消息后滚动位置不对 | `ScrollToIndex` 在 `SetTextData` 之前调用 | 确保先 `SetTextData` 更新内容高度，再 `ScrollToIndex` |

---

## 自动化聊天气泡（推荐）

针对聊天场景，提供了两个高层封装组件，可自动处理背景创建、尺寸计算、左右对齐和高度预计算。

### 文件

```
Assets/Scripts/UI/
├── ChatMessageItem.cs   # 聊天气泡项（挂在 itemPrefab 上）
└── ChatMessageList.cs   # 聊天列表管理器（挂在场景中）
```

### 快速开始

**1. 创建 itemPrefab**

创建一个空 GameObject，挂上 `ChatMessageItem`：
- 点击组件右上角 **⋮ → Initialize**，会自动创建 `Background` + `Text` 子对象
- 配置 `Max Bubble Width`、`Bubble Padding` 和对齐边距
- 拖入用户/AI 的气泡图片、颜色
- 把这个 GameObject 拖成 Prefab

**2. 配置 ChatMessageList**

在场景中创建一个空 GameObject，挂上 `ChatMessageList`：
- `Scroll List` → 拖入挂载了 `VirtualScrollList` 的 GameObject
- `Item Prefab` → 拖入上面的 Prefab
- `Input Field` / `Send Button`（可选）→ 拖入对应 UI 组件

**3. 代码中使用**

```csharp
// 发送用户消息
chatMessageList.AddMessage("你好", isUser: true);

// 接收 AI 回复
chatMessageList.AddAIResponse("你好！有什么可以帮你的？");

// 清空
chatMessageList.ClearMessages();
```

`ChatMessageList` 会自动：
1. 预计算每条消息的高度
2. 调用 `VirtualScrollList.SetDataCount(count, heights)` 启用动态高度
3. 在 `OnItemBind` 中调用 `ChatMessageItem.Setup(text, isUser)` 自动设置文本、背景尺寸和对齐

### ChatMessageItem 配置

| 字段 | 说明 |
|------|------|
| `Max Bubble Width` | 气泡最大宽度，超过自动换行 |
| `Bubble Padding` | 背景比文本大多少，X=左右总和，Y=上下总和 |
| `Side Margin` | 气泡距离屏幕左右边缘的边距 |
| `User Bubble Sprite/Color` | 用户消息的样式 |
| `AI Bubble Sprite/Color` | AI 消息的样式 |

### 手动初始化（编辑器中）

如果 Prefab 上的子对象结构不对，选中 Prefab 后点击 Inspector 中 `ChatMessageItem` 组件右上角的 **⋮ → Initialize**，会自动：
- 查找现有的 `Background` / `Text` 子对象，找不到则自动创建
- 配置所有 RectTransform 的 Anchor、Pivot
- 配置 TMP_Text 的 Word Wrapping、Overflow
