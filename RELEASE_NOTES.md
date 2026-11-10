# dsh-FocusCapture v1.4.0

本次版本修复 1.3.0 的跨机器安装问题，并加入插件页自定义快捷键、Josefin Sans 字体和更精细的工具栏绘制。

## 下载

- **`dsh-FocusCapture-1.4.0-install.zip`**：推荐安装包，包含运行所需 Bundle、原生 EXE、字体、图标、README 与许可证。
- **`SHA256SUMS.txt`**：安装包 SHA-256 校验文件。
- **Source code (zip / tar.gz)**：完整源码与测试，供开发和审计使用。

## 环境要求

- Windows 10 / 11
- DeepSeek Harness `>=0.2.2-rc.1 <0.3.0`
- 无需 `npm install`，无需另装 Python 或系统字体
- 默认从 `$DSH_HOME/dsh-runtimes/dsh-primary-runtime` 发现 DSH Python；未设置 `DSH_HOME` 时使用当前用户 `~/.dsh`

## 从 1.3.0 升级

1. 下载并解压新版安装 ZIP。
2. 在 DSH 插件管理中更新或重新安装解压目录中的 Bundle。
3. 启用 **dsh-FocusCapture**，刷新已经打开的聊天页面。

不需要手工缩小图标，也不需要修改 `keyboard-runtime.json`。如果运行时发现或启动失败，截图按钮旁会显示简短原因，插件组件页保留 Host 启动诊断。

## 自定义快捷键

1. 打开 **插件 → dsh-FocusCapture**，在截图组件行点击 **配置**。
2. 保留默认 **左 Ctrl + 右 Ctrl**，或选择自定义组合。
3. 自定义支持 Ctrl、Alt、Shift 与 A–Z、0–9、F1–F12；至少选择一个修饰键。
4. 保存后只重启快捷键 worker，新的组合立即生效；可随时恢复默认。

配置保存在当前用户 `$DSH_HOME/focuscapture/settings.json`。默认只读取左右 Ctrl；自定义后只读取所选组合键，不记录文字内容或其他按键。

## 字体与界面

拉丁文字使用随包附带的 Josefin Sans 并适度收紧英文间距，中文回退到 Microsoft YaHei UI。字号与行距可同时独立调整。字体不安装到系统，完整 SIL OFL 1.1 许可证位于 `native/fonts/OFL.txt`。圆角取消硬裁剪，图标与边框采用高质量像素对齐；同时修复 Delete/Backspace 删除文字时的旧字形残留。

框选、移动和调整选区边框改为鼠标事件内同步重绘，不再等待合并定时器；移动选区复用缓存位图，降低重复合成开销。

## 验证

- 39 项 Node 测试通过，包含真实 Ctrl+Alt+S 自定义快捷键触发与恢复默认。
- 原生 1.4.0 自检和真实编辑器交互测试通过。
- 最终安装 ZIP 的 29 个文件通过 CRC、PNG 图标、个人路径与运行时配置门禁。

另一台设备的首次安装、普通权限账号、物理键盘、多屏混合 DPI 和输入法组合仍需实际验收；本版本不宣称覆盖所有环境。

完整说明见 [README](https://github.com/janehk416-alt/dsh-FocusCapture/blob/main/README.md)，版本历史见 [CHANGELOG](https://github.com/janehk416-alt/dsh-FocusCapture/blob/main/CHANGELOG.md)。

> 图标来自用户提供的图片，不随代码 MIT 许可证自动重新授权；公开展示或再分发前请确认具备相应权利或许可。
