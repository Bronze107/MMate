# Phase 1 验证文档

## 概述

Phase 1 目标：最小可用产品（MVP）
- [x] 项目框架搭建
- [x] LLM 客户端实现（OpenAI 兼容接口）
- [x] 基础对话功能
- [x] 简单 3D 角色显示
- [x] 状态机：在线/离线/发呆

---

## 验证环境

| 项目 | 要求 |
|------|------|
| Unity 版本 | 2022.3.62f2 LTS |
| 目标平台 | Windows Desktop |
| .NET 版本 | Standard 2.1 |
| 渲染管线 | URP |

---

## 1. 项目框架验证

### 1.1 目录结构

确认 `Assets/Scripts/` 目录结构：

```
Assets/Scripts/
├── Core/
│   ├── LLM/
│   │   ├── LLMClient.cs
│   │   ├── OpenAICompatibleClient.cs
│   │   └── LLMConfig.cs
│   └── State/
│       ├── AvatarStateMachine.cs
│       ├── AvatarState.cs
│       └── States/
│           ├── OnlineState.cs
│           ├── OfflineState.cs
│           └── IdleState.cs
├── Avatar/
│   └── AvatarController.cs
├── Interaction/
│   └── ChatManager.cs
└── UI/
    └── ChatUI.cs
```

### 1.2 命名空间检查

每个脚本顶部应包含正确的命名空间：

| 脚本 | 命名空间 |
|------|----------|
| LLMClient.cs | `MMate.Core.LLM` |
| OpenAICompatibleClient.cs | `MMate.Core.LLM` |
| LLMConfig.cs | `MMate.Core.LLM` |
| AvatarStateMachine.cs | `MMate.Core.State` |
| AvatarState.cs | `MMate.Core.State` |
| OnlineState.cs | `MMate.Core.State` |
| OfflineState.cs | `MMate.Core.State` |
| IdleState.cs | `MMate.Core.State` |
| AvatarController.cs | `MMate.Avatar` |
| ChatManager.cs | `MMate.Interaction` |
| ChatUI.cs | `MMate.UI` |

### 1.3 Assembly Definition

确认 `Assets/Scripts/MMate.Runtime.asmdef` 存在且：
- 引用了 `Unity.TextMeshPro`
- 无编译错误

---

## 2. LLM 客户端验证

### 2.1 配置 LLMConfig

在 Inspector 中配置 `LLMConfig`：

| 字段 | 测试值示例 |
|------|-----------|
| provider | `siliconflow` |
| baseUrl | `https://api.siliconflow.cn/v1` |
| model | `Qwen/Qwen3.5-4B` 或 `deepseek-ai/DeepSeek-V3` |
| apiKey | 你的 API Key |
| maxTokens | `2048` |
| temperature | `0.7` |

### 2.2 非流式请求测试

```csharp
// 在任意 MonoBehaviour 中测试
public async void TestLLM()
{
    var client = FindObjectOfType<OpenAICompatibleClient>();
    var messages = new List<ChatMessage>
    {
        new ChatMessage("user", "Hello")
    };
    string result = await client.SendAsync(messages);
    Debug.Log($"Response: {result}");
}
```

**预期结果：** Console 输出 `[LLM] Response code: 200` 和回复内容。

### 2.3 流式请求测试

```csharp
public async void TestStreaming()
{
    var client = FindObjectOfType<OpenAICompatibleClient>();
    var messages = new List<ChatMessage>
    {
        new ChatMessage("user", "Tell me a story")
    };
    await client.SendStreamingAsync(messages, chunk =>
    {
        Debug.Log($"Chunk: {chunk}");
    });
}
```

**预期结果：** Console 逐 chunk 输出，最后输出 `[LLM] Stream complete.`。

### 2.4 连接检查

```csharp
var client = FindObjectOfType<OpenAICompatibleClient>();
bool ok = client.CheckConnection();
Debug.Log($"Connection check: {ok}");
```

**预期结果：** `true`（config 和 apiKey 不为空时）。

---

## 3. 基础对话功能验证

### 3.1 场景搭建

创建测试场景，按以下层次挂载组件：

```
Managers (空物体)
  ├── ChatManager (Script: ChatManager)
  │   ├── maxHistoryLength: 20
  │   ├── systemPrompt: "You are a friendly desktop companion."
  │   └── llmClient: 拖拽 OpenAICompatibleClient
  ├── OpenAICompatibleClient (Script: OpenAICompatibleClient)
  │   └── config: 拖拽 LLMConfig ScriptableObject
  └── AvatarStateMachine (Script: AvatarStateMachine)

UI Canvas
  ├── ChatPanel
  │   ├── ScrollView (ScrollRect)
  │   ├── MessageContainer (Transform)
  │   ├── UserMessagePrefab (GameObject with TMP_Text)
  │   ├── AIMessagePrefab (GameObject with TMP_Text)
  │   ├── InputField (TMP_InputField)
  │   └── SendButton (Button)
  └── ChatUI (Script: ChatUI)
      ├── messageInput: 拖拽 InputField
      ├── sendButton: 拖拽 SendButton
      ├── messageContainer: 拖拽 MessageContainer
      ├── userMessagePrefab: 拖拽 UserMessagePrefab
      ├── aiMessagePrefab: 拖拽 AIMessagePrefab
      └── scrollRect: 拖拽 ScrollView
```

### 3.2 发送消息测试

1. 进入 Play Mode
2. 在 InputField 输入文字，点击 Send 或按 Enter
3. **预期结果：**
   - 用户消息立即出现在聊天列表
   - 等待片刻后 AI 消息逐字/逐块出现
   - 消息自动滚动到底部

### 3.3 事件监听测试

```csharp
public class TestEvents : MonoBehaviour
{
    void Start()
    {
        ChatManager.Instance.OnConversationUpdate += msg =>
            Debug.Log($"[{msg.role}] {msg.content}");

        ChatManager.Instance.OnStreamingChunk += chunk =>
            Debug.Log($"[Chunk] {chunk}");

        ChatManager.Instance.OnResponseComplete += () =>
            Debug.Log("[Complete]");

        ChatManager.Instance.OnError += error =>
            Debug.LogError($"[Error] {error}");
    }
}
```

**预期结果：** 每个事件按顺序触发。

### 3.4 历史记录测试

连续发送多条消息，检查 `ChatManager.ConversationHistory` 列表。

**预期结果：**
- 列表按 `user` -> `assistant` -> `user` -> `assistant` 交替
- 超过 `maxHistoryLength` 时，最早的消息被移除

### 3.5 清空历史测试

```csharp
ChatManager.Instance.ClearHistory();
```

**预期结果：** UI 清空，Console 输出 `[ChatManager] Conversation history cleared.`

---

## 4. 3D 角色验证

### 4.1 场景搭建

1. 导入或创建一个带 Animator 的 3D 模型
2. 创建 Animator Controller，添加以下参数：
   - `isIdle` (Bool)
   - `isTalking` (Bool)
   - `idleActionIndex` (Int)
3. 挂载 `AvatarController` 脚本
4. 配置 Animator 引用

### 4.2 动画切换测试

```csharp
var avatar = FindObjectOfType<AvatarController>();
avatar.PlayIdle();   // 预期：isIdle=true, isTalking=false
avatar.PlayTalk();   // 预期：isIdle=false, isTalking=true
```

### 4.3 空闲动作测试

```csharp
avatar.PlayIdleAction();        // 随机索引 0-9
avatar.PlayIdleAction(3);       // 指定索引 3
```

**预期结果：** Animator 的 `idleActionIndex` 参数被设置。

---

## 5. 状态机验证

### 5.1 状态切换测试

```csharp
var sm = AvatarStateMachine.Instance;

// 切换到在线状态
sm.ChangeState(new OnlineState());
Debug.Log($"Current: {sm.CurrentStateType}");

// 切换到发呆状态
sm.ChangeState(new IdleState());
Debug.Log($"Current: {sm.CurrentStateType}");

// 切换到离线状态
sm.ChangeState(new OfflineState());
Debug.Log($"Current: {sm.CurrentStateType}");
```

**预期结果：**
- Console 输出状态切换日志
- `CurrentStateType` 正确反映当前状态

### 5.2 状态事件测试

```csharp
AvatarStateMachine.Instance.OnStateChanged += (prev, next) =>
{
    Debug.Log($"State changed: {prev?.StateType} -> {next.StateType}");
};
```

**预期结果：** 每次 `ChangeState` 调用都触发事件。

### 5.3 状态 Update 测试

在 Play Mode 中切换到 `IdleState`，观察一段时间。

**预期结果：** Console 定期输出 `[IdleState] Triggering idle action.`

---

## 6. 集成测试

### 6.1 完整对话流程

1. 进入 Play Mode
2. 确保状态机处于 `OnlineState`
3. 发送一条消息
4. **验证点：**
   - 消息正确显示在 UI
   - LLM 返回流式响应
   - 对话历史正确维护

### 6.2 错误处理测试

| 场景 | 操作 | 预期结果 |
|------|------|----------|
| 空消息 | 发送空字符串 | Console 警告，不发送 |
| 重复发送 | 快速点击 Send 两次 | 第二次被忽略，Console 警告 |
| 无 LLMClient | 移除 LLMClient 引用 | Console 错误，UI 提示 |
| 无效 API Key | 配置错误 key | HTTP 401/403，Console 报错 |

---

## 7. 已知问题与限制

| 问题 | 说明 | 解决方案 |
|------|------|----------|
| UnityWebRequest POST timeout | Windows 上 POST 请求卡住 | 已切换为 HttpClient |
| 流式响应 chunk 大小 | 取决于服务端，可能一次多个字符 | 正常行为，服务端控制 |
| 证书验证 | 开发环境跳过验证 | `BypassCertificateHandler`（生产环境需恢复） |
| 3D 模型资源 | 需要自备模型和动画 | Phase 2 补充角色定制 |

---

## 8. 检查清单

- [ ] Unity 编译无错误
- [ ] LLM 配置正确（baseUrl、apiKey、model）
- [ ] 非流式请求返回 200
- [ ] 流式请求逐 chunk 输出
- [ ] UI 消息正确显示和滚动
- [ ] ChatManager 历史记录维护正确
- [ ] 状态机三状态切换正常
- [ ] AvatarController 动画切换正常
- [ ] 错误场景有适当日志提示

---

## 附录：快速测试脚本

将以下脚本挂载到场景中的任意物体，进入 Play Mode 后按数字键测试：

```csharp
using UnityEngine;
using MMate.Core.LLM;
using MMate.Core.State;
using MMate.Interaction;
using System.Collections.Generic;

public class Phase1QuickTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            TestLLMNonStreaming();
        if (Input.GetKeyDown(KeyCode.Alpha2))
            TestLLMStreaming();
        if (Input.GetKeyDown(KeyCode.Alpha3))
            TestStateMachine();
        if (Input.GetKeyDown(KeyCode.Alpha4))
            TestChatManager();
        if (Input.GetKeyDown(KeyCode.Alpha5))
            TestAvatarAnimation();
    }

    async void TestLLMNonStreaming()
    {
        var client = FindObjectOfType<OpenAICompatibleClient>();
        var result = await client.SendAsync(new List<ChatMessage>
        {
            new ChatMessage("user", "Say hello")
        });
        Debug.Log($"[Test1] Result: {result}");
    }

    async void TestLLMStreaming()
    {
        var client = FindObjectOfType<OpenAICompatibleClient>();
        await client.SendStreamingAsync(new List<ChatMessage>
        {
            new ChatMessage("user", "Count 1 to 5")
        }, chunk => Debug.Log($"[Test2] Chunk: {chunk}"));
    }

    void TestStateMachine()
    {
        var sm = AvatarStateMachine.Instance;
        sm.ChangeState(new OnlineState());
        sm.ChangeState(new IdleState());
        sm.ChangeState(new OfflineState());
    }

    void TestChatManager()
    {
        ChatManager.Instance.SendMessage("Test message from script");
    }

    void TestAvatarAnimation()
    {
        var avatar = FindObjectOfType<AvatarController>();
        avatar?.PlayIdle();
        avatar?.PlayTalk();
    }
}
```
