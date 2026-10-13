# Product

<!-- impeccable:product-schema 1 -->

## Platform
Windows 桌面程序，优先验证本机 Windows 11。

## Stack
Delegated：用户已全权委托实现。采用 .NET 10 / WinForms 管理桌面窗口，WebView2 播放视频及网页并承载自编写的管理界面。

## Users
本机用户，用本地视频和网页动画管理动态桌面。

## Product Purpose
最终目标是按 Wallpaper Engine 2.8.42 的功能和界面逐项重建。只有所有基线验收通过才能宣称完整一致。

## Operating Context
用户要求每次修改更新测试，通过全部验证后提交并同步 GitHub。原版安装在本机，运行截图作为视觉参照。

## Capabilities and Constraints
当前实施阶段交付独立桌面宿主、视频/网页导入、显示器分配、播放列表、持久化和托盘管理。场景引擎、编辑器、Steam、手机和硬件插件继续保留在完整验收清单中。

0.2 新增 Tavily 公开 Steam 创意工坊页面搜索及 Windows 凭据管理器中的 API Key 配置。订阅与下载仍通过 Steam 页面完成；当前应用未配置真实 Tavily Key，正式搜索返回仍待验证。

## Brand Commitments
保留原版管理界面的布局和中文操作词；独立程序命名为 Wallpaper Desktop。公开仓库只提交自编写源码，不包含原版图片、程序或用户订阅资源。

## Evidence on Hand
docs/reference/ 固定版本的 61 项验收基线；本机 work/reference/ 的主界面、性能和常规设置截图。
