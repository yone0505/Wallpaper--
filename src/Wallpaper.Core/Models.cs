namespace Wallpaper.Core;

public sealed record WallpaperItem(string Id, string Title, string Kind, string Path, string? Preview = null);

public sealed class Settings
{
    public int Volume { get; set; } = 30;
    public bool Muted { get; set; } = true;
    public string Fit { get; set; } = "cover";
    public bool PauseOnFullscreen { get; set; } = true;
    public bool PlaylistEnabled { get; set; }
    public int IntervalSeconds { get; set; } = 300;
    public void Normalize()
    {
        Volume = Math.Clamp(Volume, 0, 100);
        IntervalSeconds = Math.Clamp(IntervalSeconds, 10, 86400);
        if (Fit is not ("cover" or "contain" or "fill")) Fit = "cover";
    }
}

public sealed class AppState
{
    public int SchemaVersion { get; set; } = 1;
    public List<WallpaperItem> Items { get; set; } = [];
    public Dictionary<string, string> Assignments { get; set; } = [];
    public List<string> Playlist { get; set; } = [];
    public Settings Settings { get; set; } = new();
    public bool Add(WallpaperItem item)
    {
        if (Items.Any(x => x.Id == item.Id)) return false;
        Items.Add(item); return true;
    }
    public void Remove(string id)
    {
        Items.RemoveAll(x => x.Id == id);
        Playlist.RemoveAll(x => x == id);
        foreach (var monitor in Assignments.Where(x => x.Value == id).Select(x => x.Key).ToArray()) Assignments.Remove(monitor);
    }
    public void Normalize()
    {
        if (SchemaVersion != 1 || Items is null || Assignments is null || Playlist is null || Settings is null)
            throw new InvalidDataException("配置结构不支持");
        if (Items.Any(x => x is null || string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.Path) || x.Kind is not ("video" or "web")))
            throw new InvalidDataException("配置资源记录不完整");
        Items = Items.DistinctBy(x => x.Id).ToList();
        var ids = Items.Select(x => x.Id).ToHashSet();
        Playlist = Playlist.Where(ids.Contains).Distinct().ToList();
        foreach (var key in Assignments.Where(x => !ids.Contains(x.Value)).Select(x => x.Key).ToArray()) Assignments.Remove(key);
        Settings.Normalize();
    }
}
