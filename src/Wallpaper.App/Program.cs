namespace Wallpaper.App;

internal sealed record LaunchOptions(string DataDirectory, int DebugPort, string[] Imports);

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WallpaperDesktop");
        var debug = 0; var imports = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--data-dir" && i + 1 < args.Length) data = Path.GetFullPath(args[++i]);
            else if (args[i] == "--debug-port" && i + 1 < args.Length && int.TryParse(args[++i], out var port) && port is >= 1024 and <= 65535) debug = port;
            else if (args[i] == "--import" && i + 1 < args.Length) imports.Add(args[++i]);
        }
        var mutexName = "Local\\WallpaperDesktop-" + Wallpaper.Core.Library.Id(data);
        using var mutex = new Mutex(true, mutexName, out var first);
        if (!first) { MessageBox.Show("这个配置目录已有 Wallpaper Desktop 正在运行。请从托盘打开管理窗口。", "Wallpaper Desktop"); return; }
        try { Application.Run(new ManagerForm(new(data, debug, imports.ToArray()))); }
        catch (Exception ex) { MessageBox.Show("启动失败：" + ex.Message + "\n请确认已安装 Microsoft Edge WebView2 Runtime。", "Wallpaper Desktop"); }
    }
}
