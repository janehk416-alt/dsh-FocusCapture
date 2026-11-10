# Changelog

本项目记录 notable changes，版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)，文档格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [1.4.0] - 2026-11-10

### Added

- 在插件组件配置页提供自定义快捷键：Ctrl、Alt、Shift 与字母、数字或 F1–F12；保存后立即生效并可恢复默认双 Ctrl。
- 快捷键保存到当前用户 `$DSH_HOME/focuscapture/settings.json`，不包含开发机路径。
- 使用随包附带的 Josefin Sans 显示拉丁文字并适度收紧英文间距，中文回退到 Microsoft YaHei UI；附带完整 SIL OFL 1.1 许可证。
- 文字工具支持同时独立调整字号与 1.0–1.6 倍行距，行距随标注克隆、撤销、缩放和导出保留。
- 图标使用高质量抗锯齿与合成，工具栏及选区边框采用更准确的像素对齐；圆角取消 Windows Region 硬裁剪，改用透明背景与抗锯齿填充/描边。
- 框选、移动和调整选区边框改为鼠标事件内同步重绘，不再等待 16ms 合并定时器；移动选区复用缓存位图，其他标注保留合并刷新。
- 增加 Host、原生 helper、键盘 worker、Client 绑定状态及可读启动错误。
- 新增确定性安装 ZIP 构建、最终产物图标解码/大小检查、个人路径扫描和 GitHub Actions 验证工作流。

### Fixed

- 将插件图标从 297,994 字节降至 98,925 字节，解决宿主 `icon exceeds 256 KiB` 元数据错误。
- 移除 `C:/Users/EDY/...` 的 Python 硬编码，按 DSH 官方相同规则解析 `DSH_HOME`，回退到当前用户 `~/.dsh`。
- 校验 DSH 主运行时平台、架构和 Python 文件；不回退到任意系统 Python 或 PATH。
- 为原生 helper 与快捷键 worker 增加 8 秒启动超时、最多两次有限重试和停用清理。
- 快捷键测试使用隔离的临时 DSH_HOME，避免读写用户真实快捷键配置。
- 修复截图文字输入时删除字符留下的旧字形叠加：文字变化后强制完整擦除并重绘编辑区；Delete 与 Backspace 均加入剩余文字像素一致性回归测试。

### Verified

- 39 项 Node 测试通过，包含真实 Ctrl+Alt+S 自定义快捷键触发及恢复双 Ctrl。
- 原生 1.4.0 自检与真实编辑器交互测试通过，覆盖中文文字、PNG 导出、置顶贴图与进程清理。
- 最终安装 ZIP 的 29 个文件通过 CRC、图标、配置与个人路径门禁。

### Verification limits

- 另一台设备的首次安装、普通权限用户、物理键盘、多屏混合 DPI 与所有输入法组合仍需实际验收；本版本不作全环境保证。

## [1.3.0] - 2026-11-09

首个发布到 GitHub 的正式版本。

### Added

- 通过聊天输入框图标或左 Ctrl + 右 Ctrl 开始区域截图。
- 提供选区、矩形、椭圆、箭头、涂鸦、文字与马赛克工具。
- 支持七色、线宽、字号、形状填充、撤销、重做和删除。
- 支持复制图片、保存 PNG、跨应用置顶贴图和添加到当前聊天附件草稿。
- 锁定截图目标聊天，避免页面切换造成误投；添加附件后不自动发送。
- 提供中文说明、界面预览、安装步骤、版本发布说明和 SHA-256 校验文件。
- 使用用户提供的图片作为插件管理页图标。

### Verified

- 20 项 Node 自动测试全部通过。
- Windows 原生 EXE 自检通过。
- 安装 ZIP 解压后，Client 构建结果可复现，Node 测试与原生自检均通过。

### Notes

- 仅支持 Windows 10/11 与 `DSH >=0.2.2-rc.1 <0.3.0`。
- 多屏混合 DPI、UAC 安全桌面、独占全屏和输入法组合存在 README 所述边界。

[1.4.0]: https://github.com/janehk416-alt/dsh-FocusCapture/releases/tag/v1.4.0
[1.3.0]: https://github.com/janehk416-alt/dsh-FocusCapture/releases/tag/v1.3.0
