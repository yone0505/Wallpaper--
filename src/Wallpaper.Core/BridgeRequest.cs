using System.Text.Json;

namespace Wallpaper.Core;

public sealed record BridgeRequest(int Id, string Method, JsonElement Args)
{
    private static readonly HashSet<string> Methods = ["state", "import", "importFolder", "apply", "stop", "pause", "remove", "settings", "playlist", "next", "window", "diagnostics"];
    public static BridgeRequest Parse(string value)
    {
        try
        {
            if (value.Length > 65536) throw new InvalidDataException("请求过大。");
            using var doc = JsonDocument.Parse(value);
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetInt32();
            var method = root.GetProperty("method").GetString();
            if (id <= 0 || method is null || !Methods.Contains(method)) throw new InvalidDataException("不支持的操作。");
            var args = root.GetProperty("args");
            if (args.ValueKind != JsonValueKind.Object) throw new InvalidDataException("操作参数不正确。");
            return new(id, method, args.Clone());
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new InvalidDataException("请求格式不正确。", ex); }
    }
}
