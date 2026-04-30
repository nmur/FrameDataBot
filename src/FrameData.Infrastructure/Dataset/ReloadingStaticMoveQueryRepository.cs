using FrameData.Domain.Datasets;
using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FrameData.Infrastructure.Dataset;

public sealed class ReloadingStaticMoveQueryRepository : IMoveQueryRepository
{
    private readonly StaticFrameDataDatasetLoader _loader;
    private readonly string _activeDatasetPath;
    private readonly ILogger<ReloadingStaticMoveQueryRepository> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private DatasetSnapshot? _snapshot;
    private StaticMoveQueryRepository? _current;

    public ReloadingStaticMoveQueryRepository(
        StaticFrameDataDatasetLoader loader,
        string activeDatasetPath,
        ILogger<ReloadingStaticMoveQueryRepository>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(activeDatasetPath))
        {
            throw new ArgumentException("Active dataset path is required.", nameof(activeDatasetPath));
        }

        _loader = loader;
        _activeDatasetPath = activeDatasetPath;
        _logger = logger ?? NullLogger<ReloadingStaticMoveQueryRepository>.Instance;
    }

    public async Task<bool> SupportsCharacterAsync(string character, CancellationToken cancellationToken = default)
    {
        var repository = await GetCurrentRepositoryAsync(cancellationToken);
        return await repository.SupportsCharacterAsync(character, cancellationToken);
    }

    public async Task<Move?> FindExactMoveAsync(string character, string move, CancellationToken cancellationToken = default)
    {
        var repository = await GetCurrentRepositoryAsync(cancellationToken);
        return await repository.FindExactMoveAsync(character, move, cancellationToken);
    }

    public async Task<IReadOnlyList<Move>> GetMovesForCharacterAsync(
        string character,
        CancellationToken cancellationToken = default)
    {
        var repository = await GetCurrentRepositoryAsync(cancellationToken);
        return await repository.GetMovesForCharacterAsync(character, cancellationToken);
    }

    private async Task<StaticMoveQueryRepository> GetCurrentRepositoryAsync(CancellationToken cancellationToken)
    {
        var latestSnapshot = ReadSnapshot();
        if (_current is not null && latestSnapshot == _snapshot)
        {
            return _current;
        }

        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            latestSnapshot = ReadSnapshot();
            if (_current is not null && latestSnapshot == _snapshot)
            {
                return _current;
            }

            try
            {
                var dataset = await _loader.LoadAsync(_activeDatasetPath, cancellationToken);
                _current = new StaticMoveQueryRepository(dataset);
                _snapshot = latestSnapshot;
                LogLoadedDataset(dataset);
                return _current;
            }
            catch (Exception ex) when (_current is not null)
            {
                _logger.LogWarning(
                    ex,
                    "Could not reload static frame-data dataset from {ActiveDatasetPath}; continuing to serve the previous dataset snapshot.",
                    _activeDatasetPath);

                return _current;
            }
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private DatasetSnapshot ReadSnapshot()
    {
        var activePath = Path.GetFullPath(_activeDatasetPath);
        var manifestPath = Path.Combine(activePath, "manifest.json");
        var manifestLastWriteTime = File.Exists(manifestPath)
            ? File.GetLastWriteTimeUtc(manifestPath)
            : DateTime.MinValue;

        var linkTarget = Directory.Exists(activePath)
            ? new DirectoryInfo(activePath).LinkTarget
            : null;

        return new DatasetSnapshot(activePath, linkTarget ?? string.Empty, manifestLastWriteTime);
    }

    private void LogLoadedDataset(StaticFrameDataDataset dataset)
    {
        _logger.LogInformation(
            "Loaded static frame-data dataset {DatasetId} from {ActiveDatasetPath}. Characters={CharacterCount}; Moves={MoveCount}; Media={MediaCount}.",
            dataset.DatasetId,
            _activeDatasetPath,
            dataset.Characters.Count,
            dataset.Moves.Count,
            dataset.MediaCount);
    }

    private sealed record DatasetSnapshot(
        string ActivePath,
        string LinkTarget,
        DateTime ManifestLastWriteTime);
}
