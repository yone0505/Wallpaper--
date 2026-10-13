using System.Text.Json;

namespace Wallpaper.Core;

public sealed class StateStore(string directory)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    public (AppState State, string? Warning) Load()
    {
        var file = Path.Combine(DirectoryPath, "state.json");
        if (!File.Exists(file)) return (new(), null);
        try
        {
            var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(file), Json) ?? throw new InvalidDataException("空配置");
            state.Normalize(); return (state, null);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            var backup = Path.Combine(DirectoryPath, $"state.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.json");
            File.Move(file, backup);
            return (new(), "配置损坏，原文件已保留：" + backup);
        }
    }
    public void Save(AppState state)
    {
        state.Normalize(); Directory.CreateDirectory(DirectoryPath);
        var file = Path.Combine(DirectoryPath, "state.json"); var temp = file + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, state, Json); stream.Flush(true);
        }
        File.Move(temp, file, true);
    }
}
