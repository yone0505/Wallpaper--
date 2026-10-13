using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Wallpaper.Core;

namespace Wallpaper.App;

internal sealed class WallpaperWindow : Form
{
    private readonly WebView2 view = new() { Dock=DockStyle.Fill, DefaultBackgroundColor=Color.Black };
    private readonly CoreWebView2Environment environment;
    private readonly WallpaperItem item;
    private bool loaded;
    private bool paused;
    private readonly PictureBox still=new() { Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.StretchImage,BackColor=Color.Black,Visible=false };
    private readonly SemaphoreSlim updateGate=new(1,1);
    private Settings settings;
    internal nint Host { get; private set; }
    internal string Monitor { get; }
    internal string ItemId => item.Id;
    internal string? Error { get; private set; }
    internal bool Ready => loaded;
    internal bool Paused => paused;
    internal bool AudioMuted => view.CoreWebView2?.IsMuted??settings.Muted;
    internal bool Suspended => view.CoreWebView2?.IsSuspended??false;
    internal string? PauseFrameHash { get; private set; }
    protected override bool ShowWithoutActivation => true;
    internal WallpaperWindow(CoreWebView2Environment env, WallpaperItem item, Screen screen, Settings settings)
    {
        this.environment=env; this.item=item; this.settings=settings; Monitor=screen.DeviceName;
        Text="Wallpaper Desktop renderer"; FormBorderStyle=FormBorderStyle.None; ShowInTaskbar=false;
        StartPosition=FormStartPosition.Manual; Bounds=screen.Bounds; BackColor=Color.Black;
        Controls.Add(view); Controls.Add(still);
    }
    internal async Task Start(Screen screen)
    {
        if (!File.Exists(item.Path)) throw new FileNotFoundException("资源已移动或删除，请重新导入。",item.Path);
        _=Handle;
        Host=DesktopHost.Attach(Handle,screen.Bounds); Show(); DesktopHost.Position(Handle,Host,screen.Bounds);
        await view.EnsureCoreWebView2Async(environment);
        var core=view.CoreWebView2;
        core.IsMuted=settings.Muted;
        core.Settings.AreHostObjectsAllowed=false; core.Settings.IsWebMessageEnabled=false;
        core.Settings.AreDefaultContextMenusEnabled=false; core.Settings.AreDevToolsEnabled=false;
        core.Settings.IsStatusBarEnabled=false; core.Settings.IsZoomControlEnabled=false;
        core.PermissionRequested+=(_,e)=>e.State=CoreWebView2PermissionState.Deny;
        core.NewWindowRequested+=(_,e)=>e.Handled=true;
        core.DownloadStarting+=(_,e)=>e.Cancel=true;
        core.ProcessFailed+=(_,e)=>Error="播放进程退出："+e.ProcessFailedKind;
        core.NavigationCompleted+=async (_,e)=>
        {
            if (!e.IsSuccess) { Error="无法加载壁纸："+e.WebErrorStatus; return; }
            loaded=true; await Update(settings,paused);
        };
        var folder=item.Root??Path.GetDirectoryName(item.Path)!;
        core.SetVirtualHostNameToFolderMapping("content.wallpaper.local",folder,CoreWebView2HostResourceAccessKind.DenyCors);
        var source="https://content.wallpaper.local/"+Library.ResourcePath(item);
        if (item.Kind=="video")
        {
            core.SetVirtualHostNameToFolderMapping("player.wallpaper.local",Path.Combine(AppContext.BaseDirectory,"ui"),CoreWebView2HostResourceAccessKind.DenyCors);
            core.Navigate("https://player.wallpaper.local/player.html?src="+Uri.EscapeDataString(source));
        }
        else core.Navigate(source);
    }
    internal async Task Update(Settings value, bool pause)
    {
        if (IsDisposed || view.IsDisposed) return;
        settings=value; paused=pause;
        if (view.CoreWebView2 is not null) view.CoreWebView2.IsMuted=value.Muted||pause;
        if (!loaded || view.CoreWebView2 is null) return;
        await updateGate.WaitAsync();
        try
        {
            if (IsDisposed || view.CoreWebView2 is null) return;
            if (view.CoreWebView2.IsSuspended) view.CoreWebView2.Resume();
            var json=JsonSerializer.Serialize(new { volume=settings.Volume/100.0, muted=settings.Muted,fit=settings.Fit,paused },StateStore.Json);
            await view.CoreWebView2.ExecuteScriptAsync($"window.wallpaperPlayback = {json}; document.querySelectorAll('video,audio').forEach(m=>{{m.volume=wallpaperPlayback.volume;m.muted=wallpaperPlayback.muted;if(m.tagName==='VIDEO')m.style.objectFit=wallpaperPlayback.fit;if(wallpaperPlayback.paused)m.pause();else m.play().catch(()=>{{}});}}); window.dispatchEvent(new CustomEvent('wallpaper-playback',{{detail:wallpaperPlayback}}));");
            if (item.Kind=="web" && paused)
            {
                if (still.Image is null)
                {
                    using var capture=new MemoryStream();
                    await view.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,capture);
                    PauseFrameHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(capture.ToArray()));
                    capture.Position=0; using var image=Image.FromStream(capture); still.Image=new Bitmap(image);
                }
                still.Visible=true; still.BringToFront(); view.Visible=false;
                if (!await view.CoreWebView2.TrySuspendAsync()) Error="网页未能完全休眠；画面已暂停。";
            }
            else
            {
                view.Visible=true; view.BringToFront(); still.Visible=false; still.Image?.Dispose(); still.Image=null; PauseFrameHash=null;
            }
        }
        catch (Exception ex) { Error="更新播放状态失败："+ex.Message; }
        finally { updateGate.Release(); }
    }
    internal bool Reattach(Screen screen)
    {
        if (Native.IsWindow(Host) && Native.GetParent(Handle)==Host) { DesktopHost.Position(Handle,Host,screen.Bounds); return false; }
        Host=DesktopHost.Attach(Handle,screen.Bounds); return true;
    }
    protected override void Dispose(bool disposing) { if (disposing) { still.Image?.Dispose(); still.Image=null; still.Dispose(); view.Dispose(); } base.Dispose(disposing); }
}
