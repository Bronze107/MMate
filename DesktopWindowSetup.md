# Desktop Window 验证文档

本文档说明如何验证 `DesktopWindowManager` 让角色显示在桌面上的功能。

---

## 1. 关键前提（必须先做）

### 1.1 关闭 DXGI Flip Mode

**这是透明背景生效的必要条件。** Unity 默认开启的 DXGI Flip Mode 会改变窗口合成方式，导致 `SetLayeredWindowAttributes` 的 Color Key 透明在构建版本中失效。

1. `Edit > Project Settings > Player`
2. 展开 `Resolution and Presentation`
3. 找到 **Use DXGI Flip Mode Swapchain for D3D11**
4. **取消勾选** 该选项
5. 保存设置

> **为什么编辑器里能正常工作，构建后不行？**
> 编辑器使用 D3D9 或不同的渲染路径，而 Standalone Build 默认启用 DXGIFlipModel。该模式下 DWM 以不同方式合成窗口，GDI Color Key 透明不再有效。

---

## 2. Unity 场景设置

### 2.1 创建场景
1. 打开 Unity，新建或打开一个场景（如 `Assets/Scenes/DesktopAvatar.unity`）
2. 删除默认的 Main Camera 和 Directional Light（如果不需要光照的话）
3. 新建一个 Camera：`GameObject > Camera`，命名为 `AvatarCamera`

### 2.2 配置 Camera
选中 `AvatarCamera`，在 Inspector 中设置：
- **Projection**: Perspective 或 Orthographic（推荐 Perspective）
- **Clear Flags**: `Solid Color`
- **Background**: 与 `DesktopWindowManager.transparentColor` 完全一致（默认 `Color.black`）
- **Depth**: 0
- **Culling Mask**: 只勾选角色所在 Layer

### 2.3 放置角色模型
1. 将角色模型放入场景，调整位置和缩放
2. **确保角色材质中没有使用与 `transparentColor` 完全相同的颜色**（默认黑色），否则该部分也会被透明

### 2.4 挂载 DesktopWindowManager
1. 创建一个空 GameObject，命名为 `DesktopManager`
2. 将 `DesktopWindowManager.cs` 拖到该物体上
3. 配置 Inspector：
   - `targetCamera`: 拖入 `AvatarCamera`
   - `Borderless`: ✔️
   - `Topmost`: ✔️
   - `Transparent Color`: `Color.black`（或角色不用的其他颜色）
   - `Click Through`: ❌
   - `Show Debug Logs`: ✔️

---

## 3. 构建设置

### 3.1 Player Settings
1. `File > Build Settings > Player Settings`
2. **Resolution and Presentation**：
   - `Fullscreen Mode`: `Windowed`
   - `Resizable Window`: 取消勾选
   - `Use DXGI Flip Mode Swapchain for D3D11`: **取消勾选**（再次确认！）
3. **Other Settings**：
   - `Color Space`: `Linear`

### 3.2 Build
1. `File > Build Settings`
2. 目标平台：`PC, Mac & Linux Standalone`
3. 架构：`x86_64`
4. 勾选场景，点击 `Build`

---

## 4. 验证步骤

### 4.1 基础功能

| # | 检查项 | 预期结果 |
|---|--------|----------|
| 1 | 窗口外观 | 没有标题栏、边框、关闭按钮 |
| 2 | 窗口层级 | 打开其他应用，角色窗口始终在上方 |
| 3 | 背景透明 | 角色背后的桌面/壁纸可见，黑色背景完全透明 |
| 4 | 角色显示 | 角色模型正常渲染，没有缺失部件 |
| 5 | 日志输出 | 出现 `ColorKey applied: ...` |

### 4.2 交互功能

| # | 操作 | 预期结果 |
|---|------|----------|
| 6 | 鼠标悬停角色区域 | 可以正常点击（`Click Through` 默认关闭） |
| 7 | 按下键盘 `T` | 鼠标可穿透角色点击桌面 |
| 8 | 再次按下 `T` | 恢复可点击状态 |

---

## 5. 常见问题排查

### 5.1 构建后背景不透明（显示黑色）
- **原因 1（最常见）**：未关闭 `Use DXGI Flip Mode Swapchain for D3D11`
- **原因 2**：`transparentColor` 与 Camera Background 不匹配
- **解决**：检查 Player.log 中 `ColorKey applied: ...` 的 RGB 值，确认 Camera Background 完全相同

### 5.2 角色部分变透明
- **原因**：角色材质/贴图上有与 `transparentColor` 完全匹配的像素
- **解决**：将 `transparentColor` 改为更冷门的颜色（如 `R=0, G=0, B=1` 深蓝），同时修改 Camera Background

### 5.3 整个窗口都看不见了
- **原因**：Color Key 匹配了绝大多数像素
- **解决**：改用深色如黑色，避免用白色或中性灰

### 5.4 窗口有边框
- **原因**：Windows API 调用被覆盖或句柄获取失败
- **解决**：检查 Player.log 中 `HWND` 是否为非零值

---

## 6. 透明原理

使用传统的 GDI `SetLayeredWindowAttributes` 配合 `LWA_COLORKEY`。窗口中所有与 `transparentColor` 颜色完全匹配的像素会被 Windows 渲染为透明。因此要求：
1. Camera Background 颜色 = `transparentColor`
2. 角色上不能出现完全相同的颜色
3. **必须关闭 DXGI Flip Mode**（否则 DWM 合成方式改变，Color Key 失效）

---

## 7. 后续优化方向

1. **窗口拖动**：添加 `OnMouseDrag` 或 Windows API 实现无边框拖动
2. **位置记忆**：保存窗口关闭前的位置
3. **多分辨率适配**：根据屏幕 DPI 自动调整角色大小
4. **任务栏图标隐藏**
