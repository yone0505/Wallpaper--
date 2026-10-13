# 独立桌面播放与管理

用户已委托全部实现，最终范围以 docs/reference/parity-baseline.json 为准。本阶段是其可运行基础，不等价于完整复刻。

采用自编写 WinForms 窗口及 WebView2。管理页面拥有有限 JSON 消息桥；壁纸页面没有原生桥。内容从本地视频、HTML 或 Wallpaper Engine video/web project.json 导入，不依赖原版进程。应用数据在 LocalAppData/WallpaperDesktop，资源引用保留原位置。

交付：中文深色管理界面、本地导入/去重/删除、搜索排序、详情预览、每显示器播放/停止、静音音量和填充模式、顺序播放列表、全屏暂停、托盘显示/暂停/退出、退出释放窗口、重启恢复。删除库条目不删除用户源文件。加载损坏配置须保留原文件并提示用户。

桌面窗口只附着验证的 Explorer 背景窗口，禁止以置顶窗口冒充桌面。覆盖 Windows 11 Progman 内 WorkerW 和旧版图标窗口后的顶层 WorkerW；显示器坐标可为负数；Explorer 重启后尝试重附着。

提供两个自行编写的网页示例。视频实测使用本地现有视频或生成的测试视频，媒体不提交。

验证：无外部测试框架的 .NET 行为测试、Node UI 模型测试、既有四项仓库验证、WebView2 CDP 页面操作和桌面 HWND/播放诊断。软件发布到 outputs/WallpaperDesktop；测试用户数据隔离，测试结束关闭自己的进程。

未实施：scene.pkg 渲染、场景编辑器、Steam 社区、Android、RGB 插件、原版全部高级属性。这些仍未验收，不由按钮或此阶段测试冒充完成。
