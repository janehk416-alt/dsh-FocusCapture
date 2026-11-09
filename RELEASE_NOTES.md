# dsh-FocusCapture v1.3.0

首个正式 GitHub Release：为 DeepSeek Harness 提供 Windows 区域截图、标注、置顶贴图，以及将截图添加到当前聊天附件草稿的能力。

## 下载

- **`dsh-FocusCapture-1.3.0-install.zip`**：推荐安装包，包含运行所需 Bundle、原生 EXE、图标、README 和许可证。
- **`SHA256SUMS.txt`**：安装包的 SHA-256 校验文件。
- **Source code (zip / tar.gz)**：GitHub 自动生成的完整源码，供开发、审计和二次修改使用；日常安装不必下载。

## 环境要求

- Windows 10 / 11
- DeepSeek Harness `>=0.2.2-rc.1 <0.3.0`
- 无需 `npm install`，无需另装 Python；安装包已包含 `native/DualCtrlSnip.exe`

## 安装步骤

1. 下载并解压 `dsh-FocusCapture-1.3.0-install.zip`。
2. 确认解压后的 `dsh-FocusCapture` 目录内直接包含 `package.json`。
3. 在 DSH 插件管理的安装向导中，将 Bundle 安装来源设为该目录的绝对路径，不要选择 ZIP 本身。需要显式来源类型时使用：

   ```text
   link:D:\Plugins\dsh-FocusCapture
   ```

4. 启用 **dsh-FocusCapture**；已经打开的聊天页面请刷新。

## 本版功能

- 聊天截图按钮与左 Ctrl + 右 Ctrl 双入口。
- 矩形、椭圆、箭头、涂鸦、文字、马赛克与选区编辑。
- 七色、线宽、字号、填充、撤销、重做和删除。
- 复制、保存 PNG、置顶贴图、添加到聊天附件草稿。
- 锁定目标聊天，切换页面不误投；不自动发送消息。
- 本地处理，不联网、无遥测，不记录其他按键。

## 验证

- 20 项 Node 自动测试通过。
- Windows 原生 EXE 自检通过。
- 从安装 ZIP 解压后，Bundle 清单、图标导出和原生自检再次核验通过。

完整功能、快捷键、隐私边界和已知限制请参阅 [README](https://github.com/janehk416-alt/dsh-FocusCapture/blob/main/README.md)。版本历史见 [CHANGELOG](https://github.com/janehk416-alt/dsh-FocusCapture/blob/main/CHANGELOG.md)。

> 图标来自用户提供的图片，不随代码 MIT 许可证自动重新授权；公开展示或再分发前请确认具备相应权利或许可。
