# Desktop Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 交付可独立运行的视频及网页动态壁纸管理软件。

**Architecture:** Core 处理导入、持久化、策略和播放列表；App 负责 WinForms、Explorer 桌面窗口及隔离的 WebView2。HTML/CSS/JS 自编写参照界面，通过受限消息桥调用应用功能。

**Tech Stack:** .NET 10、WinForms、Microsoft.Web.WebView2 1.0.4258.31、Node 内置测试；Playwright 用于本机 CDP 验证。

**Spec:** docs/superpowers/specs/2026-10-13-desktop-foundation.md

## Global Constraints

- Windows 桌面程序，数据位于 LocalAppData/WallpaperDesktop。
- 只提交自行编写的源码，全部验证后 commit / push。
- 最终范围保留全部 61 项，禁止宣称完整复刻。
- 壁纸无原生消息桥，界面消息必须校验来源和参数。

## Review Focus

- 路径含中文、空格；失效资源须显示明确错误。
- 配置损坏须备份；取消导入不改变列表。
- Explorer 背景层和负坐标；找不到宿主不可遮盖桌面图标。
- 壁纸页面跳转不得取得管理桥。
- 播放列表空项/删除当前项、关闭程序必须释放宿主。

### Task 1: 本地库与行为模型

Files: src/Wallpaper.Core/{Models,Library,StateStore,PlaybackPolicy,DesktopTopology}.cs; tests/Wallpaper.Core.Tests/{Wallpaper.Core.Tests.csproj,Program.cs}。

Interfaces: Library.Import(path), StateStore.Load/Save(AppState), Playlist.Next(ids,current), PlaybackPolicy.ShouldPause(fullscreen,enabled), DesktopTopology.Select(nodes)。

- [ ] 写测试：视频/web 项目导入、场景拒绝、去重、危险项目相对路径、配置恢复、音量范围、列表删除、Windows 桌面拓扑。
- [ ] 运行看到缺失实现失败；实现并通过全部行为测试。

### Task 2: 桌面宿主和参照管理页

Files: src/Wallpaper.App/{Program,ManagerForm,WallpaperWindow,DesktopHost,Native,AppController}.cs; src/Wallpaper.App/ui/*; src/Wallpaper.App/demos/*; tests/ui-model.test.mjs。

Interfaces: UI JSON {id,method,args} / {id,result,error}; Controller.State, Apply(id,monitor), Stop(monitor), SaveSettings(settings)。

- [ ] 写 UI 搜索/排序/选择和无效桥消息测试，先失败再实现。
- [ ] 实现主界面、真实导入、播放、显示分配、属性、播放列表、托盘、暂停和持久化。
- [ ] 构建并运行本机窗口；验证视频播放时间、网页动画、桌面父窗口、移除与设置保存。

### Task 3: 发布、验收与同步

Files: scripts/{Test-All,Publish}.ps1; tests/gui-smoke.mjs; README.md; docs/reference/desktop-foundation-validation.md。

- [ ] 完整测试，检查截图与交互，修复失败并回归。
- [ ] 使用执行计划技能要求的一次独立最终审查，解决实际问题。
- [ ] 发布可执行文件；记录实测及未完成项目；合并回 main，提交并推送，核对远端 SHA。
