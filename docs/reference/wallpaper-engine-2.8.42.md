# Wallpaper Engine 2.8.42 对照基线

## 目标和当前状态

目标是重建与本机固定版本对应的功能和界面。此文档是原版调查和验收范围，尚未交付独立应用；列出的项目目前都未通过重建软件的一致性验收。

原版产品版本为 2.8.42，主程序文件版本为 2.8.0.42，运行于 Windows 11 10.0.26200。当前比较环境为简体中文、dark 主题、96 DPI、2560×1440 单显示器。主窗口实测 2213×1201，设置窗口实测 550×610。

## 已观察到的资料

- 原版主窗口、设置性能页、常规页已通过本机窗口直接采集并查看；采集不要求用户逐页提供截图。原始截图位于本机忽略目录 work/reference，不上传到公开仓库。
- 桌面渲染窗口实测嵌入 Progman 下的 WorkerW；图标视图 SHELLDLL_DefView 为其同级。此结构是本机观察，不外推为所有 Windows 版本的保证。
- UI 入口为 ui/dist/index.html，使用 AngularJS；脚本内嵌 89 个模板，5 个路由。观察到 170 个编辑器字面方法和 97 组延迟调用的命名空间/方法组合；动态生成的调用尚未穷尽。
- 原生宿主注入 global、ui、browseWallpaperObject、settingsObject、editorObject 等对象，JavaScript 经回调请求原生数据和操作。能够读出模板不能证明对应原生引擎已恢复，也不能证明新实现的操作结果一致。
- 之前提取的 PKG/TEX/模型资源仍是壁纸内容资料；原版原生渲染和编辑器完整源码尚未恢复。

## 界面静态基线

主页面包含已安装、发现、创意工坊、搜索排序筛选、网格、右侧详情、播放列表、移动设备/显示/设置入口和底部操作。dark 主题静态色值为背景 #222、正文 #eee、强调色 #4183f5，中文正文使用 Microsoft YaHei UI；正文 14px、标题栏30px。详情区依据视口采用205/320/400px宽度，设置包含性能、常规、插件、关于。这些值来自资源观察；实际视觉验收仍需在匹配的窗口、内容、缩放和操作状态下进行。

编辑器包括图层、模型、粒子、材质、骨骼、曲线、物理、脚本、资源和预览等工具。每个工具必须有实际实现和运行测试，不能以按钮出现作为完成依据。

## 证据覆盖

UI 脚本 SHA-256：be104cf40bd7fe1e3598f7387f1cfa545c4a71282d9a3a3b30dcc4a8b262373e。

REA 父记录：ev_b6b5a5e17dc110e276bc26e65c7986d17eb38f55549cd347e0c593eb2733f0e1。Summary：ev_5733180283a92c5d59f993de489ec9d51c9272f26c3416ad808aa492fe6adc89。Modules：ev_af8c58bbc2923f679e44d382290aa23bdbe5c3bc1d447891c36597c6898a84d8。

REA读取了2个JavaScript文件、2,040,845文本字节，无解析失败；scripts.js语义图超出结果预算，vendor.js的20秒分析超时，因此应用和语义覆盖为partial。模板、尺寸、颜色和协议由额外静态阅读支持；接口名称不能证明原生运行行为。当前运行截图只覆盖已列明的三个页面。

## 验收记录

机器可读清单位于 parity-baseline.json。验收状态区分 unverified、implemented、verified、blocked；implemented也不允许声明一致。只有每个已识别项目都提供对应运行、视觉、外部集成证据并通过，才能允许完整一致的声明。调查中新增的功能也必须进入清单。

范围记录测试只检查记录结构、覆盖类别和声明的一致性；软件功能另需实际运行测试。当前完整一致声明标志为false。

| ID | 类别 | 对照项目 | 当前状态 |
|---|---|---|---|
| UI-01 | interface | 主界面布局与主题 | unverified |
| UI-02 | interface | 设置与弹窗 | unverified |
| UI-03 | interface | 交互与异常状态 | unverified |
| UI-04 | interface | 编辑器布局 | unverified |
| LIB-01 | library | 本地库和导入 | unverified |
| LIB-02 | library | 搜索排序筛选 | unverified |
| LIB-03 | library | 收藏与文件夹 | unverified |
| LIB-04 | library | 壁纸属性 | unverified |
| LIB-05 | library | 详情与预览 | unverified |
| LIB-06 | library | 本地备份和源文件定位 | unverified |
| MON-01 | displays | 独立显示器壁纸 | unverified |
| MON-02 | displays | 跨屏与克隆 | unverified |
| MON-03 | displays | 显示配置文件 | unverified |
| MON-04 | displays | 标识与热插拔 | unverified |
| MON-05 | displays | 对齐裁剪和翻转 | unverified |
| MON-06 | displays | 显示器拆分和移除 | unverified |
| REN-01 | rendering | 桌面窗口宿主 | unverified |
| REN-02 | rendering | 本地视频 | unverified |
| REN-03 | rendering | 视频框架与HDR | unverified |
| REN-04 | rendering | 网页壁纸 | unverified |
| REN-05 | rendering | 二维场景 | unverified |
| REN-06 | rendering | 三维场景 | unverified |
| REN-07 | rendering | 本地应用壁纸 | unverified |
| REN-08 | rendering | 包与原始资源兼容 | unverified |
| REN-09 | rendering | SceneScript | unverified |
| PLAY-01 | playback | 前台最大化全屏策略 | unverified |
| PLAY-02 | playback | 电源锁定休眠策略 | unverified |
| PLAY-03 | playback | 应用程序规则 | unverified |
| PLAY-04 | playback | 品质与帧率 | unverified |
| PLAY-05 | playback | 暂停与列表切换 | unverified |
| AUD-01 | audio | 全局每屏与壁纸音量 | unverified |
| AUD-02 | audio | 自动静音与规则例外 | unverified |
| AUD-03 | audio | 系统音频可视化 | unverified |
| AUD-04 | audio | 媒体集成 | unverified |
| PL-01 | playlists | 列表编辑和保存 | unverified |
| PL-02 | playlists | 时间触发与间隔 | unverified |
| PL-03 | playlists | 启动与状态恢复 | unverified |
| PL-04 | playlists | 切换过渡 | unverified |
| SYS-01 | system | 系统托盘 | unverified |
| SYS-02 | system | 自动启动 | unverified |
| SYS-03 | system | 热键与命令行 | unverified |
| SYS-04 | system | 屏幕保护程序 | unverified |
| SYS-05 | system | 桌面图标与静态锁屏 | unverified |
| SYS-06 | system | 异常恢复与清理 | unverified |
| ED-01 | editor | 项目创建导入保存 | unverified |
| ED-02 | editor | 图层与属性操作 | unverified |
| ED-03 | editor | 图像效果与遮罩 | unverified |
| ED-04 | editor | 粒子系统 | unverified |
| ED-05 | editor | 时间轴与曲线 | unverified |
| ED-06 | editor | 角色变形与骨骼 | unverified |
| ED-07 | editor | 模型材质灯光物理 | unverified |
| ED-08 | editor | 脚本与着色器编辑 | unverified |
| ED-09 | editor | 用户属性和条件 | unverified |
| ED-10 | editor | 资源共享与性能统计 | unverified |
| ED-11 | editor | 发布与预览 | unverified |
| STEAM-01 | steam | 发现和创意工坊 | unverified |
| STEAM-02 | steam | 订阅与下载 | unverified |
| STEAM-03 | steam | Steamworks身份和发布 | unverified |
| MOB-01 | mobile | 移动设备发现与配对 | unverified |
| MOB-02 | mobile | 传输与移动包 | unverified |
| RGB-01 | rgb | iCUE与Chroma | unverified |

## 外部条件与版本边界

原版的Steam在线行为需要已登录且拥有产品的账号，独立实现的Steam访问和发布能力尚未验证。移动配对和RGB功能需要实际兼容设备及相应环境；目前没有这些功能已通过的证据。

2.8.42保留本地应用壁纸，但官方已移除其Workshop分发。原版不支持一般场景导出为视频/GIF；预览GIF录制是不同功能。动态Windows锁屏不属于原版支持项，静态快照和屏保要分别核对。打包场景与可编辑源项目不能混为同一项能力。

官方资料提供功能范围；资料不是本机所有行为已核对的证据，完整重建仍须逐项实测。

- [原版产品功能](https://store.steampowered.com/app/431960/Wallpaper_Engine/)
- [官方版本公告](https://store.steampowered.com/news/posts/?appids=431960&feed=steam_community_announcements)
- [命令行控制](https://help.wallpaperengine.io/en/functionality/cli.html)
- [Steamworks运行条件](https://help.wallpaperengine.io/en/steam/steamworks.html)
- [场景编辑器](https://docs.wallpaperengine.io/en/scene/overview.html)
- [网页壁纸接口](https://docs.wallpaperengine.io/en/web/overview.html)
- [移动端配对与传输](https://help.wallpaperengine.io/en/mobile/pairing.html)
- [场景导出限制](https://help.wallpaperengine.io/en/functionality/export.html)
- [锁屏限制](https://help.wallpaperengine.io/en/general/lockscreen.html)
