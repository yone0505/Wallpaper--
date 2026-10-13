using Wallpaper.Core;

namespace Wallpaper.App;

internal static class DesktopHost
{
    internal static nint Find()
    {
        var nodes = new List<DesktopNode>();
        Native.EnumWindows((h, _) =>
        {
            nodes.Add(new((long)h, 0, Native.Class(h)));
            Native.EnumChildWindows(h, (c, _) => { nodes.Add(new((long)c,(long)Native.GetParent(c),Native.Class(c))); return true; }, 0);
            return true;
        }, 0);
        return (nint)DesktopTopology.Select(nodes);
    }
    internal static nint Attach(nint window, Rectangle bounds)
    {
        var host = Find();
        if (host == 0)
        {
            var progman = Native.FindWindow("Progman", null);
            if (progman != 0) Native.SendMessageTimeout(progman,0x052C,0,0,2,1000,out _);
            host=Find();
        }
        if (host == 0) throw new InvalidOperationException("未找到 Explorer 桌面背景层。请重新打开资源管理器后再应用壁纸。");
        var style=(long)Native.GetWindowLongPtr(window,-16);
        Native.SetWindowLongPtr(window,-16,(nint)((style | 0x40000000L) & ~0x80000000L));
        Native.SetParent(window,host);
        if (Native.GetParent(window)!=host) throw new InvalidOperationException("无法附着桌面背景层，系统可能正在重启 Explorer。");
        Position(window,host,bounds); return host;
    }
    internal static void Position(nint window, nint host, Rectangle bounds)
    {
        var origin=new Native.Point { X=bounds.X,Y=bounds.Y }; Native.ScreenToClient(host,ref origin);
        if (!Native.SetWindowPos(window,0,origin.X,origin.Y,bounds.Width,bounds.Height,0x0010|0x0040|0x0020))
            throw new InvalidOperationException("无法设置桌面窗口尺寸。");
    }
    internal static bool ForegroundFullscreen(Screen screen)
    {
        var hwnd=Native.GetForegroundWindow();
        if (hwnd==0 || !Native.IsWindowVisible(hwnd) || Native.Class(hwnd) is "Progman" or "WorkerW" or "Shell_TrayWnd") return false;
        Native.GetWindowThreadProcessId(hwnd,out var pid);
        if (pid==(uint)Environment.ProcessId || !Native.GetWindowRect(hwnd,out var r)) return false;
        var b=screen.Bounds;
        return r.Left<=b.Left && r.Top<=b.Top && r.Right>=b.Right && r.Bottom>=b.Bottom;
    }
}
