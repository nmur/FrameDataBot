using FrameData.Infrastructure.Persistence;
using FrameData.Ingestion.Backup;
using FrameData.Ingestion.Catalog;
using FrameData.Ingestion.Services;
using Microsoft.Extensions.Logging;

namespace FrameData.Ingestion.Hosting;

public sealed class IngestionWorker
{
    private readonly IngestionWorkerOptions _options;
    private readonly SchemaBootstrapper _schemaBootstrapper;
    private readonly ISupportedCharacterCatalog _catalog;
    private readonly IngestionOrchestrator _orchestrator;
    private readonly FrameDataBackupService _backupService;
    private readonly ILogger<IngestionWorker> _logger;

    public IngestionWorker(
        IngestionWorkerOptions options,
        SchemaBootstrapper schemaBootstrapper,
        ISupportedCharacterCatalog catalog,
        IngestionOrchestrator orchestrator,
        FrameDataBackupService backupService,
        ILogger<IngestionWorker> logger)
    {
        _options = options;
        _schemaBootstrapper = schemaBootstrapper;
        _catalog = catalog;
        _orchestrator = orchestrator;
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await _schemaBootstrapper.RunAsync(cancellationToken);

        if (_options.Mode == IngestionWorkerMode.Backup)
        {
            var manifest = await _backupService.ExportAsync(_options.BackupPath, cancellationToken);
            _logger.LogInformation(
                "Exported frame data backup to {BackupPath}. Characters: {CharacterCount}; moves: {MoveCount}.",
                _options.BackupPath,
                manifest.CharacterCount,
                manifest.MoveCount);
            return IngestionWorkerExitCodes.Success;
        }

        if (_options.Mode == IngestionWorkerMode.Restore)
        {
            var manifest = await _backupService.ImportAsync(_options.RestorePath, cancellationToken);
            _logger.LogInformation(
                "Restored frame data backup from {RestorePath}. Characters: {CharacterCount}; moves: {MoveCount}.",
                _options.RestorePath,
                manifest.CharacterCount,
                manifest.MoveCount);
            return IngestionWorkerExitCodes.Success;
        }

        var scope = _catalog.ResolveScope(_options.CharacterIds);
        _logger.LogInformation("Starting ingestion for {CharacterCount} character(s).", scope.Count);

        var run = await _orchestrator.RunAsync(scope, cancellationToken);
        _logger.LogInformation(
            "Ingestion run {RunId} finished with status {Status}. Characters: {CharactersProcessed}; moves: {MovesProcessed}.",
            run.Id,
            run.Status,
            run.CharactersProcessed,
            run.MovesProcessed);

        return IngestionWorkerExitCodeMapper.MapRunStatus(run);
    }
}
