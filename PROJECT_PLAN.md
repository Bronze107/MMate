# MMate 项目规划

LLM驱动的桌面3D伙伴软件

---

## 项目概述

MMate 是一款由 LLM 驱动的桌面3D伙伴应用，用户可以自定义角色的外表、人格，并与角色进行自然交互。角色具备持久化记忆，能够记住用户的偏好和历史对话。

---

## 核心特性

### 1. 角色定制

| 功能 | 描述 | 优先级 |
|------|------|--------|
| 体型定制 | 身高、体重、体型比例 | P1 |
| 脸部定制 | 五官参数化调整 | P1 |
| 发型定制 | 多种发型选择 + 颜色 | P1 |
| 人格定制 | 性格、说话风格、兴趣偏好 | P1 |

### 2. 记忆系统

| 功能 | 描述 | 优先级 |
|------|------|--------|
| 短期记忆 | 当前会话上下文 | P0 |
| 长期记忆 | 持久化重要信息 | P0 |
| 记忆导出 | JSON格式导出配置和记忆 | P1 |
| 云端同步 | 可选的云备份功能 | P2 |

### 3. 交互功能

| 功能 | 描述 | 优先级 |
|------|------|--------|
| 文字对话 | 基础聊天功能 | P0 |
| 文件投喂 | 拖拽文件给角色，触发自定义行为 | P1 |
| 动作定制 | 自定义角色动作/表情 | P1 |
| 语音交互 | 语音输入/输出 | P2 |
| 多模态 | 图片、视频输入/输出 | P2 |

### 4. 渲染系统

| 功能 | 描述 | 优先级 |
|------|------|--------|
| 卡通渲染 | 风格化渲染（默认） | P0 |
| 写实渲染 | PBR物理渲染 | P2 |

### 5. LLM 集成

| 功能 | 描述 | 优先级 |
|------|------|--------|
| 云端 API | 支持 OpenAI/Claude/Doubao 等 | P0 |
| 本地推理 | 支持 Ollama/LM Studio/llama.cpp | P1 |
| 离线模式 | LLM 不可用时退回发呆 | P0 |
| 多模态支持 | 预留接口，适配未来多模态LLM | P2 |

### 6. Agent 功能（未来）

| 功能 | 描述 | 优先级 |
|------|------|--------|
| 网页搜索 | 联网搜索资料 | P2 |
| 系统操作 | 打开应用、添加日程等 | P2 |
| Todo 管理 | 任务列表管理 | P2 |

---

## 技术架构

### 技术栈

- **引擎**: Unity 2022.3.62f2 LTS
- **渲染管线**: URP (Universal Render Pipeline)
- **目标平台**: Windows Desktop

### 系统架构

```
┌─────────────────────────────────────────────────────┐
│                    UI Layer                         │
│  (对话界面 / 设置面板 / 角色编辑器)                   │
├─────────────────────────────────────────────────────┤
│                 Interaction Layer                    │
│  (文字输入 / 文件投喂 / 语音输入)                     │
├─────────────────────────────────────────────────────┤
│                   Core Layer                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐          │
│  │LLMClient │  │  Memory  │  │  State   │          │
│  │  Module  │  │  System  │  │  Machine │          │
│  └──────────┘  └──────────┘  └──────────┘          │
├─────────────────────────────────────────────────────┤
│                  Avatar Layer                        │
│  (模型渲染 / 动画控制 / 表情系统 / IK)               │
├─────────────────────────────────────────────────────┤
│                Data Persistence                      │
│  (SQLite / JSON / 云同步)                           │
└─────────────────────────────────────────────────────┘
```

### 目录结构

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── LLM/
│   │   │   ├── LLMClient.cs          # LLM客户端基类
│   │   │   ├── OpenAICompatible.cs   # OpenAI兼容接口
│   │   │   └── LLMConfig.cs          # 配置数据结构
│   │   ├── Memory/
│   │   │   ├── MemoryManager.cs      # 记忆管理器
│   │   │   ├── ShortTermMemory.cs    # 短期记忆
│   │   │   └── LongTermMemory.cs     # 长期记忆
│   │   └── State/
│   │       ├── AvatarStateMachine.cs # 角色状态机
│   │       └── AvatarState.cs        # 状态定义
│   ├── Avatar/
│   │   ├── AvatarController.cs       # 角色控制器
│   │   ├── FaceController.cs         # 表情控制
│   │   └── AnimationController.cs    # 动画控制
│   ├── Interaction/
│   │   ├── ChatManager.cs            # 聊天管理
│   │   └── FileFeeding.cs            # 文件投喂
│   ├── UI/
│   │   ├── ChatUI.cs                 # 聊天界面
│   │   ├── SettingsUI.cs             # 设置界面
│   │   └── AvatarEditorUI.cs         # 角色编辑器
│   └── Utils/
│       ├── JsonHelper.cs             # JSON工具
│       └── FileHelper.cs             # 文件工具
├── Models/
│   └── Avatar/                       # 角色模型
├── Animations/
│   └── Avatar/                       # 角色动画
├── Materials/
│   ├── Toon/                         # 卡通材质
│   └── PBR/                          # 写实材质（未来）
├── Prefabs/
│   └── Avatar/                       # 角色预制体
├── Resources/
│   └── Config/                       # 默认配置
└── StreamingAssets/
    └── DefaultAvatar/                # 默认角色资源
```

---

## 开发阶段

### Phase 1: 基础框架 (MVP)

**目标**: 最小可用产品

- [ ] 项目框架搭建
- [ ] LLM 客户端实现（OpenAI兼容接口）
- [ ] 基础对话功能
- [ ] 简单3D角色显示
- [ ] 状态机：在线/离线/发呆

### Phase 2: 角色系统

**目标**: 完整的角色定制能力

- [ ] 角色编辑器 UI
- [ ] 体型/脸部/发型定制
- [ ] 人格系统
- [ ] 基础动画系统
- [ ] 表情系统

### Phase 3: 记忆与交互

**目标**: 持久化和高级交互

- [ ] 记忆系统实现
- [ ] 配置/记忆导出
- [ ] 文件投喂功能
- [ ] 自定义动作

### Phase 4: 多模态

**目标**: 语音和多模态支持

- [ ] TTS 集成（或等待多模态LLM）
- [ ] ASR 语音识别
- [ ] 图片输入支持

### Phase 5: 进阶功能

**目标**: Agent能力和扩展

- [ ] Agent 工具调用
- [ ] 云端同步
- [ ] 写实渲染支持

---

## 数据结构设计

### 角色配置 (AvatarConfig.json)

```json
{
  "avatarId": "unique-id",
  "name": "角色名称",
  "appearance": {
    "bodyHeight": 1.0,
    "bodyWeight": 0.5,
    "faceParams": {},
    "hairStyle": "style_01",
    "hairColor": "#000000"
  },
  "personality": {
    "traits": ["friendly", "curious"],
    "speakingStyle": "casual",
    "interests": ["games", "music"]
  },
  "customActions": [
    { "trigger": "wave", "animationClip": "wave_anim" }
  ]
}
```

### 记忆数据 (Memory.json)

```json
{
  "userId": "user-001",
  "longTermMemory": [
    {
      "id": "mem-001",
      "content": "用户喜欢玩原神",
      "importance": 0.8,
      "createdAt": "2026-04-18T10:00:00Z",
      "lastAccessed": "2026-04-18T15:30:00Z"
    }
  ],
  "conversationSummary": "用户之前讨论过..."
}
```

### LLM 配置 (LLMConfig.json)

```json
{
  "provider": "openai",
  "baseUrl": "https://api.openai.com/v1",
  "model": "gpt-4",
  "apiKey": "",
  "maxTokens": 2048,
  "temperature": 0.7,
  "systemPrompt": "你是一个友好的桌面伙伴..."
}
```

---

## 风险与应对

| 风险 | 影响 | 应对策略 |
|------|------|----------|
| LLM API 延迟高 | 用户体验差 | 流式输出 + 加载动画 |
| 角色模型资源不足 | 定制受限 | 参考 VRM 标准或购买 Asset |
| 多模态 LLM 变化快 | 接口不稳定 | 抽象层隔离，预留扩展 |
| 记忆数据量增长 | 性能下降 | 分级存储 + 定期压缩 |
| 文件投喂误操作 | 用户数据丢失 | 回收站机制 + 确认提示 |

---

## 参考资源

- [VRM Specification](https://vrm.dev/en/) - 虚拟人标准格式
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference) - OpenAI 兼容接口
- [Unity URP Documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/) - URP 文档
- [Ollama API](https://github.com/ollama/ollama/blob/main/docs/api.md) - 本地推理接口

---

## 更新日志

| 日期 | 版本 | 更新内容 |
|------|------|----------|
| 2026-04-18 | v0.1 | 初始项目规划 |
