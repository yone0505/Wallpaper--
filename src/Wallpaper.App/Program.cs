namespace Wallpaper.App;

internal sealed record LaunchOptions(string DataDirectory, int DebugPort, string[] Imports,Uri? TavilyTestEndpoint=null);

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WallpaperDesktop");
        var debug = 0; var imports = new List<string>(); string? testEndpoint=null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--data-dir" && i + 1 < args.Length) data = Path.GetFullPath(args[++i]);
            else if (args[i] == "--debug-port" && i + 1 < args.Length && int.TryParse(args[++i], out var port) && port is >= 1024 and <= 65535) debug = port;
            else if (args[i] == "--import" && i + 1 < args.Length) imports.Add(args[++i]);
            else if (args[i] == "--qa-tavily-url" && i + 1 < args.Length) testEndpoint=args[++i];
        }
        Uri? endpoint=null;
        if(testEndpoint is not null && (debug==0 || !Uri.TryCreate(testEndpoint,UriKind.Absolute,out endpoint) || !endpoint.IsLoopback || endpoint.Scheme!="http" || endpoint.UserInfo.Length>0 || endpoint.Query.Length>0))
        { MessageBox.Show("Tavily 测试地址仅用于启用调试端口的本机 HTTP 测试。","Wallpaper Desktop"); return; }
        var mutexName = "Local\\WallpaperDesktop-" + Wallpaper.Core.Library.Id(data);
        using var mutex = new Mutex(true, mutexName, out var first);
        if (!first) { MessageBox.Show("这个配置目录已有 Wallpaper Desktop 正在运行。请从托盘打开管理窗口。", "Wallpaper Desktop"); return; }
        try { Application.Run(new ManagerForm(new(data, debug, imports.ToArray(),endpoint))); }
        catch (Exception ex) { MessageBox.Show("启动失败：" + ex.Message + "\n请确认已安装 Microsoft Edge WebView2 Runtime。", "Wallpaper Desktop"); }
    }
}
