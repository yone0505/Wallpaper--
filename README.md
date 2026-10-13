# Wallpaper--

Windows 动态壁纸软件重建项目。目标是对照本机 Wallpaper Engine 2.8.42 的功能、界面和实际行为，而不是仅制作可导入的壁纸。

现在已有可独立运行的 **Wallpaper Desktop 0.2**：自己的 Explorer 桌面宿主、WebView2 视频/网页播放器、中文管理窗口和 Tavily 创意工坊页面搜索。最终目标仍是全部功能与界面逐项一致；当前版本尚未达到完整复刻。

- [原版版本与界面对照基线](docs/reference/wallpaper-engine-2.8.42.md)
- [61 项功能与界面验收记录](docs/reference/parity-baseline.json)
- [开发与 GitHub 同步规范](AGENTS.md)
- [第一阶段实现、实测与剩余范围](docs/reference/desktop-foundation-validation.md)
- [Tavily 搜索接入与验证范围](docs/reference/tavily-workshop-validation.md)

## 运行

在 Windows 10/11 x64 安装 Microsoft Edge WebView2 Runtime。执行下面的发布命令后，双击 `outputs/WallpaperDesktop/WallpaperDesktop.exe`。发布包包含 .NET 运行库，不要求用户安装 SDK；移动程序时保留整个文件夹。

点击“打开壁纸”选择 MP4、WebM、HTML 或 video/web 项目的 project.json；“导入目录”支持扫描项目目录。选中壁纸，选择右侧显示器后点击“应用壁纸”。两个网页示例为本项目原创。

关闭管理窗口后壁纸继续在托盘运行。双击托盘打开窗口，右键可暂停、停止全部壁纸或退出。音量控制文档中的 video/audio 元素，静音作用于整个网页播放器。

用户配置在 `%LOCALAPPDATA%/WallpaperDesktop/state.json`，媒体引用原位置；移除条目不会删除源文件。资源移动后需要重新导入。配置损坏会保留 `state.corrupt-*.json` 并显示恢复提示。

## Tavily 创意工坊搜索

点击“创意工坊”→“配置 Tavily”，或进入“设置”→“插件”。点击“获取 Key”打开 Tavily 控制台，创建 API Key 后粘贴并点击“保存 Key”。之后在创意工坊输入壁纸关键词，搜索相关 Steam 页面；选中结果点击“打开 Steam 页面”。

密钥保存在当前 Windows 用户的凭据管理器中，不写入 state.json 或仓库。界面只显示配置状态，不读取回填密钥。也支持 `TAVILY_API_KEY` 环境变量；本机保存的 Key 优先，删除本机 Key 后环境变量仍可生效。

每次手动搜索请求 Tavily basic 检索，最多展示 10 个 Steam 条目链接。结果取决于 Tavily 对公开页面的收录，不构成完整创意工坊目录；游戏归属、资源类型和可用性以 Steam 详情为准。订阅、下载在 Steam 中进行，下载的本地视频/网页项目可继续导入。

场景包、编辑器、Steam 订阅/下载接口、移动设备和 RGB 插件尚未实现，相应入口显示说明。

## 构建与验证

开发需要 .NET 10 SDK、Node.js 和 Git。在项目根目录 Windows PowerShell 中运行：

```powershell
./scripts/Test-All.ps1
./scripts/Publish.ps1
./scripts/Test-Gui.ps1 -ExePath ./outputs/WallpaperDesktop/WallpaperDesktop.exe
```

Test-All 包含四项仓库规范验证、31 项核心行为、15 项 HTTP 搜索契约、9 项 Windows 凭据测试、11 项 Node 测试及应用构建。Test-Gui 使用 Playwright 1.64.0 连接测试实例的 WebView2，验证实际播放、桌面父窗口、休眠/恢复、操作竞争、重启恢复及 Tavily 页面交互；首次运行会下载 Playwright，不下载额外浏览器。搜索 GUI 测试使用本机 HTTP 模拟服务及测试密钥，界面显式显示测试模式，不消耗真实 Tavily 额度。测试使用独立数据目录及原创 WebM 素材，暂时覆盖桌面背景，结束后退出测试实例。需要本机可交互的 Explorer 桌面；测试专用端口为 32222/32223，正常启动不开放调试端口。

运行截图与诊断在 `work/validation/`，发布文件在 `outputs/`，均不会提交 GitHub。61 项最终一致性基线仍需要更完整的运行、视觉和外部集成证据；当前测试通过只能证明此阶段的具体行为。
