namespace Wallpaper.Core;

// Controller operations run on the WinForms UI thread; awaits may interleave them.
public sealed class OperationVersions
{
    private readonly Dictionary<string, long> versions=[];
    private long serial;
    private bool closed;
    public long Begin(string key)
    {
        ObjectDisposedException.ThrowIf(closed,this);
        return versions[key]=++serial;
    }
    public bool IsCurrent(string key,long version) => !closed && versions.GetValueOrDefault(key)==version;
    public void Cancel(string key) => versions.Remove(key);
    public void Close() { closed=true; versions.Clear(); }
}
