using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wallpaper.Core;

public static class Library
{
    public static string Id(string path) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(System.IO.Path.GetFullPath(path).ToUpperInvariant())))[..24].ToLowerInvariant();

    public static WallpaperItem Import(string path)
    {
        try
        {
            path = System.IO.Path.GetFullPath(path);
            if (Directory.Exists(path)) path = System.IO.Path.Combine(path, "project.json");
            if (!File.Exists(path)) throw new InvalidDataException("找不到资源文件，请重新选择。");
            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".json")
            {
                using var json = JsonDocument.Parse(File.ReadAllText(path));
                var doc = json.RootElement;
                var kind = doc.GetProperty("type").GetString();
                if (kind is not ("video" or "web")) throw new InvalidDataException("当前版本支持视频和网页；场景包的渲染器尚未完成。");
                var folder = System.IO.Path.GetDirectoryName(path)!;
                var entry = ChildPath(folder, doc.GetProperty("file").GetString() ?? "");
                var item = Import(entry);
                if (item.Kind != kind) throw new InvalidDataException("项目类型与入口文件不匹配。");
                string? preview = null;
                if (doc.TryGetProperty("preview", out var p) && p.ValueKind == JsonValueKind.String)
                {
                    var candidate = ChildPath(folder, p.GetString()!);
                    if (File.Exists(candidate) && System.IO.Path.GetExtension(candidate).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp") preview = candidate;
                }
                return item with { Title = doc.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String ? title.GetString()! : item.Title, Preview = preview };
            }
            var type = ext switch { ".mp4" or ".webm" or ".m4v" or ".mov" => "video", ".html" or ".htm" => "web", _ => throw new InvalidDataException("请选择 MP4、WebM、HTML 或视频/网页项目的 project.json。") };
            return new(Id(path), System.IO.Path.GetFileNameWithoutExtension(path), type, path);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or ArgumentException or InvalidOperationException)
        { throw new InvalidDataException("项目文件格式不正确，请选择有效的 video/web 项目。", ex); }
    }

    private static string ChildPath(string folder, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || System.IO.Path.IsPathRooted(relative)) throw new InvalidDataException("项目入口必须是项目目录内的相对路径。");
        var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(folder, relative));
        if (!full.StartsWith(System.IO.Path.GetFullPath(folder).TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("项目路径超出项目目录。");
        return full;
    }
}
