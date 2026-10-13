using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Wallpaper.Core;

namespace Wallpaper.App;

internal sealed class AppController : IDisposable
{
    private readonly StateStore store;
    private readonly Dictionary<string, WallpaperWindow> windows = [];
    private readonly HashSet<WallpaperWindow> pending=[];
    private readonly OperationVersions operations=new();
    private readonly CoreWebView2Environment wallpaperEnvironment;
    private readonly System.Windows.Forms.Timer timer=new() { Interval=1000 };
    private bool ticking;
    private DateTime nextChange=DateTime.UtcNow;
    internal AppState State { get; }
    internal string? Warning { get; private set; }
    internal bool ManualPause { get; private set; }
    internal event Action? Changed;

    internal AppController(StateStore store, CoreWebView2Environment environment, IEnumerable<string> imports)
    {
        this.store=store; wallpaperEnvironment=environment;
        var firstRun=!File.Exists(Path.Combine(store.DirectoryPath,"state.json"));
        var loaded=store.Load(); State=loaded.State; Warning=loaded.Warning;
        if (firstRun && State.Items.Count==0)
            foreach (var folder in new[] { "aurora", "orbit" }) State.Add(Library.Import(Path.Combine(AppContext.BaseDirectory,"demos",folder,"project.json")));
        foreach (var path in imports) { try { State.Add(Library.Import(path)); } catch (Exception ex) { Warning=ex.Message; } }
        store.Save(State); nextChange=DateTime.UtcNow.AddSeconds(State.Settings.IntervalSeconds);
        timer.Tick+=async (_,_)=>await Tick(); timer.Start();
    }
    internal async Task Restore()
    {
        foreach (var entry in State.Assignments.ToArray())
        {
            if (!Screen.AllScreens.Any(x=>x.DeviceName==entry.Key)) continue;
            try { await Apply(entry.Value,entry.Key); } catch (Exception ex) { Warning=ex.Message; }
        }
        Changed?.Invoke();
    }
    internal object Snapshot() => new
    {
        items=State.Items, settings=State.Settings, assignments=State.Assignments, playlist=State.Playlist,
        monitors=Screen.AllScreens.Select((s,i)=>new { id=s.DeviceName,name=$"显示器 {i+1} · {s.Bounds.Width} × {s.Bounds.Height}",primary=s.Primary }),
        playback=windows.Values.Select(w=>new { monitor=w.Monitor,id=w.ItemId,paused=w.Paused,ready=w.Ready,error=w.Error }),
        manualPause=ManualPause, warning=Warning
    };
    internal object Diagnostics() => new
    {
        processId=Environment.ProcessId,
        windows=windows.Values.Select(w=>new { monitor=w.Monitor,id=w.ItemId,hwnd=(long)w.Handle,parent=(long)Native.GetParent(w.Handle),host=(long)w.Host,attached=Native.GetParent(w.Handle)==w.Host,ready=w.Ready,paused=w.Paused,audioMuted=w.AudioMuted,suspended=w.Suspended,pauseFrameHash=w.PauseFrameHash,error=w.Error }),
        dataDirectory=store.DirectoryPath
    };
    internal int Import(IEnumerable<string> paths)
    {
        var count=0; var errors=new List<string>();
        foreach (var path in paths)
        {
            try { if (State.Add(Library.Import(path))) count++; }
            catch (Exception ex) { errors.Add(Path.GetFileName(path)+"："+ex.Message); }
        }
        Warning=errors.Count>0?string.Join("\n",errors.Take(5)):null;
        Save(); return count;
    }
    internal async Task Apply(string id,string monitor)
    {
        var item=State.Items.FirstOrDefault(x=>x.Id==id)??throw new InvalidDataException("资源不在本地库中。");
        var screen=Screen.AllScreens.FirstOrDefault(x=>x.DeviceName==monitor)??throw new InvalidDataException("显示器已断开，请重新选择。");
        var version=operations.Begin(monitor);
        var candidate=new WallpaperWindow(wallpaperEnvironment,item,screen,State.Settings);
        pending.Add(candidate);
        try { await candidate.Start(screen); }
        catch { candidate.Dispose(); throw; }
        finally { pending.Remove(candidate); }
        if (!operations.IsCurrent(monitor,version)) { candidate.Dispose(); return; }
        if (windows.Remove(monitor,out var old)) { old.Close(); old.Dispose(); }
        windows[monitor]=candidate; State.Assignments[monitor]=id;
        await candidate.Update(State.Settings,ManualPause||PlaybackPolicy.ShouldPause(DesktopHost.ForegroundFullscreen(screen),State.Settings.PauseOnFullscreen));
        Save();
    }
    internal void Stop(string monitor)
    {
        operations.Cancel(monitor);
        foreach (var candidate in pending.Where(x=>x.Monitor==monitor).ToArray()) { pending.Remove(candidate); candidate.Dispose(); }
        if (windows.Remove(monitor,out var old)) { old.Close(); old.Dispose(); }
        State.Assignments.Remove(monitor); Save();
    }
    internal async Task Pause()
    {
        ManualPause=!ManualPause; await UpdateWindows(); Changed?.Invoke();
    }
    internal void Remove(string id)
    {
        foreach (var monitor in pending.Where(x=>x.ItemId==id).Select(x=>x.Monitor).Distinct().ToArray()) Stop(monitor);
        foreach (var monitor in windows.Where(x=>x.Value.ItemId==id).Select(x=>x.Key).ToArray()) Stop(monitor);
        State.Remove(id); Save();
    }
    internal async Task SetSettings(JsonElement value)
    {
        var settings=value.Deserialize<Settings>(StateStore.Json)??throw new InvalidDataException("设置格式不正确。");
        settings.Normalize(); State.Settings=settings; nextChange=DateTime.UtcNow.AddSeconds(settings.IntervalSeconds);
        await UpdateWindows(); Save();
    }
    internal void SetPlaylist(string[] ids)
    {
        var valid=State.Items.Select(x=>x.Id).ToHashSet();
        if (ids.Any(id=>!valid.Contains(id))) throw new InvalidDataException("播放列表含有已移除的资源。");
        State.Playlist=ids.Distinct().ToList(); Save();
    }
    internal async Task Next(string monitor)
    {
        var current=State.Assignments.GetValueOrDefault(monitor);
        var next=Playlist.Next(State.Playlist,current);
        if (next is not null) await Apply(next,monitor);
        nextChange=DateTime.UtcNow.AddSeconds(State.Settings.IntervalSeconds);
    }
    private async Task UpdateWindows()
    {
        foreach (var pair in windows.ToArray())
        {
            var screen=Screen.AllScreens.FirstOrDefault(x=>x.DeviceName==pair.Key);
            if (screen is null) { Stop(pair.Key); continue; }
            await pair.Value.Update(State.Settings,ManualPause||PlaybackPolicy.ShouldPause(DesktopHost.ForegroundFullscreen(screen),State.Settings.PauseOnFullscreen));
        }
    }
    private async Task Tick()
    {
        if (ticking) return; ticking=true;
        try
        {
            var statusChanged=false;
            foreach (var pair in windows.ToArray())
            {
                var screen=Screen.AllScreens.FirstOrDefault(x=>x.DeviceName==pair.Key);
                if (screen is null) { Stop(pair.Key); continue; }
                try { statusChanged|=pair.Value.Reattach(screen); }
                catch (Exception ex) { Warning=ex.Message; continue; }
                var pause=ManualPause||PlaybackPolicy.ShouldPause(DesktopHost.ForegroundFullscreen(screen),State.Settings.PauseOnFullscreen);
                if (pause!=pair.Value.Paused) { await pair.Value.Update(State.Settings,pause); statusChanged=true; }
            }
            if (!ManualPause && State.Settings.PlaylistEnabled && DateTime.UtcNow>=nextChange)
            {
                foreach (var pair in windows.ToArray()) if (!pair.Value.Paused) await Next(pair.Key);
                nextChange=DateTime.UtcNow.AddSeconds(State.Settings.IntervalSeconds);
            }
            if (statusChanged) Changed?.Invoke();
        }
        catch (Exception ex) { Warning=ex.Message; Changed?.Invoke(); }
        finally { ticking=false; }
    }
    private void Save() { store.Save(State); Changed?.Invoke(); }
    public void Dispose()
    {
        operations.Close();
        timer.Stop(); timer.Dispose();
        foreach (var candidate in pending) candidate.Dispose();
        pending.Clear();
        foreach (var window in windows.Values) { window.Close(); window.Dispose(); }
        windows.Clear();
    }
}
