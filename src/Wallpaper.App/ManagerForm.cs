using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Wallpaper.Core;

namespace Wallpaper.App;

internal sealed class ManagerForm : Form
{
    private readonly LaunchOptions options;
    private readonly WebView2 view=new() { Dock=DockStyle.Fill,DefaultBackgroundColor=Color.FromArgb(34,34,34) };
    private readonly NotifyIcon tray=new() { Icon=SystemIcons.Application,Text="Wallpaper Desktop" };
    private AppController? controller;
    private bool exiting;
    private readonly HashSet<string> mediaHosts=[];
    internal ManagerForm(LaunchOptions options)
    {
        this.options=options; Text="Wallpaper Desktop"; BackColor=Color.FromArgb(34,34,34);
        FormBorderStyle=FormBorderStyle.None; StartPosition=FormStartPosition.CenterScreen;
        Size=new(1240,820); MinimumSize=new(850,600); Controls.Add(view);
        AllowDrop=true;
        DragEnter+=(_,e)=> { if (e.Data?.GetDataPresent(DataFormats.FileDrop)==true) e.Effect=DragDropEffects.Copy; };
        DragDrop+=(_,e)=> { if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) controller?.Import(paths); };
        var menu=new ContextMenuStrip(); menu.Items.Add("打开管理窗口",null,(_,_)=>OpenManager());
        menu.Items.Add("暂停 / 继续",null,async (_,_)=> { if (controller is not null) await controller.Pause(); });
        menu.Items.Add("停止全部壁纸",null,(_,_)=> { if (controller is not null) foreach (var screen in Screen.AllScreens) controller.Stop(screen.DeviceName); });
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("退出",null,(_,_)=>Exit());
        tray.ContextMenuStrip=menu; tray.DoubleClick+=(_,_)=>OpenManager(); tray.Visible=true;
        Shown+=async (_,_)=>await Initialize();
        FormClosing+=(_,e)=> { if (!exiting) { e.Cancel=true; Hide(); tray.ShowBalloonTip(2500,"Wallpaper Desktop","壁纸继续运行。双击托盘图标打开管理窗口，或从托盘退出。",ToolTipIcon.Info); } };
    }
    private async Task Initialize()
    {
        try
        {
            Directory.CreateDirectory(options.DataDirectory);
            var args="--autoplay-policy=no-user-gesture-required"+(options.DebugPort>0?$" --remote-debugging-port={options.DebugPort}":"");
            var uiEnv=await CoreWebView2Environment.CreateAsync(null,Path.Combine(options.DataDirectory,"browser-manager"),new(args));
            var playerArgs="--autoplay-policy=no-user-gesture-required"+(options.DebugPort>0?$" --remote-debugging-port={(options.DebugPort==65535?65534:options.DebugPort+1)}":"");
            var playerEnv=await CoreWebView2Environment.CreateAsync(null,Path.Combine(options.DataDirectory,"browser-player"),new(playerArgs));
            controller=new(new StateStore(options.DataDirectory),playerEnv,options.Imports);
            await view.EnsureCoreWebView2Async(uiEnv);
            var core=view.CoreWebView2;
            core.Settings.AreHostObjectsAllowed=false; core.Settings.AreDefaultContextMenusEnabled=false;
            core.Settings.IsStatusBarEnabled=false; core.Settings.AreDevToolsEnabled=options.DebugPort>0;
            core.Settings.IsZoomControlEnabled=false;
            core.SetVirtualHostNameToFolderMapping("manager.wallpaper.local",Path.Combine(AppContext.BaseDirectory,"ui"),CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting+=(_,e)=> { if (!e.Uri.StartsWith("https://manager.wallpaper.local/",StringComparison.Ordinal)) e.Cancel=true; };
            core.NewWindowRequested+=(_,e)=>e.Handled=true;
            core.DownloadStarting+=(_,e)=>e.Cancel=true;
            core.PermissionRequested+=(_,e)=>e.State=CoreWebView2PermissionState.Deny;
            core.WebMessageReceived+=async (_,e)=>await Message(e);
            controller.Changed+=PushState;
            core.Navigate("https://manager.wallpaper.local/index.html");
            await controller.Restore();
        }
        catch (Exception ex) { MessageBox.Show(this,"初始化失败："+ex.Message,"Wallpaper Desktop"); Exit(); }
    }
    private object Snapshot()
    {
        foreach (var item in controller!.State.Items)
        {
            var host="media-"+item.Id+".wallpaper.local";
            var root=item.Root??Path.GetDirectoryName(item.Path)!;
            if (mediaHosts.Add(host) && Directory.Exists(root))
                view.CoreWebView2.SetVirtualHostNameToFolderMapping(host,root,CoreWebView2HostResourceAccessKind.DenyCors);
        }
        var s=controller.Snapshot();
        return new { state=s, thumbnails=controller.State.Items.ToDictionary(x=>x.Id,x=>new { source="https://media-"+x.Id+".wallpaper.local/"+Library.ResourcePath(x,true),hasPreview=x.Preview is not null }) };
    }
    private void PushState()
    {
        if (view.CoreWebView2 is not null && !IsDisposed) view.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { eventName="state",result=Snapshot() },StateStore.Json));
    }
    private async Task Message(CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.Source!="https://manager.wallpaper.local/index.html" || controller is null) return;
        BridgeRequest? request=null;
        try
        {
            request=BridgeRequest.Parse(e.WebMessageAsJson); object? result=null;
            string Arg(string key)=>request.Args.GetProperty(key).GetString()??throw new InvalidDataException("参数不能为空。");
            switch (request.Method)
            {
                case "state": result=Snapshot(); break;
                case "diagnostics": result=controller.Diagnostics(); break;
                case "import":
                    using (var dialog=new OpenFileDialog { Multiselect=true,Title="选择视频、网页或 project.json",Filter="壁纸资源|*.mp4;*.webm;*.m4v;*.mov;*.html;*.htm;*.json|所有文件|*.*" })
                        result=dialog.ShowDialog(this)==DialogResult.OK?controller.Import(dialog.FileNames):0;
                    break;
                case "importFolder":
                    using (var dialog=new FolderBrowserDialog { Description="选择壁纸项目目录或资源库" })
                    {
                        if (dialog.ShowDialog(this)!=DialogResult.OK) { result=0; break; }
                        var folder=dialog.SelectedPath; var project=Path.Combine(folder,"project.json");
                        var paths=File.Exists(project)?[project]:Directory.EnumerateFiles(folder,"project.json",new EnumerationOptions { RecurseSubdirectories=true,IgnoreInaccessible=true,AttributesToSkip=FileAttributes.ReparsePoint,MaxRecursionDepth=3 }).Take(1000).ToArray();
                        if (paths.Length==0) paths=Directory.GetFiles(folder).Where(x=>Path.GetExtension(x).ToLowerInvariant() is ".mp4" or ".webm" or ".html" or ".htm").ToArray();
                        result=controller.Import(paths);
                    }
                    break;
                case "apply": await controller.Apply(Arg("id"),Arg("monitor")); break;
                case "stop": controller.Stop(Arg("monitor")); break;
                case "pause": await controller.Pause(); break;
                case "remove": controller.Remove(Arg("id")); break;
                case "settings": await controller.SetSettings(request.Args); break;
                case "playlist": controller.SetPlaylist(request.Args.GetProperty("ids").Deserialize<string[]>()??[]); break;
                case "next": await controller.Next(Arg("monitor")); break;
                case "window":
                    switch (Arg("action"))
                    {
                        case "minimize": Hide(); break;
                        case "maximize": WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized; break;
                        case "close": Hide(); break;
                        case "exit": BeginInvoke(Exit); break;
                        case "drag": Native.ReleaseCapture(); Native.SendMessage(Handle,0xA1,2,0); break;
                        default: throw new InvalidDataException("未知窗口操作。");
                    }
                    break;
            }
            coreReply(new { id=request.Id,result });
        }
        catch (Exception ex) { coreReply(new { id=request?.Id??0,error=ex.Message }); }
        void coreReply(object value) { if (view.CoreWebView2 is not null) view.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(value,StateStore.Json)); }
    }
    private void OpenManager() { Show(); WindowState=FormWindowState.Normal; Activate(); }
    private void Exit() { exiting=true; Close(); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { controller?.Dispose(); tray.Visible=false; tray.ContextMenuStrip?.Dispose(); tray.Dispose(); view.Dispose(); }
        base.Dispose(disposing);
    }
}
