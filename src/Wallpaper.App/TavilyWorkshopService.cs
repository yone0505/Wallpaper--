using Wallpaper.Core;

namespace Wallpaper.App;

internal sealed class TavilyWorkshopService : IDisposable
{
    private readonly TavilyKeyStore keys;
    private readonly HttpClient http=new(new HttpClientHandler { AllowAutoRedirect=false }) { Timeout=TimeSpan.FromSeconds(30) };
    private readonly TavilyWorkshopClient client;
    private readonly CancellationTokenSource lifetime=new();
    private CancellationTokenSource? active;
    internal bool TestMode { get; }
    internal TavilyWorkshopService(string dataDirectory,Uri? testEndpoint)
    {
        keys=new("WallpaperDesktop/Tavily/"+Library.Id(dataDirectory));
        client=new(http,testEndpoint); TestMode=testEndpoint is not null;
    }
    internal object Status() => new { key=keys.Status(),testMode=TestMode };
    internal object SaveKey(string key) { keys.Save(key); return Status(); }
    internal object ClearKey() { keys.Clear(); return Status(); }
    internal async Task<WorkshopSearchPage> Search(string query)
    {
        Cancel();
        using var owner=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        active=owner;
        try { return await client.SearchAsync(query,keys.Read(),owner.Token); }
        finally { if(ReferenceEquals(active,owner))active=null; }
    }
    internal void Cancel() => active?.Cancel();
    public void Dispose() { lifetime.Cancel(); http.Dispose(); lifetime.Dispose(); }
}
