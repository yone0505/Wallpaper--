# Tavily 创意工坊搜索 · 0.2

2026-10-13，用户确认将 Tavily 接入现有创意工坊入口，用于查找 Steam Wallpaper Engine 页面。该改动扩展搜索、设置及有限原生消息桥；不改变壁纸播放器。

## 行为

输入关键词，程序从本机原生 HTTP 客户端请求 Tavily `POST https://api.tavily.com/search`，Bearer 鉴权、basic 深度、Steam 域名限制、最多 10 项、关闭自动参数。API 使用参照 [Tavily 官方 Search 文档](https://docs.tavily.com/documentation/api-reference/endpoint/search)。

结果只保留 https://steamcommunity.com 的 filedetails 条目，ID 为正整数，去重并去除跟踪参数；带其他 appid 的链接被过滤。以文字展示标题及摘要，不把不相关图片当壁纸预览。搜索关键词包含 Wallpaper Engine，但不以搜索索引替代 Steam 对游戏归属和资源类型的确认。

详情及底部按钮调用同一受限浏览器打开命令，原生层再次校验 Steam URL。订阅和下载交给 Steam 页面，当前软件仍通过本地导入加载媒体。

Key 由设置 → 插件保存到 Windows 当前用户凭据管理器；服务读取，UI 仅接收 configured/source/hasSavedKey，保存或关闭设置后清空密码框。支持环境变量回退；API Key 不写入 AppState、仓库或错误响应正文。

搜索可取消，新搜索取代等待中的旧请求；旧结果无法覆盖最新结果。完整请求（含响应正文）有 30 秒上限，正文最多 1 MiB。缺 Key、无结果、401/403、429、配额不足、连接失败和超时都有中文状态。

## 验证

| 项目 | 结果 |
| --- | --- |
| HTTP 契约与防错 | 15 项通过：地址/鉴权/域名/参数、去重过滤、空关键词、缺 Key、不合法 Key、401/403/429/432/433/500、错误 JSON、空结果、调用者取消、正文超时、危险打开地址 |
| Windows 凭据 | 8 项实际 Windows vault 测试通过：保存/读取/删除、状态不含秘密、环境变量回退、保存项优先、错误 Key 不覆盖有效项；测试目标最终删除 |
| UI 模型 | 4 项新增通过，连同原有 7 项共 11 项：过期响应、取消、空结果选中态、URL 限制 |
| 真实 WebView2 操作 | 本机 HTTP 测试服务器接收到正确请求；保存 Key、清空输入、搜索、过滤重复/外部条目、标题/摘要转义、空状态、请求限制、取消/替代、返回本地库、重启保留 Key、删除 Key 全部通过 |
| 原功能回归 | 原有实际视频/网页播放、原生休眠、快速继续、静音、桌面父窗口、停止竞争、配置和重启恢复继续通过 |
| 构建 | Release 0 警告 / 0 错误，win-x64 自包含包成功 |
| 官方网络探测 | 未携带密钥的官方端点调用收到鉴权拒绝；未执行带真实 Key 的有效搜索 |

浏览器环境：本机 Windows 11、WebView2，管理窗口 1240×820。Browser plugin not available，使用 Playwright CDP。页面标题和内容正确，测试操作无未捕获 JS 异常。已检查搜索结果与 Key 设置截图；沿用既定原版深色样式。截图在被忽略的 `work/validation/workshop.png`、`tavily-settings.png`，其中搜索画面有明确模拟测试标识。

测试入口：`scripts/Test-All.ps1` 与 `scripts/Test-Gui.ps1`。测试注入仅通过带调试端口的 CLI `--qa-tavily-url` 启用，只接受本机 HTTP 地址；生产入口固定 Tavily HTTPS，正常启动不启用测试模式。该注入不由网页消息配置。

审查发现 HTTP ResponseHeadersRead 的 HttpClient 超时不覆盖正文，已用覆盖 SendAsync 和所有读取的关联截止令牌修复，并先失败后通过延迟正文测试。

## 实际范围

当前应用没有真实 Tavily Key。带真实 Key 的搜索相关性、账号配额及打开后的 Steam 页面需要配置后验证。本次通过的是正式 HTTP 请求实现的契约与本机交互测试，不能报告为已验证真实 Tavily 搜索结果。完整原版功能及 Steam 订阅/下载接口仍未验收，不改变 61 项完整一致性声明门槛。
