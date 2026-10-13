using System.Runtime.InteropServices;
using System.Text;

namespace Wallpaper.App;

internal static class Native
{
    internal delegate bool EnumCallback(nint hwnd, nint param);
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumCallback callback, nint param);
    [DllImport("user32.dll")] internal static extern bool EnumChildWindows(nint parent, EnumCallback callback, nint param);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern int GetClassName(nint hwnd, StringBuilder value, int size);
    [DllImport("user32.dll")] internal static extern nint GetParent(nint hwnd);
    [DllImport("user32.dll", SetLastError=true)] internal static extern nint SetParent(nint hwnd, nint parent);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] internal static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW", SetLastError=true)] internal static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern nint FindWindow(string className, string? title);
    [DllImport("user32.dll", SetLastError=true)] internal static extern nint SendMessageTimeout(nint hwnd,uint msg,nuint wParam,nint lParam,uint flags,uint timeout,out nuint result);
    [DllImport("user32.dll")] internal static extern bool ScreenToClient(nint hwnd, ref Point point);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint hwnd,out Rect rect);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd,out uint pid);
    [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
    [DllImport("user32.dll")] internal static extern nint SendMessage(nint hwnd,uint msg,nint wParam,nint lParam);
    internal static string Class(nint hwnd) { var b=new StringBuilder(128); GetClassName(hwnd,b,b.Capacity); return b.ToString(); }
}
