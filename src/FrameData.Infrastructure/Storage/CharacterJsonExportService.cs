using System.Text.Json;

namespace FrameData.Infrastructure.Storage;

public sealed class CharacterJsonExportService
{
    public async Task ExportAsync<T>(string path, T payload, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, payload, cancellationToken: cancellationToken);
    }
}
