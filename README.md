# dsh-FocusCapture

<p align="center">
  <img src="icon.png" alt="dsh-FocusCapture 图标" width="128">
</p>

<p align="center">
  <strong>为 DeepSeek Harness 打造的 Windows 截图、标注与置顶贴图插件</strong>
</p>

<p align="center">
  <a href="https://github.com/janehk416-alt/dsh-FocusCapture/releases/latest"><img src="https://img.shields.io/github/v/release/janehk416-alt/dsh-FocusCapture?display_name=tag&amp;color=3464e8" alt="Latest release"></a>
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6" alt="Windows 10 / 11">
  <img src="https://img.shields.io/badge/DSH-%3E%3D0.2.2--rc.1%20%3C0.3.0-3464e8" alt="DSH compatibility">
  <a href="LICENSE"><img src="https://img.shields.io/badge/Code%20License-MIT-green" alt="MIT license"></a>
</p>

可框选屏幕、添加标注、复制或保存截图，把图片作为附件放入当前聊天框中。默认使用 **左 Ctrl + 右 Ctrl** 全局快捷键，也可在插件页自定义组合键；提供紧凑编辑工具栏和跨应用置顶贴图。

**English:** A Windows screenshot and annotation plugin for DeepSeek Harness, with a dual-Ctrl shortcut, always-on-top pinned images, PNG export, and direct attachment to the current chat draft. See [Releases](https://github.com/janehk416-alt/dsh-FocusCapture/releases) for downloads.

> [!NOTE]
> 本项目仅支持 **Windows 10/11 + DeepSeek Harness（DSH）**。插件不会自动发送聊天消息，也不会修改输入框中已有的文字。

## 功能亮点

- **两种截图入口**：点击聊天输入框的截图图标，或同时按住左 Ctrl 与右 Ctrl。
- **自定义快捷键**：在插件组件的配置页选择 Ctrl、Alt、Shift 与字母、数字或 F1–F12，保存后立即生效。
- **边框即时跟随**：框选、移动和调整选区时在鼠标事件内直接重绘，不等待合并定时器；移动选区复用缓存位图，其他标注继续使用合并刷新。
- **完整标注工具**：选区移动与缩放、矩形、椭圆、箭头、自由涂鸦、文字和马赛克。
- **可编辑样式**：七色调色板、线宽、字号、形状填充，以及已选标注的样式修改。
- **字体与精细绘制**：拉丁文字使用随包附带的 Josefin Sans并适度收紧字距，中文回退到 Microsoft YaHei UI；字号与行距可同时独立调整，圆角取消硬裁剪，图标和边框采用高质量像素对齐。
- **便捷导出**：复制到剪贴板、另存为 PNG，或添加到当前聊天附件草稿。
- **置顶贴图**：生成跨应用置顶图片，支持拖动、滚轮缩放、复制、保存与关闭。
- **本地处理**：截图和标注在本机完成，不联网、无遥测；默认只读取左右 Ctrl，自定义后只读取所选组合键，不记录文字内容。
- **会话防误投**：开始截图时锁定目标聊天，切换页面不会把截图投递到其他聊天。

## 界面预览

<p align="center">
  <img src="docs/images/compact-toolbar.png" alt="dsh-FocusCapture 紧凑截图工具栏">
</p>

工具栏保持紧凑：没有底部按钮栏、说明输入框或序号标注工具。空间不足时，工具栏会自动调整到选区上方或屏幕内。界面拉丁文字使用随包附带的 Josefin Sans，无需安装系统字体；中文自动回退到 Microsoft YaHei UI。

## 环境要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 10 或 Windows 11 |
| DSH | `>=0.2.2-rc.1 <0.3.0` |
| 运行依赖 | 使用 DSH 自带的 Node.js 与 Python，无需单独安装 |
| 运行时定位 | 遵循 `DSH_HOME` → 当前用户 `~/.dsh`；不使用作者路径或 PATH 中的任意 Python |
| 安装准备 | 无需 `npm install`；安装包已包含原生 EXE、Josefin Sans 字体及 OFL 许可证 |

## 安装

### 方法一：从 GitHub Release 安装（推荐）

1. 打开 [**Latest Release**](https://github.com/janehk416-alt/dsh-FocusCapture/releases/latest)。
2. 下载 **`dsh-FocusCapture-1.4.0-install.zip`**；日常安装不需要下载 GitHub 自动生成的 Source code。
3. 解压 ZIP，得到 `dsh-FocusCapture` 文件夹。该文件夹内应直接包含 `package.json`、`host-v1.1.js`、`icon.png` 和 `native` 目录。
4. 在 DSH 插件管理的安装向导中，将 Bundle 安装来源设为该**解压目录的绝对路径**，不要选择 ZIP 文件本身。需要显式来源类型时，使用以下格式：

   ```text
   link:D:\Plugins\dsh-FocusCapture
   ```

5. 安装后启用 **dsh-FocusCapture**。如果聊天页面已经打开，刷新页面以加载最新 Client 和图标。
6. 可通过 Release 中的 `SHA256SUMS.txt` 核对安装包完整性。

更新时，在插件管理中对同一 Bundle 执行更新即可；内部标识保持为 `@local/dsh-focuscapture`，便于保留已有启用状态。

从 1.3.0 升级时，不需要手工缩小图标或修改 `keyboard-runtime.json`。1.4.0 已把图标控制在宿主 256 KiB 限制以内，并自动发现当前设备的 DSH Python。启用后图标按钮可用即表示原生组件与快捷键 worker 均已就绪；失败时会在按钮旁显示简短原因，插件组件页也会保留 Host 启动诊断。

### 方法二：从源码安装

```bash
git clone https://github.com/janehk416-alt/dsh-FocusCapture.git
```

然后在 DSH 插件管理中安装克隆下来的仓库根目录。仓库已包含生成的 `client.js` 和 Windows 原生 EXE，因此直接安装不需要先执行构建。

## 快速使用

1. 打开需要接收截图的 DSH 聊天。
2. 点击输入框中的截图图标，或同时按住 **左 Ctrl + 右 Ctrl**。
3. 拖动框选屏幕区域，按需添加标注。
4. 选择复制、保存、置顶，或点击 **“添加到对话”** 回车图标。
5. 截图进入附件草稿后，可以继续在输入框中编辑文字，再由你手动发送。

两个 Ctrl 都松开后，快捷键才允许再次触发。没有可接收的聊天、页面连接失效或组件尚未就绪时，插件会拒绝开始截图，而不是把图片投到未知位置。

## 自定义快捷键

1. 打开 **插件 → dsh-FocusCapture**，在截图组件行点击 **配置**。
2. 保留“默认：左 Ctrl + 右 Ctrl”，或切换到“自定义组合键”。
3. 自定义时至少选择 Ctrl、Alt、Shift 之一，再选择字母、数字或 F1–F12。Ctrl、Alt、Shift 均支持左右物理按键。
4. 点击“保存快捷键”。插件只重启快捷键 worker，不重新打开截图窗口；新的组合立即生效。
5. 点击“恢复默认”可回到左右 Ctrl。

配置保存在当前用户的 `$DSH_HOME/focuscapture/settings.json`，不包含插件开发机路径。自定义组合仍采用“全部松开后重新待命”，并避开文字记录；如果与其他软件的全局快捷键冲突，请换一组组合键。

## 工具与快捷键

| 操作 | 功能 |
| --- | --- |
| 选择 / 移动 / 缩放 | 移动标注、四角调整大小；空白处移动选区，选区外重新框选 |
| 矩形 / 椭圆 | 拖动绘制；椭圆按住 Shift 可画正圆 |
| 箭头 | 从起点拖到终点，支持任意方向 |
| 涂鸦 | 按住左键自由绘制 |
| 文字 | 保留截图与标注背景，支持中文输入法、光标、选择和删除；字号与行距可同时独立调整 |
| 马赛克 | 拖动涂抹，可在设置中调整强度 |
| 图钉 | 创建独立置顶贴图；拖动移动、滚轮缩放，右键管理 |
| Ctrl+Z | 撤销 |
| Ctrl+Y / Ctrl+Shift+Z | 重做 |
| Delete | 删除所选标注；未选中时清空全部，可撤销 |
| Ctrl+S | 另存为 PNG，保留编辑器 |
| Ctrl+C | 复制图片，保留编辑器 |
| Enter | 非文字编辑状态下，添加到当前聊天附件草稿 |
| Enter（文字编辑中） | 换行 |
| Ctrl+Enter（文字编辑中） | 完成文字编辑 |
| Esc / 右键 | 取消当前编辑或退出；贴图窗口中 Esc 关闭当前贴图 |

> 敏感信息建议使用实心黑色矩形完全覆盖；马赛克不等同于不可逆的加密处理。

## 隐私与安全边界

- 不联网、无遥测。只有主动复制才修改剪贴板，主动保存并确认文件名后才写出图片。
- 默认快捷键只读取左右 Ctrl；自定义后只读取所选组合键。不安装键盘钩子，不记录文字内容或未选择的按键。
- 图片与贴图暂存在进程内存中；工具栏、颜色板、光标和编辑控件不会进入导出图。
- 使用 DSH 的认证接口和官方附件草稿入口，不插入假路径，不改写已有聊天文本。
- 截图目标在开始时即被锁定；添加附件后**不自动发送消息**。
- 无开机自启、计划任务、注册表修改或额外监听端口。

## 已知限制

- UAC 安全桌面、系统强制遮挡、独占全屏及其他强制最上层窗口不保证覆盖。
- 自定义全局快捷键可能与其他软件冲突；插件不会抢占或改写其他软件的快捷键设置。
- 多屏混合 DPI 和具体输入法组合仍需在实际设备上验收。
- 网页刷新或关闭会丢失该页面尚未投递的截图；页面连接失效超过 3 分钟后不开始截图。
- 2 分钟无交互会取消编辑；停用或退出 DSH 会关闭贴图、编辑器和键检测组件。
- 虚拟桌面上限 6000 万像素；选区上限 2500 万像素、20 MiB；最多暂存 3 张截图，5 分钟释放。
- 每张最多 200 个标注，每笔涂鸦最多 4096 点，每次文字最多 4096 字，历史最多 30 步。

## 源码构建与验证

### Web Client

```bash
node build-client.mjs
```

该命令从 `client.source.js`、`delivery.js` 和 `config.source.js` 生成 `client.js`，无新增前端依赖。`client.js` 应提交到仓库，便于用户直接安装。

### 自动测试

```bash
npm test
./native/DualCtrlSnip.exe --self-test
```

- Node 测试：39 项，覆盖目标聊天锁定、附件草稿、运行时解析、快捷键配置持久化、真实双 Ctrl 与自定义组合键事件、UI 契约等。
- 原生自检：覆盖绘图、历史、裁剪、Josefin Sans 加载与中文回退、图标抗锯齿、边框对齐、文字输入与置顶样式等。
- `tests/ui-native-smoke.py` 提供真实编辑器交互、连续框选/移动/缩放边框、字号/行距调整及 Delete/Backspace 删除像素一致性测试；默认不修改剪贴板、不保存截图、不发送聊天。

涉及真实系统按键事件的测试，运行前建议先停用当前插件，避免触发真实截图。局部绘制基准不等同于界面实际 FPS。

### 安装包发布门禁

```bash
python -m pip install -r requirements-dev.txt
python scripts/release.py build
python scripts/release.py verify dist/dsh-FocusCapture-1.4.0-install.zip
```

门禁直接读取最终 ZIP，检查 Bundle 文件、图标 PNG 解码与 256 KiB 限制、运行时配置、个人绝对路径残留、ZIP CRC 和 SHA-256。只有插件使用不要求 Pillow；Pillow 仅是发行构建/验证依赖。GitHub Actions 的 `Validate` 工作流也会执行测试与安装包门禁，但不会自动发布 Release。

## 项目结构

```text
dsh-FocusCapture/
├── package.json              # Bundle 清单、版本、图标与兼容范围
├── host-v1.1.js / index.js   # Node Host 入口、会话绑定与启动诊断
├── runtime.js / settings.js  # DSH 运行时定位与快捷键持久化
├── hotkey.js                 # 默认及自定义组合键校验
├── client.source.js          # Web Client 源码
├── config.source.js          # 插件组件行的快捷键配置页
├── client.js                 # 已生成、可直接安装的 Client
├── core.js / delivery.js     # 状态管理与附件草稿投递
├── native/                   # C# 源码、快捷键组件、运行 EXE、字体与 OFL
├── locale/                   # 中英文显示名称
├── tests/                    # Node 与原生交互测试
├── scripts/release.py        # 确定性 ZIP 构建与发布门禁
├── docs/images/              # README 界面预览
├── icon.png                  # 合规的 256×256 插件图标
├── CHANGELOG.md              # 版本历史
└── LICENSE                   # 代码 MIT 许可证
```

## 版本与问题反馈

- 下载与发布说明：[GitHub Releases](https://github.com/janehk416-alt/dsh-FocusCapture/releases)
- 版本历史：[CHANGELOG.md](CHANGELOG.md)
- 验收范围与待复测项：[VALIDATION.md](VALIDATION.md)
- 问题反馈：[GitHub Issues](https://github.com/janehk416-alt/dsh-FocusCapture/issues)

反馈问题时，请附上 Windows 版本、DSH 版本、复现步骤和预期结果。截图前请遮盖聊天内容、账号、路径及其他敏感信息。

## 许可证与图标说明

代码采用 [MIT License](LICENSE)。Josefin Sans 字体由 The Josefin Sans Project Authors 提供，采用 SIL Open Font License 1.1；完整版权与许可随包位于 `native/fonts/OFL.txt`。字体不安装到系统，也不改用其他许可证。

`icon.png` 来自用户提供的图片；第三方图片不因其包含在本仓库中而自动适用 MIT 许可证。公开展示或再分发该图标时，请确保具备相应权利或许可。
