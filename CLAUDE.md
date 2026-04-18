# MMate 项目规范

LLM 驱动的桌面 3D 伙伴软件

## 技术栈

- **引擎**: Unity 2022.3.62f2 LTS
- **渲染管线**: URP (Universal Render Pipeline)
- **目标平台**: Windows Desktop
- **.NET 版本**: .NET Standard 2.1

## 目录结构

```
Assets/Scripts/
├── Core/           # 核心模块（LLM、状态机等）
│   ├── LLM/        # LLM 客户端和配置
│   └── State/      # 状态机系统
├── Avatar/         # 角色控制
├── Interaction/    # 用户交互（聊天、文件投喂等）
├── UI/             # 界面组件
└── Utils/          # 工具类
```

## 代码规范

### 命名空间

所有代码使用 `MMate.*` 命名空间，按模块划分：

```csharp
namespace MMate.Core.LLM      // LLM 相关
namespace MMate.Core.State    // 状态机
namespace MMate.Avatar        // 角色控制
namespace MMate.Interaction   // 交互系统
namespace MMate.UI            // 界面
namespace MMate.Utils         // 工具
```

### 命名约定

| 类型 | 命名风格 | 示例 |
|-----|---------|------|
| 类 | PascalCase | `ChatManager`, `AvatarController` |
| 方法 | PascalCase | `SendMessage()`, `PlayIdle()` |
| 属性 | PascalCase | `IsProcessing`, `CurrentState` |
| 私有字段 | _camelCase | `_instance`, `_currentState` |
| 序列化字段 | camelCase | `[SerializeField] private int maxHistoryLength;` |
| 常量 | SCREAMING_SNAKE | `MAX_RETRY_COUNT`, `DEFAULT_TIMEOUT` |
| 事件 | PascalCase | `OnConversationUpdate`, `OnStateChanged` |

### 接口和抽象类

- 接口以 `I` 开头：`ILLMClient`, `IAvatarState`
- 抽象类以 `Base` 开头或作为基类名：`LLMClient`, `BaseAvatarState`
- **共享接口和数据结构统一放在 Core 模块**，避免重复定义

### Unity 组件

```csharp
public class ExampleComponent : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int configValue;

    [Header("References")]
    [SerializeField] private Transform targetTransform;

    public event Action OnSomethingHappened;

    private void Awake() { /* 初始化 */ }
    private void Start() { /* 启动逻辑 */ }
    private void OnDestroy() { /* 清理事件订阅 */ }
}
```

## Unity 特定约定

### .meta 文件

- **永远不要手动创建 .meta 文件**
- 让 Unity 自动生成，确保 GUID 唯一

### Assembly Definition (asmdef)

项目使用 `Assets/Scripts/MMate.Runtime.asmdef`：
- 所有脚本在同一个程序集中编译
- 已引用 TextMeshPro (GUID:6055be8ebefd69e48b49212b09b47b2f)
- 新增外部依赖时需更新 asmdef

### API 兼容性

Unity 2022.3 LTS 限制：
- ❌ 不要使用 `FindFirstObjectByType<T>()` → ✅ 使用 `FindObjectOfType<T>()`
- ❌ 不要使用 `FindObjectsByType<T>()` → ✅ 使用 `FindObjectsOfType<T>()`
- ✅ 可使用 `async/await` (.NET Standard 2.1 支持)
- ✅ 可使用 `Task<T>` 和 `TaskCompletionSource`

### 序列化

- 使用 `UnityEngine.JsonUtility` 处理简单序列化
- 复杂 JSON 可考虑 `Newtonsoft.Json`（需安装包）
- 配置文件放在 `Assets/StreamingAssets/` 或 `Resources/`

## 架构原则

### 模块依赖

```
UI → Interaction → Core.LLM / Core.State / Avatar
                    ↓
              UnityEngine / TMPro
```

- UI 层只依赖 Interaction 层，不直接依赖 LLM
- Interaction 层协调 Core 模块
- Core 模块之间可相互依赖，但不应依赖 UI/Interaction

### 单例模式

```csharp
public class Manager : MonoBehaviour
{
    private static Manager _instance;
    public static Manager Instance => _instance ??= FindObjectOfType<Manager>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
}
```

### 事件驱动

- 模块间通信优先使用 C# 事件而非直接引用
- 订阅事件时必须在 `OnDestroy` 中取消订阅

```csharp
private void Start()
{
    ChatManager.Instance.OnConversationUpdate += HandleUpdate;
}

private void OnDestroy()
{
    if (ChatManager.Instance != null)
        ChatManager.Instance.OnConversationUpdate -= HandleUpdate;
}
```

## 常见陷阱

| 问题 | 解决方案 |
|-----|---------|
| 类型找不到 | 检查命名空间引用 `using MMate.xxx;` |
| .meta GUID 冲突 | 删除 .meta 让 Unity 重新生成 |
| TMPro 找不到 | 检查 asmdef 是否引用 TMP |
| 协程不执行 | 确保 MonoBehaviour 启用且未被销毁 |
| async 死锁 | 使用 `TaskScheduler.FromCurrentSynchronizationContext()` |

## Git 规范

### 提交信息格式

```
type: description

- Detail 1
- Detail 2

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
```

类型：
- `feat`: 新功能
- `fix`: 修复 bug
- `refactor`: 重构
- `docs`: 文档
- `chore`: 杂项（构建、配置等）

### .gitignore

已配置忽略：
- `/Library/` - Unity 缓存
- `*.csproj`, `*.sln` - IDE 项目文件
- `.vs/`, `.idea/` - IDE 配置
- `/Build/`, `/Builds/` - 构建输出
- `.claude/` - Claude 工作区

## 开发流程

1. **开始新功能**：从 dev 创建分支
2. **编写代码**：遵循命名空间和代码规范
3. **Unity 编译**：确保无编译错误，让 Unity 生成 .meta
4. **提交**：包含代码和对应的 .meta 文件
5. **推送**：推送到 remote，创建 PR 合并到 dev

## 参考资源

- [PROJECT_PLAN.md](PROJECT_PLAN.md) - 完整项目规划
- [Unity 2022.3 文档](https://docs.unity3d.com/2022.3/Documentation/Manual/')
- [URP 文档](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/)
