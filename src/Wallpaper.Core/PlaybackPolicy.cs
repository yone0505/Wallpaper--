namespace Wallpaper.Core;

public static class PlaybackPolicy
{
    public static bool ShouldPause(bool fullscreen, bool enabled) => fullscreen && enabled;
}

public static class Playlist
{
    public static string? Next(IReadOnlyList<string> ids, string? current)
    {
        if (ids.Count == 0) return null;
        var index = -1;
        for (var i = 0; i < ids.Count; i++) if (ids[i] == current) { index = i; break; }
        return ids[(index + 1) % ids.Count];
    }
}
