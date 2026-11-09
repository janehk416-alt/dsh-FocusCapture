# Changelog

本项目记录 notable changes，版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)，文档格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

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

[1.3.0]: https://github.com/janehk416-alt/dsh-FocusCapture/releases/tag/v1.3.0
