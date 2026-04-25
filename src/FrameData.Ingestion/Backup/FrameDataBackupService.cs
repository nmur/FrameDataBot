using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Persistence.Repositories;

namespace FrameData.Ingestion.Backup;

public sealed class FrameDataBackupService
{
    private const int CurrentBackupVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly FrameDataDatasetRepository _datasetRepository;

    public FrameDataBackupService(FrameDataDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
    }

    public async Task<FrameDataBackupManifest> ExportAsync(string backupDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            throw new ArgumentException("Backup output directory is required.", nameof(backupDirectory));
        }

        var dataset = await _datasetRepository.GetAllAsync(cancellationToken);
        var charactersDirectory = Path.Combine(backupDirectory, "characters");
        Directory.CreateDirectory(charactersDirectory);

        var manifestCharacters = new List<FrameDataBackupManifestCharacter>();
        foreach (var character in dataset.Characters)
        {
            var characterMoves = dataset.Moves
                .Where(move => string.Equals(move.CharacterId, character.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(move => move.DisplayOrder ?? int.MaxValue)
                .ThenBy(move => move.Section, StringComparer.Ordinal)
                .ThenBy(move => move.CanonicalName, StringComparer.Ordinal)
                .ToArray();

            var fileName = $"{character.Id}.json";
            var document = new FrameDataCharacterBackupDocument
            {
                Character = FrameDataCharacterBackup.FromDomain(character),
                Moves = characterMoves.Select(FrameDataMoveBackup.FromDomain).ToArray()
            };

            await WriteJsonAsync(Path.Combine(charactersDirectory, fileName), document, cancellationToken);
            manifestCharacters.Add(new FrameDataBackupManifestCharacter
            {
                Id = character.Id,
                Name = character.Name,
                File = Path.Combine("characters", fileName).Replace('\\', '/'),
                MoveCount = characterMoves.Length
            });
        }

        var manifest = new FrameDataBackupManifest
        {
            BackupVersion = CurrentBackupVersion,
            ExportedAt = DateTimeOffset.UtcNow,
            CharacterCount = dataset.Characters.Count,
            MoveCount = dataset.Moves.Count,
            Characters = manifestCharacters
        };

        await WriteJsonAsync(Path.Combine(backupDirectory, "manifest.json"), manifest, cancellationToken);
        return manifest;
    }

    public async Task<FrameDataBackupManifest> ImportAsync(string backupDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            throw new ArgumentException("Backup input directory is required.", nameof(backupDirectory));
        }

        var manifestPath = Path.Combine(backupDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Backup manifest was not found.", manifestPath);
        }

        var manifest = await ReadJsonAsync<FrameDataBackupManifest>(manifestPath, cancellationToken);
        if (manifest.BackupVersion != CurrentBackupVersion)
        {
            throw new InvalidDataException($"Unsupported backup version: {manifest.BackupVersion}.");
        }

        var characters = new List<Character>();
        var moves = new List<Move>();
        var seenCharacterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifestCharacter in manifest.Characters)
        {
            if (!seenCharacterIds.Add(manifestCharacter.Id))
            {
                throw new InvalidDataException($"Duplicate character in backup manifest: {manifestCharacter.Id}.");
            }

            var documentPath = Path.Combine(backupDirectory, manifestCharacter.File.Replace('/', Path.DirectorySeparatorChar));
            var document = await ReadJsonAsync<FrameDataCharacterBackupDocument>(documentPath, cancellationToken);
            if (!string.Equals(document.Character.Id, manifestCharacter.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Character file mismatch for manifest entry: {manifestCharacter.Id}.");
            }

            characters.Add(document.Character.ToDomain());
            foreach (var move in document.Moves)
            {
                if (!string.Equals(move.CharacterId, document.Character.Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Move {move.Id} belongs to {move.CharacterId}, not {document.Character.Id}.");
                }

                moves.Add(move.ToDomain(document.Character.Name, document.Character.Game));
            }
        }

        if (characters.Count != manifest.CharacterCount || moves.Count != manifest.MoveCount)
        {
            throw new InvalidDataException("Backup manifest counts do not match character file contents.");
        }

        await _datasetRepository.ReplaceAsync(characters, moves, cancellationToken);
        return manifest;
    }

    private static async Task WriteJsonAsync<T>(string path, T payload, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken);
    }

    private static async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException($"Could not deserialize JSON backup file: {path}");
    }
}

public sealed class FrameDataBackupManifest
{
    public int BackupVersion { get; init; }
    public DateTimeOffset ExportedAt { get; init; }
    public int CharacterCount { get; init; }
    public int MoveCount { get; init; }
    public IReadOnlyList<FrameDataBackupManifestCharacter> Characters { get; init; } = [];
}

public sealed class FrameDataBackupManifestCharacter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string File { get; init; }
    public int MoveCount { get; init; }
}

public sealed class FrameDataCharacterBackupDocument
{
    public required FrameDataCharacterBackup Character { get; init; }
    public IReadOnlyList<FrameDataMoveBackup> Moves { get; init; } = [];
}

public sealed class FrameDataCharacterBackup
{
    public required string Id { get; init; }
    public required string Game { get; init; }
    public required string Name { get; init; }
    public int? SourceCharacterId { get; init; }
    public int DisplayOrder { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];

    public static FrameDataCharacterBackup FromDomain(Character character)
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

    public Character ToDomain()
        => new()
        {
            Id = Id,
            Game = Game,
            Name = Name,
            SourceCharacterId = SourceCharacterId,
            DisplayOrder = DisplayOrder,
            UpdatedAt = UpdatedAt,
            Aliases = Aliases
        };
}

public sealed class FrameDataMoveBackup
{
    public required string Id { get; init; }
    public required string CharacterId { get; init; }
    public required string Section { get; init; }
    public required string CanonicalName { get; init; }
    public int? DisplayOrder { get; init; }
    public string? SourceMoveId { get; init; }
    public required FrameDataMoveFrameBackup FrameData { get; init; }

    public static FrameDataMoveBackup FromDomain(Move move)
        => new()
        {
            Id = move.Id,
            CharacterId = move.CharacterId,
            Section = move.Section,
            CanonicalName = move.CanonicalName,
            DisplayOrder = move.DisplayOrder,
            SourceMoveId = move.SourceMoveId,
            FrameData = FrameDataMoveFrameBackup.FromDomain(move.FrameData)
        };

    public Move ToDomain(string characterName, string game)
        => new()
        {
            Id = Id,
            CharacterId = CharacterId,
            Game = game,
            CharacterName = characterName,
            Section = Section,
            CanonicalName = CanonicalName,
            DisplayOrder = DisplayOrder,
            SourceMoveId = SourceMoveId,
            FrameData = FrameData.ToDomain()
        };
}

public sealed class FrameDataMoveFrameBackup
{
    public string? Startup { get; init; }
    public string? Active { get; init; }
    public string? Recovery { get; init; }
    public string? OnHit { get; init; }
    public string? OnBlock { get; init; }
    public string? FrameAdvantage { get; init; }
    public string? Notes { get; init; }

    public static FrameDataMoveFrameBackup FromDomain(MoveFrameData frameData)
        => new()
        {
            Startup = frameData.Startup,
            Active = frameData.Active,
            Recovery = frameData.Recovery,
            OnHit = frameData.OnHit,
            OnBlock = frameData.OnBlock,
            FrameAdvantage = frameData.FrameAdvantage,
            Notes = frameData.Notes
        };

    public MoveFrameData ToDomain()
        => new()
        {
            Startup = Startup,
            Active = Active,
            Recovery = Recovery,
            OnHit = OnHit,
            OnBlock = OnBlock,
            FrameAdvantage = FrameAdvantage,
            Notes = Notes
        };
}
