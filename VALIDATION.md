# dsh-FocusCapture 1.4.0 本地验收报告

状态：**本地候选版本，尚未推送或发布到 GitHub。** 本文用于发布前人工验收。

## 1. 验证环境

| 项目 | 实测值 |
| --- | --- |
| OS | Windows 10.0.26100.8037（Windows 11 系列） |
| 架构 | Windows x64 |
| DSH | 0.2.2-rc.1 |
| DSH Node.js | 24.21.0 |
| DSH Python | 3.12.14 |
| 插件包 | `@local/dsh-focuscapture` 1.4.0 |

## 2. 已修复的问题

| 问题 | 修复 | 验证 |
| --- | --- | --- |
| 图标 297,994 字节，超过宿主 256 KiB | 改为 256×256 PNG，98,925 字节 | 源码及最终 ZIP 均完成 PNG 解码和大小检查；真实插件管理器可读取 1.4.0 元数据，无图标错误 |
| Python 路径写死为作者账号 | 默认配置为空；按 `DSH_HOME` → 当前用户 `~/.dsh` 定位 DSH 主运行时 | 自定义目录、中文、空格、空配置、无效覆盖、错误架构均有自动测试；真实 Host → Python worker 启动通过 |
| 启动失败不可见 | 增加 Host/native/worker/Client 状态和脱敏错误；8 秒超时、最多两次重试 | 本机 Host 与 worker 就绪；本机插件行状态为 `active` |
| 缺少最终产物门禁 | `scripts/release.py` 直接读取安装 ZIP | 29 文件、CRC、图标、配置、个人路径和 SHA-256 全部通过 |

## 3. 新增功能

### 自定义快捷键

- 默认仍为 `LeftCtrl+RightCtrl`。
- 插件组件行提供官方配置页入口。
- 自定义支持 Ctrl、Alt、Shift 与 A–Z、0–9、F1–F12。
- 设置保存在当前用户 `$DSH_HOME/focuscapture/settings.json`。
- 保存后仅重启快捷键 worker，可恢复默认。
- 自动测试已完成 `Ctrl+Alt+S` 的真实 Windows 按键事件触发，并恢复双 Ctrl。

### 字体与像素精细度

- 拉丁文字使用包内 Josefin Sans，英文绘制横向比例收紧为 0.92；中文回退 Microsoft YaHei UI，不做横向压缩。
- 字号与 1.0–1.6 倍行距可在同一设置区独立调整，并随标注、撤销和导出保留。
- 字体不向系统安装；Regular、SemiBold 与完整 OFL 许可证随源码提供。
- 圆角取消 Windows Region 硬裁剪，改用透明背景、抗锯齿填充与描边；角部放大对比显示阶梯减少、透明过渡增加。
- 修复文字删除时默认 EDIT 局部重绘留下的旧字形叠加，文字变化后强制完整擦除并重绘。
- 原生自检新增字体加载、中文回退、字距/行距、图标抗锯齿、边框边界和透明圆角测试。

实际渲染图：
- [工具栏圆角与图标](docs/images/validation-toolbar-1.4.0.png)
- [字号与行距同时调整](docs/images/validation-text-settings-1.4.0.png)

### 边框跟随与缓存

- 旧路径使用约 16ms 的合并重绘定时器，边框可能多等待一个排队周期。
- 框选、移动选区、调整选区边框现在在鼠标事件内执行 `Invalidate + Update`，绘制完成才返回，不再等待该定时器。
- 涂鸦、马赛克和标注拖动仍保留合并刷新；移动选区复用缓存位图，不重复合成已有标注。
- 本机连续操作实测（包含鼠标消息与同步绘制，单位 ms）：

| 操作 | 平均耗时 | 最大耗时 | 步数 |
| --- | ---: | ---: | ---: |
| 连续框选 | 18.08 | 23.83 | 40 |
| 调整边框 | 16.47 | 31.84 | 20 |
| 移动选区 | 22.67 | 24.73 | 20 |

这些是同步绘制耗时，不表示数学上的零延迟；不同硬件与屏幕内容会变化。移动选区在加入缓存复用前的一次同流程测量为平均 36.09ms，本轮为 22.67ms。

## 4. 已执行测试

| 测试 | 结果 |
| --- | --- |
| `npm test` | **39/39 通过** |
| `native/DualCtrlSnip.exe --self-test` | **通过**，`ok: true` |
| `tests/ui-native-smoke.py` | **通过**，真实连续框选/移动/缩放边框、标注、中文输入、字号/行距、Delete/Backspace 剩余文字像素一致性、PNG、贴图和清理均成功 |
| 默认双 Ctrl OS 事件 | 通过；未绑定聊天时不打开截图 |
| 自定义 Ctrl+Alt+S OS 事件 | 通过；触发一次，无重复弹窗 |
| 快捷键保存与重启后读取 | 通过；测试使用隔离的临时 DSH_HOME |
| 真实插件管理器启用 | 通过；包版本 1.4.0，Host 行 `active` |
| 安装 ZIP 构建与门禁 | 通过；29 个安装文件，未检出个人用户路径 |
| 最终 ZIP 解压启动 | 通过；在含中文/空格的自定义 DSH_HOME 下，Host、native 与 DSH Python 就绪；双 Ctrl 可切换为 Ctrl+Shift+F7 |
| 最终 ZIP 原生自检 | 通过；解压后的 EXE 输出 `ok: true`，字体与像素测试通过 |

最终安装包 SHA-256：

```text
da022f82be33df3df1d87758597bfb70ce2c094bf0826aeb03f665065529cf39
```

测试中的快捷键事件由 Windows `keybd_event` 生成，**不是另一台设备上的物理键盘验收**。

## 5. 本地候选产物

- `dist/dsh-FocusCapture-1.4.0-install.zip`
- `dist/SHA256SUMS.txt`

构建与复核命令：

```bash
npm test
./native/DualCtrlSnip.exe --self-test
python scripts/release.py build
python scripts/release.py verify dist/dsh-FocusCapture-1.4.0-install.zip
```

## 6. 仍需人工或其他设备验收

1. 在另一台受支持 Windows 设备上，从原始 ZIP 通过插件管理器完成首次安装与启用。
2. 确认无需手工修改图标、Python 路径或安装目录。
3. 使用物理键盘验证默认双 Ctrl、左/右先按、松开再触发和自定义组合。
4. 验证聊天截图按钮、选区、取消、复制、保存、置顶和附件草稿回传。
5. 重启 DSH 后确认快捷键配置保留，组件无孤儿进程或重复监听。
6. 使用普通非管理员账号、自定义 DSH_HOME、中文/空格路径和多屏混合 DPI 复测。
7. 检查具体输入法候选窗口与 Josefin Sans/中文回退的实际视觉效果。
8. 人工打开插件组件行的“配置”页，确认快捷键表单显示、保存和恢复默认的实际交互。

在上述项目完成前，不宣称“所有设备都能用”或“全环境验收完成”。

## 7. 发布边界

本次只生成并验证本地候选版本。**未执行 Git push、未创建新标签、未修改 GitHub Release。** 待人工确认合格合规后，再进行源码推送、标签创建和 Release 上传。
