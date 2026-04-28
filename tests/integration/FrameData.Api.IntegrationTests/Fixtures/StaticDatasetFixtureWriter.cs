using System.Text.Json;
using FrameData.Shared.Contracts;

namespace FrameData.Api.IntegrationTests.Fixtures;

internal static class StaticDatasetFixtureWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task<string> CreateAsync(params StaticDatasetCharacterDocument[] characterDocuments)
    {
        var root = Path.Combine(Path.GetTempPath(), $"framedata-api-dataset-{Guid.NewGuid():N}");
        var charactersDirectory = Path.Combine(root, "characters");
        Directory.CreateDirectory(charactersDirectory);
        Directory.CreateDirectory(Path.Combine(root, "media"));

        var manifestCharacters = new List<StaticDatasetManifestCharacter>();
        foreach (var document in characterDocuments)
        {
            var fileName = $"{document.Character.Id}.json";
            await WriteJsonAsync(Path.Combine(charactersDirectory, fileName), document);
            manifestCharacters.Add(new StaticDatasetManifestCharacter
            {
                Id = document.Character.Id,
                Name = document.Character.Name,
                File = $"characters/{fileName}",
                SourceCharacterId = document.Character.SourceCharacterId,
                DisplayOrder = document.Character.DisplayOrder,
                MoveCount = document.Moves.Count
            });
        }

        await WriteJsonAsync(Path.Combine(root, "manifest.json"), new StaticDatasetManifest
        {
            DatasetId = $"test-{Guid.NewGuid():N}",
            GeneratedAt = DateTimeOffset.UtcNow,
            SourceBaseUrl = "http://example.test/source.php",
            CharacterCount = characterDocuments.Length,
            MoveCount = characterDocuments.Sum(document => document.Moves.Count),
            MediaCount = 0,
            Characters = manifestCharacters
        });

        return root;
    }

    public static StaticDatasetCharacterDocument Character(
        string id,
        string name,
        IReadOnlyList<string> aliases,
        params StaticDatasetMove[] moves)
        => new()
        {
            Character = new StaticDatasetCharacter
            {
                Id = id,
                Game = "sf3_3s",
                Name = name,
                SourceCharacterId = 1,
                DisplayOrder = 1,
                UpdatedAt = DateTimeOffset.UtcNow,
                Aliases = aliases
            },
            Moves = moves
        };

    public static StaticDatasetMove Move(
        string characterId,
        string canonicalName,
        int displayOrder = 1,
        string startup = "6",
        string? motion = null,
        string? damage = null,
        string? stun = null)
        => new()
        {
            Id = $"{characterId}-normals-{canonicalName}",
            CharacterId = characterId,
            Section = "Normals",
            CanonicalName = canonicalName,
            DisplayOrder = displayOrder,
            Motion = motion,
            Damage = damage,
            Stun = stun,
            FrameData = new StaticDatasetMoveFrameData
            {
                Startup = startup,
                Active = "3",
                Recovery = "17",
                OnHit = "+1",
                OnBlock = "-2"
            }
        };

    public static void Delete(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task WriteJsonAsync<T>(string path, T payload)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, payload, JsonOptions);
    }
}
