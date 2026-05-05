using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Datasets;
using FrameData.Domain.Media;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Dataset;
using FrameData.Ingestion.Media;
using FrameData.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrameData.Ingestion.Publishing;

public sealed class StaticDatasetPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly StaticDatasetPublisherOptions _options;
    private readonly StaticFrameDataDatasetLoader _loader;
    private readonly ILogger<StaticDatasetPublisher> _logger;

    public StaticDatasetPublisher(
        StaticDatasetPublisherOptions options,
        StaticFrameDataDatasetLoader? loader = null,
        ILogger<StaticDatasetPublisher>? logger = null)
    {
        _options = options;
        _loader = loader ?? new StaticFrameDataDatasetLoader();
        _logger = logger ?? NullLogger<StaticDatasetPublisher>.Instance;
    }

    public async Task<StaticDatasetManifest> PublishAsync(
        IReadOnlyCollection<Character> characters,
        IReadOnlyCollection<Move> moves,
        string? sourceBaseUrl = null,
        CancellationToken cancellationToken = default)
        => await PublishAsync(characters, moves, [], sourceBaseUrl, cancellationToken);

    public async Task<StaticDatasetManifest> PublishAsync(
        IReadOnlyCollection<Character> characters,
        IReadOnlyCollection<Move> moves,
        IReadOnlyCollection<MoveImageDatasetAsset> mediaAssets,
        string? sourceBaseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = _options.Validate();
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
        }

        var datasetId = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var root = Path.GetFullPath(_options.DatasetRoot);
        var stagingDirectory = Path.Combine(root, ".staging", datasetId);
        var versionDirectory = Path.Combine(root, "versions", datasetId);

        if (Directory.Exists(stagingDirectory))
        {
            Directory.Delete(stagingDirectory, recursive: true);
        }

        Directory.CreateDirectory(stagingDirectory);
        Directory.CreateDirectory(Path.Combine(root, "versions"));

        try
        {
            var manifest = await WriteDatasetAsync(
                stagingDirectory,
                datasetId,
                characters,
                moves,
                mediaAssets,
                sourceBaseUrl,
                cancellationToken);

            await _loader.LoadAsync(stagingDirectory, cancellationToken);
            Directory.Move(stagingDirectory, versionDirectory);
            PublishActiveDataset(versionDirectory);

            _logger.LogInformation(
                "Published static frame-data dataset {DatasetId} with {CharacterCount} character(s) and {MoveCount} move(s) to {ActiveDatasetPath}.",
                manifest.DatasetId,
                manifest.CharacterCount,
                manifest.MoveCount,
                _options.ActiveDatasetPath);

            return manifest;
        }
        catch
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, recursive: true);
            }

            throw;
        }
    }

    private async Task<StaticDatasetManifest> WriteDatasetAsync(
        string datasetDirectory,
        string datasetId,
        IReadOnlyCollection<Character> characters,
        IReadOnlyCollection<Move> moves,
        IReadOnlyCollection<MoveImageDatasetAsset> mediaAssets,
        string? sourceBaseUrl,
        CancellationToken cancellationToken)
    {
        var charactersDirectory = Path.Combine(datasetDirectory, "characters");
        Directory.CreateDirectory(charactersDirectory);
        Directory.CreateDirectory(Path.Combine(datasetDirectory, "media"));

        if (mediaAssets.Count == 0)
        {
            _logger.LogInformation(
                "Static dataset {DatasetId}: no representative media assets were provided for publishing.",
                datasetId);
        }
        else
        {
            _logger.LogInformation(
                "Static dataset {DatasetId}: writing {MediaAssetCount} representative media asset(s) into the media tree.",
                datasetId,
                mediaAssets.Count);
        }

        foreach (var asset in mediaAssets)
        {
            await WriteMediaAssetAsync(datasetDirectory, asset, cancellationToken);
            _logger.LogInformation(
                "Static dataset {DatasetId}: wrote representative media asset for {MoveId}. StoragePath={StoragePath}; Status={CaptureStatus}; Bytes={ByteCount}.",
                datasetId,
                asset.Image.MoveId,
                asset.Image.StoragePath,
                asset.Image.CaptureStatus,
                asset.Content.Length);
        }

        var mediaByMoveId = mediaAssets
            .Select(asset => asset.Image)
            .GroupBy(image => image.MoveId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MoveImage>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var manifestCharacters = new List<StaticDatasetManifestCharacter>();
        foreach (var character in characters.OrderBy(character => character.DisplayOrder).ThenBy(character => character.Id))
        {
            var characterMoves = moves
                .Where(move => string.Equals(move.CharacterId, character.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(move => move.DisplayOrder ?? int.MaxValue)
                .ThenBy(move => move.Section, StringComparer.Ordinal)
                .ThenBy(move => move.CanonicalName, StringComparer.Ordinal)
                .ToArray();

            var fileName = $"{character.Id}.json";
            var document = new StaticDatasetCharacterDocument
            {
                Character = FromDomain(character),
                Moves = characterMoves
                    .Select(move => FromDomain(
                        move,
                        mediaByMoveId.TryGetValue(move.Id, out var moveMedia)
                            ? moveMedia
                            : move.Media))
                    .ToArray()
            };

            await WriteJsonAsync(Path.Combine(charactersDirectory, fileName), document, cancellationToken);
            manifestCharacters.Add(new StaticDatasetManifestCharacter
            {
                Id = character.Id,
                Name = character.Name,
                File = Path.Combine("characters", fileName).Replace('\\', '/'),
                SourceCharacterId = character.SourceCharacterId,
                DisplayOrder = character.DisplayOrder,
                MoveCount = characterMoves.Length
            });
        }

        var manifest = new StaticDatasetManifest
        {
            DatasetId = datasetId,
            GeneratedAt = DateTimeOffset.UtcNow,
            SourceBaseUrl = sourceBaseUrl,
            CharacterCount = characters.Count,
            MoveCount = moves.Count,
            MediaCount = mediaAssets.Count > 0 ? mediaAssets.Count : moves.Sum(move => move.Media.Count),
            Characters = manifestCharacters
        };

        await WriteJsonAsync(Path.Combine(datasetDirectory, "manifest.json"), manifest, cancellationToken);
        return manifest;
    }

    private void PublishActiveDataset(string versionDirectory)
    {
        var activePath = Path.GetFullPath(_options.ActiveDatasetPath);
        var activeParent = Path.GetDirectoryName(activePath)
            ?? throw new InvalidOperationException("Active dataset path must include a parent directory.");

        Directory.CreateDirectory(activeParent);
        if (Directory.Exists(activePath) && !IsDirectorySymlink(activePath))
        {
            ReplacePhysicalActiveDirectory(versionDirectory, activePath);
            return;
        }

        if (!Directory.Exists(activePath) && !File.Exists(activePath))
        {
            CreatePhysicalActiveDirectory(versionDirectory, activePath, activeParent);
            return;
        }

        var temporaryLink = Path.Combine(activeParent, $".{Path.GetFileName(activePath)}.{Guid.NewGuid():N}.tmp");
        var relativeTarget = Path.GetRelativePath(activeParent, versionDirectory);
        Directory.CreateSymbolicLink(temporaryLink, relativeTarget);

        string? previousTarget = null;
        if (Directory.Exists(activePath) || File.Exists(activePath))
        {
            previousTarget = new DirectoryInfo(activePath).LinkTarget;
            Directory.Delete(activePath);
        }

        try
        {
            Directory.Move(temporaryLink, activePath);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(previousTarget))
            {
                Directory.CreateSymbolicLink(activePath, previousTarget);
            }

            if (Directory.Exists(temporaryLink) || File.Exists(temporaryLink))
            {
                Directory.Delete(temporaryLink);
            }

            throw;
        }
    }

    private static bool IsDirectorySymlink(string path)
        => new DirectoryInfo(path).LinkTarget is not null;

    private static void CreatePhysicalActiveDirectory(string versionDirectory, string activePath, string activeParent)
    {
        var temporaryPath = Path.Combine(activeParent, $".{Path.GetFileName(activePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            CopyDirectory(versionDirectory, temporaryPath);
            Directory.Move(temporaryPath, activePath);
        }
        catch
        {
            if (Directory.Exists(temporaryPath))
            {
                Directory.Delete(temporaryPath, recursive: true);
            }

            throw;
        }
    }

    private static void ReplacePhysicalActiveDirectory(string versionDirectory, string activePath)
    {
        var previousPath = $"{activePath}.previous-{Guid.NewGuid():N}";
        Directory.Move(activePath, previousPath);

        try
        {
            CopyDirectory(versionDirectory, activePath);
            Directory.Delete(previousPath, recursive: true);
        }
        catch
        {
            if (Directory.Exists(activePath))
            {
                Directory.Delete(activePath, recursive: true);
            }

            Directory.Move(previousPath, activePath);
            throw;
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal));
        }
    }

    private static StaticDatasetCharacter FromDomain(Character character)
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

    private static StaticDatasetMove FromDomain(Move move, IReadOnlyList<MoveImage> media)
        => new()
        {
            Id = move.Id,
            CharacterId = move.CharacterId,
            Section = move.Section,
            CanonicalName = move.CanonicalName,
            DisplayOrder = move.DisplayOrder,
            SourceMoveId = move.SourceMoveId,
            SourceHitboxPath = move.SourceHitboxPath,
            Motion = move.Motion,
            Damage = move.Damage,
            Stun = move.Stun,
            FrameData = new StaticDatasetMoveFrameData
            {
                Startup = move.FrameData.Startup,
                Active = move.FrameData.Active,
                Recovery = move.FrameData.Recovery,
                OnHit = move.FrameData.OnHit,
                OnBlock = move.FrameData.OnBlock,
                OnCrouchingHit = move.FrameData.OnCrouchingHit,
                Notes = move.FrameData.Notes
            },
            Media = media.Select(FromDomain).ToArray()
        };

    private static StaticDatasetMoveMedia FromDomain(MoveImage image)
        => new()
        {
            Type = image.ImageType.ToString(),
            Path = image.StoragePath,
            SourceUrl = image.SourceUrl,
            SourceFrameImageUrl = image.SourceFrameImageUrl,
            SelectedFrame = image.SelectedFrame,
            SelectionStrategy = image.SelectionStrategy,
            ActiveHitboxArea = image.ActiveHitboxArea,
            OverlayHitboxes = image.OverlayHitboxes,
            FallbackReason = image.FallbackReason,
            CapturedAt = image.CapturedAt,
            CaptureStatus = image.CaptureStatus.ToString()
        };

    private static async Task WriteMediaAssetAsync(
        string datasetDirectory,
        MoveImageDatasetAsset asset,
        CancellationToken cancellationToken)
    {
        var destination = ResolveMediaPath(datasetDirectory, asset.Image.StoragePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        await File.WriteAllBytesAsync(destination, asset.Content, cancellationToken);
    }

    private static string ResolveMediaPath(string datasetDirectory, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"Media path must be relative: {relativePath}");
        }

        var root = Path.GetFullPath(datasetDirectory);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Media path escapes the dataset directory: {relativePath}");
        }

        if (!assetPathStartsWithMedia(relativePath))
        {
            throw new InvalidDataException($"Media path must be under media/: {relativePath}");
        }

        return fullPath;

        static bool assetPathStartsWithMedia(string path)
            => path.Replace('\\', '/').StartsWith("media/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteJsonAsync<T>(string path, T payload, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken);
    }
}
