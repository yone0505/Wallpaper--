namespace Wallpaper.Core;

public sealed record DesktopNode(long Handle, long Parent, string ClassName);

public static class DesktopTopology
{
    public static long Select(IReadOnlyList<DesktopNode> nodes)
    {
        var progman = nodes.FirstOrDefault(x => x.ClassName == "Progman");
        if (progman is not null && nodes.Any(x => x.Parent == progman.Handle && x.ClassName == "SHELLDLL_DefView"))
        {
            var modern = nodes.FirstOrDefault(x => x.Parent == progman.Handle && x.ClassName == "WorkerW");
            if (modern is not null) return modern.Handle;
        }
        var roots = nodes.Where(x => x.Parent == 0).ToArray();
        for (var i = 0; i < roots.Length; i++)
        {
            if (!nodes.Any(x => x.Parent == roots[i].Handle && x.ClassName == "SHELLDLL_DefView")) continue;
            for (var j = i + 1; j < roots.Length; j++) if (roots[j].ClassName == "WorkerW") return roots[j].Handle;
        }
        return 0;
    }
}
