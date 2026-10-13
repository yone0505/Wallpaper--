# Wallpaper Desktop 0.1 阶段验证

2026-10-13，本机 Windows 11 10.0.26200，单显示器 2560×1440、DPI 96。原版参照仍为 Wallpaper Engine 2.8.42。此次交付是可运行的独立桌面播放器及管理基础，61 项完整一致性验收保持未通过。

## 已实现

- 自编写 WinForms 管理窗口与 HTML/CSS/JS 深色中文界面，库网格、搜索/排序/类型筛选、详情、显示器选择、视频属性与设置。
- MP4/WebM、HTML 和 video/web 项目导入，路径校验、去重、项目内预览路径、保留原媒体、不删除用户文件。
- 自己的桌面窗口挂到 Explorer WorkerW，视频循环、网页动画、媒体音量、整个播放器静音、显示器分配、顺序播放列表及定时切换。
- 暂停网页时保存最后画面，隐藏 WebView2 并调用原生 TrySuspendAsync；恢复后继续绘制。全屏暂停策略、托盘控制、停止/退出释放、配置持久化与重启恢复。
- 管理页面限制导航和消息来源；播放页面关闭原生消息桥、宿主对象、下载、新窗口及设备权限。

## 验证结果

| 验证 | 结果与证据 |
| --- | --- |
| .NET 核心行为 | 30 项通过：导入、相对路径、递归项目拒绝、配置损坏/空标题恢复、列表、策略、窗口拓扑和操作版本 |
| Node 模型与资源 | 7 项通过：搜索/排序/选择、标题转义、预览 URL、示例入口/PNG、原创 WebM |
| 仓库规则 | 四项 PowerShell 脚本全部通过，保留完整 61 项范围与完成声明门槛 |
| 构建 | Release 0 警告 / 0 错误；win-x64 自包含发布成功 |
| 视频 | 原创 WebM 实际 videoWidth=320，currentTime>1；另以本机已有 MP4 实测 videoWidth=1920，currentTime>1；暂停/继续成功 |
| 网页 | Canvas 绘制计数增长；暂停时 IsSuspended=true，原生保留画面哈希稳定；快速暂停/继续、暂停期间修改设置后能恢复 |
| 桌面宿主 | 新程序 HWND 的 GetParent 等于所选 WorkerW，attached=true；停止后窗口记录为零 |
| 操作竞争 | 初始化期间停止不会恢复播放；并发应用时最后一次选择生效 |
| 交互与持久化 | 搜索空结果、应用、播放列表、设置音量 42、全局静音、页面重载；正常退出并重启后恢复分配/填充方式；移除当前项释放窗口且源文件仍存在 |
| 页面检查 | manager.wallpaper.local/index.html、标题正确、内容可见，已测试操作无 JS 未捕获异常；1240×820 管理窗口截图及设置截图已人工检查 |

浏览器路径：Browser plugin not available，因此使用 Playwright 通过本机 CDP 连接 WebView2。测试脚本 `tests/gui-smoke.mjs`、`tests/gui-restart.mjs`；入口 `scripts/Test-Gui.ps1`。截图及诊断保存到被忽略的 `work/validation/{manager.png,settings.png,web.png,video.png,desktop.json,video.json,restart.json}`。

审查修复：等待中的应用可被停止/移除/退出取消；拒绝 project.json 自引用，避免栈溢出；缺失标题配置保留并恢复；使用原生 IsMuted 约束 Web Audio；串行更新播放状态；实测证实可见网页的 CDP freeze 不足以暂停，改为 WebView2 原生休眠及静态保留帧。

WebView2 休眠的可见性要求及恢复方式参照 [Microsoft TrySuspendAsync 文档](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.trysuspendasync)。休眠是尽力执行，若运行时拒绝，仍保留暂停画面并报告状态。

## 视觉差异与未验证范围

主界面的分区、深色配色、网格、右侧详情、底部播放列表与四页签设置遵循本机截图；目前控件和设置数量较少，图标、标题栏、尺寸、完整页面状态未达到原版像素一致。保留参照的蓝色按钮及对话框边界样式；静态检测器提出按钮对比度和边界/阴影建议，本阶段服从指定参照，并记录为后续视觉验收项。

实际设备只验证一台显示器；多显示器、负坐标、Windows 10、Explorer 重启/显示器热插拔未作真实设备验证。全屏策略有行为模型测试，仍需游戏/独占全屏等实际应用对照。自动切换计时器与部分托盘菜单未作完整长期稳定性验收。

scene.pkg 渲染、场景编辑器、Steam 社区、手机配对、RGB 插件、音频响应和原版高级效果未实现。此阶段没有将这些条目标为通过，不能宣称功能和界面完整一致。
