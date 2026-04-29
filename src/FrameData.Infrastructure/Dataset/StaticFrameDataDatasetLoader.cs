using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Datasets;
using FrameData.Domain.Media;
using FrameData.Domain.Moves;
using FrameData.Shared.Contracts;

namespace FrameData.Infrastructure.Dataset;

public sealed class StaticFrameDataDatasetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<StaticFrameDataDataset> LoadAsync(
        string activeDatasetPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(activeDatasetPath))
        {
            throw new ArgumentException("Active dataset path is required.", nameof(activeDatasetPath));
        }

        var datasetPath = Path.GetFullPath(activeDatasetPath);
        if (!Directory.Exists(datasetPath))
        {
            throw new DirectoryNotFoundException($"Static frame-data dataset was not found: {datasetPath}");
        }

        var manifestPath = Path.Combine(datasetPath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Static frame-data dataset manifest was not found.", manifestPath);
        }

        var manifest = await ReadJsonAsync<StaticDatasetManifest>(manifestPath, cancellationToken);
        ValidateManifest(manifest);

        var characters = new List<Character>();
        var moves = new List<Move>();
        var seenCharacterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifestCharacter in manifest.Characters.OrderBy(character => character.DisplayOrder))
        {
            if (!seenCharacterIds.Add(manifestCharacter.Id))
            {
                throw new InvalidDataException($"Duplicate character id in dataset manifest: {manifestCharacter.Id}.");
            }

            var documentPath = ResolveDatasetFile(datasetPath, manifestCharacter.File);
            var document = await ReadJsonAsync<StaticDatasetCharacterDocument>(documentPath, cancellationToken);
            if (!string.Equals(document.Character.Id, manifestCharacter.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Character file mismatch for manifest entry: {manifestCharacter.Id}.");
            }

            if (document.Moves.Count != manifestCharacter.MoveCount)
            {
                throw new InvalidDataException($"Move count mismatch for character {manifestCharacter.Id}.");
            }

            var character = ToDomain(document.Character);
            characters.Add(character);

            foreach (var move in document.Moves)
            {
                if (!string.Equals(move.CharacterId, character.Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Move {move.Id} belongs to {move.CharacterId}, not {character.Id}.");
                }

                moves.Add(ToDomain(move, character));
            }
        }

        if (characters.Count != manifest.CharacterCount || moves.Count != manifest.MoveCount)
        {
            throw new InvalidDataException("Dataset manifest counts do not match character file contents.");
        }

        return new StaticFrameDataDataset
        {
            DatasetId = manifest.DatasetId,
            GeneratedAt = manifest.GeneratedAt,
            SourceBaseUrl = manifest.SourceBaseUrl,
            Characters = characters,
            Moves = moves,
            MediaCount = manifest.MediaCount
        };
    }

    private static void ValidateManifest(StaticDatasetManifest manifest)
    {
        if (manifest.SchemaVersion != StaticDatasetManifest.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported static dataset schema version: {manifest.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(manifest.DatasetId))
        {
            throw new InvalidDataException("Static dataset manifest is missing a dataset id.");
        }

        if (manifest.CharacterCount != manifest.Characters.Count)
        {
            throw new InvalidDataException("Static dataset manifest character count does not match listed characters.");
        }
    }

    private static string ResolveDatasetFile(string datasetPath, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"Dataset file path must be relative: {relativePath}");
        }

        var fullPath = Path.GetFullPath(Path.Combine(datasetPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = datasetPath.EndsWith(Path.DirectorySeparatorChar)
            ? datasetPath
            : datasetPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Dataset file path escapes the dataset directory: {relativePath}");
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Static dataset character file was not found.", fullPath);
        }

        return fullPath;
    }

    private static Character ToDomain(StaticDatasetCharacter character)
        => new()
        {
            Id = character.Id,
            Game = character.Game,
            Name = character.Name,
            SourceCharacterId = character.SourceCharacterId,
            DisplayOrder = character.DisplayOrder,
            UpdatedAt = character.UpdatedAt,
            Aliases = character.Aliases
        };

    private static Move ToDomain(StaticDatasetMove move, Character character)
        => new()
        {
            Id = move.Id,
            CharacterId = move.CharacterId,
            Game = character.Game,
            CharacterName = character.Name,
            Section = move.Section,
            CanonicalName = move.CanonicalName,
            DisplayOrder = move.DisplayOrder,
            SourceMoveId = move.SourceMoveId,
            SourceHitboxPath = move.SourceHitboxPath,
            Motion = move.Motion,
            Damage = move.Damage,
            Stun = move.Stun,
            FrameData = new MoveFrameData
            {
                Startup = move.FrameData.Startup,
                Active = move.FrameData.Active,
                Recovery = move.FrameData.Recovery,
                OnHit = move.FrameData.OnHit,
                OnBlock = move.FrameData.OnBlock,
                FrameAdvantage = move.FrameData.FrameAdvantage,
                Notes = move.FrameData.Notes
            },
            Media = move.Media.Select(media => ToDomain(media, move.Id)).ToArray()
        };

    private static MoveImage ToDomain(StaticDatasetMoveMedia media, string moveId)
        => new()
        {
            Id = $"{moveId}:{media.Type}:{media.Path}",
            MoveId = moveId,
            ImageType = ParseEnum(media.Type, MoveImageType.RepresentativeActiveFrame),
            StoragePath = media.Path,
            SourceUrl = media.SourceUrl ?? string.Empty,
            SourceFrameImageUrl = media.SourceFrameImageUrl,
            SelectedFrame = media.SelectedFrame,
            SelectionStrategy = media.SelectionStrategy ?? RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
            ActiveHitboxArea = media.ActiveHitboxArea,
            OverlayHitboxes = media.OverlayHitboxes,
            FallbackReason = media.FallbackReason,
            CapturedAt = media.CapturedAt ?? DateTimeOffset.MinValue,
            CaptureStatus = ParseEnum(media.CaptureStatus, MoveImageCaptureStatus.Success)
        };

    private static T ParseEnum<T>(string? value, T fallback)
        where T : struct
        => Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException($"Could not deserialize static dataset JSON file: {path}");
    }
}
