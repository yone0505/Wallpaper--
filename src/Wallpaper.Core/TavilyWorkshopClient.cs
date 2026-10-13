using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wallpaper.Core;

public sealed record WorkshopResult(string Id,string Title,string Url,string Description);
public sealed record WorkshopSearchPage(IReadOnlyList<WorkshopResult> Results);

public sealed class TavilyWorkshopClient
{
    private readonly HttpClient http;
    private readonly Uri endpoint;
    public TavilyWorkshopClient(HttpClient http,Uri? endpoint=null)
    {
        this.http=http;
        this.endpoint=endpoint??new("https://api.tavily.com/search");
        if(this.endpoint.AbsoluteUri!="https://api.tavily.com/search" &&
            !(this.endpoint.Scheme=="http" && this.endpoint.IsLoopback && this.endpoint.UserInfo.Length==0 && this.endpoint.Query.Length==0))
            throw new ArgumentException("测试地址必须是本机 HTTP 地址。");
    }
    public static string ValidateKey(string key)
    {
        key=key.Trim();
        if(key.Length==0) throw new InvalidDataException("请先在设置 → 插件中配置 Tavily API Key。");
        if(key.Length>1024 || key.Any(c=>c<'!' || c>'~')) throw new InvalidDataException("API Key 格式不正确，请重新粘贴。");
        return key;
    }
    public async Task<WorkshopSearchPage> SearchAsync(string query,string key,CancellationToken token=default)
    {
        query=query.Trim();
        if(query.Length==0) throw new InvalidDataException("请输入壁纸关键词。");
        if(query.Length>200) throw new InvalidDataException("关键词不能超过 200 个字符。");
        key=ValidateKey(key);
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);
        var timeout=TimeSpan.FromSeconds(30);
        if(http.Timeout!=Timeout.InfiniteTimeSpan && http.Timeout<timeout)timeout=http.Timeout;
        deadline.CancelAfter(timeout);
        var operationToken=deadline.Token;
        using var request=new HttpRequestMessage(HttpMethod.Post,endpoint);
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        request.Content=JsonContent.Create(new
        {
            query=$"Wallpaper Engine Steam Workshop {query} site:steamcommunity.com/sharedfiles/filedetails/",
            include_domains=new[]{"steamcommunity.com"},include_domains_mode="restrict",
            search_depth="basic",topic="general",max_results=10,auto_parameters=false,
            include_answer=false,include_raw_content=false,include_images=false
        });
        try
        {
            using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,operationToken);
            if(!response.IsSuccessStatusCode)
            {
                var message=(int)response.StatusCode switch
                {
                    401 or 403 => "Tavily Key 无效或权限不足，请检查设置中的 API Key。",
                    429 => "Tavily 请求过于频繁，请稍后再试。",
                    432 or 433 => "Tavily 额度已用完，请在 Tavily 控制台检查配额。",
                    >=500 => "Tavily 服务暂时不可用，请稍后再试。",
                    _ => "Tavily 无法完成搜索，请更换关键词后重试。"
                };
                throw new InvalidDataException(message);
            }
            const int limit=1024*1024;
            if(response.Content.Headers.ContentLength>limit) throw new InvalidDataException("Tavily 返回内容过大，请缩小搜索范围。");
            await using var stream=await response.Content.ReadAsStreamAsync(operationToken);
            using var buffer=new MemoryStream(); var chunk=new byte[8192];
            int read;
            while((read=await stream.ReadAsync(chunk,operationToken))>0)
            {
                if(buffer.Length+read>limit) throw new InvalidDataException("Tavily 返回内容过大，请缩小搜索范围。");
                buffer.Write(chunk,0,read);
            }
            using var doc=JsonDocument.Parse(buffer.ToArray());
            if(doc.RootElement.ValueKind!=JsonValueKind.Object || !doc.RootElement.TryGetProperty("results",out var entries) || entries.ValueKind!=JsonValueKind.Array)
                throw new InvalidDataException("Tavily 返回格式不正确，请稍后重试。");
            var items=new List<WorkshopResult>(); var ids=new HashSet<string>();
            foreach(var entry in entries.EnumerateArray().Take(40))
            {
                if(entry.ValueKind!=JsonValueKind.Object) continue;
                var url=SteamWorkshop.CanonicalUrl(Text(entry,"url",4096));
                if(url is null || !ids.Add(url)) continue;
                items.Add(new(url[(url.LastIndexOf('=')+1)..],Text(entry,"title",256)??"Steam 创意工坊条目",url,Text(entry,"content",1600)??""));
                if(items.Count==10) break;
            }
            return new(items);
        }
        catch(JsonException) { throw new InvalidDataException("Tavily 返回格式不正确，请稍后重试。"); }
        catch(HttpRequestException) { throw new InvalidDataException("无法连接 Tavily，请检查网络或代理设置。"); }
        catch(OperationCanceledException) when(!token.IsCancellationRequested) { throw new InvalidDataException("Tavily 搜索超时，请重试。"); }
    }
    private static string? Text(JsonElement item,string name,int limit)
    {
        if(!item.TryGetProperty(name,out var value) || value.ValueKind!=JsonValueKind.String) return null;
        var text=value.GetString()!; return text.Length>limit?text[..limit]:text;
    }
}
